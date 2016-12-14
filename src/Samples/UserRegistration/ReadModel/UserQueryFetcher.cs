using System.Collections.Generic;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Fetching;
using UserRegistration.Events;

namespace UserRegistration.ReadModel
{
    public class UserQueryFetcher : 
        IQueryMultipleFetcher<FindAllUser, UserModel>,
        IQueryFetcher<UserAuthentication, bool>
    {
        private readonly IUserDao dao;
        private readonly IMessageBus bus;

        public UserQueryFetcher(IUserDao userDao, IMessageBus messageBus)
        {
            this.dao = userDao;
            this.bus = messageBus;
        }

        //public object Fetch(FindAllData parameter)
        //{
        //    return dao.GetAll();
        //}


        #region IQueryMultipleFetcher<FindAllData,UserModel> 成员

        public IEnumerable<UserModel> Fetch(FindAllUser parameter)
        {
            return dao.GetAll();
        }

        #endregion

        #region IQueryFetcher<UserAuthentication,bool> 成员

        public bool Fetch(UserAuthentication parameter)
        {
            var user = dao.Find(parameter.LoginId);
            if(user == null)
                return false;

            if(user.Password != parameter.Password)
                return false;

            var userSigned = new UserSigned(parameter.LoginId, parameter.IpAddress);
            bus.PublishAsync(userSigned);

            return true;
        }

        #endregion
    }
}
