using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QuaNode {

    class HttpTask {

        public string baseUrl;
        
        public HttpTask(string baseUrl) {

            this.baseUrl = baseUrl;
        }

        public Task Start(string path, string method, Dictionary<string, string> headers, Dictionary<string, object> body,
            Action<Dictionary<string, object>, Dictionary<string, string>, BehaviourError> cb) {

            return Task.Run(async () => {

                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(getHttpMethod(method), this.baseUrl + path);
                if (headers != null) foreach (var header in headers) request.Headers.Add(header.Key, header.Value);
                if (body != null) request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body)));
                HttpResponseMessage response = null;
                try {

                    response = await client.SendAsync(request);
                    if (response == null) throw new Exception("Request Failed");
                    var responseBody = JsonConvert.DeserializeObject<Dictionary<string, object>>(await response.Content.ReadAsStringAsync());
                    Dictionary<string, string> responseHeaders = new Dictionary<string, string>();
                    BehaviourError responseError = null;
                    foreach (var header in response.Headers) responseHeaders[header.Key] = string.Join("", header.Value.ToArray());
                    if (response.StatusCode != HttpStatusCode.OK) {

                        var errorMessage = await response.RequestMessage.Content.ReadAsStringAsync();
                        if ((responseBody["response"] as Dictionary<string, object>) != null &&
                            ((Dictionary<string, object>)responseBody["response"])["message"] != null)
                            errorMessage = (string)((Dictionary<string, object>)responseBody["response"])["message"];
                        responseError = new BehaviourError(errorMessage);
                        responseError.Code = (int)((object)response.StatusCode);
                    }
                    cb(responseBody, responseHeaders, responseError);
                } catch (Exception exception) {

                    cb(null, null, new BehaviourError(exception.Message));
                } finally {

                    if (client != null) client.Dispose();
                    if (request != null) request.Dispose();
                    if (response != null) response.Dispose();
                }
            });            
        }

        private HttpMethod getHttpMethod(string method) {
            
            try {
                return new HttpMethod(method);
                //return (HttpMethod)Enum.Parse(typeof(HttpMethod), method);
            } catch (Exception ex) {

                return null;
            }
        }
    }
}
