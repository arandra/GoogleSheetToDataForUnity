#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GSheetToDataCore;
using Newtonsoft.Json;
using SerializableTypes;
using UnityEditor;
using UnityEngine;

namespace GSheetToDataForUnity.Editor
{
    internal static class GSheetToDataAssetBuilder
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new PairArrayJsonConverter() }
        };

        internal static bool TryCreate(GSheetToDataGenerationJob job)
        {
            if (job == null)
            {
                return true;
            }

            var dataType = FindType(job.DataClassFullName);
            var scriptableType = FindType(job.ScriptableObjectFullName);

            if (dataType == null || scriptableType == null)
            {
                return false;
            }

            if (!typeof(ScriptableObject).IsAssignableFrom(scriptableType))
            {
                Debug.LogError($"[GSheetToData] {job.ScriptableObjectFullName} is not a ScriptableObject.");
                return true;
            }

            var assetInstance = ScriptableObject.CreateInstance(scriptableType);
            if (job.SheetType == SheetDataType.Const)
            {
                var constValue = JsonConvert.DeserializeObject(job.JsonPayload, dataType, SerializerSettings)
                                ?? Activator.CreateInstance(dataType);
                AssignConstValue(scriptableType, assetInstance, constValue);
            }
            else
            {
                var listType = typeof(List<>).MakeGenericType(dataType);
                var values = JsonConvert.DeserializeObject(job.JsonPayload, listType, SerializerSettings)
                             ?? Activator.CreateInstance(listType);
                AssignTableValues(scriptableType, assetInstance, values);
            }
            var absoluteAssetPath = GSheetToDataPathUtility.GetAbsoluteFromAssetPath(job.AssetRelativePath);
            var directory = Path.GetDirectoryName(absoluteAssetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var existingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(job.AssetRelativePath);
            if (existingAsset == null)
            {
                AssetDatabase.CreateAsset(assetInstance, job.AssetRelativePath);
                Debug.Log($"[GSheetToData] Created ScriptableObject at {job.AssetRelativePath}.");
            }
            else
            {
                EditorUtility.CopySerialized(assetInstance, existingAsset);
                EditorUtility.SetDirty(existingAsset);
                UnityEngine.Object.DestroyImmediate(assetInstance);
                Debug.Log($"[GSheetToData] Updated ScriptableObject at {job.AssetRelativePath}.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        private static void AssignTableValues(Type scriptableType, ScriptableObject instance, object values)
        {
            var field = scriptableType.GetField("Values", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                Debug.LogWarning($"[GSheetToData] Unable to populate data because {scriptableType.Name} is missing a Values field.");
                return;
            }

            field.SetValue(instance, values);
        }

        private static void AssignConstValue(Type scriptableType, ScriptableObject instance, object value)
        {
            var method = scriptableType.GetMethod("SetValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
            {
                method.Invoke(instance, new[] { value });
                return;
            }

            var field = scriptableType.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? scriptableType.GetField("Value", BindingFlags.Instance | BindingFlags.Public);

            if (field == null)
            {
                Debug.LogWarning($"[GSheetToData] Unable to store CONST data because {scriptableType.Name} has no suitable field.");
                return;
            }

            field.SetValue(instance, value);
        }

        private static Type FindType(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return null;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
#endif
