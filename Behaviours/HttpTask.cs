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
                if (body != null) {

                    request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8,
                        headers.Get("Content-Type")?.ToString() ?? "application/json");
                }
                HttpResponseMessage response = null;
                Dictionary<string, object> responseBody = null;
                Dictionary<string, string> responseHeaders = null;
                BehaviourError responseError = null;
                try {

                    response = await client.SendAsync(request);
                    if (response == null) throw new Exception("Request Failed");
                    responseBody =
                        JsonConvert.DeserializeObject<Dictionary<string, object>>(await response.Content.ReadAsStringAsync());
                    responseBody.Parse();
                    responseHeaders = new Dictionary<string, string>();
                    foreach (var header in response.Headers) responseHeaders[header.Key] = string.Join("", header.Value.ToArray());
                    foreach (var header in response.Content.Headers) responseHeaders[header.Key] = string.Join("", header.Value.ToArray());
                    if (response.StatusCode != HttpStatusCode.OK) {

                        var errorMessage = await response.RequestMessage.Content.ReadAsStringAsync();
                        if (responseBody.Get("message") != null) errorMessage = responseBody.Get("message")?.ToString();
                        responseError = new BehaviourError(errorMessage);
                        responseError.Code = (int)((object)response.StatusCode);
                    }
                } catch (Exception exception) {

                    responseError = new BehaviourError(exception.Message);
                } finally {

                    if (client != null) client.Dispose();
                    if (request != null) request.Dispose();
                    if (response != null) response.Dispose();
                }
                cb(responseBody, responseHeaders, responseError);
            });            
        }

        private HttpMethod getHttpMethod(string method) {
            
            try {

                return new HttpMethod(method);
            } catch {

                return null;
            }
        }
    }
}
