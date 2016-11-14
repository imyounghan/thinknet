using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using System.Linq;
using System.Reflection;

namespace ThinkNet.Common.Composition
{
    internal class OwnObjectContainer : ObjectContainer
    {
        private readonly CompositionContainer container;
        private readonly AggregateCatalog catalog;

        private Dictionary<Assembly, RegistrationBuilder> dict;
        public OwnObjectContainer()
        {
            this.catalog = new AggregateCatalog();
            this.container = new CompositionContainer(catalog);
            this.dict = new Dictionary<Assembly, RegistrationBuilder>();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                container.Dispose();
                catalog.Dispose();
            }
        }

        public override bool IsRegistered(Type type, string name)
        {
            var contractName = AttributedModelServices.GetContractName(type);
            if (!string.IsNullOrEmpty(name)) {
                contractName = name.Insert(0, "|").Insert(0, contractName);
            }
            return catalog.Catalogs.SelectMany(p => p.ToArray()).SelectMany(p => p.ExportDefinitions).Any(p => p.ContractName == contractName);
        }

        public override void RegisterInstance(Type type, string name, object instance)
        {
            throw new NotImplementedException();
        }

        public override void RegisterType(Type type, string name, Lifecycle lifetime)
        {
            var builder = dict.GetOrAdd(type.Assembly, () => new RegistrationBuilder()).ForType(type);

            if (!string.IsNullOrEmpty(name)) {
                var contractName = name.Insert(0, "|").Insert(0, AttributedModelServices.GetContractName(type));
                builder = builder.Export(p => p.AsContractName(contractName));
            }

            switch (lifetime) {
                case Lifecycle.Singleton:
                    builder.SetCreationPolicy(CreationPolicy.Shared);
                    break;
                case Lifecycle.Transient:
                    builder.SetCreationPolicy(CreationPolicy.NonShared);
                    break;
            }
        }

        public override void RegisterType(Type from, Type to, string name, Lifecycle lifetime)
        {
            var builder = dict.GetOrAdd(to.Assembly, () => new RegistrationBuilder()).ForType(to).Export(p => p.AsContractType(from));

            if (!string.IsNullOrEmpty(name)) {
                var contractName = name.Insert(0, "|").Insert(0, AttributedModelServices.GetContractName(from));
                builder = builder.Export(p => p.AsContractName(contractName));
            }

            switch (lifetime) {
                case Lifecycle.Singleton:
                    builder.SetCreationPolicy(CreationPolicy.Shared);
                    break;
                case Lifecycle.Transient:
                    builder.SetCreationPolicy(CreationPolicy.NonShared);
                    break;
            }
        }

        public override object Resolve(Type type, string name)
        {
            var contractName = AttributedModelServices.GetContractName(type);
            if (string.IsNullOrEmpty(name)) {
                return container.GetExportedValueOrDefault<object>(contractName);
            }
            else {
                return container.GetExportedValueOrDefault<object>(name.Insert(0, "|").Insert(0, contractName));
            }
        }

        public override IEnumerable<object> ResolveAll(Type type)
        {
            var contractName = AttributedModelServices.GetContractName(type);
            return container.GetExportedValues<object>(contractName);
        }

        public void Complete()
        {
            foreach (var item in dict) {
                catalog.Catalogs.Add(new AssemblyCatalog(item.Key, item.Value));
            }
            container.ComposeParts();

            this.dict.Clear();
            this.dict = null;
        }
    }
}
