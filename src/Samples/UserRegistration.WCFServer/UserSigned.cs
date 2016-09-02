using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ThinkNet.Messaging;

namespace UserRegistration.Application
{
    [DataContract]
    [Serializable]
    public class UserSigned : Event
    {
        public UserSigned(string loginid, string clientIp)
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
