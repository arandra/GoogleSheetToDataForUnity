
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GSheetToDataCore
{
    public class DataParser
    {
        public ParsedSheetData Parse(string sheetName, ValueRange valueRange, SheetDataType sheetType)
        {
            if (valueRange?.Values == null)
            {
                return new ParsedSheetData { SheetType = sheetType };
            }

            var parsedData = new ParsedSheetData
            {
                ClassName = sheetName,
                SheetType = sheetType
            };

            switch (sheetType)
            {
                case SheetDataType.Const:
                    ParseConstSheet(valueRange.Values, parsedData);
                    break;
                case SheetDataType.Enum:
                    ParseEnumSheet(valueRange.Values, parsedData);
                    break;
                default:
                    ParseTableSheet(valueRange.Values, parsedData);
                    break;
            }

            if (sheetType != SheetDataType.Enum)
            {
                NormalizeMultiCellListFields(parsedData);
            }

            return parsedData;
        }

        private static void ParseTableSheet(IList<IList<object>> rows, ParsedSheetData parsedData)
        {
            if (rows.Count < 2)
            {
                return;
            }

            parsedData.FieldTypes = rows[0].Select(c => c?.ToString()?.Trim() ?? string.Empty).ToList();
            parsedData.FieldNames = rows[1].Select(c => c?.ToString()?.Trim() ?? string.Empty).ToList();
            parsedData.DataRows = rows.Skip(2).Select(r => (IList<object>)r).ToList();
        }

        private static void ParseConstSheet(IList<IList<object>> rows, ParsedSheetData parsedData)
        {
            if (rows.Count == 0)
            {
                return;
            }

            var constRow = new List<object>();
            foreach (var row in rows)
            {
                var typeCell = GetCellString(row, 0);
                var nameCell = GetCellString(row, 1);
                var dataCell = row.Count > 2 ? row[2] : string.Empty;

                if (string.IsNullOrWhiteSpace(nameCell))
                {
                    continue;
                }

                parsedData.FieldTypes.Add(typeCell);
                parsedData.FieldNames.Add(nameCell);
                constRow.Add(dataCell ?? string.Empty);
            }

            if (parsedData.FieldNames.Count > 0)
            {
                parsedData.DataRows.Add(constRow);
            }
        }

        private static void ParseEnumSheet(IList<IList<object>> rows, ParsedSheetData parsedData)
        {
            if (rows.Count < 2)
            {
                return;
            }

            var headers = rows[0];
            var names = rows[1];
            var usedValueColumns = new HashSet<int>();
            int columnCount = Math.Max(headers.Count, names.Count);

            for (int i = 0; i < columnCount; i++)
            {
                var header = GetCellString(headers, i);
                if (!string.Equals(header, "enum", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var enumName = GetCellString(names, i);
                if (string.IsNullOrWhiteSpace(enumName))
                {
                    continue;
                }

                var valueColumnIndex = FindMatchingValueColumn(headers, names, enumName, usedValueColumns);
                if (valueColumnIndex >= 0)
                {
                    usedValueColumns.Add(valueColumnIndex);
                }

                var enumDefinition = new EnumDefinitionData
                {
                    Name = enumName
                };

                for (int rowIndex = 2; rowIndex < rows.Count; rowIndex++)
                {
                    var row = rows[rowIndex];
                    var memberName = GetCellString(row, i);
                    if (string.IsNullOrWhiteSpace(memberName))
                    {
                        continue;
                    }

                    var memberValue = valueColumnIndex >= 0 ? GetCellString(row, valueColumnIndex) : string.Empty;
                    enumDefinition.Members.Add(new EnumMemberData
                    {
                        Name = memberName,
                        Value = memberValue
                    });
                }

                parsedData.EnumDefinitions.Add(enumDefinition);
            }
        }

        private static int FindMatchingValueColumn(
            IList<object> headers,
            IList<object> names,
            string enumName,
            HashSet<int> usedValueColumns)
        {
            int columnCount = Math.Max(headers.Count, names.Count);
            for (int i = 0; i < columnCount; i++)
            {
                if (usedValueColumns.Contains(i))
                {
                    continue;
                }

                var header = GetCellString(headers, i);
                if (!string.Equals(header, "value", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var valueEnumName = GetCellString(names, i);
                if (string.Equals(enumName, valueEnumName, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private class ColumnMap
        {
            public bool IsGroup { get; set; }
            public int ColumnIndex { get; set; }
            public string GroupName { get; set; } = string.Empty;
            public string BaseType { get; set; } = string.Empty;
            public List<int> ColumnIndices { get; } = new List<int>();
        }

        private class GroupInfo
        {
            public string BaseName { get; set; } = string.Empty;
            public string FieldType { get; set; } = string.Empty;
            public int ExpectedIndex { get; set; }
            public ColumnMap Map { get; set; } = new ColumnMap();
        }

        private static void NormalizeMultiCellListFields(ParsedSheetData parsedData)
        {
            if (parsedData == null)
            {
                return;
            }

            var fieldNames = parsedData.FieldNames ?? new List<string>();
            var fieldTypes = parsedData.FieldTypes ?? new List<string>();

            if (fieldNames.Count == 0)
            {
                return;
            }

            var normalizedFieldNames = new List<string>();
            var normalizedFieldTypes = new List<string>();
            var columnMaps = new List<ColumnMap>();
            var groups = new Dictionary<string, GroupInfo>(StringComparer.Ordinal);

            int columnCount = Math.Max(fieldNames.Count, fieldTypes.Count);
            for (int i = 0; i < columnCount; i++)
            {
                var fieldName = i < fieldNames.Count ? fieldNames[i] : string.Empty;
                var fieldType = i < fieldTypes.Count ? fieldTypes[i] : string.Empty;

                if (TryParseGroupedFieldName(fieldName, out var baseName, out var index))
                {
                    if (!groups.TryGetValue(baseName, out var group))
                    {
                        if (index != 0)
                        {
                            throw new ArgumentException($"List field '{baseName}' must start at #0.");
                        }

                        var map = new ColumnMap
                        {
                            IsGroup = true,
                            GroupName = baseName,
                            BaseType = fieldType
                        };

                        group = new GroupInfo
                        {
                            BaseName = baseName,
                            FieldType = fieldType,
                            ExpectedIndex = 0,
                            Map = map
                        };

                        groups.Add(baseName, group);
                        normalizedFieldNames.Add(baseName);
                        normalizedFieldTypes.Add($"{fieldType}[]");
                        columnMaps.Add(map);
                    }
                    else if (!string.Equals(group.FieldType, fieldType, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException($"List field '{baseName}' has inconsistent field types: '{group.FieldType}' vs '{fieldType}'.");
                    }

                    if (index != group.ExpectedIndex)
                    {
                        throw new ArgumentException($"List field '{baseName}' has invalid index order. Expected #{group.ExpectedIndex} but found #{index}.");
                    }

                    group.Map.ColumnIndices.Add(i);
                    group.ExpectedIndex++;
                    continue;
                }

                normalizedFieldNames.Add(fieldName);
                normalizedFieldTypes.Add(fieldType);
                columnMaps.Add(new ColumnMap { IsGroup = false, ColumnIndex = i });
            }

            if (columnMaps.Count == 0)
            {
                return;
            }

            var normalizedRows = new List<IList<object>>();
            foreach (var row in parsedData.DataRows)
            {
                var newRow = new List<object>(columnMaps.Count);
                foreach (var map in columnMaps)
                {
                    if (!map.IsGroup)
                    {
                        newRow.Add(GetCellObject(row, map.ColumnIndex));
                        continue;
                    }

                    var list = new List<object>();
                    bool seenEmpty = false;
                    foreach (var colIndex in map.ColumnIndices)
                    {
                        var cellObj = GetCellObject(row, colIndex);
                        var cellStr = cellObj?.ToString() ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(cellStr))
                        {
                            seenEmpty = true;
                            continue;
                        }

                        if (seenEmpty)
                        {
                            throw new ArgumentException($"List field '{map.GroupName}' has non-empty cell after an empty cell.");
                        }

                        list.Add(cellObj ?? string.Empty);
                    }

                    newRow.Add(list);
                }

                normalizedRows.Add(newRow);
            }

            parsedData.FieldNames = normalizedFieldNames;
            parsedData.FieldTypes = normalizedFieldTypes;
            parsedData.DataRows = normalizedRows;
        }

        private static bool TryParseGroupedFieldName(string fieldName, out string baseName, out int index)
        {
            baseName = string.Empty;
            index = -1;

            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            var hashIndex = fieldName.LastIndexOf('#');
            if (hashIndex <= 0 || hashIndex >= fieldName.Length - 1)
            {
                return false;
            }

            var suffix = fieldName.Substring(hashIndex + 1);
            if (!int.TryParse(suffix, out index) || index < 0)
            {
                return false;
            }

            baseName = fieldName.Substring(0, hashIndex).Trim();
            return !string.IsNullOrWhiteSpace(baseName);
        }

        private static object GetCellObject(IList<object> row, int index)
        {
            if (row == null || index >= row.Count)
            {
                return string.Empty;
            }

            return row[index] ?? string.Empty;
        }

        private static string GetCellString(IList<object> row, int index)
        {
            if (row == null || index >= row.Count)
            {
                return string.Empty;
            }

            return row[index]?.ToString()?.Trim() ?? string.Empty;
        }
    }
}
