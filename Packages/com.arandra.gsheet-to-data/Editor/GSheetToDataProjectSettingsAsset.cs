#if UNITY_EDITOR
using UnityEngine;

namespace GSheetToDataForUnity.Editor
{
    internal sealed class GSheetToDataProjectSettingsAsset : ScriptableObject
    {
        [SerializeField] private string scriptOutputPath = "Assets/GSheetToData/Generated/Scripts";
        [SerializeField] private string scriptableScriptOutputPath = "Assets/GSheetToData/Generated/Scripts";
        [SerializeField] private string assetOutputPath = "Assets/GSheetToData/Generated/Assets";
        [SerializeField] private string @namespace = "Game.Data";
        [SerializeField] private string scriptableNamespace = "Game.Data";
        [SerializeField] private bool linkScriptPaths = true;
        [SerializeField] private bool linkNamespaces = true;

        internal string ScriptOutputPath
        {
            get => scriptOutputPath;
            set => scriptOutputPath = value;
        }

        internal string AssetOutputPath
        {
            get => assetOutputPath;
            set => assetOutputPath = value;
        }

        internal string Namespace
        {
            get => @namespace;
            set => @namespace = value;
        }

        internal string ScriptableScriptOutputPath
        {
            get => scriptableScriptOutputPath;
            set => scriptableScriptOutputPath = value;
        }

        internal string ScriptableNamespace
        {
            get => scriptableNamespace;
            set => scriptableNamespace = value;
        }

        internal bool LinkScriptPaths
        {
            get => linkScriptPaths;
            set => linkScriptPaths = value;
        }

        internal bool LinkNamespaces
        {
            get => linkNamespaces;
            set => linkNamespaces = value;
        }
    }
}
#endif
