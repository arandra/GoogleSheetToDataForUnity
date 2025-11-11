#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using GSheetToDataCore;
using UnityEngine;

namespace GSheetToDataForUnity.Editor
{
    [Serializable]
    internal class GSheetToDataAssetRegistryEntry
    {
        public string SheetId = string.Empty;
        public string SheetName = string.Empty;
        public SheetDataType SheetType = SheetDataType.Table;
        public string DataClassName = string.Empty;
        public string ScriptableClassName = string.Empty;
        public string DataClassFullName = string.Empty;
        public string ScriptableObjectFullName = string.Empty;
        public string ScriptOutputPath = string.Empty;
        public string ScriptableScriptOutputPath = string.Empty;
        public string AssetOutputPath = string.Empty;
        public string Namespace = string.Empty;
        public string ScriptableNamespace = string.Empty;
        public bool OverrideScriptOutputPath;
        public bool OverrideScriptableScriptOutputPath;
        public bool OverrideAssetOutputPath;
        public bool OverrideNamespace;
        public bool OverrideScriptableNamespace;
        public string AssetRelativePath = string.Empty;
        public string LastSyncedUtc = DateTime.UtcNow.ToString("o");
        public List<string> LastFieldNames = new List<string>();
        public List<string> LastFieldTypes = new List<string>();
    }

    internal sealed class GSheetToDataAssetRegistry : ScriptableObject
    {
        [SerializeField] private List<GSheetToDataAssetRegistryEntry> entries = new();

        internal IReadOnlyList<GSheetToDataAssetRegistryEntry> Entries => entries;

        internal void Upsert(GSheetToDataAssetRegistryEntry entry)
        {
            var existing = entries.Find(e => string.Equals(e.SheetId, entry.SheetId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(e.SheetName, entry.SheetName, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                entries.Add(entry);
            }
            else
            {
                existing.SheetType = entry.SheetType;
                existing.DataClassName = entry.DataClassName;
                existing.ScriptableClassName = entry.ScriptableClassName;
                existing.DataClassFullName = entry.DataClassFullName;
                existing.ScriptableObjectFullName = entry.ScriptableObjectFullName;
                existing.ScriptOutputPath = entry.ScriptOutputPath;
                existing.ScriptableScriptOutputPath = entry.ScriptableScriptOutputPath;
                existing.AssetOutputPath = entry.AssetOutputPath;
                existing.Namespace = entry.Namespace;
                existing.ScriptableNamespace = entry.ScriptableNamespace;
                existing.OverrideScriptOutputPath = entry.OverrideScriptOutputPath;
                existing.OverrideScriptableScriptOutputPath = entry.OverrideScriptableScriptOutputPath;
                existing.OverrideAssetOutputPath = entry.OverrideAssetOutputPath;
                existing.OverrideNamespace = entry.OverrideNamespace;
                existing.OverrideScriptableNamespace = entry.OverrideScriptableNamespace;
                existing.AssetRelativePath = entry.AssetRelativePath;
                existing.LastSyncedUtc = entry.LastSyncedUtc;
                existing.LastFieldNames = new List<string>(entry.LastFieldNames ?? new List<string>());
                existing.LastFieldTypes = new List<string>(entry.LastFieldTypes ?? new List<string>());
            }
        }

        internal void Remove(string sheetId, string sheetName)
        {
            entries.RemoveAll(e =>
                string.Equals(e.SheetId, sheetId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.SheetName, sheetName, StringComparison.OrdinalIgnoreCase));
        }

        internal void Clear()
        {
            entries.Clear();
        }
    }
}
#endif
