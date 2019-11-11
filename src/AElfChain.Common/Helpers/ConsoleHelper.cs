using System;

namespace AElf.Automation.Common.Helpers
{
    /// <summary>
    ///     控制台帮助类
    /// </summary>
    public static class ConsoleHelper
    {
        private static void WriteColorLine(string str, ConsoleColor color, bool changeLine = true)
        {
            var currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            if (changeLine)
                Console.WriteLine(str);
            else
                Console.Write(str);
            Console.ForegroundColor = currentForeColor;
        }

        /// <summary>
        ///     打印错误信息
        /// </summary>
        /// <param name="str">待打印的字符串</param>
        /// <param name="color">想要打印的颜色</param>
        /// <param name="changeLine"></param>
        public static void WriteErrorLine(this string str, ConsoleColor color = ConsoleColor.Red, bool changeLine = true)
        {
            WriteColorLine(str, color, changeLine);
        }

        /// <summary>
        ///     打印警告信息
        /// </summary>
        /// <param name="str">待打印的字符串</param>
        /// <param name="color">想要打印的颜色</param>
        /// <param name="changeLine"></param>
        public static void WriteWarningLine(this string str, ConsoleColor color = ConsoleColor.Yellow, bool changeLine = true)
        {
            WriteColorLine(str, color, changeLine);
        }

        /// <summary>
        ///     打印正常信息
        /// </summary>
        /// <param name="str">待打印的字符串</param>
        /// <param name="color">想要打印的颜色</param>
        /// <param name="changeLine"></param>
        public static void InfoLine(this string str, ConsoleColor color = ConsoleColor.White, bool changeLine = true)
        {
            WriteColorLine(str, color, changeLine);
        }

        /// <summary>
        ///     打印成功的信息
        /// </summary>
        /// <param name="str">待打印的字符串</param>
        /// <param name="color">想要打印的颜色</param>
        /// <param name="changeLine"></param>
        public static void WriteSuccessLine(this string str, ConsoleColor color = ConsoleColor.Green, bool changeLine = true)
        {
            WriteColorLine(str, color, changeLine);
        }
    }
}