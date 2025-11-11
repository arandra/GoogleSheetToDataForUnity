#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GSheetToDataForUnity.Editor
{
    internal static class GSheetToDataSettingsStore
    {
        private const string SettingsAssetPath = "Assets/Settings/Editor/GSheetToDataProjectSettings.asset";
        private const string RegistryAssetPath = "Assets/Settings/Editor/GSheetToDataAssetRegistry.asset";
        private const string ClientSecretPrefsKey = "GSheetToDataForUnity.ClientSecretPath";
        private const string TokenStorePrefsKey = "GSheetToDataForUnity.TokenStorePath";

        internal static GSheetToDataAssetRegistry LoadOrCreateRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<GSheetToDataAssetRegistry>(RegistryAssetPath);
            if (registry != null)
            {
                return registry;
            }

            var directory = Path.GetDirectoryName(RegistryAssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            registry = ScriptableObject.CreateInstance<GSheetToDataAssetRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryAssetPath);
            AssetDatabase.SaveAssets();
            return registry;
        }

        internal static GSheetToDataAppSettings Load()
        {
            var projectSettings = LoadOrCreateAsset();
            var settings = new GSheetToDataAppSettings
            {
                ScriptOutputPath = projectSettings.ScriptOutputPath,
                ScriptableScriptOutputPath = projectSettings.ScriptableScriptOutputPath,
                AssetOutputPath = projectSettings.AssetOutputPath,
                Namespace = projectSettings.Namespace,
                ScriptableNamespace = projectSettings.ScriptableNamespace,
                LinkScriptPaths = projectSettings.LinkScriptPaths,
                LinkNamespaces = projectSettings.LinkNamespaces,
                ClientSecretPath = EditorPrefs.GetString(ClientSecretPrefsKey, string.Empty),
                TokenStorePath = EditorPrefs.GetString(TokenStorePrefsKey, string.Empty)
            };

            if (settings.LinkScriptPaths)
            {
                settings.ScriptableScriptOutputPath = settings.ScriptOutputPath;
            }

            if (settings.LinkNamespaces)
            {
                settings.ScriptableNamespace = settings.Namespace;
            }

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
            projectSettings.ScriptableScriptOutputPath = settings.LinkScriptPaths
                ? settings.ScriptOutputPath
                : settings.ScriptableScriptOutputPath;
            projectSettings.AssetOutputPath = settings.AssetOutputPath;
            projectSettings.Namespace = settings.Namespace;
            projectSettings.ScriptableNamespace = settings.LinkNamespaces
                ? settings.Namespace
                : settings.ScriptableNamespace;
            projectSettings.LinkScriptPaths = settings.LinkScriptPaths;
            projectSettings.LinkNamespaces = settings.LinkNamespaces;

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
