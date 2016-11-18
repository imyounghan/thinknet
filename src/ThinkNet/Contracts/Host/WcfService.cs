using System;
using System.Collections.Generic;
using System.ServiceModel;
using ThinkNet.Common.Composition;

namespace ThinkNet.Contracts.Host
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

        public void Run()
        {
            var hosts = new List<ServiceHost>();
            hosts.Add(CreateServiceHost(typeof(ICommandService), "CommandService", ObjectContainer.Instance.Resolve<ICommandService>()));
            hosts.Add(CreateServiceHost(typeof(IQueryService), "QueryService", ObjectContainer.Instance.Resolve<IQueryService>()));

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("输入 'Exit' 退出服务 ...");
            while (Console.ReadLine() == "Exit") {
                foreach (var host in hosts) {
                    using (host as IDisposable) { }
                }
            }
        }
    }
}
