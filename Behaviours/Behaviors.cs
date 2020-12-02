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
        private  List<Action> callbacks = new List<Action>();

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
                            if (headers.ContainsKey("Content-Type")) {

                                behavioursHeaders["Content-Type"] = headers.Get("Content-Type")?.ToString();
                            }
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

                callbacks.Add(cb);
            } else cb();
        }

        private bool isEqual(Object o1, Object o2) {

            return o1 != null && o1.Equals(o2);
        }

        public Action<Dictionary<string, object>, Action<Dictionary<string, object>, BehaviourError>> GetBehaviour(string behaviourName) {

            if (behaviourName == null) throw new Exception("Invalid behaviour name");
            if (behavioursBody == null) throw new Exception("Behaviors is not ready yet");
            Dictionary<string, object> behaviour = behavioursBody.Get(behaviourName) as Dictionary<string, object>;
            if(behaviour == null) throw new Exception("This behaviour does not exist");
            return delegate (Dictionary<string, object> behaviourData, Action<Dictionary<string, object>, BehaviourError> callback) {

                if (behaviourData == null) behaviourData = new Dictionary<string, object>();
                Dictionary<string, object> parameters = Cache.getParameter().Merge(defaults);
                Dictionary<string, object> @params = new Dictionary<string, object>();
                if ((behaviour.Get("parameters") as Dictionary<string, object>) != null) {

                    foreach (var parameter in (Dictionary<string, object>)behaviour.Get("parameters")) {

                        @params[parameter.Key] = parameters.Get(parameter.Key) ?? parameter.Value;
                    }
                }
                Dictionary<string, string> headers = new Dictionary<string, string>();
                Dictionary<string, object> body = new Dictionary<string, object>();
                string url = behaviour.Get("path")?.ToString();
                foreach (var parameter in @params) {

                    var param = parameter.Value as Dictionary<string, object>;
                    if (param == null) continue;
                    var value = Cache.getValueForParameter(param, behaviourData, parameter.Key, behaviourName);
                    var type = param.Get("type")?.ToString();
                    if (value == null && type != "path") continue;
                    if (param.Get("unless")?.GetType().IsArray == true &&
                        (param.Get("unless") as object[]).Contains(behaviourName)) continue;
                    if (param.Get("for")?.GetType().IsArray == true &&
                        !(param.Get("for") as object[]).Contains(behaviourName)) continue;
                    switch (type) {

                        case "header":
                            headers[param.Get("key").ToString()] = value?.ToString();
                            break;
                        case "body":
                            string[] paths = param.Get("key")?.ToString()?.Split(new string[] { "." },
                                StringSplitOptions.RemoveEmptyEntries);
                            Dictionary<string, object> nestedData = body;
                            string lastPath = null;
                            foreach (string path in paths) {

                                if (lastPath != null) nestedData = nestedData.Get(lastPath) as Dictionary<string, object>;
                                if (nestedData != null && nestedData.Get(path) == null) {

                                    nestedData[path] = new Dictionary<string, object>();
                                }
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
                            url += and + HttpUtility.UrlEncode(param.Get("key")?.ToString(), Encoding.UTF8) + "=" +
                                HttpUtility.UrlEncode(value?.ToString(), Encoding.UTF8);
                            break;
                        case "path":
                            url = url.Replace(":" + HttpUtility.UrlEncode(param.Get("key")?.ToString(), Encoding.UTF8), value != null ?
                                HttpUtility.UrlEncode(value?.ToString(), Encoding.UTF8) : "*");
                            break;
                    }
                }
                Action<string> request = null;
                request = delegate(string signature) {

                    try {

                        var request_method = (behaviour.Get("method")?.ToString()?[0] + "").ToUpper() +
                            behaviour.Get("method")?.ToString()?.Substring(1, behaviour.Get("method").ToString().Length).ToLower();
                        if (signature != null) {

                            Dictionary<string, string> signedHeaders = new Dictionary<string, string>();
                            signedHeaders["Behaviour-Signature"] = signature;
                            headers.Merge(signedHeaders);
                        }
                        _ = httpTask.Start(url, request_method, headers, body, delegate (Dictionary<string, object> resBody,
                            Dictionary<string, string> resHeaders, BehaviourError error) {

                                if (error != null && errorCallback != null) errorCallback(error);
                                if (resBody != null && resBody.Get("signature") != null) {

                                    request(resBody.Get("signature")?.ToString());
                                    return;
                                }
                                headers = new Dictionary<string, string>();
                                body = new Dictionary<string, object>();
                                if ((behaviour.Get("returns") as Dictionary<string, object>) != null) {

                                    foreach (var @return in behaviour.Get("returns") as Dictionary<string, object>) {

                                        object paramValue = null;
                                        string paramKey = null;
                                        string paramType = (@return.Value as Dictionary<string, object>)?.Get("type")?.ToString();
                                        if (isEqual(paramType, "header")) {

                                            paramValue = (resBody?.Get("headers") as Dictionary<string, object>)?.Get(@return.Key);
                                            paramKey = (@return.Value as Dictionary<string, object>)?.Get("key")?.ToString();
                                            if (paramKey == null) paramKey = @return.Key;
                                            headers[paramKey] = paramValue as string;
                                        }
                                        if (isEqual(paramType, "body")) {

                                            paramValue = resBody?.Get("response");
                                            if ((paramValue as Dictionary<string, object>) != null)
                                                paramValue = (paramValue as Dictionary<string, object>).Get(@return.Key);
                                            paramKey = @return.Key;
                                            body[paramKey] = paramValue;
                                        }
                                        object purposes = (@return.Value as Dictionary<string, object>)?.Get("purpose");
                                        if (purposes != null && paramValue != null && paramKey != null) {

                                            if (!purposes.GetType().IsArray) {

                                                object[] ṕurposes = { purposes };
                                                purposes = ((Dictionary<string, object>)@return.Value)["purpose"] = ṕurposes;
                                            }
                                            foreach (object purpose in purposes as object[]) {

                                                switch ((purpose as Dictionary<string, object>)?.Get("as")?.ToString() ?? purpose) {

                                                    case "parameter":
                                                        Dictionary<string, object> param = new Dictionary<string, object>();
                                                        param["key"] = @return.Key;
                                                        param["type"] = paramType;
                                                        parameters[paramKey] = param;
                                                        param = Cache.getParameter();
                                                        param[paramKey] = parameters.Get(paramKey);
                                                        var @unless = (purpose as Dictionary<string, object>)?.Get("unless");
                                                        if (@unless != null) {

                                                            ((Dictionary<string, object>)parameters.Get(paramKey))["unless"] = @unless;
                                                            ((Dictionary<string, object>)param.Get(paramKey))["unless"] = @unless;
                                                        }
                                                        var @for = (purpose as Dictionary<string, object>)?.Get("for");
                                                        if (@for != null) {

                                                            ((Dictionary<string, object>)parameters.Get(paramKey))["for"] = @for;
                                                            ((Dictionary<string, object>)param.Get(paramKey))["for"] = @for;
                                                        }
                                                        foreach (object otherPurpose in purposes as object[]) {

                                                            if (isEqual(otherPurpose, "constant") ||
                                                                isEqual((otherPurpose as Dictionary<string, object>)?.Get("as")?.ToString(),
                                                                "constant")) {

                                                                ((Dictionary<string, object>)parameters.Get(paramKey))["value"] = paramValue;
                                                                ((Dictionary<string, object>)param.Get(paramKey))["value"] = paramValue;
                                                                break;
                                                            }
                                                        }
                                                        ((Dictionary<string, object>)parameters.Get(paramKey))["source"] = true;
                                                        ((Dictionary<string, object>)param.Get(paramKey))["source"] = true;
                                                        Cache.setParameter(param);
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                    if (headers.Count > 0) {

                                        if (body.Count == 0) body["data"] = resBody?.Get("response");
                                        callback(new Dictionary<string, object>((IDictionary<string, object>)headers).Merge(body), error);
                                        return;
                                    }
                                }
                                callback(resBody?.Get("response") as Dictionary<string, object>, error);
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
