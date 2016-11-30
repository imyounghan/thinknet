using System;
using ThinkLib;
using ThinkNet;
using ThinkNet.Contracts.Communication;

namespace UserRegistration.Application
{
    class Program
    {
        static void Main(string[] args)
        {
            ThinkNetBootstrapper.Current.DoneWithUnity();
            new WcfServer().Startup();

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("输入 'Exit' 退出服务 ...");
            Console.ReadKey();
        }
    }
}
