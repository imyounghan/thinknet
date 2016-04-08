using System;
using System.Runtime.Serialization;
using ThinkNet.Messaging;

namespace UserRegistration.Events
{
    [DataContract]
    [Serializable]
    public class UserLogined : Event
    {
        public UserLogined(string loginid, string clientIp)
        {
            this.LoginId = loginid;
            this.ClientIP = clientIp;
            this.LoginTime = DateTime.Now;
        }

        [DataMember]
        public string LoginId { get; private set; }
        [DataMember]
        public string ClientIP { get; private set; }
        [DataMember]
        public DateTime LoginTime { get; private set; }
    }
}
