using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using log4net;
using log4net.Config;

namespace AElf.Automation.Common.Helpers
{
    public static class Log4NetHelper
    {
        public static int LogInit() => LogInit(configFilePath: CommonHelper.MapPath("log4net.config"));

        public static int LogInit(string fileName) => LogInit(CommonHelper.MapPath("log4net.config"), fileName);

        /// <summary>
        /// log4net init
        /// </summary>
        /// <param name="configFilePath">log4net config file path</param>
        /// <param name="fileName"></param>
        /// <returns>1 config success,0 config has existed</returns>
        public static int LogInit(string configFilePath, string fileName = "")
        {
            if (null != LogManager.GetAllRepositories()
                    ?.FirstOrDefault(_ => _.Name == CommonHelper.ApplicationName)) return 0;
            GlobalContext.Properties["LogName"] = fileName;
            XmlConfigurator.Configure(LogManager.CreateRepository(CommonHelper.ApplicationName),
                new FileInfo(configFilePath));
            return 1;
        }

        /// <summary>
        /// Get a log4net logger
        /// </summary>
        public static ILog GetLogger<TCategory>()
        {
            return LogManager.GetLogger(CommonHelper.ApplicationName, typeof(TCategory));
        }

        /// <summary>
        /// Get a log4net logger
        /// </summary>
        public static ILog GetLogger(Type type)
        {
            return LogManager.GetLogger(CommonHelper.ApplicationName, type);
        }

        /// <summary>
        /// Get a log4net logger
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
        /// Get a log4net logger
        /// </summary>
        public static ILog GetLogger(string loggerName)
        {
            return LogManager.GetLogger(CommonHelper.ApplicationName, loggerName);
        }

        /// <summary>
        /// Info extension method
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="format"></param>
        /// <param name="parameters"></param>
        public static void Info(this ILog logger, string format, params object[] parameters)
        {
            var message = string.Format(format, parameters);
            logger.Info(message);
        }

        /// <summary>
        /// Warn extension method
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="format"></param>
        /// <param name="parameters"></param>
        public static void Warn(this ILog logger, string format, params object[] parameters)
        {
            var message = string.Format(format, parameters);
            logger.Warn(message);
        }

        /// <summary>
        /// Error extension method
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="format"></param>
        /// <param name="parameters"></param>
        public static void Error(this ILog logger, string format, params object[] parameters)
        {
            var message = string.Format(format, parameters);
            logger.Error(message);
        }
    }
}