using System;
using System.ServiceModel;

namespace UserRegistration.Contracts
{
    [DataContractFormat(Style = OperationFormatStyle.Rpc)]
    public class UserInfo
    {
        //public Guid UserID { get; set; }

        public string LoginId { get; set; }

        public string Password { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }
    }
}
