using System;
using System.Collections;
using System.Web;

namespace ThinkNet.Database.Context
{
    /// <summary>
    /// <see cref="ICurrentContext"/> 的抽象实现。
    /// </summary>
    public abstract class CurrentContext : ICurrentContext
    {
        private readonly IContextManager _contextManager;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected CurrentContext(IContextManager contextManager)
        {
            this._contextManager = contextManager;
        }

        /// <summary>
        /// context factory
        /// </summary>
        public IContextManager ContextManager
        {
            get { return this._contextManager; }
        }

        /// <summary>
        /// Gets or sets the currently bound context.
        /// </summary>
        public IContext Context
        {
            get
            {
                IDictionary map = GetMap();
                if (map == null) {
                    return null;
                }
                else {
                    return (IContext)map[_contextManager.UniqueId];
                }
            }
            set
            {
                IDictionary map = GetMap();
                if (map == null) {
                    map = Hashtable.Synchronized(new Hashtable());
                    SetMap(map);
                }
                map[_contextManager.UniqueId] = value;
            }
        }

        /// <summary>
        /// Get the dicitonary mapping context manager to its current context.
        /// </summary>
        protected abstract IDictionary GetMap();

        /// <summary>
        /// Set the map mapping context manager to its current context.
        /// </summary>
        protected abstract void SetMap(IDictionary value);

        /// <summary>
        /// Binds the specified context to the current context.
        /// </summary>
        public static void Bind(IContext context)
        {
            GetCurrentContext(context.ContextManager).Context = context;
        }

        /// <summary>
        /// Returns whether there is a context bound to the current context.
        /// </summary>
        public static bool HasBind(IContextManager contextManager)
        {
            return GetCurrentContext(contextManager).Context != null;
        }

        /// <summary>
        /// Unbinds and returns the current context.
        /// </summary>
        public static IContext Unbind(IContextManager contextManager)
        {
            var removedContext = GetCurrentContext(contextManager).Context;
            GetCurrentContext(contextManager).Context = null;
            return removedContext;
        }

        private static CurrentContext GetCurrentContext(IContextManager contextManager)
        {
            if (contextManager.CurrentContext == null) {
                throw new ArgumentNullException("CurrentContext", "No current context configured.");
            }

            var currentContext = (CurrentContext)contextManager.CurrentContext;
            if (currentContext == null) {
                throw new ArgumentException("Current context does not extend class CurrentContext.");
            }

            return currentContext;
        }

        IContext ICurrentContext.GetContext()
        {
            if (this.Context == null) {
                throw new ArgumentNullException("Context", "No object bound to the current context");
            }
            return this.Context;
        }
    }

    #region impl
    internal class CallContext : CurrentContext
    {
        private const string DatabaseFactoryMapKey = "ThinkLib.CallContext";

        public CallContext(IContextManager factory)
            : base(factory)
        { }

        protected override IDictionary GetMap()
        {
            return System.Runtime.Remoting.Messaging.CallContext.GetData(DatabaseFactoryMapKey) as IDictionary;
        }

        protected override void SetMap(IDictionary value)
        {
            System.Runtime.Remoting.Messaging.CallContext.SetData(DatabaseFactoryMapKey, value);
        }
    }

    internal class OperationContext : CurrentContext
    {
        public OperationContext(IContextManager factory)
            : base(factory)
        { }

        private static WcfStateExtension WcfOperationState
        {
            get
            {
                var extension = System.ServiceModel.OperationContext.Current.Extensions.Find<WcfStateExtension>();

                if (extension == null) {
                    extension = new WcfStateExtension();
                    System.ServiceModel.OperationContext.Current.Extensions.Add(extension);
                }

                return extension;
            }
        }


        protected override IDictionary GetMap()
        {
            return WcfOperationState.Map;
        }

        protected override void SetMap(IDictionary value)
        {
            WcfOperationState.Map = value;
        }


        class WcfStateExtension : System.ServiceModel.IExtension<System.ServiceModel.OperationContext>
        {
            public IDictionary Map { get; set; }

            // we don't really need implementations for these methods in this case
            public void Attach(System.ServiceModel.OperationContext owner) { }
            public void Detach(System.ServiceModel.OperationContext owner) { }
        }
    }

    internal class ThreadContext : CurrentContext
    {
        public ThreadContext(IContextManager factory)
            : base(factory)
        { }

        [ThreadStatic]
        private static IDictionary context;

        protected override IDictionary GetMap()
        {
            return context;
        }

        protected override void SetMap(IDictionary value)
        {
            context = value;
        }
    }

    internal class WebContext : CurrentContext
    {
        private const string DatabaseFactoryMapKey = "ThinkNet.WebContext";

        public WebContext(IContextManager factory)
            : base(factory)
        { }

        protected override IDictionary GetMap()
        {
            return HttpContext.Current.Items[DatabaseFactoryMapKey] as IDictionary;
        }

        protected override void SetMap(IDictionary value)
        {
            HttpContext.Current.Items[DatabaseFactoryMapKey] = value;
        }
    }
    #endregion
}
