using Autofac;
using ThinkNet.Infrastructure;

namespace ThinkNet
{
    public static class BootstrapperExtentions
    {
        public static IObjectContainer DoneWithAutofac(this Bootstrapper that)
        {
            return that.DoneWithAutofac(new ContainerBuilder());
        }

        private static IObjectContainer DoneWithAutofac(this Bootstrapper that, ContainerBuilder containerBuilder)
        {
            containerBuilder.NotNull("containerBuilder");

            var container = new AutofacObjectContainer(containerBuilder);
            that.Done(container);
            return container;
        }
    }
}
