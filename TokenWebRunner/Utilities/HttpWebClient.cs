using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using TokenWebRunner.Framework;

namespace TokenWebRunner.Utilities
{
    public interface IHttpWebClient
    {
        Task<HttpResult> DeleteAsync(string baseUri, string relativeUri, TokenInfo token = null, int timeoutSecond = 0);
        Task<HttpResult> GetAsync(string baseUri, string relativeUri, TokenInfo token = null, int timeoutSecond = 0);
        Task<HttpResult> PostAsync(string baseUri, string relativeUri, IDictionary<string, string> parameters, HttpWebClient.ContentType contentType, TokenInfo token = null, int timeoutSecond = 0);
        Task<HttpResult> PostAsync(string baseUri, string relativeUri, string postData, HttpWebClient.ContentType contentType, TokenInfo token = null, int timeoutSecond = 0);
        Task<HttpResult> PutAsync(string baseUri, string relativeUri, IDictionary<string, string> parameters, HttpWebClient.ContentType contentType, TokenInfo token = null, int timeoutSecond = 0);
        Task<HttpResult> PutAsync(string baseUri, string relativeUri, string postData, HttpWebClient.ContentType contentType, TokenInfo token = null, int timeoutSecond = 0);
        Task<HttpResult> SendAsync(string baseUri, string relativeUri, string postData, HttpWebClient.ContentType contentType, HttpMethod method, TokenInfo token = null, int timeoutSecond = 0);
    }
    public class HttpWebClient : ServiceLocator<IHttpWebClient, HttpWebClient>, IHttpWebClient
    {
        protected override Func<IHttpWebClient> GetFactory()
        {
            return () => new HttpWebClient();
        }
        private HttpClient GetClient(string baseUri, TokenInfo token, int timeoutSecond)
        {
            HttpClient client = new HttpClient(
              new HttpClientHandler
              {
                  AutomaticDecompression = DecompressionMethods.GZip
                                           | DecompressionMethods.Deflate,
              });

            client.BaseAddress = new Uri(baseUri);

            client.DefaultRequestHeaders.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("zh-CN"));
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("TokenWebRunner", "1.0"));

            if (timeoutSecond > 0)
                client.Timeout = new TimeSpan(timeoutSecond * TimeSpan.TicksPerSecond);
            if (token != null)
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(token.Token_Type, token.Access_Token);

            return client;
        }
        public async Task<HttpResult> GetAsync(string baseUri, string relativeUri, TokenInfo token = null, int timeoutSecond = 0)
        {
            return await SendAsync(baseUri, relativeUri, null, ContentType.json, HttpMethod.Get, token, timeoutSecond);
        }
        public async Task<HttpResult> PostAsync(string baseUri, string relativeUri, IDictionary<string, string> parameters, ContentType contentType, TokenInfo token = null, int timeoutSecond = 0)
        {
            string postData = BuildPostFormData(parameters);
            return await PostAsync(baseUri, relativeUri, postData, contentType, token, timeoutSecond);
        }
        public async Task<HttpResult> PostAsync(string baseUri, string relativeUri, string postData, ContentType contentType, TokenInfo token = null, int timeoutSecond = 0)
        {
            return await SendAsync(baseUri, relativeUri, postData, contentType, HttpMethod.Post, token, timeoutSecond);
        }
        public async Task<HttpResult> PutAsync(string baseUri, string relativeUri, IDictionary<string, string> parameters, ContentType contentType, TokenInfo token = null, int timeoutSecond = 0)
        {
            string postData = BuildPostFormData(parameters);
            return await PutAsync(baseUri, relativeUri, postData, contentType, token, timeoutSecond);
        }
        public async Task<HttpResult> PutAsync(string baseUri, string relativeUri, string postData, ContentType contentType, TokenInfo token = null, int timeoutSecond = 0)
        {
            return await SendAsync(baseUri, relativeUri, postData, contentType, HttpMethod.Put, token, timeoutSecond);
        }
        public async Task<HttpResult> DeleteAsync(string baseUri, string relativeUri, TokenInfo token = null, int timeoutSecond = 0)
        {
            return await SendAsync(baseUri, relativeUri, null, ContentType.json, HttpMethod.Delete, token, timeoutSecond);
        }
        public async Task<HttpResult> SendAsync(string baseUri, string relativeUri, string postData, ContentType contentType, HttpMethod method, TokenInfo token = null, int timeoutSecond = 0)
        {
            HttpResult result = new HttpResult();
            try
            {
                using (var client = this.GetClient(baseUri, token, timeoutSecond))
                {
                    HttpContent httpContent = null;
                    if (postData != null)
                    {
                        httpContent = new StringContent(postData, Encoding.UTF8);
                        httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(this.GetContentType(contentType)) { CharSet = "utf-8" };
                    }
                    HttpRequestMessage requestMessage = new HttpRequestMessage(method, relativeUri) { Content = httpContent };
                    var response = await client.SendAsync(requestMessage);
                    result.Status = response.StatusCode;
                    string strContent = await response.Content.ReadAsStringAsync();
                    result.IsSuccessStatusCode = response.IsSuccessStatusCode;
                    result.Content = strContent;
                    result.Message = response.ReasonPhrase;
                    if (!response.IsSuccessStatusCode)
                    {
                         if (!String.IsNullOrEmpty(strContent) && strContent.StartsWith("{"))
                        {
                            try
                            {
                                using (var stream = new MemoryStream(Encoding.Default.GetBytes(result.Content)))
                                {
                                    var serializer = new DataContractJsonSerializer(typeof(ResponseMsg));
                                    var msgObj = serializer.ReadObject(stream) as ResponseMsg;
                                    result.Message = msgObj.Message ?? msgObj.msg ?? msgObj.error;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                result.Status = HttpStatusCode.InternalServerError;
                var ex = e.InnerException ?? e;
                result.Message = ex.Message;
            }
            return result;
        }

        #region Constant

        public enum ContentType
        {
            json,
            form,
            xml,
            text
        }

        private string GetContentType(ContentType cType)
        {
            switch (cType)
            {
                case ContentType.json: return "application/json";
                case ContentType.form: return "application/x-www-form-urlencoded";
                case ContentType.xml: return "text/xml";
                case ContentType.text: return "text/plain";
                default: return "";
            }
        }

        #endregion

        #region Private Method

        private string BuildPostFormData(IDictionary<string, string> parameters)
        {
            StringBuilder builder = new StringBuilder();
            bool flag = false;
            IEnumerator<KeyValuePair<string, string>> enumerator = parameters.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string str = enumerator.Current.Key;
                string str2 = enumerator.Current.Value;
                if (!string.IsNullOrEmpty(str) && !string.IsNullOrEmpty(str2))
                {
                    if (flag)
                    {
                        builder.Append("&");
                    }
                    builder.Append(str);
                    builder.Append("=");
                    builder.Append(Uri.EscapeDataString(str2));
                    flag = true;
                }
            }
            return builder.ToString();
        }

        #endregion

        #region Internal Class

        [DataContract]
        class ResponseMsg
        {
            [DataMember]
            public string Message { get; set; }

            [DataMember]
            public string msg { get; set; }

            [DataMember]
            public string error { get; set; }
        }

        #endregion
    }

    public class TokenInfo
    {
        public string Token_Type { get; set; }
        public string Access_Token { get; set; }
    }

    public class HttpResult
    {
        public HttpStatusCode Status { get; set; }
        public bool IsSuccessStatusCode { get; set; }
        public string Content { get; set; }
        public string Message { get; set; }
    }
}
