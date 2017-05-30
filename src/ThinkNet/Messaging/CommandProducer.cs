

namespace ThinkNet.Messaging
{
    using System.Collections.Generic;

    using ThinkNet.Infrastructure;

    public class CommandProducer : MessageProducer<Command>, ICommandBus
    {

        public CommandProducer(ILoggerFactory loggerFactory) 
            : base(loggerFactory)
        { }

        #region ICommandBus 成员

        public void Send(Command command, TraceInfo traceInfo)
        {
            var envelope = new Envelope<Command>(command, ObjectId.GenerateNewStringId());
            envelope.Items["TraceInfo"] = traceInfo;

            this.Send(envelope);
        }

        public void Send(IEnumerable<Command> commands, TraceInfo traceInfo)
        {
            foreach(var command in commands) {
                this.Send(command, traceInfo);
            }
        }

        #endregion
    }
}
