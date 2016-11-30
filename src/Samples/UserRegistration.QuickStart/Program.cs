using System;
using System.Linq;
using ThinkLib;
using ThinkNet;
using ThinkNet.Contracts;
using UserRegistration.Commands;
using UserRegistration.ReadModel;

namespace UserRegistration.QuickStart
{
    class Program
    {
        static void Main(string[] args)
        {
            ThinkNetBootstrapper.Current.DoneWithUnity();



            Console.WriteLine("输入任意键演示...");
            Console.ReadKey();


            var commandService = ServiceProxy.GetService<ICommandService>();
            commandService.Execute(new RegisterUser {
                UserName = "hanyang",
                Password = "123456",
                LoginId = "young.han",
                Email = "19126332@qq.com"
            }, CommandReturnType.DomainEventHandled);
            //int counter = 0;
            //var tasks = new System.Threading.Tasks.Task[5000];
            //var sw = new System.Diagnostics.Stopwatch();
            //sw.Start();
            //while(counter < 5000) {
            //    var userRegister = new RegisterUser {
            //        UserName = "hanyang",
            //        Password = "123456",
            //        LoginId = "young.han," + counter.ToString(),
            //        Email = "19126332@qq.com"
            //    };

            //    tasks[counter++] = commandService.ExecuteAsync(userRegister, CommandReturnType.DomainEventHandled);
            //}
            //System.Threading.Tasks.Task.WaitAll(tasks);
            //sw.Stop();
            //Console.WriteLine("用时:{0}ms", sw.ElapsedMilliseconds);
            //Console.WriteLine("成功完成的命令数量：{0}", tasks.Where(p => p.IsCompleted).Count());

            System.Threading.Thread.Sleep(2000);

            var queryService = ServiceProxy.GetService<IQueryService>();

            var result = queryService.Execute(new FindAllUser()) as IQueryMultipleResult<UserModel>;
            //Console.ResetColor();
            Console.WriteLine("共有 " + result.Count() + " 个用户。");

            var authentication = queryService.Execute(new UserAuthentication() {
                LoginId = "young.han",
                Password = "123456",
                IpAddress = "127.0.0.1"
            }) as IQuerySingleResult<bool>;
            if(authentication.Result) {
                Console.WriteLine("登录成功。");
            }
            else {
                Console.WriteLine("用户名或密码错误。");
            }

            Console.ReadKey();
        }
    }
}
