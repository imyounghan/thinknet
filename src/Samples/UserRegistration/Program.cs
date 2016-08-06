using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.ServiceLocation;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
using UserRegistration.Application;
using UserRegistration.Commands;
using UserRegistration.ReadModel;

namespace UserRegistration
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
            

            Console.ReadKey();


            var manager = ServiceLocator.Current.GetInstance<ICommandResultManager>();
            manager.RegisterCommand(command, CommandResultType.DomainEventHandled).Wait();
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

            var userDao = ServiceLocator.Current.GetInstance<IUserDao>();

            var count = userDao.GetAll().Count();
            Console.ResetColor();
            Console.WriteLine("共有 " + count + " 个用户。");

            var authenticationService = ServiceLocator.Current.GetInstance<IAuthenticationService>();
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
