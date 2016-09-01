using System;
using ThinkNet.Configurations;
using ThinkNet.Messaging;
using UserRegistration.Commands;

namespace UserRegistration.ApplicationService
{
    class Program
    {
        static void Main(string[] args)
        {
            Bootstrapper.Current.UsingKafka().Done();


            Console.WriteLine("输入任意键继续...");
            Console.ReadKey();


            var commandService = ObjectContainer.Instance.Resolve<ICommandService>();
            var command = new RegisterUser {
                UserName = "hanyang",
                Password = "123456",
                LoginId = "young.han",
                Email = "19126332@qq.com"
            };
            commandService.ExecuteAsync(command, CommandReturnType.DomainEventHandled)
                .ContinueWith(task => {
                    Console.Write("创建客户：");
                    Console.WriteLine(task.Result.Status);
                });
            

            Console.ReadKey();
        }
    }
}
