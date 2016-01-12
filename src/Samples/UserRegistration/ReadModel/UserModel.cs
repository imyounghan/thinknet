using System;

namespace UserRegistration.ReadModel
{
    public class UserModel
    {
        public Guid UserID { get; set; }

        public string LoginId { get; set; }

        public string Password { get; set; }

        public string UserName { get; set; }
    }
}
