using System.Runtime.Serialization;
using System.Threading.Tasks;
using ThinkNet.Contracts;
using ThinkNet.Runtime.Routing;

namespace ThinkNet.Runtime
{
    /// <summary>
    /// <see cref="ICommandService"/> 的默认实现类
    /// </summary>
    public class DefaultCommandService : CommandService
    {
        private readonly IEnvelopeSender _sender;
        /// <summary>
        /// Default constructor.
        /// </summary>
        public DefaultCommandService(IEnvelopeSender sender)
        {
            this._sender = sender;
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
            var envelope = new Envelope(command);
            envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.CommandKind;
            envelope.Metadata[StandardMetadata.SourceId] = command.Id;
            var attribute = command.GetType().GetCustomAttribute<DataContractAttribute>(false);
            if(attribute != null) {
                bool clearAssemblyName = false;

                if(!string.IsNullOrEmpty(attribute.Namespace)) {
                    envelope.Metadata[StandardMetadata.Namespace] = attribute.Namespace;
                    clearAssemblyName = true;
                }

                if(!string.IsNullOrEmpty(attribute.Name)) {
                    envelope.Metadata[StandardMetadata.TypeName] = attribute.Name;
                    clearAssemblyName = true;
                }

                if(clearAssemblyName)
                    envelope.Metadata.Remove(StandardMetadata.AssemblyName);
            }

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
