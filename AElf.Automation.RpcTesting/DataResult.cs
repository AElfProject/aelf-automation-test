using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Automation.RpcTesting
{
    public class DataResult
    {
        public string Key { get; set; }
        public string TypeInfo { get; }
        public string ValueInfo { get; }
        public DataResult(string key, string type, string value)
        {
            Key = key;
            TypeInfo = type;
            ValueInfo = value;
        }
    }

    public class TypeSummarySet
    {
        public List<TypeSummary> SummaryInfo { get; }
        public List<DataResult> DataCollection { get; }

        public TypeSummarySet()
        {
            SummaryInfo = new List<TypeSummary>();
            DataCollection = new List<DataResult>();
        }

        public void AddDataResult(DataResult data)
        {
            DataCollection.Add(data);
        }

        public void AddTypeSummary(DataResult result)
        {
            var typeSummay = SummaryInfo.FindLast(x => x.TypeName == result.TypeInfo);
            if (typeSummay != null)
            {
                typeSummay.Count++;
            }
            else
            {
                SummaryInfo.Add(new TypeSummary(result.TypeInfo));
            }
        }
    }

    public class TypeSummary
    {
        public string TypeName { get; }
        public int Count { get; set; }

        public TypeSummary(string name)
        {
            TypeName = name;
            Count = 1;
        }
    }
}
