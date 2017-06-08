

namespace ThinkNet.Messaging
{
    using System.Collections.Generic;

    using ThinkNet.Infrastructure;

    public class CommandProducer : MessageProducer<ICommand>, ICommandBus
    {

        public CommandProducer(ILoggerFactory loggerFactory) 
            : base(loggerFactory)
        { }

        #region ICommandBus 成员

        public override void Send(ICommand message)
        {
            this.Send(message, TraceInfo.Empty);
        }

        public void Send(ICommand command, TraceInfo traceInfo)
        {
            var envelope = new Envelope<ICommand>(command, ObjectId.GenerateNewStringId());
            envelope.Items["TraceInfo"] = traceInfo;

            this.Send(envelope);
        }

        public override void Send(IEnumerable<ICommand> messages)
        {
            this.Send(messages, TraceInfo.Empty);
        }

        public void Send(IEnumerable<ICommand> commands, TraceInfo traceInfo)
        {
            foreach(var command in commands) {
                this.Send(command, traceInfo);
            }
        }

        #endregion
    }
}
