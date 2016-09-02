using System.Collections.Generic;
using System.Linq;
using ThinkNet;
using UserRegistration.Contracts;

namespace UserRegistration.QuickStart
{
    [Register(typeof(IUserDao))]
    public class UserDao : IUserDao
    {
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
    }
}
