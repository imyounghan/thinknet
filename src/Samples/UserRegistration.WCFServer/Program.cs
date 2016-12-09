using System;
using System.ServiceModel;
using ThinkNet;

namespace UserRegistration.Application
{
    class Program
    {
        static ServiceHost CreateServiceHost(Type type)
        {
            var host = new ServiceHost(type);
            host.AddServiceEndpoint(type, new NetTcpBinding(), 
                new Uri("net.tcp://127.0.0.1:9999/".AfterContact(type.Name)));

            host.Opened += (sender, e) => {
                Console.WriteLine("{0} Started.", type.Name);
            };
            
            return host;
        }

        static void Main(string[] args)
        {
            Bootstrapper.Current.UsingKafka().DoneWithUnity();

            var hosts = new ServiceHost[] {
                CreateServiceHost(typeof(CommandService)),
                CreateServiceHost(typeof(QueryService))
            };

            hosts.ForEach(host => host.Open());


            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("type 'ESC' to exit service...");
            while(Console.ReadLine() == "ESC") {
                hosts.ForEach(host => host.Close());
            }
        }
    }
}
