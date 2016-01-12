
namespace UserRegistration.Application
{
    public interface IAuthenticationService
    {
        bool Authenticate(string loginid, string password);
    }
}
