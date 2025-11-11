#if UNITY_EDITOR
using System;

namespace GSheetToDataForUnity.Editor
{
    [Serializable]
    internal class GSheetToDataAppSettings
    {
        public string ScriptOutputPath = "Assets/GSheetToData/Generated/Scripts";
        public string ScriptableScriptOutputPath = "Assets/GSheetToData/Generated/Scripts";
        public string AssetOutputPath = "Assets/GSheetToData/Generated/Assets";
        public string Namespace = "Game.Data";
        public string ScriptableNamespace = "Game.Data";
        public bool LinkScriptPaths = true;
        public bool LinkNamespaces = true;
        public string ClientSecretPath = string.Empty;
        public string TokenStorePath = string.Empty;

        public bool HasRequiredPaths()
        {
            return !string.IsNullOrWhiteSpace(ScriptOutputPath)
                && !string.IsNullOrWhiteSpace(AssetOutputPath)
                && (LinkScriptPaths || !string.IsNullOrWhiteSpace(ScriptableScriptOutputPath));
        }

        public string GetResolvedClientSecretPath()
        {
            return string.IsNullOrWhiteSpace(ClientSecretPath)
                ? string.Empty
                : GSheetToDataPathUtility.ToAbsolutePath(ClientSecretPath);
        }

        public string GetResolvedTokenStorePath()
        {
            if (string.IsNullOrWhiteSpace(TokenStorePath))
            {
                return GSheetToDataPathUtility.GetDefaultTokenStorePath();
            }

            return GSheetToDataPathUtility.ToAbsolutePath(TokenStorePath);
        }

        public string GetScriptableScriptOutputPath()
        {
            if (LinkScriptPaths || string.IsNullOrWhiteSpace(ScriptableScriptOutputPath))
            {
                return ScriptOutputPath;
            }

            return ScriptableScriptOutputPath;
        }

        public string GetScriptableNamespace()
        {
            if (LinkNamespaces || string.IsNullOrWhiteSpace(ScriptableNamespace))
            {
                return Namespace;
            }

            return ScriptableNamespace;
        }
    }
}
#endif
