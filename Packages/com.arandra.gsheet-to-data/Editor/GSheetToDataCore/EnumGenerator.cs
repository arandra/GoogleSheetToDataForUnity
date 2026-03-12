using System;
using System.Collections.Generic;
using System.Text;

namespace GSheetToDataCore
{
    public class EnumGenerator
    {
        public IDictionary<string, string> GenerateEnumStrings(ParsedSheetData parsedData)
        {
            var results = new Dictionary<string, string>(StringComparer.Ordinal);
            if (parsedData?.EnumDefinitions == null)
            {
                return results;
            }

            foreach (var enumDefinition in parsedData.EnumDefinitions)
            {
                if (enumDefinition == null || string.IsNullOrWhiteSpace(enumDefinition.Name))
                {
                    continue;
                }

                results[enumDefinition.Name] = GenerateEnumString(enumDefinition);
            }

            return results;
        }

        private static string GenerateEnumString(EnumDefinitionData enumDefinition)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"public enum {enumDefinition.Name}");
            sb.AppendLine("{");

            foreach (var member in enumDefinition.Members)
            {
                if (string.IsNullOrWhiteSpace(member.Name))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(member.Value))
                {
                    sb.AppendLine($"    {member.Name},");
                    continue;
                }

                sb.AppendLine($"    {member.Name} = {member.Value},");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
