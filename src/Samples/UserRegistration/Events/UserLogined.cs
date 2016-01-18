using System;
using ThinkNet.Kernel;

namespace UserRegistration.Events
{
    [Serializable]
    public class UserLogined : Event<Guid>
    {
        public UserLogined(string loginid, string clientIp)
        {
            this.LoginId = loginid;
            this.ClientIP = clientIp;
            this.LoginTime = DateTime.Now;
        }

        public string LoginId { get; private set; }


        public string ClientIP { get; private set; }

        public DateTime LoginTime { get; private set; }
    }
}
