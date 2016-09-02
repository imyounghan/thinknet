using System.Collections.Generic;
using System.ServiceModel;

namespace UserRegistration.Contracts
{
    [ServiceContract(Name = "UserQueryService")]
    public interface IUserQueryService
    {
        [OperationContract]
        UserInfo FindByLoginid(string loginid);
        [OperationContract]
        IEnumerable<UserInfo> FindAll();
    }
}
