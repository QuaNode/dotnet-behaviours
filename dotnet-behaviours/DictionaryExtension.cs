using System.Collections.Generic;

namespace dotnet_behaviours
{
    public static class DictionaryExtension
    {
        public static void Clone<T, S>(this Dictionary<T, S> source, Dictionary<T, S> collection)
        {
            if (collection == null) return;

            foreach(var item in collection)
            {
                if (!source.ContainsKey(item.Key)) source.Add(item.Key, item.Value);
            }
        }
    }
}
