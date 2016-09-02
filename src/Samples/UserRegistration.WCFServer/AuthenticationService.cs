using ThinkNet;
using ThinkNet.Messaging;
using UserRegistration.Contracts;
using UserRegistration.ReadModel;

namespace UserRegistration.Application
{
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

            var userSigned = new UserSigned(loginid, ip);
            eventBus.Publish(userSigned);

            return true;
        }

        #endregion
    }
}
