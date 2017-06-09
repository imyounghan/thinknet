using ThinkNet.Messaging;

namespace UserRegistration.ReadModel
{
    public class UserAuthentication : IQuery
    {
        public string LoginId { get; set; }

        public string Password { get; set; }

        public string IpAddress { get; set; }
    }
}
