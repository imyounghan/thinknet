
namespace ThinkNet.Messaging
{
    using System;
    using System.Collections.Generic;

    using ThinkNet.Infrastructure;

    public class QueryProducer : MessageProducer<IQuery>, IQueryBus
    {
        public QueryProducer(ILoggerFactory loggerFactory) 
            : base(loggerFactory)
        { }

        public override void Send(IQuery message)
        {
            throw new NotSupportedException();
        }

        public override void Send(IEnumerable<IQuery> messages)
        {
            throw new NotSupportedException();
        }

        #region IQueryBus 成员

        public void Send(IQuery query, TraceInfo traceInfo)
        {
            var envelope = new Envelope<IQuery>(query, ObjectId.GenerateNewStringId());
            envelope.Items["TraceInfo"] = traceInfo;

            this.Send(envelope);
        }

        #endregion
    }
}
