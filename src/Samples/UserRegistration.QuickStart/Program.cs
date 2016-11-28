﻿using System;
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

            
            var commandService = ServiceProxy.CreateService<ICommandService>();
            var command = new RegisterUser {
                UserName = "hanyang",
                Password = "123456",
                LoginId = "young.han",
                Email = "19126332@qq.com"
            };
            commandService.Execute(command, CommandReturnType.DomainEventHandled);
            //int counter = 0;
            //var tasks = new System.Threading.Tasks.Task[5000];
            //var sw = new System.Diagnostics.Stopwatch();
            //sw.Start();
            //while (counter < 5000) {
            //    var userRegister = new RegisterUser {
            //        UserName = "hanyang",
            //        Password = "123456",
            //        LoginId = "young.han",
            //        Email = "19126332@qq.com"
            //    };

            //    tasks[counter++] = commandService.ExecuteAsync(userRegister, CommandReturnType.DomainEventHandled);
            //}
            //System.Threading.Tasks.Task.WaitAll(tasks, TimeSpan.FromSeconds(30));
            //sw.Stop();
            //Console.WriteLine("用时:{0}ms", sw.ElapsedMilliseconds);
            //Console.WriteLine("成功完成的命令数量：{0}", tasks.Where(p => p.IsCompleted).Count());

            System.Threading.Thread.Sleep(2000);

            var queryService = ServiceProxy.CreateService<IQueryService>();

            var result = queryService.Execute(new FindAllData()) as IQueryMultipleResult<UserModel>;
            Console.ResetColor();
            Console.WriteLine("共有 " + result.Count() + " 个用户。");

            //var authenticationService = ObjectContainer.Instance.Resolve<IAuthenticationService>();
            //if(!authenticationService.Authenticate("young.han", "123456", "127.0.0.1")) {
            //    Console.WriteLine("用户名或密码错误");
            //}
            //else {
            //    Console.WriteLine("登录成功。");
            //}

            Console.ReadKey();
        }
    }
}
