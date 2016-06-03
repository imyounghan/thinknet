using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThinkNet.Messaging.Handling;
using UserRegistration.Commands;

namespace UserRegistration.Handlers
{
    public class CommandInterception : IInterceptor<RegisterUser>
    {
        #region IMessageInterception<RegisterUser> 成员

        public void OnHandlerExecuting(RegisterUser message)
        {
            //Console.WriteLine("Before Handle");
        }

        public void OnHandlerExecuted(RegisterUser message, Exception exception)
        {
            //Console.WriteLine("After Handle");
        }

        #endregion
    }
}
