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
        public string From { get; set; }
        public string To { get; set; }
        public IMessage ParameterInput { get; set; }
        public string Parameter { get; set; }
        public ApiMethods Method { get; set; }
        public string ContractMethod { get; set; }

        public bool Result { get; set; }
        public JObject JsonInfo { get; set; }
        public object InfoMsg { get; set; }
        public object ErrorMsg { get; set; }

        private readonly ILog _logger = LogHelper.GetLogHelper();

        public CommandInfo()
        {
        }

        public CommandInfo(string cmd, string category = "")
        {
            Category = (category == "") ? cmd : category;
            Cmd = cmd;
            Parameter = string.Empty;
            Result = false;
        }

        public CommandInfo(ApiMethods method, string from, string to, string contractMethod)
        {
            Category = method.ToString();
            Cmd = method.ToString();
            Method = method;
            From = from;
            To = to;
            ContractMethod = contractMethod;
            Result = false;
        }

        public CommandInfo(ApiMethods method, params object[] objects)
        {
            Category = method.ToString();
            Cmd = method.ToString();
            Method = method;
            foreach (var parameter in objects)
            {
                Parameter += parameter + " ";
            }

            Parameter = Parameter?.Trim();
            Result = false;
        }

        public void GetJsonInfo()
        {
            JsonInfo = JsonConvert.DeserializeObject<JObject>(Result ? InfoMsg.ToString() : ErrorMsg.ToString());
        }

        public void PrintResultMessage()
        {
            if (Result)
            {
                _logger.Info("Request: {0}: Result: {1}", Category, "Pass");
                _logger.Info(JsonConvert.SerializeObject(InfoMsg));
            }
            else
            {
                _logger.Error("Request: {0}: Result: {1}", Category, "Failed");
                _logger.Error(JsonConvert.SerializeObject(ErrorMsg));
            }
        }

        public string GetErrorMessage()
        {
            return ErrorMsg != null ? ErrorMsg.ToString() : string.Empty;
        }

        public bool CheckParameterValid(int count)
        {
            if (count == 1 && Parameter.Trim() == "")
                return false;

            var paraArray = Parameter.Split(" ");
            if (paraArray.Length == count) return true;

            ErrorMsg = "Parameter error.";
            _logger.Error("{0} command parameter is invalid.", Category);
            return false;
        }
    }

    public class CategoryRequest
    {
        public string Category { get; set; }
        public int Count { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public List<CommandInfo> Commands { get; set; }

        public CategoryRequest()
        {
            Commands = new List<CommandInfo>();
            Count = 0;
            PassCount = 0;
            FailCount = 0;
        }
    }

    public class CategoryInfoSet
    {
        private List<CommandInfo> CommandList { get; set; }
        private List<CategoryRequest> CategoryList { get; set; }
        private readonly ILog _logger = LogHelper.GetLogHelper();

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
                _logger.Info("Command Category: {0}", item.Category);
                item.Count = item.Commands.Count;
                item.PassCount = item.Commands.FindAll(x => x.Result).Count;
                item.FailCount = item.Commands.FindAll(x => x.Result == false).Count;
            }
        }

        public string SaveTestResultXml(int threadCount, int transactionCount)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null));
            var el = xmlDoc.CreateElement("WebApiResults");
            xmlDoc.AppendChild(el);

            var thread = xmlDoc.CreateAttribute("ThreadCount");
            thread.Value = threadCount.ToString();
            el.Attributes.Append(thread);
            var transactions = xmlDoc.CreateAttribute("TxCount");
            transactions.Value = transactionCount.ToString();
            el.Attributes.Append(transactions);

            foreach (var item in CategoryList)
            {
                var rpc = xmlDoc.CreateElement("WebApi");

                var category = xmlDoc.CreateAttribute("Category");
                category.Value = item.Category;

                rpc.Attributes.Append(category);

                var passCount = xmlDoc.CreateElement("PassCount");
                passCount.InnerText = item.PassCount.ToString();

                var failCount = xmlDoc.CreateElement("FailCount");
                failCount.InnerText = item.FailCount.ToString();

                rpc.AppendChild(passCount);
                rpc.AppendChild(failCount);

                el.AppendChild(rpc);
            }

            var fileName = "WebTh_" + threadCount + "_Tx_" + transactionCount + "_" +
                           DateTime.Now.ToString("MMddHHmmss") + ".xml";
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", fileName);
            xmlDoc.Save(fullPath);
            return fullPath;
        }
    }
}