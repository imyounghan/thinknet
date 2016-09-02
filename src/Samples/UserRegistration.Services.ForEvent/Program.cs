using System;
using ThinkNet.Configurations;

namespace UserRegistration.EventService
{
    class Program
    {
        static void Main(string[] args)
        {
            Bootstrapper.Current.UsingKafka().Done();

            Console.WriteLine("这是一个表示消费事件的服务。");
            Console.ReadKey();
        }
    }
}
