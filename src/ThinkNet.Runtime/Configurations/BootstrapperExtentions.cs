using System;
using System.Linq;
using System.Reflection;
using ThinkLib.Common;
using ThinkNet.Infrastructure;


namespace ThinkNet.Configurations
{
    /// <summary>
    /// 引导程序
    /// </summary>
    public static class BootstrapperExtentions
    {

        public static void RegisterHandler(this Bootstrapper that, Type type)
        {
            var interfaceTypes = type.GetInterfaces().Where(p => TypeHelper.IsCommandHandlerInterfaceType(p) ||
                TypeHelper.IsEventHandlerInterfaceType(p) || TypeHelper.IsMessageHandlerInterfaceType(p));

            var lifecycle = (Lifecycle)LifeCycleAttribute.GetLifecycle(type);

            object instance = null;
            if (lifecycle == Lifecycle.Singleton) {
                var member = type.GetMember("Instance", MemberTypes.Field | MemberTypes.Property,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase).FirstOrDefault();

                if (member != null) {
                    instance = member.GetMemberValue(null);
                }
            }

            foreach (var interfaceType in interfaceTypes) {
                if (instance == null)
                    that.RegisterType(interfaceType, type, lifecycle, type.FullName);
                else
                    that.RegisterInstance(interfaceType, instance, type.FullName);
            }
        }

    }
}
