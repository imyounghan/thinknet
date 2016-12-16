using System;
using System.Threading;
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
            Bootstrapper.Current.DoneWithUnity();


            //var serializer = ObjectContainer.Instance.Resolve<ITextSerializer>();
            //var serialized = serializer.Serialize(new RegisterUser {
            //    UserName = "hanyang",
            //    Password = "123456",
            //    LoginId = "young.han",
            //    Email = "19126332@qq.com"
            //}, true);
            //Console.WriteLine(serialized);


            Console.WriteLine("输入任意键演示...");
            Console.ReadKey();


            Console.WriteLine("开始添加用户...");
            var commandService = ServiceGateway.Current.GetService<ICommandService>();
            commandService.Execute(new RegisterUser {
                UserName = "hanyang",
                Password = "123456",
                LoginId = "young.han",
                Email = "19126332@qq.com"
            }, CommandReturnMode.DomainEventHandled);
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
