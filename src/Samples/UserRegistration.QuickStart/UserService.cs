using System.Collections.Generic;
using System.Linq;
using ThinkNet;
using ThinkNet.Messaging;
using UserRegistration.Contracts;
using UserRegistration.Events;

namespace UserRegistration.QuickStart
{
    [Register(typeof(IAuthenticationService))]
    [Register(typeof(IUserDao))]
    public class UserService : IAuthenticationService, IUserDao
    {
        private readonly IEventBus eventBus;
        public UserService(IEventBus eventBus)
        {
            this.eventBus = eventBus;
        }

        private readonly HashSet<UserModel> cache = new HashSet<UserModel>();

        #region IUserDao 成员
        public void Save(UserModel user)
        {
            cache.Add(user);
        }


        public UserModel Find(string loginid)
        {
            return this.GetAll().FirstOrDefault(p => p.LoginId == loginid);
        }

        public IEnumerable<UserModel> GetAll()
        {
            return cache;
        }

        #endregion

        #region IAuthenticationService 成员

        public bool Authenticate(string loginid, string password, string ip)
        {
            var user = this.Find(loginid);
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
