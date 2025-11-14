#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GSheetToDataCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using System.Text;

namespace GSheetToDataForUnity.Editor
{
    internal sealed class GSheetToDataAssetManagerWindow : EditorWindow
    {
        private const string WindowTitle = "GSheetToData Asset Manager";

        private GSheetToDataAssetRegistry registry;
        private GSheetToDataAppSettings defaults;
        private Vector2 listScroll;
        private Vector2 detailScroll;
        private int selectedIndex = -1;
        private string newSheetId = string.Empty;
        private string newSheetName = string.Empty;
        private SheetDataType newSheetType = SheetDataType.Table;
        private bool isProcessing;
        private string statusMessage = string.Empty;

        [MenuItem("Tools/GSheetToData/Asset Manager")]
        private static void ShowWindow()
        {
            var window = GetWindow<GSheetToDataAssetManagerWindow>(WindowTitle);
            window.minSize = new Vector2(720f, 460f);
        }

        private void OnEnable()
        {
            registry = GSheetToDataSettingsStore.LoadOrCreateRegistry();
            defaults = GSheetToDataSettingsStore.Load();
        }

        private void OnGUI()
        {
            defaults = GSheetToDataSettingsStore.Load();
            EditorGUILayout.Space();
            DrawGlobalLinks();
            EditorGUILayout.Space();
            DrawNewEntrySection();
            EditorGUILayout.Space();
            DrawMainArea();
            DrawStatus();
        }

        private void DrawGlobalLinks()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Global defaults live in the Settings window.", EditorStyles.miniLabel);
                if (GUILayout.Button("Open Settings", GUILayout.Width(140)))
                {
                    GSheetToDataSettingsWindow.ShowWindow();
                }
            }
        }

        private void DrawNewEntrySection()
        {
            EditorGUILayout.LabelField("Register New Sheet", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                newSheetId = EditorGUILayout.TextField("Sheet ID", newSheetId);
                newSheetName = EditorGUILayout.TextField("Sheet Name", newSheetName);
                newSheetType = (SheetDataType)EditorGUILayout.EnumPopup("Sheet Type", newSheetType);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUI.enabled = !isProcessing;
                    if (GUILayout.Button("Add Sheet", GUILayout.Width(120)))
                    {
                        AddNewEntry();
                    }
                    GUI.enabled = true;
                }
            }
        }

        private void DrawMainArea()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawEntryList(GUILayout.Width(260));
                EditorGUILayout.Space();
                DrawEntryDetails();
            }
        }

        private void DrawEntryList(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, options);
            EditorGUILayout.LabelField("Sheets", EditorStyles.boldLabel);
            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            var entries = registry?.Entries;
            if (entries == null || entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No sheets registered yet.", MessageType.Info);
            }
            else
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    var label = string.IsNullOrWhiteSpace(entry.SheetName)
                        ? entry.SheetId
                        : $"{entry.SheetName} ({entry.SheetId})";
                    var buttonStyle = (selectedIndex == i)
                        ? EditorStyles.miniButtonMid
                        : EditorStyles.miniButton;

                    if (GUILayout.Toggle(selectedIndex == i, label, buttonStyle))
                    {
                        selectedIndex = i;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh"))
                {
                    registry = GSheetToDataSettingsStore.LoadOrCreateRegistry();
                }
                if (GUILayout.Button("Remove All"))
                {
                    if (EditorUtility.DisplayDialog("Remove All Entries", "This will clear the registry metadata (assets/scripts remain). Continue?", "Remove", "Cancel"))
                    {
                        Undo.RecordObject(registry, "Clear GSheet Registry");
                        registry.Clear();
                        selectedIndex = -1;
                        MarkRegistryDirty();
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawEntryDetails()
        {
            var entries = registry?.Entries;
            if (entries == null || entries.Count == 0 || selectedIndex < 0 || selectedIndex >= entries.Count)
            {
                EditorGUILayout.HelpBox("Select a sheet on the left to view or edit its details.", MessageType.Info);
                return;
            }

            var entry = entries[selectedIndex];
            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
            EditorGUILayout.LabelField("Sheet Details", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            entry.SheetName = EditorGUILayout.TextField("Sheet Name", entry.SheetName);
            entry.SheetId = EditorGUILayout.TextField("Sheet ID", entry.SheetId);
            entry.SheetType = (SheetDataType)EditorGUILayout.EnumPopup("Sheet Type", entry.SheetType);
            if (EditorGUI.EndChangeCheck())
            {
                MarkRegistryDirty();
            }

            EditorGUILayout.Space();
            DrawOverrideField("Data Script Output Path", defaults.ScriptOutputPath, ref entry.OverrideScriptOutputPath, ref entry.ScriptOutputPath);
            DrawOverrideField("SO Script Output Path", GetDefaultScriptablePath(defaults), ref entry.OverrideScriptableScriptOutputPath, ref entry.ScriptableScriptOutputPath);
            DrawOverrideField("Asset Output Path", defaults.AssetOutputPath, ref entry.OverrideAssetOutputPath, ref entry.AssetOutputPath);
            DrawOverrideField("Data Namespace", defaults.Namespace, ref entry.OverrideNamespace, ref entry.Namespace);
            DrawOverrideField("SO Namespace", GetDefaultScriptableNamespace(defaults), ref entry.OverrideScriptableNamespace, ref entry.ScriptableNamespace);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generated Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Data Class", entry.DataClassFullName);
            EditorGUILayout.LabelField("SO Class", entry.ScriptableObjectFullName);
            EditorGUILayout.LabelField("Asset Path", entry.AssetRelativePath);
            EditorGUILayout.LabelField("Last Synced (UTC)", string.IsNullOrEmpty(entry.LastSyncedUtc) ? "-" : entry.LastSyncedUtc);

            if (entry.LastFieldNames != null && entry.LastFieldNames.Count > 0)
            {
                EditorGUILayout.LabelField("Last Fields", string.Join(", ", entry.LastFieldNames));
            }

            EditorGUILayout.Space(10f);
            using (new EditorGUI.DisabledScope(isProcessing))
            {
                if (GUILayout.Button("Generate / Re-sync", GUILayout.Height(30)))
                {
                    GenerateEntryAsync(entry);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Sheet"))
                {
                    OpenSheetInBrowser(entry);
                }

                if (GUILayout.Button("Delete Assets"))
                {
                    DeleteGeneratedAssets(entry);
                }

                if (GUILayout.Button("Remove Entry"))
                {
                    RemoveEntry(entry);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, MessageType.None);
            }
        }

        private void DrawOverrideField(string label, string defaultValue, ref bool overrideFlag, ref string value)
        {
            overrideFlag = EditorGUILayout.ToggleLeft($"Override {label}", overrideFlag);
            if (overrideFlag)
            {
                EditorGUI.BeginChangeCheck();
                value = EditorGUILayout.TextField(label, value);
                if (EditorGUI.EndChangeCheck())
                {
                    MarkRegistryDirty();
                }
            }
            else
            {
                EditorGUILayout.LabelField(label, defaultValue);
            }
        }

        private void AddNewEntry()
        {
            if (string.IsNullOrWhiteSpace(newSheetId) || string.IsNullOrWhiteSpace(newSheetName))
            {
                EditorUtility.DisplayDialog("Invalid Input", "Enter both Sheet ID and Sheet Name.", "OK");
                return;
            }

            var entry = new GSheetToDataAssetRegistryEntry
            {
                SheetId = newSheetId.Trim(),
                SheetName = newSheetName.Trim(),
                SheetType = newSheetType,
                ScriptOutputPath = defaults.ScriptOutputPath,
                ScriptableScriptOutputPath = GetDefaultScriptablePath(defaults),
                AssetOutputPath = defaults.AssetOutputPath,
                Namespace = defaults.Namespace,
                ScriptableNamespace = GetDefaultScriptableNamespace(defaults),
                OverrideAssetOutputPath = false,
                OverrideNamespace = false,
                OverrideScriptOutputPath = false,
                OverrideScriptableNamespace = false,
                OverrideScriptableScriptOutputPath = false,
                AssetRelativePath = string.Empty,
                LastFieldNames = new List<string>(),
                LastFieldTypes = new List<string>()
            };

            registry.Upsert(entry);
            MarkRegistryDirty();
            selectedIndex = registry.Entries.Count - 1;
            newSheetId = string.Empty;
            newSheetName = string.Empty;
        }

        private void RemoveEntry(GSheetToDataAssetRegistryEntry entry)
        {
            if (!EditorUtility.DisplayDialog("Remove Entry", $"Remove metadata for '{entry.SheetName}'?", "Remove", "Cancel"))
            {
                return;
            }

            Undo.RecordObject(registry, "Remove GSheet Entry");
            registry.Remove(entry.SheetId, entry.SheetName);
            selectedIndex = Mathf.Clamp(selectedIndex, 0, registry.Entries.Count - 1);
            MarkRegistryDirty();
        }

        private void DeleteGeneratedAssets(GSheetToDataAssetRegistryEntry entry)
        {
            var paths = new List<string>
            {
                entry.AssetRelativePath,
                CombineAssetPath(entry.ScriptOutputPath, entry.DataClassName + ".cs"),
                CombineAssetPath(entry.ScriptableScriptOutputPath, entry.ScriptableClassName + ".cs")
            };

            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var normalized = NormalizeAssetPath(path);
                if (!string.IsNullOrEmpty(normalized))
                {
                    AssetDatabase.DeleteAsset(normalized);
                }
            }

            AssetDatabase.Refresh();
            statusMessage = $"Deleted generated assets for {entry.SheetName}.";
        }

        private async void GenerateEntryAsync(GSheetToDataAssetRegistryEntry entry)
        {
            if (isProcessing)
            {
                EditorUtility.DisplayDialog(WindowTitle, "Generation already in progress.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.SheetId) || string.IsNullOrWhiteSpace(entry.SheetName))
            {
                EditorUtility.DisplayDialog(WindowTitle, "Sheet ID and Name are required.", "OK");
                return;
            }

            var currentDefaults = defaults;
            var clientSecretPath = currentDefaults.GetResolvedClientSecretPath();
            if (string.IsNullOrWhiteSpace(clientSecretPath) || !File.Exists(clientSecretPath))
            {
                    EditorUtility.DisplayDialog(WindowTitle, "client_secret.json path is invalid. Update it in the Settings window.", "OK");
                return;
            }

            var tokenStorePath = currentDefaults.GetResolvedTokenStorePath();
            GSheetToDataPathUtility.EnsureDirectory(tokenStorePath);

            var dataScriptPath = entry.OverrideScriptOutputPath ? entry.ScriptOutputPath : currentDefaults.ScriptOutputPath;
            var scriptableScriptPath = entry.OverrideScriptableScriptOutputPath
                ? entry.ScriptableScriptOutputPath
                : currentDefaults.GetScriptableScriptOutputPath();
            var assetOutputPath = entry.OverrideAssetOutputPath ? entry.AssetOutputPath : currentDefaults.AssetOutputPath;
            var dataNamespace = entry.OverrideNamespace ? entry.Namespace : currentDefaults.Namespace;
            var scriptableNamespace = entry.OverrideScriptableNamespace
                ? entry.ScriptableNamespace
                : currentDefaults.GetScriptableNamespace();

            try
            {
                GSheetToDataGenerationUtilities.EnsureProjectRelative(dataScriptPath);
                GSheetToDataGenerationUtilities.EnsureProjectRelative(scriptableScriptPath);
                GSheetToDataGenerationUtilities.EnsureProjectRelative(assetOutputPath);

                isProcessing = true;
                EditorUtility.DisplayProgressBar(WindowTitle, "Loading Google Sheet...", 0.25f);

                var loader = new SheetLoader();
                var values = await loader.LoadSheetAsync(entry.SheetId, entry.SheetName, clientSecretPath, tokenStorePath);

                EditorUtility.DisplayProgressBar(WindowTitle, "Parsing sheet data...", 0.5f);
                var parser = new DataParser();
                var parsedData = parser.Parse(entry.SheetName, values, entry.SheetType);
                if (string.IsNullOrEmpty(parsedData.ClassName))
                {
                    throw new InvalidOperationException("Failed to parse sheet data.");
                }

                var addedFields = ComputeDiff(parsedData.FieldNames, entry.LastFieldNames);
                var removedFields = ComputeDiff(entry.LastFieldNames, parsedData.FieldNames);
                if ((addedFields.Count > 0 || removedFields.Count > 0) && entry.LastFieldNames != null && entry.LastFieldNames.Count > 0)
                {
                    var diffMessage = BuildDiffMessage(addedFields, removedFields);
                    if (!EditorUtility.DisplayDialog("Schema changed", diffMessage, "Re-generate", "Cancel"))
                    {
                        return;
                    }
                }

                var classGenerator = new ClassGenerator();
                var baseClassCode = GSheetToDataGenerationUtilities.WrapWithNamespace(dataNamespace, classGenerator.GenerateClassString(parsedData));

                var scriptableClassName = ClassGenerator.Pluralize(parsedData.ClassName);
                var scriptableUsesSameNamespace = string.Equals(dataNamespace, scriptableNamespace, StringComparison.Ordinal);
                var dataNamespaceForScriptable = scriptableUsesSameNamespace ? string.Empty : dataNamespace;
                var scriptableCode = GSheetToDataGenerationUtilities.WrapWithNamespace(
                    scriptableNamespace,
                    GSheetToDataGenerationUtilities.GenerateScriptableObjectClass(
                        parsedData.ClassName,
                        scriptableClassName,
                        parsedData.SheetType,
                        dataNamespaceForScriptable));

                EditorUtility.DisplayProgressBar(WindowTitle, "Saving scripts...", 0.75f);
                var baseScriptPath = GSheetToDataGenerationUtilities.WriteScriptFile(parsedData.ClassName + ".cs", baseClassCode, dataScriptPath);
                var soScriptPath = GSheetToDataGenerationUtilities.WriteScriptFile(scriptableClassName + ".cs", scriptableCode, scriptableScriptPath);

                var jsonGenerator = new JsonGenerator();
                var jsonPayload = jsonGenerator.GenerateJsonString(parsedData);
                var assetRelativePath = GSheetToDataGenerationUtilities.BuildAssetRelativePath(scriptableClassName, assetOutputPath);

                var job = new GSheetToDataGenerationJob
                {
                    SheetId = entry.SheetId,
                    SheetName = entry.SheetName,
                    DataClassFullName = GSheetToDataGenerationUtilities.BuildFullName(dataNamespace, parsedData.ClassName),
                    ScriptableObjectFullName = GSheetToDataGenerationUtilities.BuildFullName(scriptableNamespace, scriptableClassName),
                    AssetRelativePath = assetRelativePath,
                    JsonPayload = jsonPayload,
                    SheetType = parsedData.SheetType
                };

                GSheetToDataJobStore.Enqueue(job);
                GSheetToDataJobProcessor.RequestProcessing();
                entry.ScriptOutputPath = dataScriptPath;
                entry.ScriptableScriptOutputPath = scriptableScriptPath;
                entry.AssetOutputPath = assetOutputPath;
                entry.Namespace = dataNamespace;
                entry.ScriptableNamespace = scriptableNamespace;

                GSheetToDataAssetRegistryUtility.UpsertEntry(
                    job,
                    parsedData,
                    scriptableClassName,
                    dataScriptPath,
                    scriptableScriptPath,
                    assetOutputPath,
                    dataNamespace,
                    scriptableNamespace,
                    entry.OverrideScriptOutputPath,
                    entry.OverrideScriptableScriptOutputPath,
                    entry.OverrideAssetOutputPath,
                    entry.OverrideNamespace,
                    entry.OverrideScriptableNamespace);

                AssetDatabase.Refresh();
                statusMessage = $"Generated assets for {entry.SheetName}. Scripts: {baseScriptPath}, {soScriptPath}";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GSheetToData] Generation failed: {ex}");
                EditorUtility.DisplayDialog(WindowTitle, $"Generation failed: {ex.Message}", "OK");
            }
            finally
            {
                isProcessing = false;
                EditorUtility.ClearProgressBar();
            }
        }

        private static List<string> ComputeDiff(IReadOnlyCollection<string> source, IReadOnlyCollection<string> target)
        {
            source ??= Array.Empty<string>();
            target ??= Array.Empty<string>();
            return source.Except(target).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }

        private static string BuildDiffMessage(List<string> added, List<string> removed)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("Sheet schema changed:");
            if (added.Count > 0)
            {
                builder.AppendLine($" + Added: {string.Join(", ", added)}");
            }
            if (removed.Count > 0)
            {
                builder.AppendLine($" - Removed: {string.Join(", ", removed)}");
            }
            builder.AppendLine();
            builder.Append("Re-generate with the new schema?");
            return builder.ToString();
        }

        private void OpenSheetInBrowser(GSheetToDataAssetRegistryEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.SheetId))
            {
                EditorUtility.DisplayDialog(WindowTitle, "Sheet ID is missing.", "OK");
                return;
            }

            var url = $"https://docs.google.com/spreadsheets/d/{entry.SheetId}/edit";
            Application.OpenURL(url);
        }

        private void MarkRegistryDirty()
        {
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
        }

        private static string CombineAssetPath(string folder, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return string.Empty;
            }

            folder = folder.Replace('\\', '/').TrimEnd('/');
            return $"{folder}/{fileName}".Replace("//", "/");
        }

        private static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var normalized = path.Replace('\\', '/');
            if (normalized.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            var absolute = GSheetToDataPathUtility.ToAbsolutePath(normalized);
            return GSheetToDataPathUtility.ToAssetRelative(absolute);
        }

        private static string GetDefaultScriptablePath(GSheetToDataAppSettings settings)
        {
            return settings.LinkScriptPaths ? settings.ScriptOutputPath : settings.ScriptableScriptOutputPath;
        }

        private static string GetDefaultScriptableNamespace(GSheetToDataAppSettings settings)
        {
            return settings.LinkNamespaces ? settings.Namespace : settings.ScriptableNamespace;
        }
    }
}
#endif
