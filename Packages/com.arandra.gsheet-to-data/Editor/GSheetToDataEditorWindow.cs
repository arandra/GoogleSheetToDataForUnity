#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using GSheetToDataCore;
using UnityEditor;
using UnityEngine;

namespace GSheetToDataForUnity.Editor
{
    internal sealed class GSheetToDataEditorWindow : EditorWindow
    {
        private const string WindowTitle = "GSheetToData";

        private GSheetToDataAppSettings appSettings = new GSheetToDataAppSettings();
        private string sheetId = string.Empty;
        private string sheetName = string.Empty;
        private SheetDataType sheetType = SheetDataType.Table;
        private bool isGenerating;
        private Vector2 scroll;

        [MenuItem("Tools/GSheetToData/Generator")]
        private static void ShowWindow()
        {
            var window = GetWindow<GSheetToDataEditorWindow>(WindowTitle);
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
            EditorGUILayout.LabelField("App Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            appSettings.ScriptOutputPath = EditorGUILayout.TextField("Script Output Path", appSettings.ScriptOutputPath);
            appSettings.AssetOutputPath = EditorGUILayout.TextField("Asset Output Path", appSettings.AssetOutputPath);
            appSettings.Namespace = EditorGUILayout.TextField("Namespace", appSettings.Namespace);
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
            EditorGUILayout.LabelField("Sheet Information", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            sheetId = EditorGUILayout.TextField("Sheet ID", sheetId);
            sheetName = EditorGUILayout.TextField("Sheet Name", sheetName);
            sheetType = (SheetDataType)EditorGUILayout.EnumPopup("Sheet Type", sheetType);
            EditorGUI.indentLevel--;
        }

        private void DrawActions()
        {
            using (new EditorGUI.DisabledScope(isGenerating))
            {
                if (GUILayout.Button(isGenerating ? "Generating..." : "Generate ScriptableObject"))
                {
                    GenerateAsync();
                }
            }

            var pendingJobInfo = GSheetToDataJobStore.HasJobs()
                ? "There are pending asset generation jobs."
                : "No pending jobs.";
            EditorGUILayout.HelpBox(pendingJobInfo, MessageType.Info);
        }

        private async void GenerateAsync()
        {
            if (isGenerating)
            {
                return;
            }

            if (!ValidateSettings())
            {
                return;
            }

            isGenerating = true;
            try
            {
                EditorUtility.DisplayProgressBar(WindowTitle, "Loading Google Sheet...", 0.25f);

                var loader = new SheetLoader();
                var clientSecretPath = appSettings.GetResolvedClientSecretPath();
                var tokenStore = appSettings.GetResolvedTokenStorePath();
                GSheetToDataPathUtility.EnsureDirectory(tokenStore);
                var values = await loader.LoadSheetAsync(sheetId, sheetName, clientSecretPath, tokenStore);

                EditorUtility.DisplayProgressBar(WindowTitle, "Parsing sheet data...", 0.5f);

                var parser = new DataParser();
                var parsedData = parser.Parse(sheetName, values, sheetType);
                if (string.IsNullOrEmpty(parsedData.ClassName))
                {
                    throw new InvalidOperationException("Failed to parse sheet data.");
                }

                var classGenerator = new ClassGenerator();
                var baseClassCode = WrapWithNamespace(appSettings.Namespace, classGenerator.GenerateClassString(parsedData));

                var scriptableClassName = ClassGenerator.Pluralize(parsedData.ClassName);
                var scriptableCode = WrapWithNamespace(
                    appSettings.Namespace,
                    GenerateScriptableObjectClass(parsedData.ClassName, scriptableClassName, parsedData.SheetType));

                EditorUtility.DisplayProgressBar(WindowTitle, "Saving scripts...", 0.75f);

                var baseScriptPath = WriteScriptFile(parsedData.ClassName + ".cs", baseClassCode, appSettings.ScriptOutputPath);
                var soScriptPath = WriteScriptFile(scriptableClassName + ".cs", scriptableCode, appSettings.ScriptOutputPath);

                var jsonGenerator = new JsonGenerator();
                var jsonPayload = jsonGenerator.GenerateJsonString(parsedData);

                var assetRelativePath = BuildAssetRelativePath(scriptableClassName);

                var job = new GSheetToDataGenerationJob
                {
                    SheetId = sheetId,
                    SheetName = sheetName,
                    DataClassFullName = BuildFullName(parsedData.ClassName),
                    ScriptableObjectFullName = BuildFullName(scriptableClassName),
                    AssetRelativePath = assetRelativePath,
                    JsonPayload = jsonPayload,
                    SheetType = parsedData.SheetType
                };

                GSheetToDataJobStore.Enqueue(job);

                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(WindowTitle, "Script generation completed.\nAssets will be prepared after compilation.", "OK");
                Debug.Log($"[GSheetToData] Generated {baseScriptPath} / {soScriptPath}.");
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(WindowTitle, "An error occurred during generation. Check the Console for details.", "OK");
                Debug.LogError($"[GSheetToData] ScriptableObject generation failed: {ex}");
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
            if (string.IsNullOrWhiteSpace(clientSecretPath) || !File.Exists(clientSecretPath))
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
                EnsureProjectRelative(appSettings.ScriptOutputPath);
                EnsureProjectRelative(appSettings.AssetOutputPath);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(WindowTitle, $"Path validation failed: {ex.Message}", "OK");
                return false;
            }

            return true;
        }

        private static void EnsureProjectRelative(string path)
        {
            var assetRelative = GSheetToDataPathUtility.ToAssetRelativeFromUserInput(path);
            if (!assetRelative.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Path must reside under the Assets folder.");
            }
        }

        private string WriteScriptFile(string fileName, string contents, string targetDirectory)
        {
            var absoluteDir = GSheetToDataPathUtility.EnsureDirectory(
                GSheetToDataPathUtility.ToAbsolutePath(targetDirectory));

            var filePath = Path.Combine(absoluteDir, fileName);
            File.WriteAllText(filePath, contents, Encoding.UTF8);

            var assetPath = GSheetToDataPathUtility.ToAssetRelative(filePath).Replace('\\', '/');
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return assetPath;
        }

        private string BuildAssetRelativePath(string scriptableClassName)
        {
            var absoluteDir = GSheetToDataPathUtility.EnsureDirectory(
                GSheetToDataPathUtility.ToAbsolutePath(appSettings.AssetOutputPath));

            var assetAbsolutePath = Path.Combine(absoluteDir, scriptableClassName + ".asset");
            var assetRelativePath = GSheetToDataPathUtility.ToAssetRelative(assetAbsolutePath).Replace('\\', '/');

            if (!assetRelativePath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Asset paths must reside under the Assets folder.");
            }

            return assetRelativePath;
        }

        private string BuildFullName(string className)
        {
            return string.IsNullOrWhiteSpace(appSettings.Namespace)
                ? className
                : $"{appSettings.Namespace}.{className}";
        }

        private static string WrapWithNamespace(string namespaceName, string classCode)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return classCode;
            }

            var lines = classCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var builder = new StringBuilder();
            builder.AppendLine($"namespace {namespaceName}");
            builder.AppendLine("{");

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    builder.AppendLine();
                }
                else
                {
                    builder.AppendLine($"    {line}");
                }
            }

            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string GenerateScriptableObjectClass(string dataClassName, string scriptableClassName, SheetDataType sheetType)
        {
            var builder = new StringBuilder();
            if (sheetType == SheetDataType.Table)
            {
                builder.AppendLine("using System.Collections.Generic;");
            }
            builder.AppendLine("using UnityEngine;");
            builder.AppendLine();
            builder.AppendLine($"[CreateAssetMenu(fileName = \"{scriptableClassName}\", menuName = \"GSheetToData/{scriptableClassName}\")]");
            builder.AppendLine($"public class {scriptableClassName} : ScriptableObject");
            builder.AppendLine("{");
            builder.AppendLine("    [SerializeField] private string sheetId = string.Empty;");
            builder.AppendLine("    [SerializeField] private string sheetName = string.Empty;");
            builder.AppendLine();
            builder.AppendLine("    public string SheetId => sheetId;");
            builder.AppendLine("    public string SheetName => sheetName;");
            builder.AppendLine();
            if (sheetType == SheetDataType.Table)
            {
                builder.AppendLine($"    public List<{dataClassName}> Values = new List<{dataClassName}>();");
            }
            else
            {
                builder.AppendLine($"    [SerializeField] private {dataClassName} value = new {dataClassName}();");
                builder.AppendLine($"    public {dataClassName} Value => value;");
            }
            builder.AppendLine();
            builder.AppendLine("    public void SetSheetMetadata(string newSheetId, string newSheetName)");
            builder.AppendLine("    {");
            builder.AppendLine("        sheetId = newSheetId;");
            builder.AppendLine("        sheetName = newSheetName;");
            builder.AppendLine("    }");
            if (sheetType == SheetDataType.Const)
            {
                builder.AppendLine();
                builder.AppendLine($"    public void SetValue({dataClassName} newValue)");
                builder.AppendLine("    {");
                builder.AppendLine("        value = newValue;");
                builder.AppendLine("    }");
            }
            builder.AppendLine("}");

            return builder.ToString();
        }
    }
}
#endif
