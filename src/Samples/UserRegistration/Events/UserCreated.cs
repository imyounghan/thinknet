using System;
using ThinkNet.Kernel;

namespace UserRegistration.Events
{
    [Serializable]
    public class UserCreated : VersionedEvent<Guid>
    {
        public UserCreated()
        { }

        public UserCreated(string loginId, string password, string userName, string email)
        {
            this.LoginId = loginId;
            this.Password = password;
            this.UserName = userName;
            this.Email = email;

            this.CreateTime = DateTime.Now;
        }

        public string LoginId { get; private set; }
        public string Password { get; private set; }
        public string UserName { get; private set; }
        public string Email { get; private set; }


        public DateTime CreateTime { get; private set; }
    }
}
