using System.Configuration;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using Microsoft.Practices.Unity.InterceptionExtension;
using ThinkNet.Caching;

namespace ThinkNet.Configurations
{
    public static class BootstrapperExtentions
    {
        public static void DoneWithUnity(this Bootstrapper that, bool enableInterception = false)
        {
            var container = new UnityContainer();
            if (enableInterception)
                container.AddNewExtension<Interception>();

            that.DoneWithUnity(container);
        }
        
        private static void DoneWithUnity(this Bootstrapper that, IUnityContainer container)
        {
            container.NotNull("container");

            if(!container.IsRegistered<ICacheProvider>()) {
                container.RegisterType<ICacheProvider, MemoryCacheProvider>();
            }
            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(container));

            that.Done(new UnityObjectContainer(container));
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
