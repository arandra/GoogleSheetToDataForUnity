#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GSheetToDataForUnity.Editor
{
    internal static class GSheetToDataSettingsStore
    {
        private const string SettingsAssetPath = "ProjectSettings/GSheetToDataProjectSettings.asset";
        private const string ClientSecretPrefsKey = "GSheetToDataForUnity.ClientSecretPath";
        private const string TokenStorePrefsKey = "GSheetToDataForUnity.TokenStorePath";

        internal static GSheetToDataAppSettings Load()
        {
            var projectSettings = LoadOrCreateAsset();
            var settings = new GSheetToDataAppSettings
            {
                ScriptOutputPath = projectSettings.ScriptOutputPath,
                AssetOutputPath = projectSettings.AssetOutputPath,
                Namespace = projectSettings.Namespace,
                ClientSecretPath = EditorPrefs.GetString(ClientSecretPrefsKey, string.Empty),
                TokenStorePath = EditorPrefs.GetString(TokenStorePrefsKey, string.Empty)
            };

            return settings;
        }

        internal static void Save(GSheetToDataAppSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            var projectSettings = LoadOrCreateAsset();
            projectSettings.ScriptOutputPath = settings.ScriptOutputPath;
            projectSettings.AssetOutputPath = settings.AssetOutputPath;
            projectSettings.Namespace = settings.Namespace;

            EditorUtility.SetDirty(projectSettings);
            AssetDatabase.SaveAssets();

            EditorPrefs.SetString(ClientSecretPrefsKey, settings.ClientSecretPath ?? string.Empty);
            var tokenStoreValue = settings.TokenStorePath;
            EditorPrefs.SetString(TokenStorePrefsKey, tokenStoreValue ?? string.Empty);
        }

        private static GSheetToDataProjectSettingsAsset LoadOrCreateAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GSheetToDataProjectSettingsAsset>(SettingsAssetPath);
            if (asset != null)
            {
                return asset;
            }

            var directory = Path.GetDirectoryName(SettingsAssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            asset = ScriptableObject.CreateInstance<GSheetToDataProjectSettingsAsset>();
            AssetDatabase.CreateAsset(asset, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
#endif
