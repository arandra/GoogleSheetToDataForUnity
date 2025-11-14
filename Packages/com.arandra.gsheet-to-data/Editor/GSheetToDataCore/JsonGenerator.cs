using System.Collections.Generic;
using Newtonsoft.Json;
using SerializableTypes;
using System.Linq;
using System;
using System.Text;

namespace GSheetToDataCore
{
    public class JsonGenerator
    {
        private readonly JsonSerializerSettings jsonSerializerSettings;

        public JsonGenerator()
        {
            jsonSerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Converters = new List<JsonConverter> { new PairArrayJsonConverter() }
            };
        }

        public string GenerateJsonString(ParsedSheetData parsedData)
        {
            var isConst = parsedData?.SheetType == SheetDataType.Const;
            var emptyPayload = isConst ? "{}" : "[]";

            if (parsedData == null ||
                parsedData.FieldNames == null ||
                parsedData.FieldTypes == null ||
                parsedData.DataRows == null)
            {
                return emptyPayload;
            }

            if (isConst)
            {
                var row = parsedData.DataRows.FirstOrDefault();
                if (row == null)
                {
                    return emptyPayload;
                }

                var payload = CreateObjectFromRow(row, parsedData);
                return JsonConvert.SerializeObject(payload, jsonSerializerSettings);
            }

            var objectList = new List<Dictionary<string, object?>>();
            foreach (var row in parsedData.DataRows)
            {
                objectList.Add(CreateObjectFromRow(row, parsedData));
            }

            return JsonConvert.SerializeObject(objectList, jsonSerializerSettings);
        }

        private Dictionary<string, object?> CreateObjectFromRow(IList<object> row, ParsedSheetData parsedData)
        {
            var obj = new Dictionary<string, object?>();
            for (int i = 0; i < parsedData.FieldNames.Count; i++)
            {
                if (i >= parsedData.FieldTypes.Count)
                {
                    break;
                }

                var fieldName = NormalizeFieldName(parsedData.FieldNames[i], parsedData.FieldTypes[i]);
                if (string.IsNullOrWhiteSpace(fieldName))
                {
                    continue;
                }

                var fieldType = parsedData.FieldTypes[i];
                var stringValue = i < row.Count ? row[i]?.ToString() : null;
                obj[fieldName] = ConvertToTypedObject(stringValue, fieldType);
            }

            return obj;
        }

        private object? ConvertToTypedObject(string? value, string type)
        {
            var lowerType = type.ToLower();

            if (lowerType.EndsWith("[]"))
            {
                var baseTypeName = type.Substring(0, type.Length - 2);
                var baseType = GetSystemType(baseTypeName);

                if (baseType == null)
                {
                    return null; // Could not resolve base type
                }

                var listType = typeof(List<>).MakeGenericType(baseType);
                var list = (System.Collections.IList)Activator.CreateInstance(listType)!;

                if (string.IsNullOrWhiteSpace(value))
                {
                    return list;
                }

                // If the base type is a Pair, we need a more sophisticated split
                if (baseTypeName.ToLower().StartsWith("pair<"))
                {
                    // Regex to find (key, value) patterns
                    var pairPattern = @"\(([^)]*?)\)";
                    var matches = System.Text.RegularExpressions.Regex.Matches(value, pairPattern);
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            var pairContent = match.Groups[1].Value.Trim();
                            list.Add(ConvertToTypedObject($"({pairContent})", baseTypeName));
                        }
                    }
                }
                else
                {
                    var components = value.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList();
                    foreach (var component in components)
                    {
                        list.Add(ConvertToTypedObject(component, baseTypeName));
                    }
                }
                return list;
            }

            if (lowerType.StartsWith("pair<") && lowerType.EndsWith(">"))
            {
                // Extract generic arguments, e.g., "int,string" from "pair<int,string>"
                var genericArgsString = type.Substring("pair<".Length, type.Length - "pair<".Length - 1);
                var genericTypes = genericArgsString.Split(',').Select(t => t.Trim()).ToArray();

                if (genericTypes.Length != 2)
                {
                    // Invalid pair type definition
                    return null;
                }

                var keyTypeName = genericTypes[0];
                var valueTypeName = genericTypes[1];
                var keyType = GetSystemType(keyTypeName);
                var valueType = GetSystemType(valueTypeName);

                if (keyType == null || valueType == null)
                {
                    return null; // Could not resolve types
                }

                var pairType = typeof(Pair<,>).MakeGenericType(keyType, valueType);

                if (string.IsNullOrWhiteSpace(value))
                {
                    return Activator.CreateInstance(pairType);
                }

                // Parse the pair string value, e.g., "(1,hello)" or "1,hello"
                string cleanedValue = value.Trim();
                if (cleanedValue.StartsWith("(") && cleanedValue.EndsWith(")"))
                {
                    cleanedValue = cleanedValue.Substring(1, cleanedValue.Length - 2);
                }
                var pairComponents = cleanedValue.Split(',').Select(c => c.Trim()).ToArray();

                if (pairComponents.Length != 2)
                {
                    // Invalid pair value format
                    return null;
                }

                var keyObject = ConvertToTypedObject(pairComponents[0], keyTypeName);
                var valueObject = ConvertToTypedObject(pairComponents[1], valueTypeName);

                if (keyObject == null || valueObject == null)
                {
                    return null; // One of the components could not be converted
                }

                return Activator.CreateInstance(pairType, keyObject, valueObject);
            }

            if (string.IsNullOrEmpty(value))
            {
                return GetPrimitiveDefault(lowerType);
            }

            switch (lowerType)
            {
                case "int":
                    return int.TryParse(value, out var intVal) ? intVal : 0;
                case "float":
                    return float.TryParse(value, out var floatVal) ? floatVal : 0.0f;
                case "double":
                    return double.TryParse(value, out var doubleVal) ? doubleVal : 0.0d;
                case "bool":
                    return bool.TryParse(value, out var boolVal) && boolVal;
                case "string":
                default:
                    return value;
            }
        }

        private object? GetPrimitiveDefault(string lowerType)
        {
            switch (lowerType)
            {
                case "int":
                    return 0;
                case "float":
                    return 0.0f;
                case "double":
                    return 0.0d;
                case "bool":
                    return false;
                case "string":
                    return string.Empty;
                default:
                    return null;
            }
        }

        private Type? GetSystemType(string typeName)
        {
            var lowerType = typeName.ToLower();

            if (lowerType.StartsWith("pair<") && lowerType.EndsWith(">"))
            {
                var genericArgsString = typeName.Substring("pair<".Length, typeName.Length - "pair<".Length - 1);
                var genericTypes = genericArgsString.Split(',').Select(t => t.Trim()).ToArray();

                if (genericTypes.Length != 2)
                {
                    return null; // Invalid pair type definition
                }

                var keyType = GetSystemType(genericTypes[0]);
                var valueType = GetSystemType(genericTypes[1]);

                if (keyType == null || valueType == null)
                {
                    return null; // Could not resolve generic argument types
                }

                return typeof(Pair<,>).MakeGenericType(keyType, valueType);
            }

            switch (lowerType)
            {
                case "int": return typeof(int);
                case "float": return typeof(float);
                case "double": return typeof(double);
                case "bool": return typeof(bool);
                case "string": return typeof(string);
                // Add other types as needed
                default: return null;
            }
        }

        private string NormalizeFieldName(string fieldName, string fieldType)
        {
            var pascal = ToPascalCaseOrThrow(fieldName);
            if (IsListType(fieldType))
            {
                pascal = ClassGenerator.Pluralize(pascal);
            }
            return pascal;
        }

        private static bool IsListType(string fieldType)
        {
            if (string.IsNullOrWhiteSpace(fieldType))
            {
                return false;
            }

            var lower = fieldType.ToLowerInvariant();
            return lower.EndsWith("[]");
        }

        private string ToPascalCaseOrThrow(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException("Field name cannot be null or whitespace when generating JSON.");
            }

            foreach (var ch in fieldName)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || char.IsWhiteSpace(ch))
                {
                    continue;
                }

                throw new ArgumentException($"Field name '{fieldName}' contains invalid character '{ch}'.");
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
    }
}
