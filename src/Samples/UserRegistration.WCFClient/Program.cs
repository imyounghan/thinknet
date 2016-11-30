using System;
using System.Linq;
using ThinkNet.Contracts;
using UserRegistration.Commands;
using UserRegistration.ReadModel;

namespace UserRegistration.WCFClient
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceProxy.Mode = ServiceProxy.CommunicationMode.Wcf;

            Console.WriteLine("输入任意键开始演示...");
            Console.ReadKey();

            ServiceProxy.GetService<ICommandService>().Execute(new RegisterUser() {
                UserName = "hanyang",
                Password = "123456",
                LoginId = "young.han",
                Email = "19126332@qq.com"
            }, CommandReturnType.DomainEventHandled);

            Console.WriteLine("创建一个用户。");
            System.Threading.Thread.Sleep(2000);

            var result = ServiceProxy.GetService<IQueryService>().Execute(new FindAllUser()) as IQueryMultipleResult<UserModel>;
            Console.WriteLine("共有 {0} 个用户。", result.Count());
            System.Threading.Thread.Sleep(2000);

            var authentication = ServiceProxy.GetService<IQueryService>().Execute(new UserAuthentication()) as IQuerySingleResult<bool>;
            if(authentication.Result) {
                Console.WriteLine("用户名或密码错误");
            }
            else {
                Console.WriteLine("登录成功。");
            }
            Console.ReadKey();
        }
    }
}
