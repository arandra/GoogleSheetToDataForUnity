
using System.Collections.Generic;

namespace GSheetToDataCore
{
    public class EnumMemberData
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public EnumMemberData()
        {
            Name = string.Empty;
            Value = string.Empty;
        }
    }

    public class EnumDefinitionData
    {
        public string Name { get; set; }
        public List<EnumMemberData> Members { get; set; }

        public EnumDefinitionData()
        {
            Name = string.Empty;
            Members = new List<EnumMemberData>();
        }
    }

    public class ParsedSheetData
    {
        public string ClassName { get; set; }
        public List<string> FieldTypes { get; set; }
        public List<string> FieldNames { get; set; }
        public List<IList<object>> DataRows { get; set; }
        public List<EnumDefinitionData> EnumDefinitions { get; set; }
        public SheetDataType SheetType { get; set; }

        public ParsedSheetData()
        {
            ClassName = string.Empty;
            FieldTypes = new List<string>();
            FieldNames = new List<string>();
            DataRows = new List<IList<object>>();
            EnumDefinitions = new List<EnumDefinitionData>();
            SheetType = SheetDataType.Table;
        }
    }
}
