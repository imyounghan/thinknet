using System.Collections.Generic;
using System.Linq;
using ThinkNet;
using UserRegistration.Contracts;
using UserRegistration.ReadModel;

namespace UserRegistration.Application
{
    [Register(typeof(IUserQueryService))]
    public class UserQueryService : IUserQueryService
    {
        private readonly IUserDao _userDao;
        public UserQueryService(IUserDao userDao)
        {
            this._userDao = userDao;
        }

        #region IUserQueryService 成员

        public UserInfo FindByLoginid(string loginid)
        {
            var user = _userDao.Find(loginid);
            if (user == null)
                return null;

            return new UserInfo {
                LoginId = user.LoginId,
                UserName = user.UserName
            };
        }

        public IEnumerable<UserInfo> FindAll()
        {
            return _userDao.GetAll()
                .Select(user => new UserInfo {
                    LoginId = user.LoginId,
                    UserName = user.UserName
                }).ToArray();
        }

        #endregion
    }
}
