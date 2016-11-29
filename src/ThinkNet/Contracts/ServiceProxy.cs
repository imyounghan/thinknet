using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThinkLib.Composition;
using ThinkNet.Contracts.Communication;

namespace ThinkNet.Contracts
{
    public class ServiceProxy
    {
        public enum CommunicationMode
        {
            Local,
            Ice,
            Wcf,
            Socket
        }


        public static TService GetService<TService>()
        {
            switch(Mode) {
                case CommunicationMode.Wcf:
                    return WcfClient.Instance.CreateService<TService>();
            }

            return ObjectContainer.Instance.Resolve<TService>();
        }

        /// <summary>
        /// 通讯方式
        /// </summary>
        public static CommunicationMode Mode { get; set; }
    }
}
