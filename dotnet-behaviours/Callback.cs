using System;
using System.Collections.Generic;

namespace dotnet_behaviours
{
    class Callback : ICallback<Dictionary<string, object>>
    {
        private Dictionary<string, object> behaviour;
        private Dictionary<string, object> parameters;
        private IBehaviorCallback<Dictionary<string, object>> cb;

        public Callback(Dictionary<string, object> behaviour, Dictionary<string, object> parameters, IBehaviorCallback<Dictionary<string, object>> cb)
        {
            this.behaviour = behaviour;
            this.parameters = parameters;
            this.cb = cb;
        }

        public void callback(Dictionary<string, object> response, Exception exception)
        {
            Dictionary<string, object> headers = new Dictionary<string, object>();
            Dictionary<string, object> body = new Dictionary<string, object>();

            if (behaviour["returns"].GetType() == typeof(Dictionary<string, object>) && response != null) 
            {
                foreach(string key in ((Dictionary<string, object>)behaviour["returns"]).Keys)
                {
                    object paramValue = null;
                    string paramKey = null;

                    string paramType = ((string)((Dictionary<string, object>) ((Dictionary<string, object>)behaviour["returns"])[key])["type"]);
                    if (Utility.isEqual(paramType, "header"))
                    {
                        paramValue = ((Dictionary<string, object>) response["headers"])[key];
                        paramKey = (string)((Dictionary<string, object>)((Dictionary<string, object>)behaviour["returns"])[key])["key"];

                        if (paramKey == null) paramKey = key;
                        headers.Add(paramKey, paramValue);
                    }
                    if (Utility.isEqual(paramType, "body"))
                    {
                        paramValue = ((Dictionary<string, object>) response["response"])["response"];
                        if (paramValue.GetType() == typeof(Dictionary<string, object>))
                            paramValue = ((Dictionary<string, object>)paramValue)[key];
                        paramKey = key;
                        body.Add(paramKey, paramValue);
                    }

                    object purposes = ((Dictionary<string, object>)((Dictionary<string, object>)behaviour["returns"])[key])["purpose"];

                    if (purposes != null && paramValue != null && paramKey != null)
                    {
                        if (!(purposes.GetType() == typeof(List<object>)))
                        {
                            List<object> purposeList = new List<object>();
                            purposeList.Add(purposes);
                            ((Dictionary<string, object>)((Dictionary<string, object>)behaviour["returns"])[key]).Add("purpose", purposeList);
                            purposes = purposeList;
                        }

                        foreach (object purpose in ((List<object>)purposes))
                        {
                            switch ((string)(purpose.GetType() == typeof(Dictionary<string, object>) ? ((Dictionary<string, object>)purpose)["as"] : purpose))
                            {
                                case "parameter":
                                    Dictionary<string, object> param = new Dictionary<string, object>();
                                    param.Add("key", key);
                                    param.Add("type", paramType);
                                    parameters.Add(paramKey, param);
                                    param = Utility.getDataFromSharedPreference();
                                    param.Add(paramKey, parameters[paramKey]);

                                    if (purpose.GetType() == typeof(Dictionary<string, object>) && ((Dictionary<string, object>)purpose)["unless"] != null)
                                    {
                                        ((Dictionary<string, object>)parameters[paramKey]).Add("unless", ((Dictionary<string, object>)purpose)["unless"]);
                                        ((Dictionary<string, object>)param[paramKey]).Add("unless", ((Dictionary<string, object>)purpose)["unless"]);
                                    }

                                    if (purpose.GetType() == typeof(Dictionary<string, object>) && ((Dictionary<string, object>)purpose)["for"] != null)
                                    {
                                        ((Dictionary<string, object>)parameters[paramKey]).Add("for", ((Dictionary<string, object>)purpose)["for"]);
                                        ((Dictionary<string, object>)param[paramKey]).Add("for", ((Dictionary<string, object>)purpose)["for"]);
                                    }

                                    foreach (object p in ((List<object>)purposes))
                                    {
                                        if (Utility.isEqual(p, "constant") || (p.GetType() == typeof(Dictionary<string, object>)
                                            && Utility.isEqual(((Dictionary<string, object>)p)["as"], "constant")))
                                        {
                                            ((Dictionary<string, object>)parameters[paramKey]).Add("value", paramValue);
                                            ((Dictionary<string, object>)param[paramKey]).Add("value", paramValue);
                                            break;
                                        }
                                    }
                                    Utility.putDataIntoSharedPreference(param);
                                    break;
                            }
                        }
                    }
                }

                if(headers.Count != 0)
                {
                    if(body.Count == 0) body.Add("data", ((Dictionary<string, object>)response["response"])["response"]);
                    headers.Clone(body);
                    cb.callback(headers, new BehaviorError(exception != null ? exception.Message : null));
                    return;
                }
            }

            cb.callback(response != null ? (Dictionary<string, object>)((Dictionary<string, object>)response["response"])["response"] : null,
                                new BehaviorError(exception != null ? exception.Message : null));
        }
    }
}
