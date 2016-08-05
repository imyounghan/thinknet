using System;

namespace ThinkNet.Infrastructure
{
    public interface IMessageMetadata
    {
        string Topic { get; }

        long Offset { get; }

        int QueueId { set; }

        TimeSpan TimeForWait { get; }
    }
}
