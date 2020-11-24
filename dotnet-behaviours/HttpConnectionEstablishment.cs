using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace dotnet_behaviours
{
    class HttpConnectionEstablishment
    {
        private IGetURLFunction getURLFunction;
        
        public HttpConnectionEstablishment(IGetURLFunction getURLFunction)
        {
            this.getURLFunction = getURLFunction;
        }

        public async Task<Dictionary<string, object>> execute(string path, string method, Dictionary<string, object> headers, 
            Dictionary<string, object> requestContent)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(ConvertMethod(method), getURLFunction.apply(path));
            // Set request headers
            if(headers != null)
            {
                foreach (var header in headers) request.Headers.Add(header.Key, (string)header.Value);
            }
            // Set request content body    
            if(requestContent != null) request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestContent)));

            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request);
                if (response == null) throw new Exception("Failed to initialize Behaviors");
                    
                var result = await response.Content.ReadAsAsync<Dictionary<string, object>>();
                return result;
            }
            finally
            {
                if (client != null) client.Dispose();
                if (request != null) request.Dispose();
                if (response != null) response.Dispose();
            }
        }

        private HttpMethod ConvertMethod(string method)
        {
            HttpMethod _method;
            try
            {
                _method = (HttpMethod) Enum.Parse(typeof(HttpMethod), method);
                return _method;
            }
            catch
            {
                return null;
            }
        }
    }
}
