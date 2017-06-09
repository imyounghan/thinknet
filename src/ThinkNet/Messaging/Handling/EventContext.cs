

namespace ThinkNet.Messaging.Handling
{
    using System;
    using System.Collections.Generic;

    public class EventContext : IEventContext
    {
        private readonly List<Command> commands;

        private readonly ICommandBus commandBus;

        private readonly ISendReplyService sendReplyService;


        public EventContext(ICommandBus commandBus, ISendReplyService sendReplyService)
        {
            this.commandBus = commandBus;
            this.sendReplyService = sendReplyService;

            this.commands = new List<Command>();
        }
        //public EventContext(SourceKey sourceInfo, TraceInfo traceInfo, int version)
        //{
        //    this.SourceInfo = sourceInfo;
        //    this.TraceInfo = traceInfo;
        //    this.Version = version;

        //    this.commands = new List<ICommand>();
        //}

        //public EventContext(string commandId,
        //    string aggregateRootTypeName,
        //    string aggregateRootId,
        //    int version,
        //    string processId,
        //    string replyAddress)
        //    : this(new SourceKey(commandId, aggregateRootTypeName, aggregateRootId),
        //    new TraceInfo(processId, replyAddress),
        //    version)
        //{
        //}

        public SourceInfo SourceInfo { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; set; }

        public TraceInfo TraceInfo { get; set; }

        public void AddCommand(Command command)
        {
            this.commands.Add(command);
        }

        public void Commit()
        {
            if(this.commands.IsEmpty()) {
                var commandResult = new CommandResult(this.TraceInfo.Id);
                sendReplyService.SendReply(commandResult, this.TraceInfo.Address);
            }
            else {
                commandBus.Send(this.commands, this.TraceInfo);
            }
        }
    }
}
