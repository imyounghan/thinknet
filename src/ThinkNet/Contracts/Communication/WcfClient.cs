using System;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.ServiceModel.Channels;
using ThinkLib;

namespace ThinkNet.Contracts.Communication
{
    public class WcfClient : DisposableObject, IClient
    {
        public static readonly WcfClient Instance = new WcfClient();


        public readonly ConcurrentDictionary<Type, IChannelFactory> channelFactories;

        private WcfClient()
        {
            this.channelFactories = new ConcurrentDictionary<Type, IChannelFactory>();
        }

        public TService CreateService<TService>()
        {
            var serviceType = typeof(TService);

            if(!serviceType.IsDefined<ServiceContractAttribute>(false)) {
                throw new InvalidMessageContractException();
            }

            var channel = channelFactories.GetOrAdd(serviceType, BuildChannel<TService>) as ChannelFactory<TService>;

            return channel.CreateChannel();
        }

        private IChannelFactory BuildChannel<TService>()
        {
            var serviceType = typeof(TService);
            var attribute = serviceType.GetCustomAttribute<ServiceContractAttribute>(false);
            var contractName = (string)null;
            if(attribute == null) {
                contractName = serviceType.Name.StartsWith("I") ? serviceType.Name.Substring(1) : serviceType.Name;
            }
            else {
                contractName = attribute.Name;
            } 

            switch(WcfSetting.Scheme) {
                case WcfSetting.BindingMode.Tcp:
                    var tcpUri = string.Format("net.tcp://{0}:{1}/{2}", WcfSetting.IpAddress, WcfSetting.Port, contractName);
                    return new ChannelFactory<TService>(new NetTcpBinding(), tcpUri);
                case WcfSetting.BindingMode.Http:
                    var httpUri = string.Format("http://{0}:{1}/{2}", WcfSetting.IpAddress, WcfSetting.Port, contractName);
                    return new ChannelFactory<TService>(new WSHttpBinding(), httpUri);
                default:
                    throw new InvalidChannelBindingException();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing) {
                foreach(var channelFactory in channelFactories.Values) {
                    using(channelFactory as IDisposable) 
                    { }
                }
            }
        }
    }
}
