using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace UserRegistration.Contracts
{
    [DataContractFormat(Style = OperationFormatStyle.Document)]
    [DataContract]
    public class UserInfo
    {
        //public Guid UserID { get; set; }
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
