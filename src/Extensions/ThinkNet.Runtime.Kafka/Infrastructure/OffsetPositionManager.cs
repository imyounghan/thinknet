using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using KafkaNet.Protocol;
using ThinkNet.Configurations;

namespace ThinkNet.Infrastructure
{
    public class OffsetPositionManager
    {
        public static readonly OffsetPositionManager Instance = new OffsetPositionManager();

        private readonly Dictionary<string, ConcurrentDictionary<string, MessageMetadata>> metadatas;
        private readonly Dictionary<string, ConcurrentDictionary<int, long>> lasted;
        private readonly Dictionary<string, OffsetPosition[]> final;

        private OffsetPositionManager()
        {
            this.metadatas = new Dictionary<string, ConcurrentDictionary<string, MessageMetadata>>();
            this.lasted = new Dictionary<string, ConcurrentDictionary<int, long>>();
            this.final = new Dictionary<string, OffsetPosition[]>();

            foreach (var topic in KafkaSettings.Current.Topics) {
                metadatas[topic] = new ConcurrentDictionary<string, MessageMetadata>();
                lasted[topic] = new ConcurrentDictionary<int, long>();
            }
        }

        public void Add(string topic, string correlationId, MessageMetadata meta)
        {
            lasted[topic].AddOrUpdate(meta.PartitionId, meta.Offset, (key, value) => meta.Offset);
            if (!string.IsNullOrEmpty(correlationId)) {
                metadatas[topic].TryAdd(correlationId, meta);
            }
        }

        public void Remove(string topic, string correlationId)
        {
            if (!string.IsNullOrEmpty(correlationId)) {
                metadatas[topic].Remove(correlationId);
            }
        }

        public IDictionary<string, OffsetPosition[]> Get()
        {
            if (metadatas.All(p => p.Value.Count == 0) && lasted.All(p => p.Value.Count == 0)) {
                final.Clear();
                return final;
            }

            foreach (var topic in KafkaSettings.Current.Topics) {
                OffsetPosition[] positions;
                if (metadatas[topic].Count == 0) {
                    positions = lasted[topic].Select(p => new OffsetPosition(p.Key, p.Value + 1)).ToArray();
                }
                else {
                    positions = metadatas[topic].Values
                        .GroupBy(p => p.PartitionId, p => p.Offset)
                        .Select(p => new OffsetPosition(p.Key, p.Min() + 1))
                        .ToArray();
                }

                final[topic] = positions;
            }

            return final;
        }
    }
}
