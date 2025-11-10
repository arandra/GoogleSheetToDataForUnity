#if UNITY_EDITOR
using UnityEngine;

namespace GSheetToDataForUnity.Editor
{
    internal sealed class GSheetToDataProjectSettingsAsset : ScriptableObject
    {
        [SerializeField] private string scriptOutputPath = "Assets/GSheetToData/Generated/Scripts";
        [SerializeField] private string assetOutputPath = "Assets/GSheetToData/Generated/Assets";
        [SerializeField] private string @namespace = "Game.Data";

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
    }
}
#endif
