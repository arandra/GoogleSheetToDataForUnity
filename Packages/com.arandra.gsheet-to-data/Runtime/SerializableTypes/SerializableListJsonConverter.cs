using System;
using System.Reflection;
using Newtonsoft.Json;

namespace SerializableTypes
{
    public sealed class SerializableListJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return TryGetElementType(objectType, out _);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer? serializer)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (!TryGetElementType(objectType, out var elementType))
            {
                throw new JsonSerializationException($"{objectType} is not a serializable list type.");
            }

            var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(elementType);
            var values = serializer.Deserialize(reader, listType);
            var instance = existingValue ?? Activator.CreateInstance(objectType);

            if (instance == null)
            {
                throw new JsonSerializationException($"Could not create {objectType}.");
            }

            var valuesField = objectType.GetField("Values", BindingFlags.Instance | BindingFlags.Public);
            if (valuesField == null)
            {
                throw new JsonSerializationException($"{objectType} is missing a public Values field.");
            }

            valuesField.SetValue(instance, values ?? Activator.CreateInstance(listType));
            return instance;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer? serializer)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var valuesField = value.GetType().GetField("Values", BindingFlags.Instance | BindingFlags.Public);
            serializer.Serialize(writer, valuesField?.GetValue(value));
        }

        private static bool TryGetElementType(Type type, out Type elementType)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (current.IsGenericType &&
                    current.GetGenericTypeDefinition() == typeof(SerializableList<>))
                {
                    elementType = current.GetGenericArguments()[0];
                    return true;
                }
            }

            elementType = null!;
            return false;
        }
    }
}
