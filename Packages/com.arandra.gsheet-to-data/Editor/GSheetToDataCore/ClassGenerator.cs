
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                    var fieldName = ToPascalCaseOrThrow(parsedData.FieldNames[i]);
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
            var lowerName = name.ToLowerInvariant();
            if (lowerName.EndsWith("s") || lowerName.EndsWith("x") || lowerName.EndsWith("z") || lowerName.EndsWith("ch") || lowerName.EndsWith("sh"))
            {
                return name + "es";
            }
            else if (lowerName.EndsWith("y") && name.Length > 1 && "aeiou".IndexOf(char.ToLowerInvariant(name[name.Length - 2])) < 0)
            {
                return name.Substring(0, name.Length - 1) + "ies";
            }
            else
            {
                return name + "s";
            }
        }

        private string ToPascalCaseOrThrow(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException("Field name cannot be null or whitespace when generating classes.");
            }

            foreach (var ch in fieldName)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || char.IsWhiteSpace(ch))
                {
                    continue;
                }

                throw new ArgumentException($"Field name '{fieldName}' contains invalid character '{ch}'. Use spaces or underscores as separators.");
            }

            var normalized = new string(fieldName.Select(c => char.IsWhiteSpace(c) ? ' ' : c).ToArray());
            var tokens = normalized.Split(new[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
            {
                throw new ArgumentException($"Field name '{fieldName}' must contain alphanumeric characters.");
            }

            var sb = new StringBuilder();
            foreach (var token in tokens)
            {
                var firstChar = char.ToUpperInvariant(token[0]);
                sb.Append(firstChar);
                if (token.Length > 1)
                {
                    sb.Append(token.Substring(1));
                }
            }

            var result = sb.ToString();
            if (result.Length == 0)
            {
                throw new ArgumentException($"Field name '{fieldName}' is invalid after formatting.");
            }

            if (char.IsDigit(result[0]))
            {
                result = "_" + result;
            }

            return result;
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

            if (lowerType == "string")
            {
                return "string.Empty";
            }

            return "default";
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
                case "float": return "float";
                case "double": return "double";
                case "bool": return "bool";
                case "string": return "string";
                default: return "object"; // Default to object for unknown types
            }
        }
    }
}
