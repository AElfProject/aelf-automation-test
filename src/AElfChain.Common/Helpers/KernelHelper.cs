using System;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Helpers
{
    public enum RefBlockStatus
    {
        UnknownRefBlockStatus = 0,
        RefBlockValid = 1,
        RefBlockInvalid = -1,
        RefBlockExpired = -2
    }

    public class QueuedTransaction
    {
        public Transaction Transaction { get; set; }
        public Hash TransactionId { get; set; }
        public Timestamp EnqueueTime { get; set; }
        public RefBlockStatus RefBlockStatus { get; set; }
    }

    public static class KernelHelper
    {
        public const int DefaultRunnerCategory = 0;
        public const int CodeCoverageRunnerCategory = 30;

        public static Timestamp GetUtcNow()
        {
            return DateTime.UtcNow.ToTimestamp();
        }

        public static Duration DurationFromMilliseconds(long milliseconds)
        {
            return new Duration
            {
                Seconds = milliseconds / 1000L,
                Nanos = (int) (milliseconds % 1000L) * 1000000
            };
        }

        public static Duration DurationFromSeconds(long seconds)
        {
            return new Duration {Seconds = seconds};
        }

        public static Duration DurationFromMinutes(long minutes)
        {
            return new Duration {Seconds = minutes * 60L};
        }
    }
}