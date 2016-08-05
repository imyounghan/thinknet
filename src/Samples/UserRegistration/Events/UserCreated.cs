using System;
using System.Runtime.Serialization;
using ThinkNet.Messaging;

namespace UserRegistration.Events
{
    [DataContract]
    [Serializable]
    public class UserCreated : Event<Guid>
    {
        public UserCreated()
        { }

        public UserCreated(string loginId, string password, string userName, string email)
        {
            this.LoginId = loginId;
            this.Password = password;
            this.UserName = userName;
            this.Email = email;
        }

        [DataMember]
        public string LoginId { get; private set; }
        [DataMember]
        public string Password { get; private set; }
        [DataMember]
        public string UserName { get; private set; }
        [DataMember]
        public string Email { get; private set; }
    }
}
