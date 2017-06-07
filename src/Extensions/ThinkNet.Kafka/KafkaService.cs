using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ThinkNet.Contracts;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime.Kafka
{
    public class KafkaService : CommandService, IMessageBus
    {
        private readonly ITextSerializer _serializer;
        private readonly KafkaClient _kafka;

        public KafkaService(ITextSerializer serializer, ITopicProvider topicProvider)
        {
            this._serializer = serializer;
            this._kafka = new KafkaClient(KafkaSettings.Current.ZookeeperAddress, topicProvider);
        }

        public override Task SendAsync(ICommand command)
        {
            if(LogManager.Default.IsDebugEnabled) {
                LogManager.Default.DebugFormat("Sending a command({0}) to kafka.", command);
            }

            return _kafka.Push<ICommand>(command, this.Serialize);
        }

        private byte[] Serialize(ICommand command)
        {
            GeneralData generalData;
            var commandType = command.GetType();
            if(command is Command) {
                generalData = new GeneralData(commandType);
            }
            else {
                var attribute = command.GetType().GetCustomAttribute<XmlTypeAttribute>(false);
                if(attribute == null || string.IsNullOrEmpty(attribute.TypeName)) {
                    string errorMessage = string.Format("Type of '{0}' is not defined XmlTypeAttribute or not set TypeName.");
                    throw new ThinkNetException(errorMessage);
                }

                Type type;
                if(!TryGetCommandType(attribute.TypeName, out type)) {
                    if(string.IsNullOrEmpty(attribute.Namespace)) {
                        string errorMessage = string.Format("Type of '{0}' XmlTypeAttribute not set Namespace.");
                        throw new ThinkNetException(errorMessage);
                    }

                    generalData = new GeneralData(attribute.Namespace, attribute.TypeName);
                }
                else {
                    generalData = new GeneralData(type);
                }
            }

            generalData.Metadata = _serializer.Serialize(command);

            return _serializer.SerializeToBinary(generalData);
        }

        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();

            using(_kafka) { };
        }

        private GeneralData Transform(object data)
        {
            var serialized = _serializer.Serialize(data);
            return new GeneralData(data.GetType()) {
                Metadata = serialized
            };
        }


        private byte[] Serialize(IMessage element)
        {
            var eventCollection = element as EventCollection;
            if(eventCollection != null) {
                var events = eventCollection.Select(Transform).ToArray();
                var stream = new EventStream() {
                    CorrelationId = eventCollection.CorrelationId,
                    Events = eventCollection.Select(Transform).ToArray(),
                    SourceAssemblyName = eventCollection.SourceId.AssemblyName,
                    SourceId = eventCollection.SourceId.Id,
                    SourceNamespace = eventCollection.SourceId.Namespace,
                    SourceTypeName = eventCollection.SourceId.TypeName,
                    Version = eventCollection.Version
                };

                return _serializer.SerializeToBinary(stream);
            }

            var commandResult = element as CommandResult;
            if(commandResult != null) {
                return _serializer.SerializeToBinary(commandResult);
            }


            return _serializer.SerializeToBinary(Transform(element));
        }

        #region IMessageBus 成员

        public Task PublishAsync(IMessage message)
        {
            if(LogManager.Default.IsDebugEnabled) {
                LogManager.Default.DebugFormat("Publishing a message({0}) to kafka.", message);
            }

            return _kafka.Push<IMessage>(message, this.Serialize);
        }

        public Task PublishAsync(IEnumerable<IMessage> messages)
        {
            if(LogManager.Default.IsDebugEnabled) {
                var stringArray = messages.Select(item => item.ToString());

                LogManager.Default.DebugFormat("Publishing a batch of messages({0}) to kafka.",
                    string.Join(",", stringArray));
            }

            return _kafka.Push<IMessage>(messages, this.Serialize);
        }

        #endregion
    }
}
