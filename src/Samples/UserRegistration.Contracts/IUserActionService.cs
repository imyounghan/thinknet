using System.ServiceModel;

namespace UserRegistration.Contracts
{
    [ServiceContract(Name = "UserActionService")]
    public interface IUserActionService
    {
        [OperationContract]
        void RegisterUser(UserInfo user);
    }
}
