using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.ServiceLocation;
using ThinkNet.Configurations;
using ThinkNet.Messaging;
using TinyIoC;
using UserRegistration.Application;
using UserRegistration.Commands;
using UserRegistration.ReadModel;

namespace UserRegistration
{

    static class ConfigurationExtentions
    {

        private static void RegisterInstance(Type type, object instance, string name)
        {
            if (string.IsNullOrWhiteSpace(name) && TinyIoCContainer.Current.CanResolve(type)) {
                return;
            }
            if (!string.IsNullOrWhiteSpace(name) && TinyIoCContainer.Current.CanResolve(type, name)) {
                return;
            }

            TinyIoCContainer.RegisterOptions options;
            if (string.IsNullOrWhiteSpace(name)) {
                options = TinyIoCContainer.Current.Register(type, instance);
            }
            else {
                options = TinyIoCContainer.Current.Register(type, instance, name);
            }
            options.AsSingleton();
        }

        private static void RegisterType(Type type, string name, Lifecycle lifecycle)
        {
            if (string.IsNullOrWhiteSpace(name) && TinyIoCContainer.Current.CanResolve(type)) {
                return;
            }
            if (!string.IsNullOrWhiteSpace(name) && TinyIoCContainer.Current.CanResolve(type, name)) {
                return;
            }

            TinyIoCContainer.RegisterOptions options;
            if (string.IsNullOrWhiteSpace(name)) {
                options = TinyIoCContainer.Current.Register(type);
            }
            else {
                options = TinyIoCContainer.Current.Register(type, name);
            }

            switch (lifecycle) {
                case Lifecycle.Singleton:
                    options.AsSingleton();
                    break;
                case Lifecycle.Transient:
                    options.AsMultiInstance();
                    break;
                case Lifecycle.PerSession:
                    options.AsPerSession();
                    break;
                case Lifecycle.PerThread:
                    options.AsPerThread();
                    break;
            }
        }

        private static void RegisterType(Type from, Type to, string name, Lifecycle lifecycle)
        {
            if (string.IsNullOrWhiteSpace(name) && TinyIoCContainer.Current.CanResolve(from)) {
                return;
            }
            if (!string.IsNullOrWhiteSpace(name) && TinyIoCContainer.Current.CanResolve(from, name)) {
                return;
            }

            TinyIoCContainer.RegisterOptions options;

            if (string.IsNullOrWhiteSpace(name)) {
                if (to == null)
                    options = TinyIoCContainer.Current.Register(from);
                else
                    options = TinyIoCContainer.Current.Register(from, to);
            }
            else {
                if (to == null)
                    options = TinyIoCContainer.Current.Register(from);
                else
                    options = TinyIoCContainer.Current.Register(from, to, name);
            }

            switch (lifecycle) {
                case Lifecycle.Singleton:
                    options.AsSingleton();
                    break;
                case Lifecycle.Transient:
                    options.AsMultiInstance();
                    break;
                case Lifecycle.PerSession:
                    options.AsPerSession();
                    break;
                case Lifecycle.PerThread:
                    options.AsPerThread();
                    break;
            }
        }

        private static void Register(Configuration.TypeRegistration registration)
        {
            if (registration.RegisterType == null)
                return;


            if (registration.Instance != null) {
                RegisterInstance(registration.RegisterType, registration.Instance, registration.Name);
                return;
            }

            RegisterType(registration.RegisterType, registration.ImplementationType, registration.Name, registration.Lifecycle);
        }

        public static void DoneWithTinyIoC(this Configuration that)
        {
            ServiceLocator.SetLocatorProvider(() => new TinyIoCServiceLocator(TinyIoCContainer.Current));

            that.Done(Register);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Configuration.Current.DoneWithTinyIoC();


            var userRegister = new RegisterUser {
                UserName = "老韩",
                Password = "hanyang",
                LoginId = "young.han",
                Email = "19126332@qq.com"
            };

            var commandBus = ServiceLocator.Current.GetInstance<ICommandBus>();

            commandBus.SendAsync(userRegister, CommandReplyType.DomainEventHandled).Wait();

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
