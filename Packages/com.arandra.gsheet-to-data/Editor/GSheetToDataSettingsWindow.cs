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
