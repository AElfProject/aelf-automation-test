using System;

namespace AElfChain.SDK
{
    /// <summary>
    /// 控制台帮助类
    /// </summary>
    public static class PrintHelper
    {
        private static void WriteColorMessage(string str, ConsoleColor color)
        {
            var currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(str);
            Console.ForegroundColor = currentForeColor;
        }

        /// <summary>
        /// 打印错误信息
        /// </summary>
        /// <param name="str">待打印的字符串</param>
        /// <param name="color">想要打印的颜色</param>
        public static void WriteErrorMessage(this string str, ConsoleColor color = ConsoleColor.Red)
        {
            WriteColorMessage(str, color);
        }

        /// <summary>
        /// 打印警告信息
        /// </summary>
        /// <param name="str">待打印的字符串</param>
        /// <param name="color">想要打印的颜色</param>
        public static void WriteWarningMessage(this string str, ConsoleColor color = ConsoleColor.Yellow)
        {
            WriteColorMessage(str, color);
        }

        /// <summary>
        /// 打印正常信息
        /// </summary>
        /// <param name="str">待打印的字符串</param>
        /// <param name="color">想要打印的颜色</param>
        public static void InfoMessage(this string str, ConsoleColor color = ConsoleColor.White)
        {
            WriteColorMessage(str, color);
        }

        /// <summary>
        /// 打印成功的信息
        /// </summary>
        /// <param name="str">待打印的字符串</param>
        /// <param name="color">想要打印的颜色</param>
        public static void WriteSuccessMessage(this string str, ConsoleColor color = ConsoleColor.Green)
        {
            WriteColorMessage(str, color);
        }
    }
}