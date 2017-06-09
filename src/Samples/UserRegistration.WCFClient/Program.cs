

namespace UserRegistration.Application
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.ComponentModel.Composition.Registration;
    using System.Reflection;
    using System.Threading;

    using ThinkNet.Messaging;

    using UserRegistration.Commands;
    using UserRegistration.ReadModel;

    class Program
    {
        static void Main(string[] args)
        {
            var builder = new RegistrationBuilder();
            builder.ForType<CommandService>().Export<ICommandService>().SetCreationPolicy(CreationPolicy.Shared);
            builder.ForType<QueryService>().Export<IQueryService>().SetCreationPolicy(CreationPolicy.Shared);

            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(Assembly.GetExecutingAssembly(), builder));
            //catelog.Catalogs.Add(new DirectoryCatalog(Directory.GetCurrentDirectory()));//查找部件，当前应用程序
           var container = new CompositionContainer(catalog);
            container.ComposeParts();

            Console.WriteLine("输入任意键开始演示...");
            Console.ReadKey();

            Console.WriteLine("开始创建用户...");
            var commandService = container.GetExportedValue<ICommandService>();
            var commandResult = commandService.Execute(new RegisterUser {
                UserName = "hanyang",
                Password = "123456",
                LoginId = "young.han",
                Email = "19126332@qq.com"
            });
            Console.WriteLine("命令处理完成(结果：{0})...", commandResult.Status);
            Thread.Sleep(2000);

            var queryService = container.GetExportedValue<IQueryService>();
            var queryResult = queryService.Execute<ICollection<UserModel>>(new FindAllUser());
            if(queryResult.Status != ExecutionStatus.Success) {
                Console.WriteLine("查询处理完成(结果：{0})...", queryResult.Status);
            }
            else {
                Console.WriteLine("共有 {0} 个用户。", queryResult.Data.Count);
            }
            Thread.Sleep(2000);

            var authoResult =
                queryService.Execute<bool>(
                    new UserAuthentication() { LoginId = "young.han", Password = "123456", IpAddress = "127.0.0.1" });
            if(authoResult.Data) {
                Console.WriteLine("登录成功。");
            }
            else {
                Console.WriteLine("用户名或密码错误。");
            }

            Console.ReadKey();
        }
    }
}
