

namespace UserRegistration.Application
{
    using System;
    using System.ServiceModel;

    using ThinkNet;
    using ThinkNet.Communication;
    using ThinkNet.Infrastructure;

    class Program
    {
        static void Main(string[] args)
        {
            Bootstrapper.Current.SetDefault<WcfRequestService>().Done();

            using(var host = new ServiceHost(ObjectContainer.Instance.Resolve<WcfRequestService>()))
            {
                host.AddServiceEndpoint(
                    typeof(WcfRequestService),
                    new NetTcpBinding(),
                    new Uri("net.tcp://127.0.0.1:9999/Request"));

                host.Opened += (sender, e) => {
                    Console.WriteLine("WCF Request Service Started.");
                };

                host.Open();

                Console.WriteLine("type 'ESC' key to exit service...");
                while(Console.ReadKey().Key == ConsoleKey.Escape) {
                    host.Close();
                    break;
                }
            }
        }
    }
}
