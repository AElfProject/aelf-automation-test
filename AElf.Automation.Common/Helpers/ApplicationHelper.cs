using System;
using System.IO;

namespace AElf.Automation.Common.Helpers
{
    public class ApplicationHelper
    {
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
    }
}