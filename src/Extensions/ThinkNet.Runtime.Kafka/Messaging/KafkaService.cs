using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using KafkaNet;
using KafkaNet.Common;
using KafkaNet.Model;
using KafkaNet.Protocol;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    public class KafkaService : EnvelopeHub
    {
        public const string OffsetPositionFile = "kafka.consumer.offset";

        private readonly ISerializer _serializer;
        private readonly ITopicProvider _topicProvider;
        private readonly Lazy<Producer> producer;

        private readonly Dictionary<string, Consumer> consumers;

        private KafkaNet.Protocol.Message Serialize(object message)
        {
            string serialized;
            if (message is EventStream) {
                serialized = _serializer.Serialize(message);
            }
            else if (message is CommandReply) {
                serialized = _serializer.Serialize(message);
            }
            else {
                var metadata = new Dictionary<string, string>();
                var type = message.GetType();
                metadata[StandardMetadata.AssemblyName] = Path.GetFileNameWithoutExtension(type.Assembly.ManifestModule.FullyQualifiedName);
                metadata[StandardMetadata.Namespace] = type.Namespace;
                metadata[StandardMetadata.TypeName] = type.Name;
                metadata["Playload"] = _serializer.Serialize(message);

                serialized = _serializer.Serialize(metadata);
            }


            return new KafkaNet.Protocol.Message(serialized);
        }

        public override Task SendAsync(Envelope envelope)
        {
            var topic = _topicProvider.GetTopic(envelope.Body);

            var message = this.Serialize(envelope.Body);
            return producer.Value.SendMessageAsync(topic, new[] { message });
        }

        private Task Push(KeyValuePair<string, KafkaNet.Protocol.Message[]> kvp)
        {
            return producer.Value.SendMessageAsync(kvp.Key, kvp.Value).ContinueWith(task => {
                if (task.IsFaulted) {
                    if (LogManager.Default.IsErrorEnabled) {
                        LogManager.Default.Error("send to kafka encountered error.", task.Exception);
                    }
                }
            });
        }

        private IDictionary<string, KafkaNet.Protocol.Message[]> GroupByTopic(IEnumerable<Envelope> envelopes)
        {
            return envelopes.AsParallel()
                .Select(item => new {
                    Topic = _topicProvider.GetTopic(item.Body),
                    Data = this.Serialize(item.Body)
                })
                .GroupBy(item => item.Topic)
                .ToDictionary(item => item.Key, item => item.Select(p => p.Data).ToArray());
        }

        public override Task SendAsync(IEnumerable<Envelope> envelopes)
        {
            return Task.Factory.StartNew(() => GroupByTopic(envelopes))
                .ContinueWith(task => {
                    var tasks = task.Result.Select(Push).ToArray();
                    Task.WaitAll(tasks);
                });
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
                //if (success) {
                //    hub.Distribute(item);
                //}
            }
        }

        private bool Deserialize(string serialized, Type type)
        {
            var metadata = _serializer.Deserialize(serialized, type);

            //string uniqueId;
            //if (metadata.TryGetValue(StandardMetadata.UniqueId, out uniqueId)) {
            //    id = string.Format("{0}@{1}", metadata[StandardMetadata.TypeName], uniqueId);
            //}
            //else {
            //    id = null;
            //}
            //var typeFullName = string.Format("{0}.{1}, {2}",
            //    metadata[StandardMetadata.Namespace],
            //    metadata[StandardMetadata.TypeName],
            //    metadata[StandardMetadata.AssemblyName]);
            //var type = Type.GetType(typeFullName);

            //obj = serializer.Deserialize(metadata["Playload"], type);

            return true;
        }
    }
}
