using System;
using System.IO;
using System.Text;

namespace AElfChain.Common.Helpers
{
    public interface ILogHelper
    {
        void InitLogHelper(string logFilePath = "");

        void Info(string logText, params object[] arg);

        void Warn(string logText, params object[] arg);

        void Error(string logText, params object[] arg);

        void Write(LogType logType, string logText, params object[] arg);

        void WriteException(Exception exception);

        void Dispose();
    }

    public enum LogType
    {
        Info,
        Warning,
        Error
    }

    public class LogHelper : ILogHelper, IDisposable
    {
        private static LogHelper _logger;
        private static readonly object InitObject = new object();
        private static readonly object WriteObject = new object();
        private static readonly object DisposeObject = new object();

        //private FileStream fileStream;
        private string _logFilePath;

        private StreamWriter _streamWriter;

        private LogHelper()
        {
        }

        public void Dispose()
        {
            if (_streamWriter == null) return;
            lock (DisposeObject)
            {
                if (_streamWriter != null)
                {
                    if (_streamWriter.BaseStream.CanRead) _streamWriter.Dispose();

                    _streamWriter = null;
                }

                _logger = null;
            }
        }

        public void InitLogHelper(string logFileSavePath = "")
        {
            if (string.IsNullOrEmpty(logFileSavePath))
                logFileSavePath = CommonHelper.MapPath($"/logs/{DateTime.Now:yyyy-M-d dddd}.log");

            try
            {
                var logDirPath = Path.GetDirectoryName(logFileSavePath);
                if (logDirPath == null) throw new ArgumentNullException(logFileSavePath);

                if (!Directory.Exists(logDirPath)) Directory.CreateDirectory(logDirPath);

                lock (InitObject)
                {
                    _logFilePath = logFileSavePath;
                    if (!File.Exists(_logFilePath)) File.Create(_logFilePath).Close();
                }

                //fileStream = new FileStream(logFilePath, FileMode.Append);
                lock (WriteObject)
                {
                    _streamWriter = new StreamWriter(_logFilePath, true, Encoding.UTF8);
                }

                Info("Initial log manager successful. Log path is: {0}", logFileSavePath);
            }
            catch (Exception exception)
            {
                throw new Exception("Create log manager fail.", exception);
            }
        }

        public void Info(string logText, params object[] arg)
        {
            Write(LogType.Info, logText, arg);
        }

        public void Warn(string logText, params object[] arg)
        {
            Write(LogType.Warning, logText, arg);
        }

        public void Error(string logText, params object[] arg)
        {
            Write(LogType.Error, logText, arg);
        }

        public void Write(LogType logType, string logText, params object[] arg)
        {
            if (logText == string.Empty)
                return;

            const string timeStamp = "yyyy-MM-dd HH:mm:ss.fff";
            lock (WriteObject)
            {
                if (string.IsNullOrEmpty(_logFilePath)) throw new Exception("Please initial log manager first.");

                try
                {
                    string text;
                    var content = arg?.Length > 0 ? string.Format(logText, arg) : logText;
                    switch (logType)
                    {
                        case LogType.Info:
                            text = "[" + DateTime.Now.ToString(timeStamp) + " - Info]: " + content;
                            text.WriteSuccessLine();
                            break;

                        case LogType.Warning:
                            text = "[" + DateTime.Now.ToString(timeStamp) + " - Warn]: " + content;
                            text.WriteWarningLine();
                            break;

                        case LogType.Error:
                            text = "[" + DateTime.Now.ToString(timeStamp) + " - Error]: " + content;
                            text.WriteErrorLine();
                            break;

                        default:
                            text = "Invalid LogType, log manager exception.\t" + DateTime.Now.ToString(timeStamp) +
                                   "\t" + content;
                            break;
                    }

                    _streamWriter.WriteLine(text);
                    _streamWriter.Flush();
                }
                catch (Exception exception)
                {
                    throw new Exception("Can write to log file.", exception);
                }
            }
        }

        public void WriteException(Exception exception)
        {
            const string timeStamp = "yyyy-MM-dd HH:mm:ss.fff";
            lock (WriteObject)
            {
                if (string.IsNullOrEmpty(_logFilePath)) throw new Exception("Please initial log manager first.");

                try
                {
                    //show tag and time info
                    _streamWriter.WriteLine("[" + DateTime.Now.ToString(timeStamp) + " - Error]: " + exception.Message);
                    _streamWriter.Flush();
                }
                catch (Exception ex)
                {
                    throw new Exception("Can write to log file.", ex);
                }
            }
        }

        public static ILogHelper GetLogger()
        {
            if (_logger != null) return _logger;
            lock (InitObject)
            {
                if (_logger == null) _logger = new LogHelper();
            }

            return _logger;
        }

        ~LogHelper()
        {
            Dispose();
        }
    }
}