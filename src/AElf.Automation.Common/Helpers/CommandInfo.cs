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
        public List<string> InfoMsg { get; set; }
        public List<string> ErrorMsg { get; set; }
        public long TimeSpan { get; set; }
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        public CommandInfo(string cmd, string category="")
        {
            Category = (category == "") ? cmd : category; 
            Cmd = cmd;
            Parameter = string.Empty;
            InfoMsg = new List<string>();
            ErrorMsg = new List<string>();
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
            
            InfoMsg = new List<string>();
            ErrorMsg = new List<string>();
            Result = false;
            TimeSpan = 0;
        }

        public CommandInfo(ApiMethods method, params object[] objects)
        {
            Category = method.ToString();
            Cmd = method.ToString();
            foreach (var parameter in objects)
            {
                Parameter += parameter.ToString() + " ";
            }
            Parameter = Parameter.Trim();
            
            InfoMsg = new List<string>();
            ErrorMsg = new List<string>();
            Result = false;
            TimeSpan = 0;
        }

        public void GetJsonInfo()
        {
            JsonInfo = JsonConvert.DeserializeObject<JObject>(Result ? InfoMsg[0] : ErrorMsg[0]);
        }

        public void PrintResultMessage()
        {
            if (Result)
            {
                _logger.WriteInfo("Request: {0}: ExecuteTime: {1}ms, Result: {2}", Category, TimeSpan, "Pass");
                foreach(var item in InfoMsg)
                    _logger.WriteInfo(item);
            }
            else
            {
                _logger.WriteError("Request: {0}: ExecuteTime: {1}ms, Result: {2}", Category, TimeSpan, "Failed");
                foreach(var item in ErrorMsg)
                    _logger.WriteError(item);
            }
        }

        public string GetErrorMessage()
        {
            if (ErrorMsg.Count > 0)
                return ErrorMsg[0];
            return string.Empty;
        }

        public bool CheckParameterValid(int count)
        {
            if (count == 1 && Parameter.Trim() == "")
                return false;
            
            var paraArray = Parameter.Split(" ");
            if (paraArray.Length != count)
            {
                ErrorMsg.Add("Parameter error.");
                _logger.WriteError("{0} command parameter is invalid.", Category);
                return false;
            }
            return true;
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
        public double AvageTimeInfo { get; set; }

        public List<CommandInfo> Commands { get; set; }

        public CategoryRequest()
        {
            Commands = new List<CommandInfo>();
            Count = 0;
            PassCount = 0;
            FailCount = 0;
            TotalTimeInfo = 0;
            AvageTimeInfo = 0;
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
                    category = new CategoryRequest();
                    category.Category = item.Category;
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
                    item.AvageTimeInfo = (double)item.TotalTimeInfo / (double)item.PassCount;
                else
                    item.AvageTimeInfo = 0;

                _logger.WriteInfo("Total: {0}, Pass: {1}, Fail: {2}, {3}",
                    item.Count, item.PassCount, item.FailCount, String.Format("AvageTime(milesecond): {0:F}", item.AvageTimeInfo));
            }
        }
        
        public string SaveTestResultXml(int threadCount, int transactionCount)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null));
            var el = xmlDoc.CreateElement("RpcApiResults");
            xmlDoc.AppendChild(el);

            XmlAttribute thread = xmlDoc.CreateAttribute("ThreadCount");
            thread.Value = threadCount.ToString();
            el.Attributes.Append(thread);
            XmlAttribute transactions = xmlDoc.CreateAttribute("TxCount");
            transactions.Value = transactionCount.ToString();
            el.Attributes.Append(transactions);

            foreach (var item in CategoryList)
            {
                XmlElement rpc = xmlDoc.CreateElement("RpcApi");

                XmlAttribute category = xmlDoc.CreateAttribute("Category");
                category.Value = item.Category;

                XmlAttribute totalTimes = xmlDoc.CreateAttribute("TotalTimes");
                totalTimes.Value = item.Count.ToString();

                XmlAttribute avageTime = xmlDoc.CreateAttribute("AvageTime");
                avageTime.Value = String.Format("{0:F}", item.AvageTimeInfo);

                rpc.Attributes.Append(category);
                rpc.Attributes.Append(totalTimes);
                rpc.Attributes.Append(avageTime);

                XmlElement totalTimeInfo = xmlDoc.CreateElement("TotalExeTime");
                totalTimeInfo.InnerText = item.TotalTimeInfo.ToString();

                XmlElement passCount = xmlDoc.CreateElement("PassCount");
                passCount.InnerText = item.PassCount.ToString();

                XmlElement failCount = xmlDoc.CreateElement("FailCount");
                failCount.InnerText = item.FailCount.ToString();

                rpc.AppendChild(totalTimeInfo);
                rpc.AppendChild(passCount);
                rpc.AppendChild(failCount);

                el.AppendChild(rpc);
            }

            string fileName = "RpcTh_" + threadCount+"_Tx_" + transactionCount + "_"+ DateTime.Now.ToString("MMddHHmmss") + ".xml";
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", fileName);
            xmlDoc.Save(fullPath);
            return fullPath;
        }
    }
}
