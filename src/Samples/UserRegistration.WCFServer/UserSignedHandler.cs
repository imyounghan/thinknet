using System;
using ThinkNet.Messaging.Handling;

namespace UserRegistration.Application
{
    public class UserSignedHandler : IMessageHandler<UserSigned>
    {
        #region IMessageHandler<UserSigned> 成员

        public void Handle(UserSigned message)
        {
            Console.WriteLine("签名成功并记录登录日志");
        }

        #endregion
    }
}
