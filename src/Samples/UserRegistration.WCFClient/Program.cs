using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using ThinkNet.Contracts;

namespace UserRegistration.WCFClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("输入任意键开始演示...");
            Console.ReadKey();

            //using(var channelFactory = new ChannelFactory<ICommandService>("CommandService")) {
            //    var userActionService = channelFactory.CreateChannel();
            //    userActionService.RegisterUser(new UserInfo() {
            //        UserName = "hanyang",
            //        Password = "123456",
            //        LoginId = "young.han",
            //        Email = "19126332@qq.com"
            //    });
            //}

            //Console.WriteLine("创建一个用户。");
            //System.Threading.Thread.Sleep(2000);

            //using(var channelFactory = new ChannelFactory<IQueryService>("UserQueryService")) {
            //    var userQueryService = channelFactory.CreateChannel();
            //    Console.WriteLine("共有 {0} 个用户。", userQueryService.FindAll().Count());
            //}
            //System.Threading.Thread.Sleep(2000);


            //using(var channelFactory = new ChannelFactory<IAuthenticationService>("AuthenticationService")) {
            //    var authenticationService = channelFactory.CreateChannel();
            //    if(!authenticationService.Authenticate("young.han", "123456", "127.0.0.1")) {
            //        Console.WriteLine("用户名或密码错误");
            //    }
            //    else {
            //        Console.WriteLine("登录成功。");
            //    }
            //}
            Console.ReadKey();
        }
    }
}
