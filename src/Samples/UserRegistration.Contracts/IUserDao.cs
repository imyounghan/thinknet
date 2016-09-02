using System.Collections.Generic;
using System.ServiceModel;

namespace UserRegistration.Contracts
{
    [ServiceContract(Name = "UserQueryService")]
    public interface IUserDao
    {
        void Save(UserModel user);

        [OperationContract]
        UserModel Find(string loginid);

        [OperationContract]
        IEnumerable<UserModel> GetAll();
    }
}
