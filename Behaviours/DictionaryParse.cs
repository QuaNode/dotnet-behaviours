using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace QuaNode {

    public static class DictionaryParse {

        public static void Parse<T>(this Dictionary<T, object> source) {

            foreach(var item in source) {

                if ((source.Get(item.Key) as JObject) != null) {

                    source[item.Key] = (source.Get(item.Key) as JObject).ToObject<Dictionary<T, object>>();
                    (source[item.Key] as Dictionary<T, object>)?.Parse();
                }
                Action<object[]> parseArray = null;
                parseArray = delegate (object[] array) {

                    for (var i = 0; i < array?.Length; i++) {

                        var nested_source = array[i];
                        if ((nested_source as JObject) != null) {

                            array[i] = (nested_source as JObject).ToObject<Dictionary<T, object>>();
                            (array[i] as Dictionary<T, object>)?.Parse();
                        }
                        if ((nested_source as JArray) != null) {

                            array[i] = (nested_source as JArray).ToObject<object[]>();
                            parseArray(array[i] as object[]);
                        }
                    }
                };
                if ((source.Get(item.Key) as JArray) != null) {

                    source[item.Key] = (source.Get(item.Key) as JArray).ToObject<object[]>();
                    parseArray(source[item.Key] as object[]);
                }
            }
        }
    }
}
