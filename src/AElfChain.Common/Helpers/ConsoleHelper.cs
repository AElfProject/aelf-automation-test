using System;

namespace AElfChain.Common.Helpers
{
    /// <summary>
    ///     console helper class
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
        ///     print error message
        /// </summary>
        /// <param name="str">print message</param>
        /// <param name="color">print color</param>
        /// <param name="changeLine"></param>
        public static void WriteErrorLine(this string str, ConsoleColor color = ConsoleColor.Red,
            bool changeLine = true)
        {
            WriteColorLine(str, color, changeLine);
        }

        /// <summary>
        ///     print warning message
        /// </summary>
        /// <param name="str">print message</param>
        /// <param name="color">print color</param>
        /// <param name="changeLine"></param>
        public static void WriteWarningLine(this string str, ConsoleColor color = ConsoleColor.Yellow,
            bool changeLine = true)
        {
            WriteColorLine(str, color, changeLine);
        }

        /// <summary>
        ///     print information message
        /// </summary>
        /// <param name="str">print message</param>
        /// <param name="color">print color</param>
        /// <param name="changeLine"></param>
        public static void InfoLine(this string str, ConsoleColor color = ConsoleColor.White, bool changeLine = true)
        {
            WriteColorLine(str, color, changeLine);
        }

        /// <summary>
        ///     print success message
        /// </summary>
        /// <param name="str">print message</param>
        /// <param name="color">print color</param>
        /// <param name="changeLine"></param>
        public static void WriteSuccessLine(this string str, ConsoleColor color = ConsoleColor.Green,
            bool changeLine = true)
        {
            WriteColorLine(str, color, changeLine);
        }
    }
}