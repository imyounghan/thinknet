using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using System.Reflection;
using ThinkNet.Contracts;

namespace UserRegistration
{
    public class WcfServiceGateway : IServiceGateway
    {
        private readonly CompositionContainer container;        

        public WcfServiceGateway()
        {
            var builder = new RegistrationBuilder();
            builder.ForType<CommandService>().Export<ICommandService>().SetCreationPolicy(CreationPolicy.Shared);
            builder.ForType<QueryService>().Export<IQueryService>().SetCreationPolicy(CreationPolicy.Shared);

            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(Assembly.GetExecutingAssembly(), builder));
            //catelog.Catalogs.Add(new DirectoryCatalog(Directory.GetCurrentDirectory()));//查找部件，当前应用程序
            this.container = new CompositionContainer(catalog);
            container.ComposeParts();
        }

        #region IServiceGateway 成员

        public TService GetService<TService>()
        {
            return container.GetExportedValue<TService>();
        }

        #endregion
    }
}
