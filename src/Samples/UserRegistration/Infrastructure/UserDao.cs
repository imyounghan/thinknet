using System.Collections.Generic;
using System.Linq;
using MemberSample.Application;
using MemberSample.Domain;
using ThinkNet;
using ThinkNet.Components;
using ThinkNet.Storage;


namespace MemberSample.Infrastructure
{
    [RegisteredComponent(typeof(IUserDao))]
    public class UserDao : IUserDao
    {
        private readonly IDataContextFactory _contextFactory;
        public UserDao(IDataContextFactory contextFactory)
        {
            this._contextFactory = contextFactory;
        }

        public IEnumerable<UserDTO> GetAllUsers()
        {
            using (var context = _contextFactory.CreateDataContext()) {
                return context.CreateQuery<User>()
                    .AsEnumerable()
                    .Select(user => new UserDTO {
                        LoginId = user.LoginId,
                        Password = user.Password,
                        UserID = user.Id,
                        UserName = user.UserName
                    }).ToArray();
            }
        }
    }
}
