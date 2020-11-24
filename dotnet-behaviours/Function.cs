using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace dotnet_behaviours
{
    class Function : IFunction<Dictionary<string, object>, IBehaviorCallback<Dictionary<string, object>>, Task<object>>
    {
        private IGetURLFunction getURL;
        private string behaviourName;
        private Dictionary<string, object> behaviour;
        private Dictionary<string, object> parameters;

        public Function(IGetURLFunction getURL, string behaviourName, Dictionary<string, object> behaviour, Dictionary<string, object> parameters)
        {
            this.getURL = getURL;
            this.behaviourName = behaviourName;
            this.behaviour = behaviour;
            this.parameters = parameters;
        }

        public async Task<object> apply(Dictionary<string, object> data, IBehaviorCallback<Dictionary<string, object>> cb)
        {
            if (data == null) data = new Dictionary<string, object>();

            Dictionary<string, object> parms = new Dictionary<string, object>();

            if (behaviour["parameters"].GetType() == typeof(Dictionary<string, object>)) parms.Clone((Dictionary<string, object>) behaviour["parameters"]);

            parms.Clone(parameters);

            Dictionary<string, object> headers = new Dictionary<string, object>();
            Dictionary<string, object> body = new Dictionary<string, object>();
            string url = (string) behaviour["path"];

            foreach(KeyValuePair<string, object> entry in parms)
            {
                if (Utility.getValueForParameter( (Dictionary<string, object>) parms[entry.Key], data, entry.Key, behaviourName) == null)
                    continue;
                if (parms[entry.Key].GetType() == typeof(Dictionary<string, object>))
                {
                    Object _unless_ = ((Dictionary<string, object>) parms[entry.Key])["unless"];
                    if (_unless_.GetType() == typeof(string[]))
                    {
                        if ((new List<string>((ICollection<string>) _unless_).Contains(behaviourName)))
                            continue;
                    }
                }
                if (((Dictionary<string, object>) parms[entry.Key])["type"] != null)
                {
                    switch ((string)((Dictionary<string, object>) parms[entry.Key])["type"])
                    {
                        case "header":
                            headers.Add((string)((Dictionary<string, object>) parms[entry.Key])["key"], 
                                Utility.getValueForParameter((Dictionary<string, object>) parms[entry.Key], data, entry.Key, behaviourName));
                            break;
                        case "body":
                            string[] paths = ((string)((Dictionary<string, object>)parms[entry.Key])["key"]).Split(new string[] { "\\." }, StringSplitOptions.RemoveEmptyEntries);
                            Dictionary<string, object> nestedData = body;
                            string lastPath = null;
                            foreach(string path in paths)
                            {
                                if (lastPath != null) nestedData = (Dictionary<string, object>)nestedData[lastPath];
                                if (nestedData[path] == null) nestedData.Add(path, new Dictionary<string, object>());
                                lastPath = path;
                            }
                            if (lastPath != null)
                                nestedData.Add(lastPath, Utility.getValueForParameter((Dictionary<string, object>) parms[entry.Key], data, entry.Key, behaviourName));
                            break;
                        case "query":
                            if (url.IndexOf('?') == -1) url += '?';
                            string behaviourKey = HttpUtility.UrlEncode((string)((Dictionary<string, object>) parms[entry.Key])["key"], Encoding.UTF8);
                            string dataValue = HttpUtility.UrlEncode(Utility.getValueForParameter((Dictionary<string, object>) parms[entry.Key], 
                                data, entry.Key, behaviourName).ToString(), Encoding.UTF8);
                            url += '&' + behaviourKey + '=' + dataValue;
                            break;
                        case "path":
                            string PDataValue = HttpUtility.UrlEncode(Utility.getValueForParameter((Dictionary<string, object>) parms[entry.Key], 
                                data, entry.Key, behaviourName).ToString(), Encoding.UTF8);
                            url = url.Replace(':' + (string)((Dictionary<string, object>) parms[entry.Key])["key"], PDataValue);
                            break;
                    }
                }
            }

            Callback callback = new Callback(behaviour, parameters, cb);

            HttpConnectionEstablishment httpConnectionEstablishment = new HttpConnectionEstablishment(this.getURL);
            try
            {
                var content = await httpConnectionEstablishment.execute(url, behaviour["method"].ToString(), headers, body);
                if (content != null) callback.callback(content, null);
            }
            catch (Exception ex)
            {
                callback.callback(null, ex);
            }

            return null;
        }

        public bool equals(object var1, object var2)
        {
            return false;
        }
    }
}
