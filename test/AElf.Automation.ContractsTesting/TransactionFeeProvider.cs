using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Sdk.CSharp;
using AElfChain.Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Automation.ContractsTesting
{
    public class TransactionFeeProvider
    {
        private static ICalculateFeeService CalService { get; set; }

        public TransactionFeeProvider()
        {
            CalService = new CalculateFeeService();
        }

        public static long TransactionSizeFee(int size)
        {
            return CalService.GetTransactionFee(size);
        }

        public static long CpuSizeFee(int size)
        {
            return CalService.GetCpuTokenCost(size);
        }

        public static long NetSizeFee(int size)
        {
            return CalService.GetNetTokenCost(size);
        }

        public static long StoSizeFee(int size)
        {
            return CalService.GetStoTokenCost(size);
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

    public interface ICalculateFeeService
    {
        long GetCpuTokenCost(int readCount);
        long GetStoTokenCost(int writeCount);
        long GetNetTokenCost(int neeCost);
        long GetTransactionFee(int txSize);
        ICalCostService GetCpuCalculator { get; }
        ICalCostService GetNetCalculator { get; }
        ICalCostService GetStoCalculator { get; }
        ICalCostService GetTxCalculator { get; }
    }

    public class CalculateFeeService : ICalculateFeeService
    {
        private ICalCostService _cpuCal;
        private ICalCostService _netCal;
        private ICalCostService _stoCal;
        private ICalCostService _txCal;

        private const int CpuExpectCount = 10;
        private const long CpuExpectCost = 900000000L;

        private const int StoExpectCount = 10;
        private const long StoExpectCost = 900000000L;

        private const int NetExpectCount = 270;
        private const long NetExpectCost = 900000000L;

        private const int TxExpectCount = 270;
        private const long TxExpectCost = 50000000L;

        private const int TokenCountPerElf = 50;

        public CalculateFeeService()
        {
            _cpuCal = new CalCostService(CpuExpectCost, CpuExpectCount);
            _cpuCal.Add(10, x => new LinerCalService
            {
                Numerator = 1,
                Denominator = 8,
                ConstantValue = 10000
            }.GetCost(x));
            _cpuCal.Add(100, x => new LinerCalService
            {
                Numerator = 1,
                Denominator = 4,
            }.GetCost(x));
            _cpuCal.Add(int.MaxValue, x => new PowCalService
            {
                Power = 2,
                ChangeSpanBase = 10, // scale  x axis         
                Weight = 333, // unit weight,  means  (10 cpu count = 333 weight) 
                WeightBase = 10,
                Decimal = 100000000L // 1 token = 100000000
            }.GetCost(x));
            //_cpuCal.Prepare();

            _stoCal = new CalCostService(StoExpectCost, StoExpectCount);
            _stoCal.Add(10, x => new LinerCalService
            {
                Numerator = 1,
                Denominator = 8,
                ConstantValue = 10000
            }.GetCost(x));
            _stoCal.Add(100, x => new LinerCalService
            {
                Numerator = 1,
                Denominator = 4,
            }.GetCost(x));
            _stoCal.Add(int.MaxValue, x => new PowCalService
            {
                Power = 2,
                ChangeSpanBase = 5,
                Weight = 333,
                WeightBase = 5,
            }.GetCost(x));
            //_stoCal.Prepare();

            _netCal = new CalCostService(NetExpectCost, NetExpectCount);
            _netCal.Add(1000000, x => new LinerCalService
            {
                Numerator = 1,
                Denominator = 32,
                ConstantValue = 10000
            }.GetCost(x));
            _netCal.Add(int.MaxValue, x => new PowCalService
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 333,
                WeightBase = 500,
                Decimal = 100000000L
            }.GetCost(x));
            //_netCal.Prepare();

            _txCal = new CalCostService(TxExpectCost, TxExpectCount);
//            _txCal.Add(1000, x => new LinerCalService
//            {
//                Numerator = 1,
//                Denominator = 15 * 50,
//                ConstantValue = 10000
//            }.GetCost(x));
            _txCal.Add(1000000, x => new LinerCalService
            {
                Numerator = 1,
                Denominator = 16 * TokenCountPerElf,
                ConstantValue = 10000
            }.GetCost(x));
            _txCal.Add(int.MaxValue, x => new PowCalService
            {
                Power = 2,
                ChangeSpanBase = 100.Mul(TokenCountPerElf),
                Weight = 1,
                WeightBase = 1,
                Decimal = 100000000L
            }.GetCost(x));
            //_txCal.Prepare();
        }

        public ICalCostService GetCpuCalculator => _cpuCal;
        public ICalCostService GetNetCalculator => _netCal;
        public ICalCostService GetStoCalculator => _stoCal;
        public ICalCostService GetTxCalculator => _txCal;

        public long GetCpuTokenCost(int readCount)
        {
            return _cpuCal.CalCost(readCount);
        }

        public long GetStoTokenCost(int writeCount)
        {
            return _stoCal.CalCost(writeCount);
        }

        public long GetNetTokenCost(int netCost)
        {
            return _netCal.CalCost(netCost);
        }

        public long GetTransactionFee(int txSize)
        {
            return _txCal.CalCost(txSize);
        }
    }

    public interface ICalCostService
    {
        Dictionary<int, Func<int, long>> PieceWise { get; set; }
        long AimAvgCost { get; set; }
        int AimKey { get; set; }
        long CalCost(int count);
        void Add(int limit, Func<int, long> func);
        void Prepare();
    }
    
    public class CalCostService : ICalCostService
    {
        public ILogger<CalCostService> Logger { get; set; }

        public CalCostService(long avgCost, int aimKey)
        {
            AimAvgCost = avgCost;
            AimKey = aimKey;
            Logger = new NullLogger<CalCostService>();
        }

        public long AimAvgCost { get; set; }
        public int AimKey { get; set; }
        public Dictionary<int, Func<int, long>> PieceWise { get; set; } = new Dictionary<int, Func<int, long>>();

        public void Add(int limit, Func<int, long> func)
        {
            // to do
            PieceWise[limit] = func;
        }

        public void Prepare()
        {
            if (!PieceWise.Any(x => x.Key >= AimKey))
            {
                Logger.LogError("piece count wrong");
            }

            if (AimAvgCost == 0)
                return;
            PieceWise = PieceWise.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

            var firstPiece = PieceWise.FirstOrDefault();
            if (firstPiece.Key <= 0 || firstPiece.Key == long.MaxValue)
            {
                return;
            }

            var midCost = CalCost(AimKey);
            var coefficient = AimAvgCost.Div(midCost);
            if (coefficient <= 0)
                return;
            var newPieceWise = PieceWise.ToDictionary(x => x.Key, x => x.Value);
            foreach (var key in newPieceWise.Keys)
                PieceWise[key] = x => newPieceWise[key].Invoke(x).Mul(coefficient);
        }

        public long CalCost(int count)
        {
            long totalCost = 0;
            int prePieceKey = 0;
            foreach (var piece in PieceWise)
            {
                if (count < piece.Key)
                {
                    totalCost = piece.Value.Invoke(count.Sub(prePieceKey)).Add(totalCost);
                    break;
                }

                var span = piece.Key.Sub(prePieceKey);
                totalCost = piece.Value.Invoke(span).Add(totalCost);
                prePieceKey = piece.Key;
                if (count == piece.Key)
                    break;
            }

            return totalCost;
        }
    }

    public class PowCalService : ICalService
    {
        public double Power { get; set; }
        public int ChangeSpanBase { get; set; }
        public long Weight { get; set; }
        public int WeightBase { get; set; }
        public long Decimal { get; set; } = 100000000L;

        public long GetCost(int cost)
        {
            return ((long) Math.Pow((double) cost / ChangeSpanBase, Power) * Decimal).Mul(Weight).Div(WeightBase);
        }
    }
    
    public class LinerCalService : ICalService
    {
        public int Numerator { get; set; }
        public int Denominator { get; set; }
        public int ConstantValue { get; set; }
        public long Decimal { get; set; } = 100000000L;

        public long GetCost(int cost)
        {
            return Decimal.Mul(cost).Mul(Numerator).Div(Denominator).Add(ConstantValue);
        }
    }

    public interface ICalService
    {
        long GetCost(int initValue);
    }
}