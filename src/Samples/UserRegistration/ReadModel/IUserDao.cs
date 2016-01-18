using System.Collections.Generic;
using System.Linq;
using ThinkLib.Common;


namespace UserRegistration.ReadModel
{
    [RequiredComponent(typeof(UserDao))]
    public interface IUserDao
    {
        UserModel Find(string loginid);

        IEnumerable<UserModel> GetAll();
    }

    public class UserDao : IUserDao
    {
        //public readonly static UserDao Instance = new UserDao();

        private readonly HashSet<UserModel> collection = new HashSet<UserModel>();
        public void Save(UserModel userModel)
        {
            collection.Add(userModel);
        }

        #region IUserDao 成员

        public UserModel Find(string loginid)
        {
            return collection.FirstOrDefault(p => p.LoginId == loginid);
        }

        public IEnumerable<UserModel> GetAll()
        {
            return collection;
        }

        #endregion
    }
}
