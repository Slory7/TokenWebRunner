using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TokenWebRequster.Utilities;
using TokenWebRunner.Logging;
using TokenWebRunner.Utilities;

namespace TokenWebRunner.TaskCenter
{
    public class TaskProcessor
    {
        readonly string _configFile;
        readonly string _tokenFile;
        readonly string _taskDir;
        readonly string _taskName;
        public TaskProcessor(string taskName)
        {
            _taskName = taskName;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _taskDir = Path.Combine(baseDir, "Tasks", _taskName);
            _configFile = Path.Combine(_taskDir, "config.json");
            _tokenFile = Path.Combine(_taskDir, "token.json");
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
                if (result.IsSuccessStatusCode)
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

        public ResultInfo Run()
        {
            Log.Instance.LogInfo("[" + TaskConfig.ToString() + "] Start");
            var httpClient = HttpWebClient.Instance;
            var method = new HttpMethod(TaskConfig.RequestMethod);
            var contentType = (HttpWebClient.ContentType)Enum.Parse(typeof(HttpWebClient.ContentType), TaskConfig.RequestContentType, true);

            var token = GetToken();

            string strResult = string.Empty;
            bool isSuccess = false;
            string requestContent = TaskConfig.RequestBody;
            if (string.IsNullOrEmpty(requestContent) && !string.IsNullOrEmpty(TaskConfig.RequestSourceFile))
            {
                string strCsvFile = Path.Combine(_taskDir, TaskConfig.RequestSourceFile);
                if (contentType == HttpWebClient.ContentType.json)
                {
                    using (var fileStream = new StreamReader(strCsvFile))
                    {
                        int nSuccessCount = 0;
                        int nRowNumber = 0;
                        foreach (var jsonRow in CsvJsonConverter.ConvertToJson(fileStream))
                        {
                            nRowNumber++;
                            var httpResult = httpClient.SendAsync(TaskConfig.BaseUrl, TaskConfig.RequestUrl, jsonRow, contentType, method, token, TaskConfig.RequestTimeout);
                            var result = httpResult.Result;
                            string strLog = null;
                            if (result.IsSuccessStatusCode)
                            {
                                nSuccessCount++;
                                strLog = $"[{TaskConfig.ToString()}] Success:{result.Status}({(int)result.Status}), Row:{nRowNumber}, Result:{ result.Content}";
                            }
                            else
                            {
                                strLog = $"[{TaskConfig.ToString()}] Failed:{result.Status}({(int)result.Status}), Row:{nRowNumber}, Result:{ result.Message}";
                            }
                            Log.Instance.LogInfo(strLog);
                        }

                        strResult = $"[{TaskConfig.ToString()}] Result:Total:{nRowNumber}, Success:{nSuccessCount}, Failed:{nRowNumber - nSuccessCount}";
                        Log.Instance.LogInfo(strResult);

                        isSuccess = nRowNumber == nSuccessCount;
                    }
                }
                //TODO: other format
            }
            else
            {
                var httpResult = httpClient.SendAsync(TaskConfig.BaseUrl, TaskConfig.RequestUrl, TaskConfig.RequestBody, contentType, method, token, TaskConfig.RequestTimeout);
                var result = httpResult.Result;
                if (result.IsSuccessStatusCode)
                {
                    strResult = $"[{TaskConfig.ToString()}] Success:{result.Status}({(int)result.Status}), Result:{ result.Content}";
                    isSuccess = true;
                }
                else
                {
                    strResult = $"[{TaskConfig.ToString()}] Failed:{result.Status}({(int)result.Status}), Result:{ result.Message}";
                }
                Log.Instance.LogInfo(strResult);
            }
            return new ResultInfo()
            {
                IsSuccess = isSuccess,
                Message = strResult
            };
        }
    }
}
