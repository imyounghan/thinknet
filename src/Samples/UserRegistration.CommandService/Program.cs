using System;
using ThinkNet.Configurations;

namespace UserRegistration.CommandService
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
