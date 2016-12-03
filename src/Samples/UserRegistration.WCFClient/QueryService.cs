using System.ComponentModel.Composition;
using System.ServiceModel;
using System.Threading.Tasks;
using ThinkNet.Contracts;
using ThinkNet.Messaging;
using UserRegistration.ReadModel;

namespace UserRegistration
{
    [ServiceContract(Name = "QueryService", Namespace = "http://www.thinknet.com")]
    [DataContractFormat(Style = OperationFormatStyle.Rpc)]
    [ServiceKnownType(typeof(FindAllUser))]
    [ServiceKnownType(typeof(UserAuthentication))]
    [ServiceKnownType(typeof(QueryResultCollection<UserModel>))]
    [ServiceKnownType(typeof(QueryResultCollection<bool>))]
    [ServiceKnownType(typeof(QueryResult))]
    public interface IQueryClient
    {
        [OperationContract]
        IQueryResult Execute(IQuery query);
    }

    [Export(typeof(IQueryService))]
    public class QueryService : IQueryService
    {
        private readonly ChannelFactory<IQueryClient> channelFactory;

        public QueryService()
        {
            channelFactory = new ChannelFactory<IQueryClient>(new NetTcpBinding(), "net.tcp://127.0.0.1:9999/QueryService");
        }

        public IQueryResult Execute(IQuery query)
        {
            return channelFactory.CreateChannel().Execute(query);
        }

        public Task<IQueryResult> ExecuteAsync(IQuery query)
        {
            return Task.Factory.StartNew(() => this.Execute(query));
        }
    }
}
