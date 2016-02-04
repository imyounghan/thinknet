using System;
using System.Runtime.Serialization;
using ThinkNet.Kernel;

namespace UserRegistration.Events
{
    [DataContract]
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

        [DataMember]
        public string LoginId { get; private set; }
        [DataMember]
        public string Password { get; private set; }
        [DataMember]
        public string UserName { get; private set; }
        [DataMember]
        public string Email { get; private set; }

        [DataMember]
        public DateTime CreateTime { get; private set; }
    }
}
