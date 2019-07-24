using System;
using System.IO;
using System.Text;
using Shouldly;

namespace AElf.Automation.Common.Helpers
{
    public static class CommonHelper
    {
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        public static string GetDefaultDataDir()
        {
            try
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "aelf");
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetCurrentDataDir()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aelf");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var keyPath = Path.Combine(path, "keys");
                if (!Directory.Exists(keyPath))
                    Directory.CreateDirectory(keyPath);

                return path;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void CopyFiles(string originPath, string desPath)
        {
            if (!File.Exists(originPath))
            {
                Logger.WriteError($"File {originPath} not exist.");
                throw new FileNotFoundException();
            }

            if (!Directory.Exists(desPath))
            {
                Directory.CreateDirectory(desPath);
                if (!Directory.Exists(desPath))
                {
                    Logger.WriteError($"Directory {desPath} not exist.");
                    throw new DirectoryNotFoundException(); 
                }
            }
            
            File.Copy(originPath, desPath, true);
        }

        public static bool DeleteDirectoryFiles(string path)
        {
            if (!Directory.Exists(path))
            {
                Logger.WriteError($"Directory {path} not exist.");
                return false;
            }
            
            Directory.Delete(path, true);
            Directory.CreateDirectory(path);

            return true;
        }

        public static string RandomString(int size, bool lowerCase)
        {
            var random = new Random(DateTime.Now.Millisecond);
            var builder = new StringBuilder(size);
            var startChar = lowerCase ? 97 : 65; //65 = A / 97 = a
            for (var i = 0; i < size; i++)
                builder.Append((char) (26 * random.NextDouble() + startChar));
            return builder.ToString();
        }
    }
}