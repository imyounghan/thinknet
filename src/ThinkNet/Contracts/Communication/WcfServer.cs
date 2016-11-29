using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using ThinkLib;
using ThinkLib.Composition;

namespace ThinkNet.Contracts.Communication
{
    public class WcfServer : IServer
    {
        static ServiceHost CreateServiceHost(Type type, string name, object instance)
        {
            var host = new ServiceHost(instance);
            if(WcfSetting.Scheme == WcfSetting.BindingMode.Http) {
                var httpUri = new Uri(string.Format("http://{0}:{1}/{2}", WcfSetting.IpAddress, WcfSetting.Port, name));
                host.AddServiceEndpoint(type, new BasicHttpBinding(), httpUri);
                //host.Description.Behaviors.Find<ServiceMetadataBehavior>();
                //var behavior  = new ServiceMetadataBehavior();
                //behavior.HttpGetEnabled = true;
                //host.Description.Behaviors.Add(behavior);
            }
            if(WcfSetting.Scheme == WcfSetting.BindingMode.Tcp) {
                var tcpUri = new Uri(string.Format("net.tcp://{0}:{1}/{2}", WcfSetting.IpAddress, WcfSetting.Port, name));
                host.AddServiceEndpoint(type, new NetTcpBinding(), tcpUri);
            }
            
            host.Opened += (sender, e) => {
                Console.WriteLine("{0}服务已经启用。", name);
            };

            host.Open();

            return host;
        }

        static ServiceHost CreateServiceHost(Type serviceType)
        {
            var instance = ObjectContainer.Instance.Resolve(serviceType);
            var attribute = serviceType.GetCustomAttribute<ServiceContractAttribute>(false);
            var contractName = (string)null;
            if(attribute == null) {
                contractName = serviceType.Name.StartsWith("I") ? serviceType.Name.Substring(1) : serviceType.Name;
            }
            else {
                contractName = attribute.Name;
            }

            return CreateServiceHost(serviceType, contractName, instance);
        }

        private readonly IEnumerable<Type> serviceTypes;
        private IEnumerable<ServiceHost> serviceHosts;
        public WcfServer()
        {
            this.serviceTypes = new Type[] {
                 typeof(ICommandService),
                 typeof(IQueryService)
            };
        }

        public WcfServer(IEnumerable<Type> types)
        {
            this.serviceTypes = types.Where(type => type.IsDefined<ServiceContractAttribute>(false));
        }
        

        #region IServer 成员

        public void Startup()
        {
            this.serviceHosts = serviceTypes.Select(CreateServiceHost).ToArray();
        }

        public void Shutdown()
        {
            foreach(var host in serviceHosts) {
                using(host as IDisposable) 
                { }
            }
        }

        #endregion
    }
}
