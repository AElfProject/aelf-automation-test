using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ServiceStack.Host;

namespace AElf.Automation.Common.Helpers
{
    public interface ILogHelper
    {
        void InitLogHelper(String logFilePath);

        void WriteInfo(String logText, params object[] arg);

        void WriteWarn(String logText, params object[] arg);

        void WriteError(String logText, params object[] arg);

        void Write(LogType logType, String logText, params object[] arg);

        void WriteException(Exception exception);

        void Dispose();
    }

    public enum LogType
    {
        INFO,
        WARNING,
        ERROR
    }

    public class LogHelper:ILogHelper, IDisposable
    {
        private static LogHelper logger;

        private static readonly Object initLogHelper = new Object();
        private static readonly Object writeLogHelper = new Object();
        private static readonly Object disposeLockHelper = new Object();

        //private FileStream fileStream;
        private String logFilePath;

        private StreamWriter streamWriter;

        private LogHelper()
        {
        }

        public void Dispose()
        {
            if (streamWriter != null)
            {
                lock (disposeLockHelper)
                {
                    if (streamWriter != null)
                    {
                        if (streamWriter.BaseStream.CanRead)
                        {
                            streamWriter.Dispose();
                        }
                        streamWriter = null;
                    }

                    logger = null;
                }
            }
        }

        public void InitLogHelper(String logFileSavePath)
        {
            if (String.IsNullOrEmpty(logFileSavePath))
            {
                throw new ArgumentNullException("Log file save path.");
            }
            try
            {
                var logDirPath = Path.GetDirectoryName(logFileSavePath);
                if (logDirPath == null)
                {
                    throw new ArgumentNullException(logFileSavePath);
                }
                if (!Directory.Exists(logDirPath))
                {
                    Directory.CreateDirectory(logDirPath);
                }
                logFilePath = logFileSavePath;
                if (!File.Exists(logFilePath))
                {
                    File.Create(logFilePath).Close();
                }
                //fileStream = new FileStream(logFilePath, FileMode.Append);
                streamWriter = new StreamWriter(logFilePath, true, Encoding.UTF8);
                WriteInfo("Initial log helper successful. Log path is: {0}", logFileSavePath);
            }
            catch (Exception exception)
            {
                throw new Exception("Create log helper fail.", exception);
            }
        }

        public void WriteInfo(String logText, params object[] arg)
        {
            Write(LogType.INFO, logText, arg);
        }

        public void WriteWarn(String logText, params object[] arg)
        {
            Write(LogType.WARNING, logText, arg);
        }

        public void WriteError(String logText, params object[] arg)
        {
            Write(LogType.ERROR, logText, arg);
        }

        public void Write(LogType logType, String logText, params object[] arg)
        {
            if (logText == string.Empty)
                return;

            string timeStamp = "yyyy-MM-dd HH:mm:ss";
            lock (writeLogHelper)
            {
                if (String.IsNullOrEmpty(logFilePath))
                {
                    throw new Exception("Please initial log helper first.");
                }
                try
                {
                    String text;
                    String content = arg?.Length>0 ? String.Format(logText, arg) : logText;
                    switch (logType)
                    {
                        case LogType.INFO:
                            text = "[" + DateTime.Now.ToString(timeStamp) + " - Info]: " + content;
                            break;

                        case LogType.WARNING:
                            text = "[" + DateTime.Now.ToString(timeStamp) + " - Warn]: " + content;
                            break;

                        case LogType.ERROR:
                            text = "[" + DateTime.Now.ToString(timeStamp) + " - Error]: " + content;
                            break;

                        default:
                            text = "Invalid LogType, log helper exception.\t" + DateTime.Now.ToString(timeStamp) + "\t" + content;
                            break;
                    }
                    streamWriter.WriteLine(text);
                    streamWriter.Flush();

                    Console.WriteLine(text);
                }
                catch (Exception exception)
                {
                    throw new Exception("Can write to log file.", exception);
                }
            }
        }

        public void WriteException(Exception exception)
        {
            string timeStamp = "yyyy-MM-dd HH:mm:ss";
            lock (writeLogHelper)
            {
                if (String.IsNullOrEmpty(logFilePath))
                {
                    throw new Exception("Please initial log helper first.");
                }
                try
                {
                    //显示调用关闭
                    streamWriter.WriteLine("[" + DateTime.Now.ToString(timeStamp) + " - Error]: " + exception.Message);
                    streamWriter.Flush();
                }
                catch (Exception ex)
                {
                    throw new Exception("Can write to log file.", ex);
                }
            }
        }

        public static ILogHelper GetLogHelper()
        {
            if (logger == null)
            {
                lock (initLogHelper)
                {
                    if (logger == null)
                    {
                        logger = new LogHelper();
                    }
                }
            }
            return logger;
        }

        ~LogHelper()
        {
            Dispose();
        }
    }
}