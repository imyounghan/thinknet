using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Description;
using ThinkNet.Configurations;
using ThinkNet.Database;
using ThinkNet.Messaging;
using UserRegistration.Contracts;

namespace UserRegistration.Application
{
    class Program
    {

        static ServiceHost CreateServiceHost(Type type, string name, object instance)
        {
            //var httpUri = new Uri(string.Concat("http://127.0.0.1:8082/", name));
            var tcpUri = new Uri(string.Concat("net.tcp://localhost:8081/", name));

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

        static void Main(string[] args)
        {
            Bootstrapper.Current.Register<IDataContextFactory, MemoryContextFactory>().UsingKafka().Done();
            
            var hosts = new List<ServiceHost>();
            //hosts.Add(CreateServiceHost(typeof(ICommandService), "CommandService", ObjectContainer.Instance.Resolve<ICommandService>()));
            hosts.Add(CreateServiceHost(typeof(IAuthenticationService), "AuthenticationService", ObjectContainer.Instance.Resolve<IAuthenticationService>()));
            hosts.Add(CreateServiceHost(typeof(IUserActionService), "UserActionService", ObjectContainer.Instance.Resolve<IUserActionService>()));
            hosts.Add(CreateServiceHost(typeof(IUserQueryService), "UserQueryService", ObjectContainer.Instance.Resolve<IUserQueryService>()));

            //Console.WriteLine("输入任意键继续...");
            Console.ReadKey();

            //var commandService = ObjectContainer.Instance.Resolve<ICommandService>();
            //var command = new RegisterUser {
            //    UserName = "hanyang",
            //    Password = "123456",
            //    LoginId = "young.han",
            //    Email = "19126332@qq.com"
            //};
            //commandService.ExecuteAsync(command, CommandReturnType.DomainEventHandled)
            //    .ContinueWith(task => {
            //        Console.Write("创建客户：");
            //        Console.WriteLine(task.Result.Status);
            //    });
            

            //Console.ReadKey();
        }
    }
}
