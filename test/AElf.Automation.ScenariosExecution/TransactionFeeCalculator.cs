using System;
using AElf.CSharp.Core;

namespace AElf.Automation.ScenariosExecution
{
    public class TransactionFeeCalculator
    {
        public TransactionFeeCalculator()
        {
            Cpu = new CpuSizeFee();
            Ram = new RamSizeFee();
            Sto = new StoSizeFee();
            Net = new NetSizeFee();
            Tx = new TxSizeFee();
        }

        public CpuSizeFee Cpu { get; set; }
        public RamSizeFee Ram { get; set; }
        public StoSizeFee Sto { get; set; }
        public NetSizeFee Net { get; set; }
        public TxSizeFee Tx { get; set; }
    }

    public class CpuSizeFee
    {
        public CpuSizeFee()
        {
            Liner1 = new LinerCalculateWay();
            Liner1.InitParameter(1, 8, 1000);

            Liner2 = new LinerCalculateWay();
            Liner2.InitParameter(1, 4, 0);

            Power = new PowerCalculateWay();
            Power.InitParameter(1, 4, 2, 5, 250, 40);
        }

        public LinerCalculateWay Liner1 { get; set; }
        public LinerCalculateWay Liner2 { get; set; }
        public PowerCalculateWay Power { get; set; }

        public long GetSizeFee(int size)
        {
            if (size <= 10) return Liner1.GetCost(size);

            if (size <= 100 && size > 10) return Liner2.GetCost(size);

            return size > 100 ? Power.GetCost(size) : 0;
        }
    }

    public class RamSizeFee
    {
        public RamSizeFee()
        {
            Liner1 = new LinerCalculateWay();
            Liner1.InitParameter(1, 8, 10000);

            Liner2 = new LinerCalculateWay();
            Liner2.InitParameter(1, 4, 0);

            Power = new PowerCalculateWay();
            Power.InitParameter(1, 4, 2, 2, 250, 40);
        }

        public LinerCalculateWay Liner1 { get; set; }
        public LinerCalculateWay Liner2 { get; set; }
        public PowerCalculateWay Power { get; set; }

        public long GetSizeFee(int size)
        {
            if (size <= 10) return Liner1.GetCost(size);

            if (size <= 100 && size > 10) return Liner2.GetCost(size);

            return size > 100 ? Power.GetCost(size) : 0;
        }
    }

    public class StoSizeFee
    {
        public StoSizeFee()
        {
            Liner1 = new LinerCalculateWay();
            Liner1.InitParameter(1, 4, 1000);

            Power = new PowerCalculateWay();
            Power.InitParameter(1, 64, 2, 100, 250, 500);
        }

        public LinerCalculateWay Liner1 { get; set; }
        public PowerCalculateWay Power { get; set; }

        public long GetSizeFee(int size)
        {
            if (size <= 1000000) return Liner1.GetCost(size);

            return size > 1000000 ? Power.GetCost(size) : 0;
        }
    }

    public class NetSizeFee
    {
        public NetSizeFee()
        {
            Liner1 = new LinerCalculateWay();
            Liner1.InitParameter(1, 64, 10000);

            Power = new PowerCalculateWay();
            Power.InitParameter(1, 64, 2, 100, 250, 500);
        }

        public LinerCalculateWay Liner1 { get; set; }
        public PowerCalculateWay Power { get; set; }

        public long GetSizeFee(int size)
        {
            if (size <= 1000000) return Liner1.GetCost(size);

            return size > 1000000 ? Power.GetCost(size) : 0;
        }
    }

    public class TxSizeFee
    {
        public TxSizeFee()
        {
            Liner1 = new LinerCalculateWay();
            Liner1.InitParameter(1, 800, 10000);

            Power = new PowerCalculateWay();
            Power.InitParameter(1, 800, 2, 100, 1, 1);
        }

        public LinerCalculateWay Liner1 { get; set; }
        public PowerCalculateWay Power { get; set; }

        public long GetSizeFee(int size)
        {
            if (size <= 1000000) return Liner1.GetCost(size);

            return size > 1000000 ? Power.GetCost(size) : 0;
        }
    }

    public class LinerCalculateWay
    {
        public int Numerator { get; set; }
        public int Denominator { get; set; } = 1;
        public int ConstantValue { get; set; }
        public long Precision { get; set; } = 100000000L;

        public void InitParameter(int numerator, int denominator, int constantValue)
        {
            Numerator = numerator;
            Denominator = denominator;
            ConstantValue = constantValue;
        }

        public long GetCost(int cost)
        {
            return Precision.Mul(cost).Mul(Numerator).Div(Denominator).Add(ConstantValue);
        }
    }

    public class PowerCalculateWay
    {
        public int Power { get; set; } = 2;
        public int ChangeSpanBase { get; set; } = 1;
        public int Weight { get; set; }
        public int WeightBase { get; set; }
        public long Precision { get; set; } = 100000000L;
        public int Numerator { get; set; }
        public int Denominator { get; set; } = 1;

        public void InitParameter(int numerator, int denominator, int power, int changeSpanBase, int weight,
            int weightBase)
        {
            ChangeSpanBase = changeSpanBase;
            Weight = weight;
            WeightBase = weightBase;
            Numerator = numerator;
            Denominator = denominator;
            Power = power;
        }

        public long GetCost(int cost)
        {
            return ((long) (Math.Pow((double) cost / ChangeSpanBase, Power) * Precision)).Mul(Weight).Div(WeightBase)
                .Add(Precision.Mul(Numerator).Div(Denominator).Mul(cost));
        }
    }
}