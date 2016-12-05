using System.ServiceModel;
using ThinkNet.Contracts;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
using UserRegistration.ReadModel;

namespace UserRegistration
{
    [ServiceContract(Name = "QueryService", Namespace = "http://www.thinknet.com")]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    [DataContractFormat(Style = OperationFormatStyle.Rpc)]
    [ServiceKnownType(typeof(FindAllUser))]
    [ServiceKnownType(typeof(UserAuthentication))]
    [ServiceKnownType(typeof(QueryResultCollection<UserModel>))]
    [ServiceKnownType(typeof(QueryResultCollection<bool>))]
    [ServiceKnownType(typeof(QueryResult))]
    public class QueryService
    {
        private readonly IQueryService realService;

        public QueryService()
        {
            realService = ObjectContainer.Instance.Resolve<IQueryService>();
        }

        [OperationContract]
        public IQueryResult Execute(IQuery query)
        {
            return realService.Execute(query);
        }
    }
}
