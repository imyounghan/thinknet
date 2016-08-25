using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.Registration;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.ServiceLocation;
using ThinkNet.Common;
using ThinkNet.Database;
using ThinkNet.EventSourcing;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;
using ThinkNet.Messaging.Processing;

namespace ThinkNet.Configurations
{
    /// <summary>
    /// 引导程序
    /// </summary>
    public sealed class Bootstrapper
    {
        class Component
        {
            ///// <summary>
            ///// Parameterized constructor.
            ///// </summary>
            //public Component(Type type, object instance, string name)
            //    : this(type, name)
            //{
            //    this.Instance = instance;
            //}
            ///// <summary>
            ///// Parameterized constructor.
            ///// </summary>
            //public Component(Type type, string name)
            //    : this(type, name, Lifecycle.Singleton)
            //{ }
            /// <summary>
            /// Parameterized constructor.
            /// </summary>
            public Component(Type type, string name, Lifecycle lifecycle)
            {
                this.ForType = type;
                this.ContractName = name ?? string.Empty;
                this.Lifecycle = lifecycle;
            }
            /// <summary>
            /// Parameterized constructor.
            /// </summary>
            public Component(Type from, Type to, string name, Lifecycle lifecycle)
                : this(to, name, lifecycle)
            {
                this.ContractType = from;
            }

            /// <summary>
            /// 要注册的名称
            /// </summary>
            public string ContractName { get; set; }
            /// <summary>
            /// 要注册的类型
            /// </summary>
            public Type ContractType { get; private set; }
            /// <summary>
            /// 要注册类型的实现类型
            /// </summary>
            public Type ForType { get; private set; }
            /// <summary>
            /// 生命周期
            /// </summary>
            public Lifecycle Lifecycle { get; private set; }

            private Type GetServiceType()
            {
                return this.ContractType ?? this.ForType;
            }

            /// <summary>
            /// 返回一个值，该值指示此实例是否与指定的对象相等。
            /// </summary>
            public override bool Equals(object obj)
            {
                var other = obj as Component;

                if (other == null)
                    return false;

                if (this.GetServiceType() != other.GetServiceType())
                    return false;

                if (!String.Equals(this.ContractName, other.ContractName))
                    return false;

                return true;
            }
            /// <summary>
            /// 返回此实例的哈希代码。
            /// </summary>
            public override int GetHashCode()
            {
                return String.Concat(this.GetServiceType().FullName, "|", this.ContractName).GetHashCode();
            }

            internal int GetUniqueCode()
            {
                if (IsInitializeType(this.ForType))
                    return this.ForType.GetHashCode();

                if (IsInitializeType(this.ContractType))
                    return this.ContractType.GetHashCode();


                return this.GetHashCode();
            }

            internal static bool MustbeInitialize(Component component)
            {
                return component.Lifecycle == Lifecycle.Singleton &&
                    (IsInitializeType(component.ContractType) || IsInitializeType(component.ForType));
            }

            private static bool IsInitializeType(Type type)
            {
                return type != null && type.IsClass && !type.IsAbstract && typeof(IInitializer).IsAssignableFrom(type);
            }

            internal static object GetInstance(Component component)
            {
                var serviceType = component.GetServiceType();
                var key = /*TypeHelper.IsHandlerInterfaceType(serviceType) ? string.Empty : */component.ContractName;
                return string.IsNullOrWhiteSpace(key) ?
                    ServiceLocator.Current.GetInstance(serviceType) :
                    ServiceLocator.Current.GetInstance(serviceType, key);
            }

            internal static void Register(Component component)
            {
                if(ObjectContainer.Instance.IsRegistered(component.GetServiceType(), component.ContractName)) {
                    return;
                }

                if(component.ContractType == null)
                    ObjectContainer.Instance.RegisterType(component.ForType, component.ContractName, component.Lifecycle);
                else
                    ObjectContainer.Instance.RegisterType(component.ContractType, component.ForType, component.ContractName, component.Lifecycle);
            }
        }

        class ComponentComparer : IEqualityComparer<Component>
        {
            public bool Equals(Component x, Component y)
            {
                return x.ForType == y.ForType;
            }

            public int GetHashCode(Component obj)
            {
                return obj.ForType.FullName.GetHashCode();
            }
        }

        /// <summary>
        /// 服务状态
        /// </summary>
        public enum Status
        {
            Running,
            Stopped
        }

        class CompositionObjectContainer : ObjectContainer
        {
            private readonly CompositionContainer container;
            private readonly AggregateCatalog catalog;

            private Dictionary<Assembly, RegistrationBuilder> dict;
            public CompositionObjectContainer()
            {
                this.catalog = new AggregateCatalog();
                this.container = new CompositionContainer(catalog);
                this.dict = new Dictionary<Assembly, RegistrationBuilder>();
            }

            protected override void Dispose(bool disposing)
            {
                if(disposing) {
                    container.Dispose();
                    catalog.Dispose();
                }
            }

            public override bool IsRegistered(Type type, string name)
            {
                return catalog.Catalogs.SelectMany(p => p.ToArray()).SelectMany(p => p.ExportDefinitions).Any(p => p.ContractName == name);
            }

            public override void RegisterInstance(Type type, string name, object instance)
            {
                throw new NotImplementedException();
            }

            public override void RegisterType(Type type, string name, Lifecycle lifetime)
            {
                var builder = dict.GetOrAdd(type.Assembly, () => new RegistrationBuilder()).ForType(type);

                if(!string.IsNullOrEmpty(name)) {
                    builder = builder.Export(p => p.AsContractName(name));
                }

                switch(lifetime) {
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

                if(!string.IsNullOrEmpty(name)) {
                    builder = builder.Export(p => p.AsContractName(name));
                }

                switch(lifetime) {
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
                if(string.IsNullOrEmpty(name)) {
                    var contractName = AttributedModelServices.GetContractName(type);
                    return container.GetExportedValueOrDefault<object>(contractName);
                }
                else {
                    return container.GetExportedValueOrDefault<object>(name);
                }
            }

            public override IEnumerable<object> ResolveAll(Type type)
            {
                var contractName = AttributedModelServices.GetContractName(type);
                return container.GetExportedValues<object>(contractName);
            }

            public void Complete()
            {
                dict.ForEach(item => {
                    catalog.Catalogs.Add(new AssemblyCatalog(item.Key, item.Value));
                });
                container.ComposeParts();

                this.dict.Clear();
                this.dict = null;
            }
        }

        /// <summary>
        /// 当前配置
        /// </summary>
        public static readonly Bootstrapper Current = new Bootstrapper();


        private List<Assembly> _assemblies;
        private HashSet<Component> _components;
        private Bootstrapper()
        {
            this._assemblies = new List<Assembly>();
            this._components = new HashSet<Component>();
        }



        /// <summary>
        /// 加载程序集
        /// </summary>
        public Bootstrapper LoadAssemblies(Assembly[] assemblies)
        {
            _assemblies.Clear();
            _assemblies.AddRange(assemblies);

            return this;
        }

        /// <summary>
        /// 扫描bin目录的程序集
        /// </summary>
        public Bootstrapper LoadAssemblies()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string relativeSearchPath = AppDomain.CurrentDomain.RelativeSearchPath;
            string binPath = string.IsNullOrEmpty(relativeSearchPath) ? baseDir : Path.Combine(baseDir, relativeSearchPath);
            //string applicationAssemblyDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bin");
            //if (!FileUtils.DirectoryExists(applicationAssemblyDirectory)) {
            //    applicationAssemblyDirectory = AppDomain.CurrentDomain.BaseDirectory;
            //}


            var assemblies = Directory.GetFiles(binPath)
                .Where(file => {
                    var ext = Path.GetExtension(file).ToLower();
                    return ext.EndsWith(".dll") || ext.EndsWith(".exe");
                })
                .Select(Assembly.LoadFrom)
                .ToArray();

            return this.LoadAssemblies(assemblies);
        }


        public void Start()
        {
            if (!_running)
                return;

            var processores = ServiceLocator.Current.GetAllInstances<IProcessor>();
            foreach (var processor in processores) {
                processor.Start();
            }
        }

        public void Stop()
        {
            if (!_running)
                return;

            ServiceLocator.Current.GetAllInstances<IProcessor>().ForEach(p => p.Stop());
        }

        private bool _running = false;

        /// <summary>
        /// 配置完成。
        /// </summary>
        public void Done()
        {
            this.Done(new CompositionObjectContainer());
        }

        /// <summary>
        /// 配置完成。
        /// </summary>
        public void Done(IObjectContainer container)
        {

            if (_assemblies.Count == 0) {
                this.LoadAssemblies();

                LogManager.Default.InfoFormat("load assemblies completed.\r\n{0}",
                    string.Join("\r\n", _assemblies.Select(item => item.FullName)));
            }


            var allTypes = _assemblies.SelectMany(assembly => assembly.GetTypes()).ToArray();

            this.RegisterComponents(allTypes);
            this.RegisterFrameworkComponents();
            this.RegisterHandlers(allTypes);
            //this.RegisterInterceptor(allTypes);

            _components.ForEach(Component.Register);

            var compositionContainer = container as CompositionObjectContainer;
            if(compositionContainer != null)
                compositionContainer.Complete();

             _components.Where(Component.MustbeInitialize).Select(Component.GetInstance).Distinct().Cast<IInitializer>().ForEach(item => item.Initialize(allTypes));

            _assemblies.Clear();
            _components.Clear();
            
            _assemblies = null;
            _components = null;

            _running = true;

            this.Start();

            LogManager.Default.Info("system is running.");
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public void RegisterType(Type type, string name, Lifecycle lifecycle)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register type, please execute before 'done' method.");
            }
            type.NotNull("type");

            _components.Add(new Component(type, name, lifecycle));
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public void RegisterType(Type from, Type to, string name, Lifecycle lifecycle)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register type, please execute before 'done' method.");
            }
            from.NotNull("from");
            to.NotNull("to");

            _components.Add(new Component(from, to, name, lifecycle));
        }

        private void RegisterComponents(IEnumerable<Type> types)
        {
            var registionTypes = types.Where(p => p.IsClass && !p.IsAbstract && p.IsDefined(typeof(RegisterAttribute), false));

            foreach (var type in registionTypes) {
                var attribute = type.GetAttribute<RegisterAttribute>(false);
                var contractType = attribute.ContractType;
                var contractName = attribute.ContractName;
                var lifecycle = (Lifecycle)LifeCycleAttribute.GetLifecycle(type);
                if (attribute.ContractType == null) {
                    this.RegisterType(type, contractName, lifecycle);
                }
                else {
                    this.RegisterType(attribute.ContractType, type, contractName, lifecycle);
                }
            }           
        }
        private void RegisterHandlers(IEnumerable<Type> types)
        {
            var handlerTypes = types.Where(TypeHelper.IsHandlerType);
            foreach (var type in handlerTypes) {
                var interfaceTypes = type.GetInterfaces().Where(TypeHelper.IsHandlerInterfaceType);
                var lifecycle = (Lifecycle)LifeCycleAttribute.GetLifecycle(type);
                foreach (var interfaceType in interfaceTypes) {
                    this.RegisterType(interfaceType, type, type.FullName, lifecycle);
                }
            }
        }
        //private void RegisterInterceptor(IEnumerable<Type> types)
        //{
        //    var interceptorTypes=types.Where(TypeHelper.IsInterceptionType);
        //    foreach (var type in interceptorTypes) {
        //        var interfaceTypes = type.GetInterfaces().Where(TypeHelper.IsInterceptionInterfaceType);
        //        var lifecycle = (Lifecycle)LifeCycleAttribute.GetLifecycle(type);
        //        foreach (var interfaceType in interfaceTypes) {
        //            this.RegisterType(interfaceType, type, lifecycle, type.FullName);
        //        }
        //    }
        //}


        private void RegisterFrameworkComponents()
        {
            this.RegisterType<IEventPublishedVersionStore, EventPublishedVersionInMemory>();
            this.RegisterType<IEventStore, MemoryEventStore>();
            this.RegisterType<ISnapshotPolicy, NoneSnapshotPolicy>();
            this.RegisterType<ISnapshotStore, NoneSnapshotStore>();
            this.RegisterType<ISerializer, DefaultSerializer>();
            this.RegisterType<ICache, DefaultMemoryCache>();            
            this.RegisterType<IRoutingKeyProvider, DefaultRoutingKeyProvider>();
            this.RegisterType<IMetadataProvider, StandardMetadataProvider>();
            this.RegisterType<IEventSourcedRepository, EventSourcedRepository>();
            this.RegisterType<IRepository, MemoryRepository>();
            this.RegisterType<ICommandBus, DefaultCommandBus>();
            this.RegisterType<ICommandResultManager, DefaultCommandNotification>();
            this.RegisterType<IEventBus, DefaultEventBus>();
            this.RegisterType<ICommandContextFactory, CommandContextFactory>();
            this.RegisterType<IEventContextFactory, EventContextFactory>();
            this.RegisterType<IHandlerRecordStore, HandlerRecordInMemory>();
            this.RegisterType<ICommandNotification, DefaultCommandNotification>();
            this.RegisterType<IHandlerProvider, DefaultHandlerProvider>();
            this.RegisterType<IEnvelopeDelivery, DefaultEnvelopeDelivery>();
            this.RegisterType<IEnvelopeHub, DefaultEnvelopeHub>();

            if (ConfigurationSetting.Current.EnableCommandProcessor)
                this.RegisterType<IProcessor, CommandProcessor>("CommandProcessor");
            if (ConfigurationSetting.Current.EnableSynchronousProcessor)
                this.RegisterType<IProcessor, SynchronousProcessor>("SynchronousProcessor");
            if (ConfigurationSetting.Current.EnableEventProcessor)
                this.RegisterType<IProcessor, EventProcessor>("EventProcessor");
        }
        
    }
}
