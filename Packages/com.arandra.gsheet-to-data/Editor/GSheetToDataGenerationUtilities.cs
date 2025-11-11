#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;

namespace GSheetToDataForUnity.Editor
{
    internal static class GSheetToDataGenerationUtilities
    {
        internal static void EnsureProjectRelative(string path)
        {
            var assetRelative = GSheetToDataPathUtility.ToAssetRelativeFromUserInput(path);
            if (!assetRelative.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Path must reside under the Assets folder.");
            }
        }

        internal static string WriteScriptFile(string fileName, string contents, string targetDirectory)
        {
            var absoluteDir = GSheetToDataPathUtility.EnsureDirectory(
                GSheetToDataPathUtility.ToAbsolutePath(targetDirectory));

            var filePath = Path.Combine(absoluteDir, fileName);
            File.WriteAllText(filePath, contents, Encoding.UTF8);

            var assetPath = GSheetToDataPathUtility.ToAssetRelative(filePath).Replace('\\', '/');
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return assetPath;
        }

        internal static string BuildAssetRelativePath(string scriptableClassName, string assetOutputPath)
        {
            var absoluteDir = GSheetToDataPathUtility.EnsureDirectory(
                GSheetToDataPathUtility.ToAbsolutePath(assetOutputPath));

            var assetAbsolutePath = Path.Combine(absoluteDir, scriptableClassName + ".asset");
            var assetRelativePath = GSheetToDataPathUtility.ToAssetRelative(assetAbsolutePath).Replace('\\', '/');

            if (!assetRelativePath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Asset paths must reside under the Assets folder.");
            }

            return assetRelativePath;
        }

        internal static string WrapWithNamespace(string namespaceName, string classCode)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return classCode;
            }

            var lines = classCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var builder = new StringBuilder();
            builder.AppendLine($"namespace {namespaceName}");
            builder.AppendLine("{");

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    builder.AppendLine();
                }
                else
                {
                    builder.AppendLine($"    {line}");
                }
            }

            builder.AppendLine("}");
            return builder.ToString();
        }

        internal static string GenerateScriptableObjectClass(string dataClassName, string scriptableClassName, GSheetToDataCore.SheetDataType sheetType)
        {
            var builder = new StringBuilder();
            if (sheetType == GSheetToDataCore.SheetDataType.Table)
            {
                builder.AppendLine("using System.Collections.Generic;");
            }
            builder.AppendLine("using UnityEngine;");
            builder.AppendLine();
            builder.AppendLine($"[CreateAssetMenu(fileName = \"{scriptableClassName}\", menuName = \"GSheetToData/{scriptableClassName}\")]");
            builder.AppendLine($"public class {scriptableClassName} : ScriptableObject");
            builder.AppendLine("{");
            builder.AppendLine("    [SerializeField] private string sheetId = string.Empty;");
            builder.AppendLine("    [SerializeField] private string sheetName = string.Empty;");
            builder.AppendLine();
            builder.AppendLine("    public string SheetId => sheetId;");
            builder.AppendLine("    public string SheetName => sheetName;");
            builder.AppendLine();
            if (sheetType == GSheetToDataCore.SheetDataType.Table)
            {
                builder.AppendLine($"    public List<{dataClassName}> Values = new List<{dataClassName}>();");
            }
            else
            {
                builder.AppendLine($"    public {dataClassName} Value;");
            }
            builder.AppendLine();
            builder.AppendLine("    public void SetSheetMetadata(string id, string name)");
            builder.AppendLine("    {");
            builder.AppendLine("        sheetId = id;");
            builder.AppendLine("        sheetName = name;");
            builder.AppendLine("    }");
            builder.AppendLine();
            if (sheetType == GSheetToDataCore.SheetDataType.Const)
            {
                builder.AppendLine($"    public void SetValue({dataClassName} value)");
                builder.AppendLine("    {");
                builder.AppendLine("        Value = value;");
                builder.AppendLine("    }");
            }
            builder.AppendLine("}");
            return builder.ToString();
        }

        internal static string BuildFullName(string namespaceName, string className)
        {
            return string.IsNullOrWhiteSpace(namespaceName)
                ? className
                : $"{namespaceName}.{className}";
        }
    }
}
#endif
