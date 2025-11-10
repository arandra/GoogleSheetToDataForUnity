#if UNITY_EDITOR
using System;
using GSheetToDataCore;

namespace GSheetToDataForUnity.Editor
{
    [Serializable]
    internal class GSheetToDataGenerationJob
    {
        public string SheetId = string.Empty;
        public string SheetName = string.Empty;
        public string DataClassFullName = string.Empty;
        public string ScriptableObjectFullName = string.Empty;
        public string AssetRelativePath = string.Empty;
        public string JsonPayload = string.Empty;
        public SheetDataType SheetType = SheetDataType.Table;
        public string EnqueuedAtUtc = DateTime.UtcNow.ToString("o");
    }
}
#endif
