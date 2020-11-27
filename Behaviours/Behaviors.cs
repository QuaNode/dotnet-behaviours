using System.Collections.Generic;
using System;
using System.Linq;
using System.Web;
using System.Text;

namespace QuaNode {

    public class Behaviours {

        private Dictionary<string, object> behavioursBody = null;
        private Dictionary<string, string> behavioursHeaders = null;
        private Dictionary<string, object> defaults = null;
        private Action<BehaviourError> errorCallback = null;
        private HttpTask httpTask;
        private Action[] callbacks = { };

        public Behaviours(string baseUrl) : this(baseUrl, null) { }

        public Behaviours(string baseUrl, Action<BehaviourError> cb) : this(baseUrl, cb, null) {}

        public Behaviours(string baseUrl, Action<BehaviourError> cb, Dictionary<string, object> defaults) {

            httpTask = new HttpTask(baseUrl);
            try {

                _ = httpTask.Start("/behaviours", "Get", null, null, delegate (Dictionary<string, object> body,
                    Dictionary<string, string> headers, BehaviourError error) {

                        if (body != null && error == null) {

                            behavioursBody = body;
                            behavioursHeaders = new Dictionary<string, string>();
                            behavioursHeaders["Content-Type"] = headers["Content-Type"];
                            foreach (Action callback in callbacks) callback();
                            errorCallback = cb;
                            this.defaults = defaults;
                        } else {

                            throw new Exception("Failed to initialize Behaviors");
                        }
                });
            } catch (Exception) {

                throw new Exception("Failed to initialize Behaviors");
            }
        }

        public string BaseURL() {

            return httpTask.baseUrl;
        }

        public void OnReady (Action cb) {

            if (cb == null) return;
            if (behavioursBody == null) {

                callbacks.Append(cb);
            } else cb();
        }

        private bool isEqual(Object o1, Object o2) {

            return o1 != null && o1.Equals(o2);
        }

        public Action<Dictionary<string, object>, Action<Dictionary<string, object>, BehaviourError>> GetBehaviour(string behaviourName) {

            if (behaviourName == null) throw new Exception("Invalid behaviour name");
            if (behavioursBody == null) throw new Exception("Behaviors is not ready yet");
            Dictionary<string, object> behaviour = (Dictionary<string, object>) behavioursBody["behaviourName"];
            if(behaviour == null) throw new Exception("This behaviour does not exist");
            return delegate (Dictionary<string, object> behaviourData, Action<Dictionary<string, object>, BehaviourError> callback) {

                if (behaviourData == null) behaviourData = new Dictionary<string, object>();
                Dictionary<string, object> parameters = Cache.getParameter().Merge(defaults);
                Dictionary<string, object> @params = new Dictionary<string, object>();
                if ((behaviour["parameters"] as Dictionary<string, object>) != null) {

                    foreach (var parameter in (Dictionary<string, object>)behaviour["parameters"]) {

                        @params[parameter.Key] = parameters[parameter.Key] ?? parameter.Value;
                    }
                }
                Dictionary<string, string> headers = new Dictionary<string, string>();
                Dictionary<string, object> body = new Dictionary<string, object>();
                string url = (string)behaviour["path"];
                foreach (var parameter in @params) {

                    var param = parameter.Value as Dictionary<string, object>;
                    if (param == null) continue;
                    var value = Cache.getValueForParameter(param, behaviourData, parameter.Key, behaviourName);
                    var type = param["type"] as string;
                    if (value == null && type != "path") continue;
                    if (param["unless"].GetType().IsArray && ((object[])param["unless"]).Contains(behaviourName)) continue;
                    if (param["for"].GetType().IsArray && !((object[])param["for"]).Contains(behaviourName)) continue;
                    switch (type) {

                        case "header":
                            headers[(string)param["key"]] = value?.ToString();
                            break;
                        case "body":
                            string[] paths = ((string)param["key"]).Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                            Dictionary<string, object> nestedData = body;
                            string lastPath = null;
                            foreach (string path in paths) {

                                if (lastPath != null) nestedData = (Dictionary<string, object>)nestedData[lastPath];
                                if (nestedData[path] == null) nestedData[path] = new Dictionary<string, object>();
                                lastPath = path;
                            }
                            if (lastPath != null) nestedData[lastPath] = value;
                            break;
                        case "query":
                            var and = "&";
                            if (!url.Contains("?")) {

                                url += "?";
                                and = "";
                            }
                            url += and + HttpUtility.UrlEncode((string)param["key"], Encoding.UTF8) + "=" +
                                HttpUtility.UrlEncode(value?.ToString(), Encoding.UTF8);
                            break;
                        case "path":
                            url = url.Replace(":" + HttpUtility.UrlEncode((string)param["key"], Encoding.UTF8), value != null ?
                                HttpUtility.UrlEncode(value?.ToString(), Encoding.UTF8) : "*");
                            break;
                    }
                }
                Action<string> request = null;
                request = delegate(string signature) {

                    try {

                        var request_method = (behaviour["method"].ToString()[0] + "").ToUpper() +
                            behaviour["method"].ToString().Substring(1, behaviour["method"].ToString().Length).ToLower();
                        if (signature != null) {

                            Dictionary<string, string> signedHeaders = new Dictionary<string, string>();
                            signedHeaders["Behaviour-Signature"] = signature;
                            headers.Merge(signedHeaders);
                        }
                        _ = httpTask.Start(url, request_method, headers, body, delegate (Dictionary<string, object> resBody,
                            Dictionary<string, string> resHeaders, BehaviourError error) {

                                if (error != null && errorCallback != null) errorCallback(error);
                                if (resBody != null && (resBody["signature"] as string) != null) {

                                    request((string)resBody["signature"]);
                                    return;
                                }
                                headers = new Dictionary<string, string>();
                                body = new Dictionary<string, object>();
                                if ((behaviour["returns"] as Dictionary<string, object>) != null) {

                                    foreach (var @return in (Dictionary<string, object>)behaviour["returns"]) {

                                        object paramValue = null;
                                        string paramKey = null;
                                        string paramType = (@return.Value as Dictionary<string, object>)?["type"] as string;
                                        if (isEqual(paramType, "header")) {

                                            paramValue = (resBody?["headers"] as Dictionary<string, object>)?[@return.Key];
                                            paramKey = (@return.Value as Dictionary<string, object>)?["key"] as string;
                                            if (paramKey == null) paramKey = @return.Key;
                                            headers[paramKey] = paramValue as string;
                                        }
                                        if (isEqual(paramType, "body")) {

                                            paramValue = resBody?["response"];
                                            if ((paramValue as Dictionary<string, object>) != null)
                                                paramValue = ((Dictionary<string, object>)paramValue)[@return.Key];
                                            paramKey = @return.Key;
                                            body[paramKey] = paramValue;
                                        }
                                        object purposes = (@return.Value as Dictionary<string, object>)?["purpose"];
                                        if (purposes != null && paramValue != null && paramKey != null) {

                                            if (!purposes.GetType().IsArray) {

                                                object[] ṕurposes = { purposes };
                                                purposes = ((Dictionary<string, object>)@return.Value)["purpose"] = ṕurposes;
                                            }
                                            foreach (object purpose in ((object[])purposes)) {

                                                switch ((purpose as Dictionary<string, object>)?["as"] ?? purpose) {

                                                    case "parameter":
                                                        Dictionary<string, object> param = new Dictionary<string, object>();
                                                        param["key"] = @return.Key;
                                                        param["type"] = paramType;
                                                        parameters[paramKey] = param;
                                                        param = Cache.getParameter();
                                                        param[paramKey] = parameters[paramKey];
                                                        var @unless = (purpose as Dictionary<string, object>)?["unless"];
                                                        if (@unless != null) {

                                                            ((Dictionary<string, object>)parameters[paramKey])["unless"] = @unless;
                                                            ((Dictionary<string, object>)param[paramKey])["unless"] = @unless;
                                                        }
                                                        var @for = (purpose as Dictionary<string, object>)?["for"];
                                                        if (@for != null) {

                                                            ((Dictionary<string, object>)parameters[paramKey])["for"] = @for;
                                                            ((Dictionary<string, object>)param[paramKey])["for"] = @for;
                                                        }
                                                        foreach (object otherPurpose in ((object[])purposes)) {

                                                            if (isEqual(otherPurpose, "constant") ||
                                                                isEqual((otherPurpose as Dictionary<string, object>)?["as"], "constant")) {

                                                                ((Dictionary<string, object>)parameters[paramKey])["value"] = paramValue;
                                                                ((Dictionary<string, object>)param[paramKey])["value"] = paramValue;
                                                                break;
                                                            }
                                                        }
                                                        ((Dictionary<string, object>)parameters[paramKey])["source"] = true;
                                                        ((Dictionary<string, object>)param[paramKey])["source"] = true;
                                                        Cache.setParameter(param);
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                    if (headers.Count > 0) {

                                        if (body.Count == 0) body["data"] = resBody?["response"];
                                        callback(new Dictionary<string, object>((IDictionary<string, object>)headers).Merge(body), error);
                                        return;
                                    }
                                }
                                callback(resBody?["response"] as Dictionary<string, object>, error);
                            });
                    } catch (Exception exception) {

                        callback(null, new BehaviourError(exception.Message));
                    }
                };
                request(null);
            };
        }
    }
}
