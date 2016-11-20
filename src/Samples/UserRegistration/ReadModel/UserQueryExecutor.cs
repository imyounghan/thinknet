

using ThinkNet.Messaging;
using ThinkNet.Messaging.Fetching;

namespace UserRegistration.ReadModel
{
    public class UserQueryExecutor : IQueryFetcher<FindAllData>
    {
        private readonly IUserDao dao;

        public object Fetch(FindAllData parameter)
        {
            return dao.GetAll();
        }
    }
}
