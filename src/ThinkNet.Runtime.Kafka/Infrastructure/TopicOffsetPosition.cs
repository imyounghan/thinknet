using System;

namespace ThinkNet.Infrastructure
{
    public struct TopicOffsetPosition : IEquatable<TopicOffsetPosition>
    {
        public TopicOffsetPosition(string topic, int partitionId, long offset)
        {
            this.Topic = topic;
            this.Offset = offset;
            this.PartitionId = partitionId;
        }

        public string Topic { get; private set; }

        public long Offset { get; private set; }

        public int PartitionId { get; private set; }

        bool IEquatable<TopicOffsetPosition>.Equals(TopicOffsetPosition other)
        {
            return this.Topic == other.Topic &&
                this.PartitionId == other.PartitionId &&
                this.Offset == other.Offset;
        }
    }
}
