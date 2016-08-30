using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using KafkaNet;
using KafkaNet.Common;
using KafkaNet.Model;
using KafkaNet.Protocol;
using ThinkNet.Common;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging.Processing
{
    public class KafkaProcessor : Processor
    {
        private const string OffsetPositionFile = "kafka.consumer.offset";

        private readonly ISerializer serializer;
        private readonly IEnvelopeHub hub;

        private readonly Dictionary<string, Consumer> consumers;


        public KafkaProcessor(IEnvelopeHub hub, ISerializer serializer)
        {
            this.hub = hub;
            this.serializer = serializer;
            this.consumers = new Dictionary<string, Consumer>();

            this.InitConsumers(new KafkaOptions(KafkaSettings.Current.KafkaUris));

            foreach (var topic in KafkaSettings.Current.Topics) {
                base.BuildWorker(PullThenForward, topic);
            }

            base.BuildWorker(RecordConsumerOffset).SetDelay(5000).SetInterval(2000);
        }


        private void InitConsumers(KafkaOptions kafkaOptions)
        {
            var offsetPositions = new Dictionary<string, OffsetPosition[]>();

            try {
                var xml = new XmlDocument();
                xml.Load(OffsetPositionFile);

                offsetPositions = xml.DocumentElement.ChildNodes.Cast<XmlElement>().AsParallel()
                    .ToDictionary(topic => topic.GetAttribute("name"), topic => {
                        return topic.ChildNodes.Cast<XmlElement>()
                            .Select(offset => new OffsetPosition() {
                                PartitionId = offset.GetAttribute("partitionId").Change(0),
                                Offset = offset.InnerText.Change(0)
                            }).ToArray();
                    });
            }
            catch (Exception) {
                //TODO...Write LOG
            }

            foreach (var topic in KafkaSettings.Current.Topics) {
                consumers[topic] = new Consumer(new ConsumerOptions(topic, new BrokerRouter(kafkaOptions)) {
                    Log = KafkaLog.Instance
                }, offsetPositions.ContainsKey(topic) ? offsetPositions[topic] : new OffsetPosition[0]);
            }
        }


        private void RecordConsumerOffset()
        {
            var offsetPositions = OffsetPositionManager.Instance.Get();
            if (offsetPositions.Count == 0)
                return;

            var xml = new XmlDocument();
            xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", null));
            var root = xml.AppendChild(xml.CreateElement("root"));

            foreach (var kvp in offsetPositions) {
                var el = xml.CreateElement("topic");
                el.SetAttribute("name", kvp.Key);
                kvp.Value.ForEach(item => {
                    var node = xml.CreateElement("offset");
                    node.SetAttribute("partitionId", item.PartitionId.ToString());
                    node.InnerText = item.Offset.ToString();
                    el.AppendChild(node);
                });
                root.AppendChild(el);
            }
            xml.Save(OffsetPositionFile);
        }

        private void PullThenForward(string topic)
        {
            var consumer = consumers[topic];

            foreach (var message in consumer.Consume()) {
                object item;
                string uniqueId;
                var success = this.Deserialize(message.Value.ToUtf8String(), out uniqueId, out item);

                OffsetPositionManager.Instance.Add(topic, uniqueId, message.Meta);
                if (success) {
                     hub.Distribute(item);
                }
            }
        }

        private bool Deserialize(string serialized, Type type)
        {
            if (!serialized.StartsWith("{") && !serialized.EndsWith("}")) {
                id = null;
                obj = DBNull.Value;
                return false;
            }

            var metadata = serializer.Deserialize<IDictionary<string, string>>(serialized);            

            string uniqueId;
            if (metadata.TryGetValue(StandardMetadata.UniqueId, out uniqueId)) {
                id = string.Format("{0}@{1}", metadata[StandardMetadata.TypeName], uniqueId);
            }
            else {
                id = null;
            }
            var typeFullName = string.Format("{0}.{1}, {2}",
                metadata[StandardMetadata.Namespace],
                metadata[StandardMetadata.TypeName],
                metadata[StandardMetadata.AssemblyName]);
            var type = Type.GetType(typeFullName);

            obj = serializer.Deserialize(metadata["Playload"], type);

            return true;
        }
    }
}
