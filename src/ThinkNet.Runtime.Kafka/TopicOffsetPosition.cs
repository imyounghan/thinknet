using System;

namespace ThinkNet.Runtime.Kafka
{
    public struct TopicOffsetPosition : IEquatable<TopicOffsetPosition>
    {
        private string topic;
        private int partitionId;
        private long offset;
        public TopicOffsetPosition(string topic, int partitionId, long offset)
        {
            this.topic = topic;
            this.offset = offset;
            this.partitionId = partitionId;
        }

        public string Topic { get { return this.topic; } }

        public long Offset { get { return this.offset; } }

        public int PartitionId { get { return this.partitionId; } }

        bool IEquatable<TopicOffsetPosition>.Equals(TopicOffsetPosition other)
        {
            return this.Topic == other.Topic &&
                this.PartitionId == other.PartitionId &&
                this.Offset == other.Offset;
        }
    }
}
