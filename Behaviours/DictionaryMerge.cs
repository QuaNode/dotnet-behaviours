using System.Collections.Generic;

namespace QuaNode {

    public static class DictionaryMerge {

        public static Dictionary<T, S> Merge<T, S>(this Dictionary<T, S> source, Dictionary<T, S> collection) {

            if (collection == null) return source;
            foreach(var item in collection) if (!source.ContainsKey(item.Key)) source.Add(item.Key, item.Value);
            return source;
        }
    }
}
