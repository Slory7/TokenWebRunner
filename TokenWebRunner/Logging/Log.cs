using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using TokenWebRunner.Framework;

namespace TokenWebRunner.Logging
{
    public interface ILog
    {
        void LogDebug(string debugMessage);
        void LogError(Exception exception);
        void LogError(Exception exception, bool isNotify);
        void LogError(string message, Exception exception);
        void LogError(string message, Exception exception, bool isNotify);
        void LogInfo(string info);
        void LogInfo(string info, bool isNotify);
        void LogWarn(string warn);
    }

    public class Log : ServiceLocator<ILog, Log>, ILog
    {
        protected override Func<ILog> GetFactory()
        {
            return () => new Log();
        }
        static Log()
        {
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(AppDomain.CurrentDomain.BaseDirectory + "log4net.config"));
        }
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger("logerror");
        private static readonly log4net.ILog _Loggerinfo = log4net.LogManager.GetLogger("loginfo");
        private static log4net.ILog LoggerError
        {
            get { return _Logger; }
        }
        private static log4net.ILog LoggerInfo
        {
            get { return _Loggerinfo; }
        }
        public void LogError(Exception exception)
        {
            LogError("", exception, false);
        }
        public void LogError(string message, Exception exception)
        {
            LogError(message, exception, false);
        }
        public void LogError(Exception exception, bool isNotify)
        {
            LogError("", exception, isNotify);
        }
        public void LogError(string message, Exception exception, bool isNotify)
        {
            if (isNotify)
            {
                StringBuilder strInfo = new StringBuilder();
                string m_OpMail = "";
                if (System.Configuration.ConfigurationManager.AppSettings != null)
                {
                    if (System.Configuration.ConfigurationManager.AppSettings["operatorMail"] != null &&
                        System.Configuration.ConfigurationManager.AppSettings["operatorMail"].Length > 0)
                    {
                        m_OpMail = System.Configuration.ConfigurationManager.AppSettings["operatorMail"];
                    }
                }
                // Create StringBuilder to maintain publishing information.
                // Append the exception text
                strInfo.AppendFormat("{0}{0}Exception Custom Message{0}{1}", Environment.NewLine, message);
                strInfo.AppendFormat("{0}{0}Exception Information{0}{1}", Environment.NewLine, exception.ToString());

                // send notification email if operatorMail attribute was provided
                if (m_OpMail.Length > 0)
                {
                    string subject = "Exception Notification";
                    string body = strInfo.ToString();
                    string senderMail = System.Configuration.ConfigurationManager.AppSettings["senderMail"];
                    var objMail = new System.Net.Mail.SmtpClient();
                    objMail.SendAsync(senderMail, m_OpMail, subject, body, new Guid());
                }
            }
            // Write the entry to the log file.
            LoggerError.Error(message, exception);
        }
        public void LogInfo(string info)
        {
            LogInfo(info, false);
        }
        public void LogInfo(string info, bool isNotify)
        {
            if (isNotify)
            {
                StringBuilder strInfo = new StringBuilder();
                string m_OpMail = "";
                if (System.Configuration.ConfigurationManager.AppSettings != null)
                {
                    if (System.Configuration.ConfigurationManager.AppSettings["operatorMail"] != null &&
                        System.Configuration.ConfigurationManager.AppSettings["operatorMail"].Length > 0)
                    {
                        m_OpMail = System.Configuration.ConfigurationManager.AppSettings["operatorMail"];
                    }
                }
                // Create StringBuilder to maintain publishing information.
                // Append the exception text
                strInfo.AppendFormat("{0}{0}Information{0}{1}", Environment.NewLine, info);

                // send notification email if operatorMail attribute was provided
                if (m_OpMail.Length > 0)
                {
                    string subject = "Information Notification";
                    string body = strInfo.ToString();
                    string senderMail = System.Configuration.ConfigurationManager.AppSettings["senderMail"];
                    var objMail = new System.Net.Mail.SmtpClient();
                    objMail.SendAsync(senderMail, m_OpMail, subject, body, new Guid());
                }
            }
            // Write the entry to the log file.
            LoggerInfo.Info(info);
        }
        public void LogWarn(string warn)
        {
            LoggerInfo.Warn(warn);
        }
        public void LogDebug(string debugMessage)
        {
            LoggerInfo.Debug(debugMessage);
        }
    }
}
