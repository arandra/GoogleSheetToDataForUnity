#if UNITY_EDITOR
using System;

namespace GSheetToDataForUnity.Editor
{
    [Serializable]
    internal class GSheetToDataAppSettings
    {
        public string ScriptOutputPath = "Assets/GSheetToData/Generated/Scripts";
        public string AssetOutputPath = "Assets/GSheetToData/Generated/Assets";
        public string Namespace = "Game.Data";
        public string ClientSecretPath = string.Empty;
        public string TokenStorePath = string.Empty;

        public bool HasRequiredPaths()
        {
            return !string.IsNullOrWhiteSpace(ScriptOutputPath)
                && !string.IsNullOrWhiteSpace(AssetOutputPath);
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
    }
}
#endif
