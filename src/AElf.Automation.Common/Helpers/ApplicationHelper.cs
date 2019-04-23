using System;
using System.IO;

namespace AElf.Automation.Common.Helpers
{
    public static class ApplicationHelper
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