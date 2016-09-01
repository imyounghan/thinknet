using System;
using ThinkNet.Configurations;

namespace UserRegistration.EventService
{
    class Program
    {
        static void Main(string[] args)
        {
            Bootstrapper.Current.UsingKafka().Done();

            Console.ReadKey();
        }
    }
}
