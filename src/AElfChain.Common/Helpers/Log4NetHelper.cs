using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using log4net;
using log4net.Config;
using Newtonsoft.Json;

namespace AElfChain.Common.Helpers
{
    public enum Format
    {
        None,
        Json
    }

    public static class Log4NetHelper
    {
        public static int LogInit()
        {
            return LogInit(CommonHelper.MapPath("config/log4net.config"), "");
        }

        public static int LogInit(string fileName)
        {
            return LogInit(CommonHelper.MapPath("config/log4net.config"), fileName);
        }

        /// <summary>
        ///     log4net init
        /// </summary>
        /// <param name="configFilePath">log4net config file path</param>
        /// <param name="fileName"></param>
        /// <returns>1 config success,0 config has existed</returns>
        public static int LogInit(string configFilePath, string fileName)
        {
            if (null != LogManager.GetAllRepositories()
                    ?.FirstOrDefault(_ => _.Name == CommonHelper.ApplicationName)) return 0;
            GlobalContext.Properties["LogName"] = $"{fileName}_";
            XmlConfigurator.Configure(LogManager.CreateRepository(CommonHelper.ApplicationName),
                new FileInfo(configFilePath));
            return 1;
        }

        /// <summary>
        ///     Get a log4net logger
        /// </summary>
        public static ILog GetLogger<TCategory>()
        {
            return LogManager.GetLogger(CommonHelper.ApplicationName, typeof(TCategory));
        }

        /// <summary>
        ///     Get a log4net logger
        /// </summary>
        public static ILog GetLogger(Type type)
        {
            return LogManager.GetLogger(CommonHelper.ApplicationName, type);
        }

        /// <summary>
        ///     Get a log4net logger
        /// </summary>
        public static ILog GetLogger()
        {
            var trace = new StackTrace();
            var frame = trace.GetFrame(1);
            var method = frame.GetMethod();
            var classType = method.ReflectedType;

            return LogManager.GetLogger(CommonHelper.ApplicationName, classType);
        }

        /// <summary>
        ///     Get a log4net logger
        /// </summary>
        public static ILog GetLogger(string loggerName)
        {
            return LogManager.GetLogger(CommonHelper.ApplicationName, loggerName);
        }

        /// <summary>
        ///     Info extension method
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="format"></param>
        /// <param name="parameters"></param>
        public static void Info(this ILog logger, string format, params object[] parameters)
        {
            CommonHelper.ConsoleChangeLine();
            var message = string.Format(format, parameters);
            logger.Info(message);
        }

        public static void Info(this ILog logger, string message, bool checkCursorLeft)
        {
            if (checkCursorLeft && Console.CursorLeft != 0)
                Console.WriteLine();

            logger.Info(message);
        }

        public static void Info(this ILog logger, string message, Format format)
        {
            if (format == Format.Json)
            {
                var info = ConvertJsonString(message);
                logger.Info(info);
                return;
            }

            CommonHelper.ConsoleChangeLine();
            logger.Info(message);
        }

        /// <summary>
        ///     Warn extension method
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="format"></param>
        /// <param name="parameters"></param>
        public static void Warn(this ILog logger, string format, params object[] parameters)
        {
            CommonHelper.ConsoleChangeLine();
            var message = string.Format(format, parameters);
            logger.Warn(message);
        }

        public static void Warn(this ILog logger, string message, bool checkCursorLeft)
        {
            if (checkCursorLeft && Console.CursorLeft != 0)
                Console.Write("\r\n");

            logger.Warn(message);
        }

        /// <summary>
        ///     Error extension method
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="format"></param>
        /// <param name="parameters"></param>
        public static void Error(this ILog logger, string format, params object[] parameters)
        {
            CommonHelper.ConsoleChangeLine();
            var message = string.Format(format, parameters);
            logger.Error(message);
        }

        public static void Error(this ILog logger, string message, bool checkCursorLeft)
        {
            if (checkCursorLeft && Console.CursorLeft != 0)
                Console.WriteLine();

            logger.Error(message);
        }

        public static string ConvertJsonString(string str)
        {
            //format json string info
            var serializer = new JsonSerializer();
            var tr = new StringReader(str);
            var jtr = new JsonTextReader(tr);
            var obj = serializer.Deserialize(jtr);
            if (obj != null)
            {
                var textWriter = new StringWriter();
                var jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };
                serializer.Serialize(jsonWriter, obj);
                return textWriter.ToString();
            }

            return str;
        }
    }
}