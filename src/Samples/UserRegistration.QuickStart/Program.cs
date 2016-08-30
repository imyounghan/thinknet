using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThinkNet.Common;
using ThinkNet.Configurations;
using ThinkNet.Messaging;
using UserRegistration.Application;
using UserRegistration.Commands;
using UserRegistration.ReadModel;

namespace UserRegistration.QuickStart
{
    class Program
    {
        static void Main(string[] args)
        {
            Bootstrapper.Current.Done();


            var command = new RegisterUser {
                UserName = "老韩",
                Password = "hanyang",
                LoginId = "young.han",
                Email = "19126332@qq.com"
            };

            //var serializer = ServiceLocator.Current.GetInstance<ISerializer>();
            //var json = serializer.Serialize(command);
            //var dict = new Dictionary<string, string>() {
            //    { "Playload", json }
            //};
            //Console.WriteLine(serializer.Serialize(dict));

            Console.ReadKey();


            var commandService = ObjectContainer.Instance.Resolve<ICommandService>();
            commandService.Execute(command, CommandReturnType.DomainEventHandled);
            //int counter = 0;
            //var tasks = new System.Threading.Tasks.Task[5000];
            //var sw = new System.Diagnostics.Stopwatch();
            //sw.Start();
            //while (counter < 5000) {
            //    var userRegister = new RegisterUser {
            //        UserName = "老韩",
            //        Password = "hanyang",
            //        LoginId = "young.han",
            //        Email = "19126332@qq.com"
            //    };

            //    tasks[counter++] = manager.RegisterCommand(userRegister, CommandReplyType.DomainEventHandled);
            //}
            //System.Threading.Tasks.Task.WaitAll(tasks, TimeSpan.FromSeconds(30));
            //sw.Stop();
            //Console.WriteLine(sw.ElapsedMilliseconds);

            //Console.WriteLine(tasks.Where(p => p.IsCompleted).Count());

            var userDao = ObjectContainer.Instance.Resolve<IUserDao>();

            var count = userDao.GetAll().Count();
            Console.ResetColor();
            Console.WriteLine("共有 " + count + " 个用户。");

            var authenticationService = ObjectContainer.Instance.Resolve<IAuthenticationService>();
            if (!authenticationService.Authenticate("young.han", "hanyang", "127.0.0.1")) {
                Console.WriteLine("用户名或密码错误");
            }
            else {
                Console.WriteLine("登录成功。");
            }

            Console.ReadKey();
        }
    }
}
