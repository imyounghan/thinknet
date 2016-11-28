using System.Collections.Generic;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Fetching;

namespace UserRegistration.ReadModel
{
    public class UserQueryExecutor : IQueryMultipleFetcher<FindAllData, UserModel>
    {
        private readonly IUserDao dao;

        public UserQueryExecutor(IUserDao userDao)
        {
            this.dao = userDao;
        }

        //public object Fetch(FindAllData parameter)
        //{
        //    return dao.GetAll();
        //}


        #region IQueryMultipleFetcher<FindAllData,UserModel> 成员

        public IEnumerable<UserModel> Fetch(FindAllData parameter)
        {
            return dao.GetAll();
        }

        #endregion
    }
}
