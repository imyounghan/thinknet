using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;

namespace TinyIoC
{

    public static class TinyContainerExtentions
    {
        class PerSessionLifetimeProvider : TinyIoCContainer.ITinyIoCObjectLifetimeProvider
        {
            private readonly string key = Guid.NewGuid().ToString();

            #region ITinyIoCObjectLifetimeProvider 成员

            public object GetObject()
            {
                return CallContext.GetData(key);
            }

            public void SetObject(object value)
            {
                CallContext.SetData(key, value);
            }

            public void ReleaseObject()
            {
                CallContext.FreeNamedDataSlot(key);
            }

            #endregion
        }

        class PerThreadLifetimeProvider : TinyIoCContainer.ITinyIoCObjectLifetimeProvider
        {
            [ThreadStatic]
            private static Dictionary<Guid, object> values;
            private readonly Guid key;

            public PerThreadLifetimeProvider()
            {
                this.key = Guid.NewGuid();
            }

            #region ITinyIoCObjectLifetimeProvider 成员

            public object GetObject()
            {
                EnsureValues();

                object result;
                values.TryGetValue(this.key, out result);
                return result;

            }

            public void SetObject(object value)
            {
                EnsureValues();

                values[this.key] = value;
            }

            public void ReleaseObject()
            {
                if (values != null) {
                    values.Values.Where(obj => obj is IDisposable).Cast<IDisposable>().ToList().ForEach(obj => obj.Dispose());
                    values.Clear();
                }
            }

            #endregion

            private static void EnsureValues()
            {
                // no need for locking, values is TLS
                if (values == null) {
                    values = new Dictionary<Guid, object>();
                }
            }

        }

        public static TinyIoCContainer.RegisterOptions AsPerSession(this TinyIoCContainer.RegisterOptions registerOptions)
        {
            return TinyIoCContainer.RegisterOptions.ToCustomLifetimeManager(registerOptions, new PerSessionLifetimeProvider(), "per session singleton");
        }
        public static TinyIoCContainer.MultiRegisterOptions AsPerSession(this TinyIoCContainer.MultiRegisterOptions registerOptions)
        {
            return TinyIoCContainer.MultiRegisterOptions.ToCustomLifetimeManager(registerOptions, new PerSessionLifetimeProvider(), "per session singleton");
        }

        public static TinyIoCContainer.RegisterOptions AsPerThread(this TinyIoCContainer.RegisterOptions registerOptions)
        {
            return TinyIoCContainer.RegisterOptions.ToCustomLifetimeManager(registerOptions, new PerThreadLifetimeProvider(), "per thread singleton");
        }
        public static TinyIoCContainer.MultiRegisterOptions AsPerThread(this TinyIoCContainer.MultiRegisterOptions registerOptions)
        {
            return TinyIoCContainer.MultiRegisterOptions.ToCustomLifetimeManager(registerOptions, new PerThreadLifetimeProvider(), "per thread singleton");
        }
    }

    
}
