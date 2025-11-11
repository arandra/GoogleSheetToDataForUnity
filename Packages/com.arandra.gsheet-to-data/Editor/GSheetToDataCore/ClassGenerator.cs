
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace GSheetToDataCore
{
    public class ClassGenerator
    {
        public string GenerateClassString(ParsedSheetData parsedData)
        {
            if (parsedData == null || string.IsNullOrEmpty(parsedData.ClassName))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using SerializableTypes;"); // For Pair<TKey, TValue>
            sb.AppendLine("");
            sb.AppendLine($"[Serializable]");
            sb.AppendLine($"public class {parsedData.ClassName}");
            sb.AppendLine("{");

            // Properties
            for (int i = 0; i < parsedData.FieldNames.Count; i++)
            {
                if (i < parsedData.FieldTypes.Count)
                {
                    var fieldName = parsedData.FieldNames[i];
                    var fieldType = parsedData.FieldTypes[i];
                    var csharpType = GetCSharpType(fieldType);
                    var defaultValue = GetDefaultValue(fieldType); // Get default value here

                    // Pluralize field name if it's a list type
                    if (csharpType.StartsWith("List<"))
                    {
                        fieldName = Pluralize(fieldName);
                    }
                    sb.AppendLine($"    public {csharpType} {fieldName} = {defaultValue};");
                }
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        public static string Pluralize(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            // Simple pluralization rules
            if (name.EndsWith("s") || name.EndsWith("x") || name.EndsWith("z") || name.EndsWith("ch") || name.EndsWith("sh"))
            {
                return name + "es";
            }
            else if (name.EndsWith("y") && name.Length > 1 && !"aeiou".Contains(name[name.Length - 2]))
            {
                return name.Substring(0, name.Length - 1) + "ies";
            }
            else
            {
                return name + "s";
            }
        }

        private string GetDefaultValue(string typeName)
        {
            var lowerType = typeName.ToLower();

            if (lowerType.EndsWith("[]"))
            {
                var baseType = GetCSharpType(typeName.Substring(0, typeName.Length - 2));
                return $"new List<{baseType}>()";
            }

            if (lowerType.StartsWith("pair<") && lowerType.EndsWith(">"))
            {
                var genericArgsString = typeName.Substring("pair<".Length, typeName.Length - "pair<".Length - 1);
                var genericTypes = genericArgsString.Split(',').Select(t => t.Trim()).ToArray();

                if (genericTypes.Length != 2)
                {
                    return "default"; // Fallback for invalid pair definition
                }

                var keyCSharpType = GetCSharpType(genericTypes[0]);
                var valueCSharpType = GetCSharpType(genericTypes[1]);

                return $"default(Pair<{keyCSharpType}, {valueCSharpType}>)";
            }

            switch (lowerType)
            {
                case "int": return "0";
                case "integer": return "0";
                case "float": return "0.0f";
                case "double": return "0.0";
                case "number": return "0.0f";
                case "bool": return "false";
                case "boolean": return "false";
                case "string": return "string.Empty";
                default: return "default"; // For object or unknown types
            }
        }

        private string GetCSharpType(string typeName)
        {
            var lowerType = typeName.ToLower();

            if (lowerType.EndsWith("[]"))
            {
                var baseType = GetCSharpType(typeName.Substring(0, typeName.Length - 2));
                return $"List<{baseType}>";
            }

            if (lowerType.StartsWith("pair<") && lowerType.EndsWith(">"))
            {
                var genericArgsString = typeName.Substring("pair<".Length, typeName.Length - "pair<".Length - 1);
                var genericTypes = genericArgsString.Split(',').Select(t => t.Trim()).ToArray();

                if (genericTypes.Length != 2)
                {
                    return "object"; // Fallback for invalid pair definition
                }

                var keyCSharpType = GetCSharpType(genericTypes[0]);
                var valueCSharpType = GetCSharpType(genericTypes[1]);

                return $"Pair<{keyCSharpType}, {valueCSharpType}>";
            }

            switch (lowerType)
            {
                case "int": return "int";
                case "integer": return "int";
                case "float": return "float";
                case "double": return "double";
                case "number": return "float"; // Assuming float for generic number
                case "bool": return "bool";
                case "boolean": return "bool";
                case "string": return "string";
                default: return "object"; // Default to object for unknown types
            }
        }
    }
}
