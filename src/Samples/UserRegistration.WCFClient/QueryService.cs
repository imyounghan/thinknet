

namespace UserRegistration
{
    using System;
    using System.ComponentModel.Composition;
    using System.ServiceModel;

    using ThinkNet.Communication;
    using ThinkNet.Infrastructure;
    using ThinkNet.Messaging;

    [Export(typeof(IQueryService))]
    public class QueryService : IQueryService
    {
        private readonly ChannelFactory<IRequest> channelFactory;

        private readonly ITextSerializer serializer;

        public QueryService()
        {
            this.channelFactory = new ChannelFactory<IRequest>(new NetTcpBinding(), "net.tcp://127.0.0.1:9999/Request");
            this.serializer = new DefaultTextSerializer();
        }

        #region IQueryService 成员

        public IQueryResult<T> Execute<T>(IQuery query)
        {
            var queryResult = this.Execute(query);

            return new QueryResult<T>(queryResult);
        }


        public IQueryResult Execute(IQuery query)
        {
            var commandName = query.GetType().Name;
            var commandData = serializer.Serialize(query);

            var response = channelFactory.CreateChannel().Execute(commandName, commandData);

            var queryResult = new QueryResult()
                       {
                           ErrorMessage = response.ErrorMessage,
                           Status = (ExecutionStatus)response.Status
                       };

            if(!string.IsNullOrEmpty(response.Result)) {
                var type = Type.GetType(response.ResultType);
                queryResult.Data = serializer.Deserialize(response.Result, type);
            }

            return queryResult;
        }

        #endregion
    }
}
