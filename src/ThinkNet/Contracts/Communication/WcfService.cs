using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using ThinkLib.Composition;

namespace ThinkNet.Contracts.Communication
{
    public class WcfService
    {
        static ServiceHost CreateServiceHost(Type type, string name, object instance)
        {
            //var httpUri = new Uri(string.Concat("http://127.0.0.1:8082/", name));
            var tcpUri = new Uri(string.Concat("net.tcp://127.0.0.1:8081/", name));

            var host = new ServiceHost(instance);
            //host.AddServiceEndpoint(type, new BasicHttpBinding(), string.Empty);
            host.AddServiceEndpoint(type, new NetTcpBinding(), tcpUri);
            //host.Description.Behaviors.Find<ServiceMetadataBehavior>()
            //var behavior  = new ServiceMetadataBehavior();
            //behavior.HttpGetEnabled = true;
            //host.Description.Behaviors.Add(behavior);
            host.Opened += (sender, e) => {
                Console.WriteLine("{0}服务已经启用。", name);
            };

            host.Open();

            return host;
        }

        static ServiceHost CreateServiceHost(Type serviceType)
        {
            var instance = ObjectContainer.Instance.Resolve(serviceType);
            var attribute = serviceType.GetAttribute<DataContractAttribute>(false);
            var name = attribute == null ? serviceType.Name.Substring(1) : attribute.Name;

            return CreateServiceHost(serviceType, name, instance);
        }

        private readonly List<Type> serviceTypes;
        public WcfService()
        {
            this.serviceTypes = new List<Type>() {
                { typeof(ICommandService) },
                { typeof(IQueryService) }
            };
        }

        public WcfService(IEnumerable<Type> types)
        {
            var filteredTypes = types.Where(type => type.IsInterface && type.IsDefined<DataContractAttribute>(false));
            this.serviceTypes = new List<Type>(filteredTypes);
        }
        

        public void Run()
        {
            var hosts = serviceTypes.Select(CreateServiceHost).ToArray();

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("输入 'Exit' 退出服务 ...");
            while (Console.ReadLine() == "Exit") {
                foreach (var host in hosts) {
                    using (host as IDisposable) { }
                }

                break;
            }
        }
    }
}
