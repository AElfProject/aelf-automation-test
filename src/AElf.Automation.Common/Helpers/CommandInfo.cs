using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Helpers
{
    public class CommandInfo
    {
        public string Category { get; set; }
        public string Cmd { get; set; }
        public string Parameter { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string ContractMethod { get; set; }
        
        public IMessage ParameterInput { get; set; }
        public ApiMethods Method { get; set; }

        public bool Result { get; set; }
        public JObject JsonInfo { get; set; }
        public List<object> InfoMsg { get; set; }
        public List<object> ErrorMsg { get; set; }
        public long TimeSpan { get; set; }
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        public CommandInfo(string cmd, string category="")
        {
            Category = (category == "") ? cmd : category; 
            Cmd = cmd;
            Parameter = string.Empty;
            InfoMsg = new List<object>();
            ErrorMsg = new List<object>();
            Result = false;
            TimeSpan = 0;
        }

        public CommandInfo(ApiMethods method, string from, string to, string contractMethod)
        {
            Category = method.ToString();
            Cmd = method.ToString();
            From = from;
            To = to;
            ContractMethod = contractMethod;
            
            InfoMsg = new List<object>();
            ErrorMsg = new List<object>();
            Result = false;
            TimeSpan = 0;
        }

        public CommandInfo(ApiMethods method, params object[] objects)
        {
            Category = method.ToString();
            Cmd = method.ToString();
            foreach (var parameter in objects)
            {
                Parameter += parameter + " ";
            }
            Parameter = Parameter?.Trim();

            InfoMsg = new List<object>();
            ErrorMsg = new List<object>();
            Result = false;
            TimeSpan = 0;
        }

        public CommandInfo Execute(IApiHelper apiHelper, bool convertJson = false)
        {
            var ci = apiHelper.ExecuteCommand(this);
            
            if(convertJson)
                ci.GetJsonInfo();

            return ci;
        }

        public void GetJsonInfo()
        {
            JsonInfo = JsonConvert.DeserializeObject<JObject>(Result ? InfoMsg[0].ToString() : ErrorMsg[0].ToString());
        }

        public void PrintResultMessage()
        {
            if (Result)
            {
                _logger.WriteInfo("Request: {0}: ExecuteTime: {1}ms, Result: {2}", Category, TimeSpan, "Pass");
                foreach(var item in InfoMsg)
                    _logger.WriteInfo(item.ToString());
            }
            else
            {
                _logger.WriteError("Request: {0}: ExecuteTime: {1}ms, Result: {2}", Category, TimeSpan, "Failed");
                foreach(var item in ErrorMsg)
                    _logger.WriteError(item.ToString());
            }
        }

        public string GetErrorMessage()
        {
            return ErrorMsg.Count > 0 ? ErrorMsg[0].ToString() : string.Empty;
        }

        public bool CheckParameterValid(int count)
        {
            if (count == 1 && Parameter.Trim() == "")
                return false;
            
            var paraArray = Parameter.Split(" ");
            if (paraArray.Length == count) return true;
            
            ErrorMsg.Add("Parameter error.");
            _logger.WriteError("{0} command parameter is invalid.", Category);
            return false;
        }

        public bool CheckParameterValid(string[] parameterArray, int count)
        {
            bool result;
            if (parameterArray == null || parameterArray.Length == 0)
                result = false;
            else if (parameterArray.Length != count)
                result = false;
            else
                result = true;

            if (result) return true;
            ErrorMsg.Add("Parameter error.");
            _logger.WriteError($"{Method.ToString()} command parameter is invalid.");

            return false;
        }
    }
    
    public class CategoryRequest
    {
        public string Category { get; set; }
        public int Count { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public long TotalTimeInfo { get; set; }
        public double AverageTimeInfo { get; set; }

        public List<CommandInfo> Commands { get; set; }

        public CategoryRequest()
        {
            Commands = new List<CommandInfo>();
            Count = 0;
            PassCount = 0;
            FailCount = 0;
            TotalTimeInfo = 0;
            AverageTimeInfo = 0;
        }
     }

    public class CategoryInfoSet
    {
        private List<CommandInfo> CommandList { get; set; }
        private List<CategoryRequest> CategoryList { get; set; }
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        public CategoryInfoSet(List<CommandInfo> commands)
        {
            CommandList = commands;
            CategoryList = new List<CategoryRequest>();
        }

        public void GetCategoryBasicInfo()
        {
            foreach (var item in CommandList)
            {
                var category = CategoryList.FindLast(x => x.Category == item.Category);
                if (category == null)
                {
                    category = new CategoryRequest {Category = item.Category};
                    CategoryList.Add(category);
                }

                category.Commands.Add(item);
            }
        }

        public void GetCategorySummaryInfo()
        {
            foreach (var item in CategoryList)
            {
                _logger.WriteInfo("Rpc Category: {0}", item.Category);
                item.Count = item.Commands.Count;
                item.PassCount = item.Commands.FindAll(x => x.Result).Count;
                item.FailCount = item.Commands.FindAll(x => x.Result == false).Count;
                foreach (var command in item.Commands.FindAll(x => x.Result))
                {
                    item.TotalTimeInfo += command.TimeSpan;
                }

                if (item.PassCount != 0)
                    item.AverageTimeInfo = (double)item.TotalTimeInfo / (double)item.PassCount;
                else
                    item.AverageTimeInfo = 0;

                _logger.WriteInfo("Total Fail: {0}, {1}",
                    item.FailCount, $"AverageTime(milliseconds): {item.AverageTimeInfo:F}");
            }
        }
        
        public string SaveTestResultXml(int threadCount, int transactionCount)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null));
            var el = xmlDoc.CreateElement("RpcApiResults");
            xmlDoc.AppendChild(el);

            var thread = xmlDoc.CreateAttribute("ThreadCount");
            thread.Value = threadCount.ToString();
            el.Attributes.Append(thread);
            var transactions = xmlDoc.CreateAttribute("TxCount");
            transactions.Value = transactionCount.ToString();
            el.Attributes.Append(transactions);

            foreach (var item in CategoryList)
            {
                var rpc = xmlDoc.CreateElement("RpcApi");

                var category = xmlDoc.CreateAttribute("Category");
                category.Value = item.Category;

                var totalTimes = xmlDoc.CreateAttribute("TotalTimes");
                totalTimes.Value = item.Count.ToString();

                var averageTime = xmlDoc.CreateAttribute("AverageTime");
                averageTime.Value = $"{item.AverageTimeInfo:F}";

                rpc.Attributes.Append(category);
                rpc.Attributes.Append(totalTimes);
                rpc.Attributes.Append(averageTime);

                var totalTimeInfo = xmlDoc.CreateElement("TotalExeTime");
                totalTimeInfo.InnerText = item.TotalTimeInfo.ToString();

                var passCount = xmlDoc.CreateElement("PassCount");
                passCount.InnerText = item.PassCount.ToString();

                var failCount = xmlDoc.CreateElement("FailCount");
                failCount.InnerText = item.FailCount.ToString();

                rpc.AppendChild(totalTimeInfo);
                rpc.AppendChild(passCount);
                rpc.AppendChild(failCount);

                el.AppendChild(rpc);
            }

            var fileName = "RpcTh_" + threadCount+"_Tx_" + transactionCount + "_"+ DateTime.Now.ToString("MMddHHmmss") + ".xml";
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", fileName);
            xmlDoc.Save(fullPath);
            return fullPath;
        }
    }
}
