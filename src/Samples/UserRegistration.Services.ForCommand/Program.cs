using System;
using ThinkNet.Configurations;

namespace UserRegistration.CommandService
{
    class Program
    {
        static void Main(string[] args)
        {
            Bootstrapper.Current.UsingKafka().Done();


            Console.WriteLine("这是一个表示消费命令的服务");
            Console.ReadKey();
        }
    }
}
