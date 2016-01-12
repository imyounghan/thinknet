using System;
using ThinkNet.Messaging;

namespace UserRegistration.Commands
{
    [Serializable]
    public class RegisterUser : Command
    {
        public string LoginId { get; set; }

        public string Password { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }
    }
}
