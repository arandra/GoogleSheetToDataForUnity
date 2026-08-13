using System;
using System.Collections.Generic;

namespace SerializableTypes
{
    [Serializable]
    public class SerializableList<T>
    {
        public List<T> Values = new List<T>();

        public SerializableList()
        {
        }

        public SerializableList(IEnumerable<T> values)
        {
            if (values != null)
            {
                Values.AddRange(values);
            }
        }
    }

    [Serializable]
    public sealed class ListInt : SerializableList<int>
    {
    }

    [Serializable]
    public sealed class ListFloat : SerializableList<float>
    {
    }

    [Serializable]
    public sealed class ListDouble : SerializableList<double>
    {
    }

    [Serializable]
    public sealed class ListBool : SerializableList<bool>
    {
    }

    [Serializable]
    public sealed class ListString : SerializableList<string>
    {
    }

    [Serializable]
    public class SerializablePairList<TKey, TValue> : SerializableList<Pair<TKey, TValue>>
    {
    }
}
