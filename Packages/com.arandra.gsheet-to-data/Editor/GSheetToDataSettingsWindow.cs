#if UNITY_EDITOR
using System;
using GSheetToDataCore;
using UnityEditor;
using UnityEngine;

namespace GSheetToDataForUnity.Editor
{
    internal sealed class GSheetToDataSettingsWindow : EditorWindow
    {
        private const string WindowTitle = "GSheetToData Settings";

        private GSheetToDataAppSettings appSettings = new GSheetToDataAppSettings();
        private string sheetId = string.Empty;
        private string sheetName = string.Empty;
        private SheetDataType sheetType = SheetDataType.Table;
        private bool isGenerating;
        private Vector2 scroll;
        private bool showSettings = true;

        [MenuItem("Tools/GSheetToData/Settings")]
        internal static void ShowWindow()
        {
            var window = GetWindow<GSheetToDataSettingsWindow>(WindowTitle);
            window.minSize = new Vector2(420f, 420f);
        }

        private void OnEnable()
        {
            appSettings = GSheetToDataSettingsStore.Load();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawSettingsSection();
            EditorGUILayout.Space(12f);
            DrawSheetSection();
            EditorGUILayout.Space(12f);
            DrawActions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSettingsSection()
        {
            showSettings = EditorGUILayout.Foldout(showSettings, "App Settings", true);
            if (!showSettings)
            {
                return;
            }

            EditorGUI.indentLevel++;

            appSettings.ScriptOutputPath = EditorGUILayout.TextField("Script Output Path", appSettings.ScriptOutputPath);
            appSettings.LinkScriptPaths = EditorGUILayout.ToggleLeft("Use same path for ScriptableObject scripts", appSettings.LinkScriptPaths);
            using (new EditorGUI.DisabledScope(appSettings.LinkScriptPaths))
            {
                appSettings.ScriptableScriptOutputPath = EditorGUILayout.TextField("SO Script Output Path", appSettings.ScriptableScriptOutputPath);
            }
            if (appSettings.LinkScriptPaths)
            {
                appSettings.ScriptableScriptOutputPath = appSettings.ScriptOutputPath;
            }

            appSettings.AssetOutputPath = EditorGUILayout.TextField("Asset Output Path", appSettings.AssetOutputPath);
            appSettings.Namespace = EditorGUILayout.TextField("Data Namespace", appSettings.Namespace);
            appSettings.LinkNamespaces = EditorGUILayout.ToggleLeft("Use same namespace for ScriptableObject scripts", appSettings.LinkNamespaces);
            using (new EditorGUI.DisabledScope(appSettings.LinkNamespaces))
            {
                appSettings.ScriptableNamespace = EditorGUILayout.TextField("SO Namespace", appSettings.ScriptableNamespace);
            }
            if (appSettings.LinkNamespaces)
            {
                appSettings.ScriptableNamespace = appSettings.Namespace;
            }

            appSettings.ClientSecretPath = EditorGUILayout.TextField("client_secret.json Path", appSettings.ClientSecretPath);
            appSettings.TokenStorePath = EditorGUILayout.TextField("Token Store Path", appSettings.TokenStorePath);
            EditorGUILayout.LabelField(
                "Resolved Token Location",
                GSheetToDataPathUtility.GetTempRelativePath(appSettings.GetResolvedTokenStorePath()),
                EditorStyles.miniLabel);
            EditorGUILayout.HelpBox("Leave the token path empty to store per-user tokens under Temp/GSheetToData/.", MessageType.Info);

            if (GUILayout.Button("Save Settings"))
            {
                GSheetToDataSettingsStore.Save(appSettings);
                ShowNotification(new GUIContent("Settings saved."));
            }

            EditorGUI.indentLevel--;
        }

        private void DrawSheetSection()
        {
            EditorGUILayout.LabelField("Manual Generation (uses current defaults)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            sheetId = EditorGUILayout.TextField("Sheet ID", sheetId);
            sheetName = EditorGUILayout.TextField("Sheet Name", sheetName);
            sheetType = (SheetDataType)EditorGUILayout.EnumPopup("Sheet Type", sheetType);
            EditorGUI.indentLevel--;
            EditorGUILayout.HelpBox("일회성 테스트나 빠른 확인용입니다. 정식 워크플로우는 Asset Manager에서 시트를 등록하세요.", MessageType.Info);
        }

        private void DrawActions()
        {
            using (new EditorGUI.DisabledScope(isGenerating))
            {
                if (GUILayout.Button(isGenerating ? "Generating..." : "Generate (one-off)"))
                {
                    GenerateAsync();
                }
            }
        }

        private async void GenerateAsync()
        {
            if (isGenerating || !ValidateSettings())
            {
                return;
            }

            isGenerating = true;
            try
            {
                EditorUtility.DisplayProgressBar(WindowTitle, "Loading Google Sheet...", 0.25f);

                var loader = new SheetLoader();
                var values = await loader.LoadSheetAsync(
                    sheetId,
                    sheetName,
                    appSettings.GetResolvedClientSecretPath(),
                    appSettings.GetResolvedTokenStorePath());

                EditorUtility.DisplayProgressBar(WindowTitle, "Parsing sheet data...", 0.5f);
                var parser = new DataParser();
                var parsedData = parser.Parse(sheetName, values, sheetType);
                if (string.IsNullOrEmpty(parsedData.ClassName))
                {
                    throw new InvalidOperationException("Failed to parse sheet data.");
                }

                var classGenerator = new ClassGenerator();
                var baseClassCode = GSheetToDataGenerationUtilities.WrapWithNamespace(
                    appSettings.Namespace,
                    classGenerator.GenerateClassString(parsedData));

                var scriptableClassName = ClassGenerator.Pluralize(parsedData.ClassName);
                var scriptableCode = GSheetToDataGenerationUtilities.WrapWithNamespace(
                    appSettings.GetScriptableNamespace(),
                    GSheetToDataGenerationUtilities.GenerateScriptableObjectClass(parsedData.ClassName, scriptableClassName, parsedData.SheetType));

                EditorUtility.DisplayProgressBar(WindowTitle, "Saving scripts...", 0.75f);

                var scriptOutputPath = appSettings.ScriptOutputPath;
                var scriptableScriptOutputPath = appSettings.GetScriptableScriptOutputPath();
                var assetOutputPath = appSettings.AssetOutputPath;

                GSheetToDataGenerationUtilities.WriteScriptFile(parsedData.ClassName + ".cs", baseClassCode, scriptOutputPath);
                GSheetToDataGenerationUtilities.WriteScriptFile(scriptableClassName + ".cs", scriptableCode, scriptableScriptOutputPath);

                var jsonGenerator = new JsonGenerator();
                var jsonPayload = jsonGenerator.GenerateJsonString(parsedData);

                var job = new GSheetToDataGenerationJob
                {
                    SheetId = sheetId,
                    SheetName = sheetName,
                    DataClassFullName = GSheetToDataGenerationUtilities.BuildFullName(appSettings.Namespace, parsedData.ClassName),
                    ScriptableObjectFullName = GSheetToDataGenerationUtilities.BuildFullName(appSettings.GetScriptableNamespace(), scriptableClassName),
                    AssetRelativePath = GSheetToDataGenerationUtilities.BuildAssetRelativePath(scriptableClassName, assetOutputPath),
                    JsonPayload = jsonPayload,
                    SheetType = parsedData.SheetType
                };

                GSheetToDataJobStore.Enqueue(job);
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(WindowTitle, "Manual generation completed.\nAssets will be prepared after compilation.", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(WindowTitle, "Generation failed. Check Console.", "OK");
                Debug.LogError($"[GSheetToData] Manual generation failed: {ex}");
            }
            finally
            {
                isGenerating = false;
            }
        }

        private bool ValidateSettings()
        {
            if (!appSettings.HasRequiredPaths())
            {
                EditorUtility.DisplayDialog(WindowTitle, "Enter script and asset output paths.", "OK");
                return false;
            }

            if (string.IsNullOrWhiteSpace(appSettings.Namespace))
            {
                EditorUtility.DisplayDialog(WindowTitle, "Enter a namespace.", "OK");
                return false;
            }

            var clientSecretPath = appSettings.GetResolvedClientSecretPath();
            if (string.IsNullOrWhiteSpace(clientSecretPath) || !System.IO.File.Exists(clientSecretPath))
            {
                EditorUtility.DisplayDialog(WindowTitle, "Verify the client_secret.json path.", "OK");
                return false;
            }

            if (string.IsNullOrWhiteSpace(sheetId) || string.IsNullOrWhiteSpace(sheetName))
            {
                EditorUtility.DisplayDialog(WindowTitle, "Enter both the Sheet ID and Sheet Name.", "OK");
                return false;
            }

            try
            {
                GSheetToDataGenerationUtilities.EnsureProjectRelative(appSettings.ScriptOutputPath);
                if (!appSettings.LinkScriptPaths)
                {
                    GSheetToDataGenerationUtilities.EnsureProjectRelative(appSettings.ScriptableScriptOutputPath);
                }
                GSheetToDataGenerationUtilities.EnsureProjectRelative(appSettings.AssetOutputPath);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(WindowTitle, $"Path validation failed: {ex.Message}", "OK");
                return false;
            }

            return true;
        }
    }
}
#endif
