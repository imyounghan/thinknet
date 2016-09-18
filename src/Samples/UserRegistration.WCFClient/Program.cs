using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using UserRegistration.Contracts;

namespace UserRegistration.WCFClient
{
    class Program
    {
        static void Main(string[] args)
        {

            var channelFactory = new ChannelFactory<IUserQueryService>("UserQueryService");

            var userQueryService = channelFactory.CreateChannel();

            var result = userQueryService.FindAll();
        }
    }
}
