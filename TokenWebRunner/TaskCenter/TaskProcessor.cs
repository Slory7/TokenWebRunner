using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TokenWebRunner.Logging;
using TokenWebRunner.Utilities;

namespace TokenWebRunner.TaskCenter
{
    public class TaskProcessor
    {
        readonly string _configFile;
        readonly string _tokenFile;
        readonly string _taskName;
        public TaskProcessor(string taskName)
        {
            _taskName = taskName;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _configFile = Path.Combine(baseDir, "Tasks", _taskName, "config.json");
            _tokenFile = Path.Combine(baseDir, "Tasks", _taskName, "token.json");
        }

        public string TaskName { get { return _taskName; } }

        TaskInfo _taskConfig = null;
        public TaskInfo TaskConfig
        {
            get
            {
                if (_taskConfig == null)
                {
                    string strFileContent = File.ReadAllText(_configFile);
                    _taskConfig = JsonConvert.DeserializeObject<TaskInfo>(strFileContent);
                    _taskConfig.TaskName = _taskName;
                }
                return _taskConfig;
            }
        }

        TokenInfoExt GetToken()
        {
            if (File.Exists(_tokenFile))
            {
                string strFileContent = File.ReadAllText(_tokenFile);
                var token = JsonConvert.DeserializeObject<TokenInfoExt>(strFileContent);
                if ((DateTime.Now - File.GetLastWriteTime(_tokenFile)).TotalSeconds + 5 * 60 <= token.Expires_In)
                {
                    return token;
                }
            }
            if (!String.IsNullOrEmpty(TaskConfig.TokenUrl))
            {
                var httpClient = HttpWebClient.Instance;
                var httpResult = httpClient.PostAsync(TaskConfig.BaseUrl, TaskConfig.TokenUrl, TaskConfig.TokenParams, HttpWebClient.ContentType.form);
                var result = httpResult.Result;
                if (result.Status < System.Net.HttpStatusCode.BadRequest)
                {
                    var token = JsonConvert.DeserializeObject<TokenInfoExt>(result.Content);
                    var strToken = JsonConvert.SerializeObject(token);
                    File.WriteAllText(_tokenFile, strToken);
                    return token;
                }
                else
                {
                    Log.Instance.LogError("[" + TaskConfig.ToString() + "] Get Token Error", new Exception(result.Status.ToString() + ":" + result.Message));
                }
            }
            return null;
        }

        public string Run()
        {
            var httpClient = HttpWebClient.Instance;
            var method = new HttpMethod(TaskConfig.RequestMethod);
            var contentType = (HttpWebClient.ContentType)Enum.Parse(typeof(HttpWebClient.ContentType), TaskConfig.RequestContentType, true);
            var token = GetToken();
            string requestContent = TaskConfig.RequestBody;            
            var httpResult = httpClient.SendAsync(TaskConfig.BaseUrl, TaskConfig.RequestUrl, TaskConfig.RequestBody, contentType, method, token, TaskConfig.RequestTimeout);
            var result = httpResult.Result;
            string strResult = string.Empty;
            if (result.Status < System.Net.HttpStatusCode.BadRequest)
            {
                strResult = "[" + TaskConfig.ToString() + "] Success:" + result.Status.ToString() + ":" + result.Content;
                Log.Instance.LogInfo(strResult);
            }
            else
            {
                strResult = "[" + TaskConfig.ToString() + "] Failed:" + result.Status.ToString() + ":" + result.Message;
                Log.Instance.LogError("[" + TaskConfig.ToString() + "] Failed", new Exception(result.Status.ToString() + ":" + result.Message));
            }
            return strResult;
        }
    }
}
