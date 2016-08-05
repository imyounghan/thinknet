using ThinkNet;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
using UserRegistration.Events;
using UserRegistration.ReadModel;

namespace UserRegistration.Application
{
    public interface IAuthenticationService
    {
        bool Authenticate(string loginid, string password, string ip);
    }

    [Register(typeof(IAuthenticationService))]
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserDao userDao;
        private readonly IEventBus eventBus;
        public AuthenticationService(IUserDao userDao, IEventBus eventBus)
        {
            this.userDao = userDao;
            this.eventBus = eventBus;
        }

        #region IAuthenticationService 成员

        public bool Authenticate(string loginid, string password, string ip)
        {
            var user = userDao.Find(loginid);
            if (user == null)
                return false;

            if (user.Password != password)
                return false;

            var userLogined = new UserLogined(loginid, ip);
            eventBus.Publish(userLogined);

            return true;
        }

        #endregion
    }
}
