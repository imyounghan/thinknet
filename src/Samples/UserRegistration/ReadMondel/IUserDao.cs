using System.Collections.Generic;
using System.Linq;
using ThinkNet;
using ThinkNet.Database;

namespace UserRegistration.ReadModel
{
    public interface IUserDao
    {
        void Save(UserModel user);

        UserModel Find(string loginid);

        IEnumerable<UserModel> GetAll();
    }

    [Register(typeof(IUserDao))]
    public class UserDao : IUserDao
    {
        private readonly IDataContextFactory _dataContextFactory;

        public UserDao(IDataContextFactory dataContextFactory)
        {
            this._dataContextFactory = dataContextFactory;
        }

        #region IUserDao 成员
        public void Save(UserModel user)
        {
            using(var context = _dataContextFactory.CreateDataContext()){
                context.Save(user);
                context.Commit();
            }
        }


        public UserModel Find(string loginid)
        {
            using (var context = _dataContextFactory.CreateDataContext()) {
                return context.Find<UserModel>(loginid);
            }
        }

        public IEnumerable<UserModel> GetAll()
        {
            using (var context = _dataContextFactory.CreateDataContext()) {
                return context.CreateQuery<UserModel>();
            }
        }

        #endregion
    }
}
