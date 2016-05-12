using ThinkNet.Infrastructure;
using ThinkLib.Common;
using ThinkNet.Messaging.Handling;


namespace ThinkNet.Configurations
{
    /// <summary>
    /// 引导程序
    /// </summary>
    public static class BootstrapperExtentions
    {

        //private static void RegisterHandlers(object sender, EventArgs<IEnumerable<Type>> args)
        //{
        //    Bootstrapper bootstrapper  = (Bootstrapper)sender;

        //    foreach (var type in args.Data.Where(TypeHelper.IsHandlerType)) {
        //        RegisterHandler(bootstrapper, type);
        //    }

            
        //}

        //private static void RegisterHandler(Bootstrapper bootstrapper, Type type)
        //{
        //    var interfaceTypes = type.GetInterfaces().Where(p => TypeHelper.IsCommandHandlerInterfaceType(p) ||
        //        TypeHelper.IsEventHandlerInterfaceType(p) || TypeHelper.IsMessageHandlerInterfaceType(p));

        //    var lifecycle = (Lifecycle)LifeCycleAttribute.GetLifecycle(type);

        //    foreach (var interfaceType in interfaceTypes) {
        //        bootstrapper.RegisterType(interfaceType, type, lifecycle, type.FullName);
        //    }
        //}

        public static Bootstrapper StartThinkNet(this Bootstrapper that)
        {
            //that.TypesLoaded += RegisterHandlers;
            //that.RegisterInstance(DefaultMessageNotification.Instance, typeof(IMessageNotification), typeof(ICommandResultManager));
            that.RegisterType(typeof(IProcessor), typeof(MessageProcessor));

            return that;
        }
    }
}
