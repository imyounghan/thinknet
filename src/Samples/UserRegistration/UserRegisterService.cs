using System;
using UserRegistration.ReadModel;

namespace UserRegistration
{
    public class UserRegisterService
    {
        private readonly IUserDao _userDao;
        public UserRegisterService(IUserDao userDao)
        {
            this._userDao = userDao;
        }


        public User Register(string loginId, string password, string userName, string email)
        {
            if (_userDao.Find(loginId) != null) {
                throw new AggregateException("用户名已存在！");
            }

            return new User(loginId, password, userName, email);
        }
    }
}
