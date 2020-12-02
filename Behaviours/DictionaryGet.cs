using System.Collections.Generic;

namespace QuaNode {

    public static class DictionaryGet {

        public static object Get<T, S>(this Dictionary<T, S> source, T key) {

            return source.ContainsKey(key) ? source[key] as object : null;
        }
    }
}
