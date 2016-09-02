using System.ServiceModel;

namespace UserRegistration.Contracts
{
    [ServiceContract(Name = "AuthenticationService")]
    public interface IAuthenticationService
    {
        [OperationContract]
        bool Authenticate(string loginid, string password, string ip);
    }   
}
