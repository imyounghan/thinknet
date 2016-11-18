using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using ThinkNet.Common;
using ThinkNet.Common.Composition;
using ThinkNet.Common.Interception;
using ThinkNet.Common.Serialization;
using ThinkNet.Contracts;
using ThinkNet.Database;
using ThinkNet.Domain;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Domain.Repositories;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;
using ThinkNet.Runtime.Routing;
using ThinkNet.Runtime.Writing;

namespace ThinkNet.Runtime
{
    /// <summary>
    /// 引导程序
    /// </summary>
    public sealed class Bootstrapper
    {
        class Component
        {
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
                var key = component.ContractName;
                return string.IsNullOrWhiteSpace(key) ?
                    ObjectContainer.Instance.Resolve(serviceType) :
                    ObjectContainer.Instance.Resolve(serviceType, key);
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
            /// <summary>
            /// 运行中
            /// </summary>
            Running,
            /// <summary>
            /// 已停止
            /// </summary>
            Stopped
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

        /// <summary>
        /// 启动程序
        /// </summary>
        public void Start()
        {
            if (!_running)
                return;

            ObjectContainer.Instance.ResolveAll<IProcessor>().ForEach(p => p.Start());
        }

        /// <summary>
        /// 停止程序
        /// </summary>
        public void Stop()
        {
            if (!_running)
                return;

            ObjectContainer.Instance.ResolveAll<IProcessor>().ForEach(p => p.Stop());
        }

        private bool _running = false;

        /// <summary>
        /// 配置完成。
        /// </summary>
        public void Done()
        {
            this.Done(new OwnObjectContainer());
        }

        /// <summary>
        /// 配置完成。
        /// </summary>
        public void Done(IObjectContainer container)
        {
            if(_running) {
                return;
            }

            ObjectContainer.Instance = container;

            var sw = Stopwatch.StartNew();
            if (_assemblies.Count == 0) {
                this.LoadAssemblies();

                Console.WriteLine("load assemblies completed.");
                Console.WriteLine("[");
                foreach (var assembly in _assemblies) {
                    Console.WriteLine(assembly.FullName);
                }
                Console.WriteLine("]");
            }


            var allTypes = _assemblies.SelectMany(assembly => assembly.GetTypes()).ToArray();

            this.RegisterComponentsAndHanders(allTypes);
            this.RegisterFrameworkComponents();
            AggregateRootInnerHandlerProvider.Instance.Initialize(allTypes);
            HandlerFetchedProvider.Instance.Initialize(allTypes);

            _components.ForEach(Component.Register);

            var compositionContainer = container as OwnObjectContainer;
            if(compositionContainer != null)
                compositionContainer.Complete();

             _components.Where(Component.MustbeInitialize)
                .Select(Component.GetInstance)
                .Distinct()
                .Cast<IInitializer>()
                .ForEach(item => item.Initialize(container, _assemblies));

            _assemblies.Clear();
            _components.Clear();
            
            _assemblies = null;
            _components = null;

            _running = true;

            this.Start();

            sw.Stop();

            Console.WriteLine("system is running, used time:{0}ms.\r\n", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public Bootstrapper Register(Type type, string name, Lifecycle lifecycle)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register type, please execute before 'Done' method.");
            }
            type.NotNull("type");

            _components.Add(new Component(type, name, lifecycle));

            return this;
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public Bootstrapper Register(Type from, Type to, string name, Lifecycle lifecycle)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register type, please execute before 'done' method.");
            }
            from.NotNull("from");
            to.NotNull("to");

            _components.Add(new Component(from, to, name, lifecycle));

            return this;
        }

        private static bool IsHandlerInterfaceType(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var genericType = type.GetGenericTypeDefinition();

            return genericType == typeof(IMessageHandler<>) ||
                genericType == typeof(ICommandHandler<>) ||
                genericType == typeof(IEventHandler<>) ||
                genericType == typeof(IEventHandler<,>) ||
                genericType == typeof(IEventHandler<,,>) ||
                genericType == typeof(IEventHandler<,,,>) ||
                genericType == typeof(IEventHandler<,,,,>);
        }

        private static bool IsRepositoryInterfaceType(Type type)
        {
            //type.GetInterfaces()
            if(!type.IsGenericType)
                return false;

            var genericType = type.GetGenericTypeDefinition();

            return genericType == typeof(IRepository<>);
        }

        private void RegisterComponentsAndHanders(IEnumerable<Type> types)
        {
            var registionTypes = types.Where(p => p.IsClass && !p.IsAbstract && p.IsDefined(typeof(RegisterAttribute), false));

            foreach (var type in types.Where(p => p.IsClass && !p.IsAbstract)) {
                var lifecycle = LifeCycleAttribute.GetLifecycle(type);

                var attribute = type.GetAttribute<RegisterAttribute>(false);
                if (attribute != null) {
                    var contractType = attribute.ContractType;
                    var contractName = attribute.ContractName;
                    if (attribute.ContractType == null) {
                        this.Register(type, contractName, lifecycle);
                    }
                    else {
                        this.Register(attribute.ContractType, type, contractName, lifecycle);
                    }
                }

                var interfaceTypes = type.GetInterfaces();
                foreach (var interfaceType in interfaceTypes.Where(IsHandlerInterfaceType)) {
                    this.Register(interfaceType, type, type.FullName, lifecycle);
                }
            }
        }

        private void RegisterFrameworkComponents()
        {
            this.Register<IDataContextFactory, MemoryContextFactory>();
            this.Register<IEventStore, EventStore>();
            this.Register<ISnapshotPolicy, NoneSnapshotPolicy>();
            this.Register<ISnapshotStore, SnapshotStore>();
            this.Register<ITextSerializer, DefaultTextSerializer>();
            this.Register<IInterceptorProvider, InterceptorProvider>();
            this.Register<ICache, LocalCache>();            
            this.Register<IRoutingKeyProvider, DefaultRoutingKeyProvider>();
            this.Register<IEventSourcedRepository, EventSourcedRepository>();
            this.Register<IRepository, Repository>();
            this.Register<IMessageBus, MessageBus>();
            this.Register<ICommandService, CommandService>();
            this.Register<ICommandResultNotification, CommandService>();
            this.Register<IHandlerRecordStore, HandlerRecordInMemory>(); 
            this.Register<IEnvelopeSender, EnvelopeHub>();
            this.Register<IEnvelopeReceiver, EnvelopeHub>();
            this.Register<IProcessor, Processor>("core");
        }
        
    }
}
