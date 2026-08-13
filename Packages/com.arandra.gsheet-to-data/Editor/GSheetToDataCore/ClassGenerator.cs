
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
            var pairListWrappers = new List<string>();

            // Properties
            for (int i = 0; i < parsedData.FieldNames.Count; i++)
            {
                if (i < parsedData.FieldTypes.Count)
                {
                    var fieldName = ToPascalCaseOrThrow(parsedData.FieldNames[i]);
                    var fieldType = parsedData.FieldTypes[i];
                    string? pairListWrapperType = null;
                    if (TryGetNestedPairTypes(fieldType, out var keyType, out var valueType))
                    {
                        pairListWrapperType = $"{fieldName}ItemList";
                        pairListWrappers.Add(
                            $"    [Serializable]\n" +
                            $"    public sealed class {pairListWrapperType} : SerializablePairList<{keyType}, {valueType}>\n" +
                            "    {\n" +
                            "    }");
                    }

                    var csharpType = GetCSharpType(fieldType, pairListWrapperType);
                    var defaultValue = GetDefaultValue(fieldType, pairListWrapperType);

                    // Pluralize field name if it's a list type
                    if (csharpType.StartsWith("List<"))
                    {
                        fieldName = Pluralize(fieldName);
                    }
                    sb.AppendLine($"    public {csharpType} {fieldName} = {defaultValue};");
                }
            }

            if (pairListWrappers.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(string.Join("\n\n", pairListWrappers));
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

        private string GetDefaultValue(string typeName, string? pairListWrapperType = null)
        {
            if (IsEnumType(typeName))
            {
                return "default";
            }

            var lowerType = typeName.ToLower();

            if (lowerType.EndsWith("[]"))
            {
                return $"new {GetCSharpType(typeName, pairListWrapperType)}()";
            }

            if (lowerType.StartsWith("pair<") && lowerType.EndsWith(">"))
            {
                var genericArgsString = typeName.Substring("pair<".Length, typeName.Length - "pair<".Length - 1);
                var genericTypes = genericArgsString.Split(',').Select(t => t.Trim()).ToArray();

                if (genericTypes.Length != 2)
                {
                    return "default"; // Fallback for invalid pair definition
                }

                var keyCSharpType = GetCSharpType(genericTypes[0], pairListWrapperType);
                var valueCSharpType = GetCSharpType(genericTypes[1], pairListWrapperType);

                return $"default(Pair<{keyCSharpType}, {valueCSharpType}>)";
            }

            if (lowerType == "string")
            {
                return "string.Empty";
            }

            return "default";
        }

        private string GetCSharpType(string typeName, string? pairListWrapperType = null)
        {
            if (IsEnumType(typeName))
            {
                return ExtractEnumTypeName(typeName);
            }

            var lowerType = typeName.ToLower();

            if (lowerType.EndsWith("[]"))
            {
                var elementTypeName = typeName.Substring(0, typeName.Length - 2);
                if (TryGetPrimitiveListWrapper(elementTypeName, out var primitiveListWrapper))
                {
                    return $"List<{primitiveListWrapper}>";
                }

                if (!string.IsNullOrEmpty(pairListWrapperType) && IsPairListType(elementTypeName))
                {
                    return $"List<{pairListWrapperType}>";
                }

                var baseType = GetCSharpType(elementTypeName, pairListWrapperType);
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

                var keyCSharpType = GetCSharpType(genericTypes[0], pairListWrapperType);
                var valueCSharpType = GetCSharpType(genericTypes[1], pairListWrapperType);

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

        private bool TryGetNestedPairTypes(string typeName, out string keyType, out string valueType)
        {
            keyType = string.Empty;
            valueType = string.Empty;

            if (string.IsNullOrWhiteSpace(typeName) || !typeName.EndsWith("[][]", StringComparison.Ordinal))
            {
                return false;
            }

            var pairTypeName = typeName.Substring(0, typeName.Length - 4);
            if (!TryGetPairTypeNames(pairTypeName, out var keyTypeName, out var valueTypeName))
            {
                return false;
            }

            keyType = GetCSharpType(keyTypeName);
            valueType = GetCSharpType(valueTypeName);
            return keyType != "object" && valueType != "object";
        }

        private static bool TryGetPrimitiveListWrapper(string typeName, out string wrapperType)
        {
            wrapperType = string.Empty;
            if (string.IsNullOrWhiteSpace(typeName) || !typeName.EndsWith("[]", StringComparison.Ordinal))
            {
                return false;
            }

            var elementType = typeName.Substring(0, typeName.Length - 2).Trim().ToLowerInvariant();
            switch (elementType)
            {
                case "int": wrapperType = "ListInt"; return true;
                case "float": wrapperType = "ListFloat"; return true;
                case "double": wrapperType = "ListDouble"; return true;
                case "bool": wrapperType = "ListBool"; return true;
                case "string": wrapperType = "ListString"; return true;
                default: return false;
            }
        }

        private static bool IsPairListType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName) || !typeName.EndsWith("[]", StringComparison.Ordinal))
            {
                return false;
            }

            return TryGetPairTypeNames(typeName.Substring(0, typeName.Length - 2), out _, out _);
        }

        private static bool TryGetPairTypeNames(string typeName, out string keyType, out string valueType)
        {
            keyType = string.Empty;
            valueType = string.Empty;
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return false;
            }

            var trimmed = typeName.Trim();
            if (!trimmed.StartsWith("pair<", StringComparison.OrdinalIgnoreCase) || !trimmed.EndsWith(">"))
            {
                return false;
            }

            var genericArgs = trimmed.Substring(5, trimmed.Length - 6)
                .Split(',')
                .Select(type => type.Trim())
                .ToArray();

            if (genericArgs.Length != 2)
            {
                return false;
            }

            keyType = genericArgs[0];
            valueType = genericArgs[1];
            return true;
        }

        private static bool IsEnumType(string typeName)
        {
            return TryExtractEnumTypeName(typeName, out _);
        }

        private static string ExtractEnumTypeName(string typeName)
        {
            if (TryExtractEnumTypeName(typeName, out var enumTypeName))
            {
                return enumTypeName;
            }

            return "object";
        }

        private static bool TryExtractEnumTypeName(string typeName, out string enumTypeName)
        {
            enumTypeName = string.Empty;
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return false;
            }

            var trimmed = typeName.Trim();
            if (!trimmed.StartsWith("enum(", StringComparison.OrdinalIgnoreCase) || !trimmed.EndsWith(")"))
            {
                return false;
            }

            enumTypeName = trimmed.Substring(5, trimmed.Length - 6).Trim();
            return !string.IsNullOrWhiteSpace(enumTypeName);
        }
    }
}
