using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElfChain.Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Automation.ContractsTesting
{
    public class TransactionFeeProvider
    {
        public TransactionFeeProvider()
        {
            CpuService = new CpuCalculateCostStrategy();
            StoService = new StoCalculateCostStrategy();
            NetService = new NetCalculateCostStrategy();
            RamService = new RamCalculateCostStrategy();
            TxService = new TxCalculateCostStrategy();
        }

        private static ICalculateCostStrategy CpuService { get; set; }
        private static ICalculateCostStrategy StoService { get; set; }
        private static ICalculateCostStrategy NetService { get; set; }
        private static ICalculateCostStrategy RamService { get; set; }
        private static ICalculateCostStrategy TxService { get; set; }

        public static long CpuSizeFee(int size)
        {
            return CpuService.GetCost(size);
        }

        public static long NetSizeFee(int size)
        {
            return NetService.GetCost(size);
        }

        public static long StoSizeFee(int size)
        {
            return StoService.GetCost(size);
        }

        public static long TransactionSizeFee(int size)
        {
            return TxService.GetCost(size);
        }

        public void CalculateTxFee()
        {
            //tx,net size
            var sizeList = new List<int>
            {
                100, 200, 300, 500, 600, 800, 900, 990,
                1000, 1100, 1200, 1500, 2000, 5000, 100_000, 200_000, 500_000, 800_000, 900_000, 990_000, 999_000,
                1000_000, 1000_001, 1100_000, 1200_000
            };
            CalculateFee(sizeList, TransactionSizeFee, "tx_size");
            CalculateFee(sizeList, NetSizeFee, "net_size");

            //cpu,sto read size
            var readList = new List<int>
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9,
                10, 11, 15, 20, 30, 40, 50, 60, 70, 80, 90, 95, 99,
                100, 101, 110, 120, 150
            };
            CalculateFee(readList, CpuSizeFee, "cpu_size");
            CalculateFee(readList, StoSizeFee, "sto_size");
        }

        public void CalculateFee(List<int> sizes, Func<int, long> func, string fileName)
        {
            var filePath = CommonHelper.MapPath($"logs/{fileName}.csv");
            using var writer = File.CreateText(filePath);
            foreach (var item in sizes)
            {
                var fee = func.Invoke(item);
                writer.WriteLine($"{item},{fee}");
            }

            writer.Flush();
            Console.WriteLine($"Calculate complete to: {filePath}");
        }
    }

    public interface ITransactionSizeFeeUnitPriceProvider
    {
        void SetUnitPrice(long unitPrice);
        Task<long> GetUnitPriceAsync();
    }

    public enum FeeType
    {
        Tx = 0,
        Cpu,
        Sto,
        Ram,
        Net
    }

    public interface ICalculateFeeService : ISingletonDependency
    {
        long CalculateFee(FeeType feeType, int cost);

        void UpdateFeeCal(FeeType feeType, int pieceKey, CalculateFunctionType funcTyoe,
            Dictionary<string, string> para);

        void DeleteFeeCal(FeeType feeType, int pieceKey);
        void AddFeeCal(FeeType feeType, int pieceKey, CalculateFunctionType funcTyoe, Dictionary<string, string> param);
    }

    internal class CalculateFeeService : ICalculateFeeService
    {
        private readonly ICalculateStradegyProvider _calculateStradegyProvider;

        public CalculateFeeService(ICalculateStradegyProvider calculateStradegyProvider)
        {
            _calculateStradegyProvider = calculateStradegyProvider;
        }

        public long CalculateFee(FeeType feeType, int cost)
        {
            return _calculateStradegyProvider.GetCalculator(feeType).GetCost(cost);
        }

        public void UpdateFeeCal(FeeType feeType, int pieceKey, CalculateFunctionType funcTyoe,
            Dictionary<string, string> param)
        {
            _calculateStradegyProvider.GetCalculator(feeType)
                .UpdateAlgorithm(AlgorithmOpCode.UpdateFunc, pieceKey, funcTyoe, param);
        }

        public void DeleteFeeCal(FeeType feeType, int pieceKey)
        {
            _calculateStradegyProvider.GetCalculator(feeType).UpdateAlgorithm(AlgorithmOpCode.DeleteFunc, pieceKey);
        }

        public void AddFeeCal(FeeType feeType, int pieceKey, CalculateFunctionType funcTyoe,
            Dictionary<string, string> param)
        {
            _calculateStradegyProvider.GetCalculator(feeType)
                .UpdateAlgorithm(AlgorithmOpCode.AddFunc, pieceKey, funcTyoe, param);
        }
    }

    internal interface ICalculateStradegyProvider : ISingletonDependency
    {
        ICalculateCostStrategy GetCalculator(FeeType feeType);
    }

    internal class CalculateStradegyProvider : ICalculateStradegyProvider
    {
        public CalculateStradegyProvider()
        {
            CalculatorDic = new Dictionary<FeeType, ICalculateCostStrategy>
            {
                [FeeType.Cpu] = new CpuCalculateCostStrategy(),
                [FeeType.Sto] = new StoCalculateCostStrategy(),
                [FeeType.Net] = new NetCalculateCostStrategy(),
                [FeeType.Ram] = new RamCalculateCostStrategy(),
                [FeeType.Tx] = new TxCalculateCostStrategy()
            };
        }

        private Dictionary<FeeType, ICalculateCostStrategy> CalculatorDic { get; }

        public ICalculateCostStrategy GetCalculator(FeeType feeType)
        {
            CalculatorDic.TryGetValue(feeType, out var cal);
            return cal;
        }
    }

    internal enum AlgorithmOpCode
    {
        AddFunc,
        DeleteFunc,
        UpdateFunc
    }

    internal interface ICalculateCostStrategy
    {
        long GetCost(int cost);

        void UpdateAlgorithm(AlgorithmOpCode opCode, int pieceKey,
            CalculateFunctionType funcType = CalculateFunctionType.Default,
            Dictionary<string, string> param = null);
    }

    internal abstract class CalculateCostStrategyBase : ICalculateCostStrategy
    {
        protected ICalculateAlgorithm CalculateAlgorithm { get; set; }

        public long GetCost(int cost)
        {
            return CalculateAlgorithm.Calculate(cost);
        }

        public void UpdateAlgorithm(AlgorithmOpCode opCode, int pieceKey, CalculateFunctionType funcType,
            Dictionary<string, string> param)
        {
            switch (opCode)
            {
                case AlgorithmOpCode.AddFunc:
                    CalculateAlgorithm.AddByParam(pieceKey, funcType, param);
                    break;
                case AlgorithmOpCode.DeleteFunc:
                    CalculateAlgorithm.Delete(pieceKey);
                    break;
                case AlgorithmOpCode.UpdateFunc:
                    CalculateAlgorithm.Update(pieceKey, funcType, param);
                    break;
            }
        }
    }

    #region concrete stradegys

    internal class CpuCalculateCostStrategy : CalculateCostStrategyBase
    {
        public CpuCalculateCostStrategy()
        {
            CalculateAlgorithm = new CalculateAlgorithm().Add(10, new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 8,
                ConstantValue = 10000
            }).Add(100, new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 4
            }).Add(int.MaxValue, new PowerCalculateWay
            {
                Power = 2,
                ChangeSpanBase = 4, // scale  x axis         
                Weight = 250, // unit weight,  means  (10 cpu count = 333 weight) 
                WeightBase = 40,
                Numerator = 1,
                Denominator = 4,
                Precision = 100000000L // 1 token = 100000000
            }).Prepare();
        }
    }

    internal class StoCalculateCostStrategy : CalculateCostStrategyBase
    {
        public StoCalculateCostStrategy()
        {
            CalculateAlgorithm = new CalculateAlgorithm().Add(1000000, new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 64,
                ConstantValue = 10000
            }).Add(int.MaxValue, new PowerCalculateWay
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 250,
                WeightBase = 500,
                Numerator = 1,
                Denominator = 64,
                Precision = 100000000L
            }).Prepare();
        }
    }

    internal class RamCalculateCostStrategy : CalculateCostStrategyBase
    {
        public RamCalculateCostStrategy()
        {
            CalculateAlgorithm = new CalculateAlgorithm().Add(10, new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 8,
                ConstantValue = 10000
            }).Add(100, new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 4
            }).Add(int.MaxValue, new PowerCalculateWay
            {
                Power = 2,
                ChangeSpanBase = 2,
                Weight = 250,
                Numerator = 1,
                Denominator = 4,
                WeightBase = 40
            }).Prepare();
        }
    }

    internal class NetCalculateCostStrategy : CalculateCostStrategyBase
    {
        public NetCalculateCostStrategy()
        {
            CalculateAlgorithm = new CalculateAlgorithm().Add(1000000, new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 64,
                ConstantValue = 10000
            }).Add(int.MaxValue, new PowerCalculateWay
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 250,
                WeightBase = 500,
                Numerator = 1,
                Denominator = 64,
                Precision = 100000000L
            }).Prepare();
        }
    }

    internal class TxCalculateCostStrategy : CalculateCostStrategyBase
    {
        public TxCalculateCostStrategy()
        {
            CalculateAlgorithm = new CalculateAlgorithm().Add(1000000, new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 16 * 50,
                ConstantValue = 10000
            }).Add(int.MaxValue, new PowerCalculateWay
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 1,
                WeightBase = 1,
                Numerator = 1,
                Denominator = 16 * 50,
                Precision = 100000000L
            }).Prepare();
        }
    }

    #endregion

    internal interface ICalculateAlgorithm
    {
        Dictionary<int, ICalculateWay> PieceWise { get; set; }
        long Calculate(int count);
        ICalculateAlgorithm Add(int limit, ICalculateWay func);
        ICalculateAlgorithm Prepare();
        void Delete(int pieceKey);
        void Update(int pieceKey, CalculateFunctionType funcType, Dictionary<string, string> parameters);
        void AddByParam(int pieceKey, CalculateFunctionType funcType, Dictionary<string, string> parameters);
    }

    #region ICalculateAlgorithm implemention

    internal class CalculateAlgorithm : ICalculateAlgorithm
    {
        public CalculateAlgorithm()
        {
            Logger = new NullLogger<CalculateAlgorithm>();
        }

        public ILogger<CalculateAlgorithm> Logger { get; set; }

        public Dictionary<int, ICalculateWay> PieceWise { get; set; } = new Dictionary<int, ICalculateWay>();

        public ICalculateAlgorithm Add(int limit, ICalculateWay func)
        {
            // to do
            PieceWise[limit] = func;
            return this;
        }

        public ICalculateAlgorithm Prepare()
        {
            if (!PieceWise.Any() || PieceWise.Any(x => x.Key <= 0)) Logger.LogError("piece key wrong");

            PieceWise = PieceWise.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            return this;
        }

        public long Calculate(int count)
        {
            long totalCost = 0;
            var prePieceKey = 0;
            foreach (var piece in PieceWise)
            {
                if (count < piece.Key)
                {
                    totalCost = piece.Value.GetCost(count.Sub(prePieceKey)).Add(totalCost);
                    break;
                }

                var span = piece.Key.Sub(prePieceKey);
                totalCost = piece.Value.GetCost(span).Add(totalCost);
                prePieceKey = piece.Key;
                if (count == piece.Key)
                    break;
            }

            return totalCost;
        }

        public void Delete(int pieceKey)
        {
            if (PieceWise.ContainsKey(pieceKey))
                PieceWise.Remove(pieceKey);
        }

        public void Update(int pieceKey, CalculateFunctionType funcType, Dictionary<string, string> parameters)
        {
            if (parameters.TryGetValue("piecekey", out var newPieceKeyStr))
            {
                if (int.TryParse(newPieceKeyStr, out var newPieceKey))
                {
                    Delete(pieceKey);
                    AddByParam(newPieceKey, funcType, parameters);
                }
            }
            else
            {
                AddByParam(pieceKey, funcType, parameters);
            }
        }

        public void AddByParam(int pieceKey, CalculateFunctionType funcType, Dictionary<string, string> parameters)
        {
            ICalculateWay newCalculateWay = null;
            switch (funcType)
            {
                case CalculateFunctionType.Constrant:
                    newCalculateWay = new ConstCalculateWay();
                    break;
                case CalculateFunctionType.Liner:
                    newCalculateWay = new LinerCalculateWay();
                    break;
                case CalculateFunctionType.Power:
                    newCalculateWay = new PowerCalculateWay();
                    break;
                case CalculateFunctionType.Ln:
                    newCalculateWay = new LnCalculateWay();
                    break;
            }

            if (newCalculateWay != null && newCalculateWay.InitParameter(parameters))
                PieceWise[pieceKey] = newCalculateWay;
        }
    }

    #endregion


    public enum CalculateFunctionType
    {
        Default = 0,
        Constrant,
        Liner,
        Power,
        Ln,
        Bancor
    }

    public interface ICalculateWay
    {
        long GetCost(int initValue);
        bool InitParameter(Dictionary<string, string> param);
    }

    public class LnCalculateWay : ICalculateWay
    {
        public int ChangeSpanBase { get; set; }
        public int Weight { get; set; }
        public int WeightBase { get; set; }
        public long Precision { get; set; } = 100000000L;

        public bool InitParameter(Dictionary<string, string> param)
        {
            param.TryGetValue(nameof(ChangeSpanBase).ToLower(), out var changeSpanBaseStr);
            int.TryParse(changeSpanBaseStr, out var changeSpanBase);
            if (changeSpanBase <= 0)
                return false;
            param.TryGetValue(nameof(Weight).ToLower(), out var weightStr);
            int.TryParse(weightStr, out var weight);
            if (weight <= 0)
                return false;
            param.TryGetValue(nameof(WeightBase).ToLower(), out var weightBaseStr);
            int.TryParse(weightBaseStr, out var weightBase);
            if (weightBase <= 0)
                return false;
            param.TryGetValue(nameof(Precision).ToLower(), out var precisionStr);
            long.TryParse(precisionStr, out var precision);
            Precision = precision > 0 ? precision : Precision;
            ChangeSpanBase = changeSpanBase;
            Weight = weight;
            WeightBase = weightBase;
            return true;
        }

        public long GetCost(int cost)
        {
            var diff = cost + 1;
            var weightChange = (double) diff / ChangeSpanBase;
            var unitValue = (double) Weight / WeightBase;
            if (weightChange <= 1)
                return 0;
            return Precision.Mul((long) (weightChange * unitValue * Math.Log(weightChange, Math.E)));
        }
    }

    public class PowerCalculateWay : ICalculateWay
    {
        public double Power { get; set; }
        public int ChangeSpanBase { get; set; }
        public int Weight { get; set; }
        public int WeightBase { get; set; }
        public long Precision { get; set; } = 100000000L;

        public int Numerator { get; set; }
        public int Denominator { get; set; } = 1;

        public bool InitParameter(Dictionary<string, string> param)
        {
            param.TryGetValue(nameof(Power).ToLower(), out var powerStr);
            double.TryParse(powerStr, out var power);
            if (power <= 0)
                return false;
            param.TryGetValue(nameof(ChangeSpanBase).ToLower(), out var changeSpanBaseStr);
            int.TryParse(changeSpanBaseStr, out var changeSpanBase);
            if (changeSpanBase <= 0)
                return false;
            param.TryGetValue(nameof(Weight).ToLower(), out var weightStr);
            int.TryParse(weightStr, out var weight);
            if (weight <= 0)
                return false;
            param.TryGetValue(nameof(WeightBase).ToLower(), out var weightBaseStr);
            int.TryParse(weightBaseStr, out var weightBase);
            if (weightBase <= 0)
                return false;
            param.TryGetValue(nameof(Precision).ToLower(), out var precisionStr);
            long.TryParse(precisionStr, out var precision);
            Precision = precision > 0 ? precision : Precision;
            Power = power;
            ChangeSpanBase = changeSpanBase;
            Weight = weight;
            WeightBase = weightBase;
            return true;
        }

        public long GetCost(int cost)
        {
            return ((long) (Math.Pow((double) cost / ChangeSpanBase, Power) * Precision)).Mul(Weight).Div(WeightBase)
                .Add(Precision.Mul(Numerator).Div(Denominator).Mul(cost));
        }
    }

    public class BancorCalculateWay : ICalculateWay
    {
        public decimal ResourceWeight { get; set; }
        public decimal TokenWeight { get; set; }
        public long ResourceConnectorBalance { get; set; }
        public long TokenConnectorBalance { get; set; }
        public long Precision { get; set; } = 100000000L;

        public bool InitParameter(Dictionary<string, string> param)
        {
            throw new NotImplementedException();
        }

        public long GetCost(int cost)
        {
            throw new NotImplementedException();
        }
    }

    public class ConstCalculateWay : ICalculateWay
    {
        public long Precision { get; set; } = 100000000L;

        public int ConstantValue { get; set; }

        public bool InitParameter(Dictionary<string, string> param)
        {
            param.TryGetValue(nameof(ConstantValue).ToLower(), out var constantValueStr);
            int.TryParse(constantValueStr, out var constantValue);
            if (constantValue <= 0)
                return false;
            param.TryGetValue(nameof(Precision).ToLower(), out var precisionStr);
            long.TryParse(precisionStr, out var precision);
            Precision = precision > 0 ? precision : Precision;
            ConstantValue = constantValue;
            return true;
        }

        public long GetCost(int cost)
        {
            return Precision.Mul(ConstantValue);
        }
    }

    public class LinerCalculateWay : ICalculateWay
    {
        public int Numerator { get; set; }
        public int Denominator { get; set; } = 1;
        public int ConstantValue { get; set; }
        public long Precision { get; set; } = 100000000L;

        public bool InitParameter(Dictionary<string, string> param)
        {
            param.TryGetValue(nameof(Numerator).ToLower(), out var numeratorStr);
            int.TryParse(numeratorStr, out var numerator);
            if (numerator <= 0)
                return false;
            param.TryGetValue(nameof(Denominator).ToLower(), out var denominatorStr);
            int.TryParse(denominatorStr, out var denominator);
            if (denominator <= 0)
                return false;
            param.TryGetValue(nameof(ConstantValue).ToLower(), out var constantValueStr);
            int.TryParse(constantValueStr, out var constantValue);
            if (constantValue <= 0)
                return false;
            param.TryGetValue(nameof(Precision).ToLower(), out var precisionStr);
            long.TryParse(precisionStr, out var precision);
            Precision = precision > 0 ? precision : Precision;
            Numerator = numerator;
            Denominator = denominator;
            ConstantValue = constantValue;
            return true;
        }

        public long GetCost(int cost)
        {
            return Precision.Mul(cost).Mul(Numerator).Div(Denominator).Add(ConstantValue);
        }
    }
}