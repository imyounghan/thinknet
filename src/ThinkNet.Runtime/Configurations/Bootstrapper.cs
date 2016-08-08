using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
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
        /// <summary>
        /// 注册类型
        /// </summary>
        public class Component
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
            ///// <summary>
            ///// 要注册类型的实例
            ///// </summary>
            //public object Instance { get; private set; }
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

            internal bool NeedInitialize()
            {
                return IsInitializeType(this.ContractType) || IsInitializeType(this.ForType);
            }

            internal static object GetInstance(Component component)
            {
                var serviceType = component.GetServiceType();
                var key = /*TypeHelper.IsHandlerInterfaceType(serviceType) ? string.Empty : */component.ContractName;
                return string.IsNullOrWhiteSpace(key) ?
                    ServiceLocator.Current.GetInstance(serviceType) :
                    ServiceLocator.Current.GetInstance(serviceType, key);
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

        class ObjectContainer : ServiceLocatorImplBase
        {
            public readonly static ObjectContainer Instance = new ObjectContainer();


            private readonly CompositionContainer container;
            private readonly AggregateCatalog catalog;
            Dictionary<Assembly, RegistrationBuilder> dict;
            private ObjectContainer()
            {
                this.catalog = new AggregateCatalog();
                this.container = new CompositionContainer(catalog);
                this.dict = new Dictionary<Assembly, RegistrationBuilder>();
            }
            public void Register(Component component)
            {
                var builder = dict.GetOrAdd(component.ForType.Assembly, () => new RegistrationBuilder());
                var partBuilder = builder.ForType(component.ForType);

                if (component.ContractType != null) {
                    partBuilder = partBuilder.Export(p => p.AsContractType(component.ContractType));
                }
                else {
                    partBuilder = partBuilder.Export();
                }

                if (!string.IsNullOrEmpty(component.ContractName) && !TypeHelper.IsHandlerInterfaceType(component.ContractType)) {
                    partBuilder = partBuilder.Export(p => p.AsContractName(component.ContractName));
                }

                switch (component.Lifecycle) {
                    case Lifecycle.Singleton:
                        partBuilder.SetCreationPolicy(CreationPolicy.Shared);
                        break;
                    case Lifecycle.Transient:
                        partBuilder.SetCreationPolicy(CreationPolicy.NonShared);
                        break;
                }
            }

            public void Compose()
            {
                dict.ForEach(item => {
                    catalog.Catalogs.Add(new AssemblyCatalog(item.Key, item.Value));
                });
                container.ComposeParts();
            }

            public void Release()
            {
                this.dict.Clear();
                this.dict = null;
            }

            protected override IEnumerable<object> DoGetAllInstances(Type serviceType)
            {
                var contractName = AttributedModelServices.GetContractName(serviceType);

                return container.GetExportedValues<object>(contractName);
            }

            protected override object DoGetInstance(Type serviceType, string key)
            {
                if (string.IsNullOrEmpty(key)) {
                    var contractName = AttributedModelServices.GetContractName(serviceType);
                    return container.GetExportedValueOrDefault<object>(contractName);
                }
                else {
                    return container.GetExportedValueOrDefault<object>(key);
                }
            }
        }


        /// <summary>
        /// 当前配置
        /// </summary>
        public static readonly Bootstrapper Current = new Bootstrapper();


        private List<Assembly> _assemblies;
        private List<Component> _registerComponents;
        private HashSet<int> _registeredObjects;
        private HashSet<int> _initializedObjects;
        private Bootstrapper()
        {
            this._assemblies = new List<Assembly>();
            this._registerComponents = new List<Component>();
            this._registeredObjects = new HashSet<int>();
            this._initializedObjects = new HashSet<int>();
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
            ServiceLocator.SetLocatorProvider(() => ObjectContainer.Instance);
            this.Done(ObjectContainer.Instance.Register, ObjectContainer.Instance.Compose);
            
            ObjectContainer.Instance.Release();
        }

        /// <summary>
        /// 配置完成。
        /// </summary>
        public void Done(Action<Component> registry)
        {
            this.Done(registry, null);
        }

        private void Done(Action<Component> registry, Action initialization)
        {
            if (_running)
                return;


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

            while (_registerComponents.Count > 0) {
                _registerComponents.ForEach(registry);
                if (initialization != null) {
                    initialization.Invoke();
                }

                var initializers = _registerComponents.Where(InitComponent).ToArray();
                _registerComponents.Clear();

                initializers.Select(Component.GetInstance).Cast<IInitializer>().ForEach(item => item.Initialize(allTypes));
            }            

            _initializedObjects.Clear();
            _registeredObjects.Clear();
            _assemblies.Clear();

            _registerComponents = null;
            _initializedObjects = null;
            _registeredObjects = null;
            _assemblies = null;

            _running = true;

            this.Start();

            LogManager.Default.Info("system is running.");
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public void RegisterType(Type type, Lifecycle lifecycle, string name)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register type, please execute before 'done' method.");
            }
            type.NotNull("type");
            if (!type.IsClass || type.IsAbstract) {
                throw new ApplicationException(string.Format("the type of '{0}' must be a class and cannot be abstract.", type.FullName));
            }


            AddComponent(new Component(type, name, lifecycle));
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public void RegisterType(Type from, Type to, Lifecycle lifecycle, string name)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register type, please execute before 'done' method.");
            }
            from.NotNull("from");
            to.NotNull("to");
            if (!to.IsClass || to.IsAbstract) {
                throw new ApplicationException(string.Format("the type of '{0}' must be a class and cannot be abstract.", to.FullName));
            }

            AddComponent(new Component(from, to, name, lifecycle));
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
                    this.RegisterType(type, lifecycle, contractName);
                }
                else {
                    this.RegisterType(attribute.ContractType, type, lifecycle, contractName);
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
                    this.RegisterType(interfaceType, type, lifecycle, type.FullName);
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

            if (ConfigurationSetting.Current.EnableCommandProcessor)
                this.RegisterType<IProcessor, CommandProcessor>("CommandProcessor");
            if (ConfigurationSetting.Current.EnableSynchronousProcessor)
                this.RegisterType<IProcessor, SynchronousProcessor>("SynchronousProcessor");
            if (ConfigurationSetting.Current.EnableEventProcessor)
                this.RegisterType<IProcessor, EventProcessor>("EventProcessor");
        }

        private bool InitComponent(Component component)
        {
            return component.NeedInitialize() && _initializedObjects.Add(component.GetUniqueCode());
        }

        private void AddComponent(Component component)
        {
            if (_registeredObjects.Add(component.GetHashCode()))
                _registerComponents.Add(component);
        }

        private static bool IsInitializeType(Type type)
        {
            return type!=null && type.IsClass && !type.IsAbstract && typeof(IInitializer).IsAssignableFrom(type);
        }
    }
}
