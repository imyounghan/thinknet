using System;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ThinkNet.Contracts;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
using ThinkNet.Runtime.Routing;

namespace ThinkNet.Runtime
{
    /// <summary>
    /// <see cref="ICommandService"/> 的默认实现类
    /// </summary>
    public class DefaultCommandService : CommandService
    {
        private readonly IEnvelopeSender _sender;
        private readonly ITextSerializer _serializer;
        /// <summary>
        /// Default constructor.
        /// </summary>
        public DefaultCommandService(IEnvelopeSender sender, ITextSerializer serializer)
        {
            this._sender = sender;
            this._serializer = serializer;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void Dispose(bool disposing)
        { }

        /// <summary>
        /// 发送一个命令
        /// </summary>
        public override void Send(ICommand command)
        {
            Envelope envelope;
            if(command is Command) {
                envelope = new Envelope(command);
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

                    type = Type.GetType(string.Format("{0}.{1}", attribute.Namespace, attribute.TypeName));
                }

                var converted = _serializer.Deserialize(_serializer.Serialize(command), type);

                envelope = new Envelope(converted, type);
            }
            
            envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.CommandKind;
            envelope.Metadata[StandardMetadata.SourceId] = command.Id;

            if(LogManager.Default.IsDebugEnabled) {
                LogManager.Default.DebugFormat("Sending a command to local queue, commandType:{0}.{1}, commandId:{2}.",
                    envelope.GetMetadata(StandardMetadata.Namespace), envelope.GetMetadata(StandardMetadata.TypeName), command.Id);
            }

            _sender.Send(envelope);            
        }

        /// <summary>
        /// 异步发送一个命令
        /// </summary>
        public override Task SendAsync(ICommand command)
        {
            return Task.Factory.StartNew(() => this.Send(command));
        }
    }
}
