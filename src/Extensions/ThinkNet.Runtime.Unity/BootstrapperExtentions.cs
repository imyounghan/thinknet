using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using Microsoft.Practices.Unity.InterceptionExtension;
using ThinkLib.Common;
using ThinkNet.Infrastructure;


namespace ThinkNet.Configurations
{
    public static class BootstrapperExtentions
    {
        private static LifetimeManager GetLifetimeManager(Lifecycle lifecycle)
        {
            switch (lifecycle) {
                case Lifecycle.Singleton:
                    return new ContainerControlledLifetimeManager();
                default:
                    return new TransientLifetimeManager();
            }
        }

        private static void RegisterInstance(Type type, object instance, string name)
        {
            if (string.IsNullOrWhiteSpace(name) && container.IsRegistered(type)) {
                return;
            }
            if (!string.IsNullOrWhiteSpace(name) && container.IsRegistered(type, name)) {
                return;
            }


            LifetimeManager lifetime = new ContainerControlledLifetimeManager();

            if (string.IsNullOrWhiteSpace(name)) {
                container.RegisterInstance(type, instance, lifetime);
            }
            else {
                container.RegisterInstance(type, name, instance, lifetime);
            }
        }

        private static void RegisterType(Type type, string name, Lifecycle lifecycle)
        {
            if (string.IsNullOrWhiteSpace(name) && container.IsRegistered(type)) {
                return;
            }
            if (!string.IsNullOrWhiteSpace(name) && container.IsRegistered(type, name)) {
                return;
            }


            var lifetime = GetLifetimeManager(lifecycle);

            var injectionMembers = InterceptionBehaviorMap.Instance.GetBehaviorTypes(type)
                .Select(behaviorType => new InterceptionBehavior(behaviorType))
                .Cast<InterceptionMember>().ToList();

            if (injectionMembers.Count > 0) {
                if (type.IsSubclassOf(typeof(MarshalByRefObject))) {
                    injectionMembers.Insert(0, new Interceptor<TransparentProxyInterceptor>());
                }
                else {
                    injectionMembers.Insert(0, new Interceptor<VirtualMethodInterceptor>());
                }
            }

            if (type.IsDefined<HandlerAttribute>(false) ||
                type.GetMembers().Any(item => item.IsDefined<HandlerAttribute>(false))) {
                int position = injectionMembers.Count > 0 ? 1 : 0;
                injectionMembers.Insert(position, new InterceptionBehavior<PolicyInjectionBehavior>());
            }

            if (string.IsNullOrWhiteSpace(name)) {
                container.RegisterType(type, lifetime, injectionMembers.ToArray());
            }
            else {
                container.RegisterType(type, name, lifetime, injectionMembers.ToArray());
            }
        }


        private static void RegisterType(Type from, Type to, string name, Lifecycle lifecycle)
        {
            if (string.IsNullOrWhiteSpace(name) && container.IsRegistered(from)) {
                return;
            }
            if (!string.IsNullOrWhiteSpace(name) && container.IsRegistered(from, name)) {
                return;
            }

            var lifetimeManager = GetLifetimeManager(lifecycle);

            var serviceBehaviorTypes = InterceptionBehaviorMap.Instance.GetBehaviorTypes(from);
            var implBehaviorTypes = InterceptionBehaviorMap.Instance.GetBehaviorTypes(to);

            var injectionMembers = serviceBehaviorTypes.Union(implBehaviorTypes)
                .Select(behaviorType => new InterceptionBehavior(behaviorType))
                .Cast<InterceptionMember>().ToList();
            if (injectionMembers.Count > 0) {
                if (implBehaviorTypes.Length > 0) {
                    if (to.IsSubclassOf(typeof(MarshalByRefObject))) {
                        injectionMembers.Insert(0, new Interceptor<TransparentProxyInterceptor>());
                    }
                    else {
                        injectionMembers.Insert(0, new Interceptor<VirtualMethodInterceptor>());
                    }
                }
                if (serviceBehaviorTypes.Length > 0 && from.IsInterface) {
                    injectionMembers.Insert(0, new Interceptor<InterfaceInterceptor>());
                }
            }

            if (to.IsDefined<HandlerAttribute>(false) ||
                to.GetMembers().Any(item => item.IsDefined<HandlerAttribute>(false))) {
                int position = injectionMembers.Count > 0 ? 1 : 0;
                injectionMembers.Insert(position, new InterceptionBehavior<PolicyInjectionBehavior>());
            }

            if (string.IsNullOrWhiteSpace(name)) {
                container.RegisterType(from, to, lifetimeManager, injectionMembers.ToArray());
            }
            else {
                container.RegisterType(from, to, name, lifetimeManager, injectionMembers.ToArray());
            }
        }

        private static void Register(Bootstrapper.Component registration)
        {
            if (registration.ForType == null)
                return;


            //if (registration.Instance != null) {
            //    RegisterInstance(registration.RegisterType, registration.Instance, registration.Name);
            //    return;
            //}

            if (registration.ContractType == null) {
                RegisterType(registration.ForType, registration.ContractName, registration.Lifecycle);
                return;
            }

            RegisterType(registration.ContractType, registration.ForType, registration.ContractName, registration.Lifecycle);
        }

        private static object Resolve(Type type, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return container.Resolve(type);
            else
                return container.Resolve(type, name);
        }

        public static void DoneWithUnity(this Bootstrapper that, bool enableInterception = false)
        {
            var container = new UnityContainer();
            if (enableInterception)
                container.AddNewExtension<Interception>();

            that.DoneWithUnity(container);
        }

        private static IUnityContainer container;
        private static void DoneWithUnity(this Bootstrapper that, IUnityContainer unityContainer)
        {
            unityContainer.NotNull("unityContainer");

            container = unityContainer;
            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(unityContainer));

            that.Done(Register);
        }

        public static void DoneWithUnityByConfig(this Bootstrapper that, string sectionName)
        {
            sectionName.NotNullOrWhiteSpace("sectionName");

            var section = ConfigurationManager.GetSection(sectionName) as UnityConfigurationSection;
            var container = section.Configure(new UnityContainer());

            that.DoneWithUnity(container);
        } 
    }
}
