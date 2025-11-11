#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using GSheetToDataCore;
using UnityEditor;

namespace GSheetToDataForUnity.Editor
{
    internal static class GSheetToDataAssetRegistryUtility
    {
        internal static void UpsertEntry(
            GSheetToDataGenerationJob job,
            ParsedSheetData parsedData,
            string scriptableClassName,
            string scriptOutputPath,
            string scriptableScriptOutputPath,
            string assetOutputPath,
            string dataNamespace,
            string scriptableNamespace,
            bool overrideScriptOutputPath,
            bool overrideScriptableScriptOutputPath,
            bool overrideAssetOutputPath,
            bool overrideNamespace,
            bool overrideScriptableNamespace)
        {
            var registry = GSheetToDataSettingsStore.LoadOrCreateRegistry();
            var entry = new GSheetToDataAssetRegistryEntry
            {
                SheetId = job.SheetId,
                SheetName = job.SheetName,
                SheetType = job.SheetType,
                DataClassName = parsedData.ClassName,
                ScriptableClassName = scriptableClassName,
                DataClassFullName = job.DataClassFullName,
                ScriptableObjectFullName = job.ScriptableObjectFullName,
                ScriptOutputPath = scriptOutputPath,
                ScriptableScriptOutputPath = scriptableScriptOutputPath,
                AssetOutputPath = assetOutputPath,
                Namespace = dataNamespace,
                ScriptableNamespace = scriptableNamespace,
                OverrideScriptOutputPath = overrideScriptOutputPath,
                OverrideScriptableScriptOutputPath = overrideScriptableScriptOutputPath,
                OverrideAssetOutputPath = overrideAssetOutputPath,
                OverrideNamespace = overrideNamespace,
                OverrideScriptableNamespace = overrideScriptableNamespace,
                AssetRelativePath = job.AssetRelativePath,
                LastSyncedUtc = DateTime.UtcNow.ToString("o"),
                LastFieldNames = new List<string>(parsedData.FieldNames ?? new List<string>()),
                LastFieldTypes = new List<string>(parsedData.FieldTypes ?? new List<string>())
            };

            registry.Upsert(entry);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
