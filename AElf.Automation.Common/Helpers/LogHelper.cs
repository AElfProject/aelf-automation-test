using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace AElf.Automation.Common.Helpers
{
    public interface ILogHelper
    {
        void InitLogHelper(String logFilePath);

        void Write(String logText, LogType logType=LogType.INFO);

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
                streamWriter = new StreamWriter(logFilePath, true, Encoding.Unicode);
                Write("Initial log helper successful.", LogType.INFO);
            }
            catch (Exception exception)
            {
                throw new Exception("Create log helper fail.", exception);
            }
        }

        public void Write(String logText, LogType logType=LogType.INFO)
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
                    String text;
                    switch (logType)
                    {
                        case LogType.INFO:
                            text = "[" + DateTime.Now.ToString(timeStamp) + " - Info]: " + logText;
                            break;

                        case LogType.WARNING:
                            text = "[" + DateTime.Now.ToString(timeStamp) + " - Warn]: " + logText;
                            break;

                        case LogType.ERROR:
                            text = "[" + DateTime.Now.ToString(timeStamp) + " - Error]: " + logText;
                            break;

                        default:
                            text = "Invalid LogType, log helper exception.\t" + DateTime.Now.ToString(timeStamp) + "\t" + logText;
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