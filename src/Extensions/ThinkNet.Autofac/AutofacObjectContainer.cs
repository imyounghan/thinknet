using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autofac;

namespace ThinkNet.Infrastructure
{
    public class AutofacObjectContainer : ObjectContainer
    {
        private readonly ContainerBuilder builder;
        private IContainer container;

        public AutofacObjectContainer()
            : this(new ContainerBuilder())
        { }

        public AutofacObjectContainer(ContainerBuilder builder)
        {
            this.builder = builder;
            //this.container = new Lazy<IContainer>(() => builder.Build());
        }

        public override bool IsRegistered(Type type, string name)
        {
            if (container == null)
                return false;

            if (string.IsNullOrEmpty(name))
                return container.IsRegistered(type);

            return container.IsRegisteredWithName(name, type);
        }

        public override void RegisterType(Type type, string name, Lifecycle lifetime)
        {
            var register = builder.RegisterType(type);
            if (!string.IsNullOrEmpty(name)) {
                register.Named(name, type);
            }
            if (lifetime == Lifecycle.Singleton) {
                register.SingleInstance();
            }
        }

        public override void RegisterType(Type from, Type to, string name, Lifecycle lifetime)
        {
            //var builder = new ContainerBuilder();
            var register = builder.RegisterType(to).As(from);
            if (!string.IsNullOrEmpty(name)) {
                register.Named(name, from);
            }
            if (lifetime == Lifecycle.Singleton) {
                register.SingleInstance();
            }
            //builder.Update(container);
        }

        public override void RegisterInstance(Type type, string name, object instance)
        {
            //var builder = new ContainerBuilder();
            var register = builder.RegisterInstance(instance).As(type).SingleInstance();
            if (!string.IsNullOrEmpty(name)) {
                register.Named(name, type);
            }
           // builder.Update(container);
        }

        public override object Resolve(Type type, string name)
        {
            object instance;

            if (string.IsNullOrEmpty(name))
                container.TryResolve(type, out instance);
            else
                container.TryResolveNamed(name, type, out instance);

            return instance;
        }

        public override IEnumerable<object> ResolveAll(Type type)
        {
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(type);
            object instance;
            if (container.TryResolve(type, out instance)) {
                return ((IEnumerable)instance).Cast<object>();
            }

            return Enumerable.Empty<object>();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                using (container) { }
            }
        }

        public void Build()
        {
            this.RegisterInstance<IObjectContainer>(this);
            this.container = builder.Build();
        }
    }
}
