using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThinkLib.Composition;

namespace ThinkNet.Contracts
{
    public class ServiceProxy
    {
        public static TService CreateService<TService>()
        {


            return ObjectContainer.Instance.Resolve<TService>();
        }
    }
}
