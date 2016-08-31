﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            Console.WriteLine("是否启用Kafka？Yes(Y)/No(N)");
            if (Console.ReadLine() == "Y") {
                Bootstrapper.Current.UsingKafka().Done();
            }
            else {
                Bootstrapper.Current.Done();
            }            
            

            Console.WriteLine("输入任意键继续...");
            Console.ReadKey();

            
            var commandService = ObjectContainer.Instance.Resolve<ICommandService>();
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
            //Console.WriteLine(sw.ElapsedMilliseconds);
            //Console.WriteLine(tasks.Where(p => p.IsCompleted).Count());

            System.Threading.Thread.Sleep(2000);

            var userDao = ObjectContainer.Instance.Resolve<IUserDao>();

            var count = userDao.GetAll().Count();
            Console.ResetColor();
            Console.WriteLine("共有 " + count + " 个用户。");

            var authenticationService = ObjectContainer.Instance.Resolve<IAuthenticationService>();
            if (!authenticationService.Authenticate("young.han", "123456", "127.0.0.1")) {
                Console.WriteLine("用户名或密码错误");
            }
            else {
                Console.WriteLine("登录成功。");
            }

            Console.ReadKey();
        }
    }
}
