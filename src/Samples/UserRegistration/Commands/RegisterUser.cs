using System;
using System.Runtime.Serialization;
using ThinkNet.Messaging;

namespace UserRegistration.Commands
{
    [DataContract(Namespace = "UserRegistration.Commands", Name = "RegisterUser")]
    [Serializable]
    public class RegisterUser : Command
    {
        [DataMember]
        public string LoginId { get; set; }
        [DataMember]
        public string Password { get; set; }
        [DataMember]
        public string UserName { get; set; }
        [DataMember]
        public string Email { get; set; }
    }
}
