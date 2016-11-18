using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThinkNet.Common.Composition;

namespace ThinkNet.Contracts
{
    public class ClientProxy
    {
        public static TService CreateService<TService>()
        {


            return ObjectContainer.Instance.Resolve<TService>();
        }
    }
}
