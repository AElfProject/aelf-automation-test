using System;
using System.Collections.Generic;
using System.IO;
using AElfChain.Common.Helpers;

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
                1000_000, 1100_000, 1200_000
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
        private static ICalCostService _cpuCal;
        private static ICalCostService _netCal;
        private static ICalCostService _stoCal;
        private static ICalCostService _txCal;

        static CalculateFeeService()
        {
            _cpuCal = new CalCostService();
            _cpuCal.Add(10, x => 5);
            _cpuCal.Add(100, x => x / 2);
            _cpuCal.Add(-1, x => new PowCalService
            {
                Power = 2,
                ChangeSpanBase = 10,
                Weight = 333,
                WeightBase = 10,
            }.GetCost(x));

            _stoCal = new CalCostService();
            _stoCal.Add(10, x => 5);
            _stoCal.Add(100, x => x);
            _stoCal.Add(-1, x => new PowCalService
            {
                Power = 2,
                ChangeSpanBase = 5,
                Weight = 333,
                WeightBase = 5,
            }.GetCost(x));

            _netCal = new CalCostService();
            _netCal.Add(1000, x => x / 30);
            _netCal.Add(1000000, x => x / 15);
            _netCal.Add(-1, x => new PowCalService
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 333,
                WeightBase = 500
            }.GetCost(x));

            _txCal = new CalCostService();
            _txCal.Add(1000, x => x * 2000000 / 15);
            _txCal.Add(1000000, x => x * 2000000 / 15);
            _txCal.Add(-1, x => new PowCalService
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 100000000,
                WeightBase = 50
            }.GetCost(x));
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
        long CalCost(int count);
        void Add(int limit, Func<int, long> func);
    }

    public class CalCostService : ICalCostService
    {
        public Dictionary<int, Func<int, long>> PieceWise { get; set; } = new Dictionary<int, Func<int, long>>();

        public void Add(int limit, Func<int, long> func)
        {
            // to do
            PieceWise[limit] = func;
        }

        public long CalCost(int count)
        {
            long totalCost = 0;
            int prePieceKey = 0;
            int spare = count;
            foreach (var piece in PieceWise)
            {
                if (piece.Key < 0)
                {
                    totalCost += piece.Value.Invoke(spare);
                    break;
                }

                int costSpan = count - piece.Key;
                if (costSpan < 0)
                {
                    totalCost += piece.Value.Invoke(count - prePieceKey);
                    break;
                }

                totalCost += piece.Value.Invoke(piece.Key - prePieceKey);
                spare -= piece.Key - prePieceKey;
                prePieceKey = piece.Key;
                if (costSpan == 0)
                    break;
            }

            return totalCost >= 0 ? totalCost : long.MaxValue;
        }
    }

    public class PowCalService : ICalService
    {
        public double Power { get; set; }
        public int ChangeSpanBase { get; set; }
        public int Weight { get; set; }
        public int WeightBase { get; set; }

        public long GetCost(int cost)
        {
            return (long) Math.Pow((double) cost / ChangeSpanBase, Power) * Weight / WeightBase;
        }
    }

    public interface ICalService
    {
        long GetCost(int initValue);
    }
}