
using Google.Apis.Sheets.v4.Data;
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
                default:
                    ParseTableSheet(valueRange.Values, parsedData);
                    break;
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
