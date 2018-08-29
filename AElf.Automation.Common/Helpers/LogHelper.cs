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
            if (this.streamWriter != null)
            {
                lock (disposeLockHelper)
                {
                    if (this.streamWriter != null)
                    {
                        if (this.streamWriter.BaseStream.CanRead)
                        {
                            this.streamWriter.Dispose();
                        }
                        this.streamWriter = null;
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
                this.logFilePath = logFileSavePath;
                if (!File.Exists(this.logFilePath))
                {
                    File.Create(this.logFilePath).Close();
                }
                //this.fileStream = new FileStream(this.logFilePath, FileMode.Append);
                this.streamWriter = new StreamWriter(this.logFilePath, true, Encoding.Unicode);
                this.Write("Initial log helper successful.", LogType.INFO);
            }
            catch (Exception exception)
            {
                throw new Exception("Create log helper fail.", exception);
            }
        }

        public void Write(String logText, LogType logType=LogType.INFO)
        {
            lock (writeLogHelper)
            {
                if (String.IsNullOrEmpty(this.logFilePath))
                {
                    throw new Exception("Please initial log helper first.");
                }
                try
                {
                    String text;
                    switch (logType)
                    {
                        case LogType.INFO:
                            text = "Info\t" + DateTime.UtcNow + "\t" + logText;
                            break;

                        case LogType.WARNING:
                            text = "Warn\t" + DateTime.UtcNow + "\t" + logText;
                            break;

                        case LogType.ERROR:
                            text = "Error\t" + DateTime.UtcNow + "\t" + logText;
                            break;

                        default:
                            text = "Invalid LogType, log helper exception.\t" + DateTime.Now + "\t" + logText;
                            break;
                    }
                    this.streamWriter.WriteLine(text);
                    this.streamWriter.Flush();
                }
                catch (Exception exception)
                {
                    throw new Exception("Can write to log file.", exception);
                }
            }
        }

        public void WriteException(Exception exception)
        {
            lock (writeLogHelper)
            {
                if (String.IsNullOrEmpty(this.logFilePath))
                {
                    throw new Exception("Please initial log helper first.");
                }
                try
                {
                    //显示调用关闭
                    this.streamWriter.WriteLine("ERROR\t" + DateTime.UtcNow + "\t" + exception);
                    this.streamWriter.Flush();
                    //使用语法糖
                    //                    using (this.streamWriter = new StreamWriter(this.logFilePath, true, Encoding.Unicode))
                    //                    {
                    //                        this.streamWriter.WriteLine("ERROR\t" + DateTime.UtcNow + "\t" + exception);
                    //                        this.streamWriter.Flush();
                    //                    }
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
            this.Dispose();
        }
    }
}