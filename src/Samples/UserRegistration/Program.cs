using System;
using System.Linq;
using Microsoft.Practices.ServiceLocation;
using ThinkNet.Configurations;
using ThinkNet.Messaging;
using ThinkLib.Common;
using UserRegistration.Application;
using UserRegistration.Commands;
using UserRegistration.ReadModel;

namespace UserRegistration
{


    class Program
    {
        static void Main(string[] args)
        {
           
            
            Bootstrapper.Current.StartThinkNet().DoneWithUnity();

            //Dictionary<string, string> dict = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            //int counter = 0;
            //while (counter++ < 1000) {
            //    var id = ObjectId.GenerateNewId().ToString();
            //    dict.Add(id, id);
            //    Console.WriteLine(id);
            //}
            System.Threading.Thread.Sleep(2000);

            var userRegister = new RegisterUser {
                UserName = "老韩",
                Password = "hanyang",
                LoginId = "young.han",
                Email = "19126332@qq.com"
            };

            var commandBus = ServiceLocator.Current.GetInstance<ICommandBus>();

            commandBus.Send(userRegister);


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
