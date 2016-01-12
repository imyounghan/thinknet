using System.Collections.Generic;


namespace UserRegistration.ReadModel
{
    public interface IUserDao
    {
        IEnumerable<UserModel> GetAllUsers();
    }
}
