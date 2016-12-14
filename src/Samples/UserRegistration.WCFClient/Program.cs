using System;
using System.Threading;
using ThinkNet.Contracts;
using UserRegistration.Commands;
using UserRegistration.ReadModel;

namespace UserRegistration.Application
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceGateway.SetGatewayProvider(() => new WcfServiceGateway());

            Console.WriteLine("输入任意键开始演示...");
            Console.ReadKey();

            var commandService = ServiceGateway.Current.GetService<ICommandService>();
            commandService.Execute(new RegisterUser() {
                UserName = "hanyang",
                Password = "123456",
                LoginId = "young.han",
                Email = "19126332@qq.com"
            }, CommandReturnMode.DomainEventHandled);

            Console.WriteLine("创建一个用户。");
            Thread.Sleep(2000);

            var queryService = ServiceGateway.Current.GetService<IQueryService>();
            var result = queryService.Execute(new FindAllUser()) as IQueryResultCollection<UserModel>;
            Console.WriteLine("共有 {0} 个用户。", result == null ? 0 : result.Count);
            Thread.Sleep(2000);

            var authentication = queryService.Execute(new UserAuthentication() {
                LoginId = "young.han",
                Password = "123456",
                IpAddress = "127.0.0.1"
            }) as IQueryResultCollection<bool>;
            if(authentication != null && authentication.Count == 1 && authentication[0]) {
                Console.WriteLine("登录成功。");
            }
            else {
                Console.WriteLine("用户名或密码错误。");
            }

            Console.ReadKey();
        }
    }
}
