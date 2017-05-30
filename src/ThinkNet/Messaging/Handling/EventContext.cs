

namespace ThinkNet.Messaging.Handling
{
    using System;
    using System.Collections.Generic;

    using ThinkNet.Infrastructure;

    public class EventContext : IEventContext, IUnitOfWork
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

        public SourceKey SourceInfo { get; set; }

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
                var commandResult = new CommandResult {
                    ProcessId = this.TraceInfo.ProcessId,
                    ReplyTime = DateTime.UtcNow
                };
                sendReplyService.SendReply(commandResult, this.TraceInfo.ReplyAddress);
            }
            else {
                commandBus.Send(this.commands, this.TraceInfo);
            }
        }
    }
}
