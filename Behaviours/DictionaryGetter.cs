using System;
using System.Collections.Generic;
using System.Text;

namespace QuaNode
{
    public static class DictionaryGetter
    {
        public static object Get<T, S>(this Dictionary<T, S> source, T key) {
            return source.ContainsKey(key) ? (object) source[key] : null;
        }
    }
}
