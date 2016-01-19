using System;
using System.Collections.Generic;
using Microsoft.Practices.ServiceLocation;


namespace TinyIoC
{
    internal class TinyIoCServiceLocator : ServiceLocatorImplBase, IDisposable
    {
        private TinyIoCContainer container;

        public TinyIoCServiceLocator(TinyIoCContainer container)
        {
            this.container = container;
            //container.Register<IServiceLocator>(this).AsSingleton();      
        }


        protected override IEnumerable<object> DoGetAllInstances(Type serviceType)
        {
            if (this.container == null)
                throw new ObjectDisposedException("TinyContainer");

            return this.container.ResolveAll(serviceType, true);
        }

        protected override object DoGetInstance(Type serviceType, string key)
        {
            if (this.container == null)
                throw new ObjectDisposedException("TinyContainer");

            return this.container.Resolve(serviceType, key ?? string.Empty);
        }

        //public override object GetInstance(Type serviceType)
        //{
        //    if (this.container == null)
        //        throw new ObjectDisposedException("TinyContainer");

        //    return this.container.Resolve(serviceType);
        //}

        #region IDisposable 成员

        public void Dispose()
        {
            if (this.container != null) {
                this.container.Dispose();
                this.container = null;
            }
        }

        #endregion
    }
}
