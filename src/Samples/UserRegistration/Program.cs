using System;
using System.Linq;
using Microsoft.Practices.ServiceLocation;
using ThinkNet.Configurations;
using ThinkNet.Messaging;
using ThinkLib.Common;
using UserRegistration.Application;
using UserRegistration.Commands;
using UserRegistration.ReadModel;
using ThinkNet.Infrastructure;
using ThinkLib.Scheduling;

namespace UserRegistration
{
    class Program
    {
        static void Main(string[] args)
        {
            int counter = 0;

            //var broker = MessageBrokerFactory.Instance.GetOrCreate("message");
            //var worker = WorkerFactory.Create<Message>(broker.Take, (msg) => {
            //    //if (msg.IsNull())
            //    //    return;
            //    //Console.WriteLine(msg);
            //}, broker.Complete);
            //worker.Start();

            //while (counter++ < 1000) {
            //    var userRegister = new RegisterUser {
            //        UserName = "老韩",
            //        Password = "hanyang",
            //        LoginId = "young.han",
            //        Email = "19126332@qq.com"
            //    };

            //    broker.TryAdd(new Message {
            //        Body = userRegister,
            //        MetadataInfo = null,
            //        RoutingKey = string.Empty,
            //        CreatedTime = DateTime.UtcNow
            //    });
            //    //task.Wait();
            //}
            //Console.WriteLine("over");

            //Console.ReadKey();
            //ConfigurationSetting.Current.QueueCount = 1;

            Bootstrapper.Current.StartThinkNet().DoneWithUnity();

            //Dictionary<string, string> dict = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            counter = 0;
            //while (counter++ < 1000) {
            //    var id = ObjectId.GenerateNewId().ToString();
            //    dict.Add(id, id);
            //    Console.WriteLine(id);
            //}


            var manager = ServiceLocator.Current.GetInstance<ICommandResultManager>();
            var tasks = new System.Threading.Tasks.Task[1000];
            var sw =new System.Diagnostics.Stopwatch();
            sw.Start();
            while (counter < 1000) {
                var userRegister = new RegisterUser {
                    UserName = "老韩",
                    Password = "hanyang",
                    LoginId = "young.han",
                    Email = "19126332@qq.com"
                };

                tasks[counter++] = manager.RegisterCommand(userRegister, CommandReplyType.CommandExecuted);
                //task.Wait();
            }
            System.Threading.Tasks.Task.WaitAll(tasks, TimeSpan.FromSeconds(30));
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);

            Console.WriteLine(tasks.Where(p => p.IsCompleted).Count()); 

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
