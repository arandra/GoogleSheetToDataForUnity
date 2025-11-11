
using System.Collections.Generic;

namespace GSheetToDataCore
{
    public class ParsedSheetData
    {
        public string ClassName { get; set; }
        public List<string> FieldTypes { get; set; }
        public List<string> FieldNames { get; set; }
        public List<IList<object>> DataRows { get; set; }
        public SheetDataType SheetType { get; set; }

        public ParsedSheetData()
        {
            ClassName = string.Empty;
            FieldTypes = new List<string>();
            FieldNames = new List<string>();
            DataRows = new List<IList<object>>();
            SheetType = SheetDataType.Table;
        }
    }
}
