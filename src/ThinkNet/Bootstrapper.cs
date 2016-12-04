using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ThinkNet.Contracts;
using ThinkNet.Database;
using ThinkNet.Database.Storage;
using ThinkNet.Domain;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Domain.Repositories;
using ThinkNet.Infrastructure;
using ThinkNet.Infrastructure.Composition;
using ThinkNet.Infrastructure.Interception;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Fetching;
using ThinkNet.Messaging.Handling;
using ThinkNet.Runtime;
using ThinkNet.Runtime.Routing;

namespace ThinkNet
{
    /// <summary>
    /// 引导程序
    /// </summary>
    public class Bootstrapper
    {
        class Component
        {
            /// <summary>
            /// Parameterized constructor.
            /// </summary>
            public Component(Type type, string name, object instance)
            {
                this.ContractType = type;
                this.ContractName = name ?? string.Empty;
                this.Instance = instance;
                this.TargetType = instance.GetType();
                this.Lifecycle = Lifecycle.Singleton;
            }

            /// <summary>
            /// Parameterized constructor.
            /// </summary>
            public Component(Type type, string name, Lifecycle lifecycle)
            {
                this.TargetType = type;
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
            public string ContractName { get; private set; }
            /// <summary>
            /// 要注册的类型
            /// </summary>
            public Type ContractType { get; private set; }
            /// <summary>
            /// 要注册类型的实现类型
            /// </summary>
            public Type TargetType { get; private set; }
            /// <summary>
            /// 要注册类型的实例
            /// </summary>
            public object Instance { get; private set; }
            /// <summary>
            /// 生命周期
            /// </summary>
            public Lifecycle Lifecycle { get; private set; }

            private Type GetServiceType()
            {
                return this.ContractType ?? this.TargetType;
            }

            /// <summary>
            /// 返回一个值，该值指示此实例是否与指定的对象相等。
            /// </summary>
            public override bool Equals(object obj)
            {
                var other = obj as Component;

                if (other == null)
                    return false;

                if (ReferenceEquals(this, other))
                    return true;

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
                return this.ToString().GetHashCode();
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append(this.GetServiceType().FullName);
                if (!string.IsNullOrEmpty(this.ContractName))
                    sb.Append("|").Append(this.ContractName);

                return sb.ToString();
            }

            //internal int GetUniqueCode()
            //{
            //    if (IsInitializeType(this.TargetType))
            //        return this.TargetType.GetHashCode();

            //    if (IsInitializeType(this.ContractType))
            //        return this.ContractType.GetHashCode();


            //    return this.GetHashCode();
            //}

            internal bool MustbeInitialize()
            {
                return this.Lifecycle == Lifecycle.Singleton &&
                    (IsInitializeType(this.ContractType) || IsInitializeType(this.TargetType) || (this.Instance is IInitializer));
            }

            private static bool IsInitializeType(Type type)
            {
                return type != null && type.IsClass && !type.IsAbstract && typeof(IInitializer).IsAssignableFrom(type);
            }

            internal object GetInstance(IObjectContainer container)
            {
                if (this.Instance != null)
                    return this.Instance;

                var serviceType = this.GetServiceType();
                var key = this.ContractName;
                return string.IsNullOrWhiteSpace(key) ?
                    container.Resolve(serviceType) :
                    container.Resolve(serviceType, key);
            }

            internal void Register(IObjectContainer container)
            {
                if (container.IsRegistered(this.GetServiceType(), this.ContractName)) {
                    return;
                }

                if (this.Instance != null) {
                    container.RegisterInstance(this.ContractType, this.Instance, this.ContractName);
                    return;
                }

                if (this.ContractType == null)
                    container.RegisterType(this.TargetType, this.ContractName, this.Lifecycle);
                else
                    container.RegisterType(this.ContractType, this.TargetType, this.ContractName, this.Lifecycle);
            }
        }
        

        /// <summary>
        /// 服务状态
        /// </summary>
        public enum ServerStatus
        {
            /// <summary>
            /// 运行中
            /// </summary>
            Running,
            /// <summary>
            /// 已启动
            /// </summary>
            Started,
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
        private readonly Stopwatch _stopwatch;
        /// <summary>
        /// Default Constructor.
        /// </summary>
        protected Bootstrapper()
        {
            this._assemblies = new List<Assembly>();
            this._components = new HashSet<Component>();
            this._stopwatch = Stopwatch.StartNew();
            this.Status = ServerStatus.Running;            
        }
        
        /// <summary>
        /// 当前服务器状态
        /// </summary>
        public ServerStatus Status { get; private set; }

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
        /// 加载程序集
        /// </summary>
        public Bootstrapper LoadAssemblies(string[] assemblyNames)
        {
            var assemblies = assemblyNames.Select(Assembly.Load).ToArray();

            return this.LoadAssemblies(assemblies);
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
        /// 启动相关Processes
        /// </summary>
        public virtual void Start()
        {
            if (this.Status == ServerStatus.Started)
                return;

            ObjectContainer.Instance.ResolveAll<IProcessor>().ForEach(p => p.Start());

            this.Status = ServerStatus.Started;
        }
        /// <summary>
        /// 停止相关Processes
        /// </summary>
        public virtual void Stop() 
        {
            if (this.Status == ServerStatus.Stopped)
                return;


            ObjectContainer.Instance.ResolveAll<IProcessor>().ForEach(p => p.Stop());

            this.Status = ServerStatus.Stopped;
        }

        /// <summary>
        /// 表示程序集加载完成后的处理方式
        /// </summary>
        protected virtual void OnAssembliesLoaded(IEnumerable<Assembly> assemblies, IEnumerable<Type> nonAbstractTypes)
        { }

        
        ///// <summary>
        ///// 配置完成。
        ///// </summary>
        //public void Done()
        //{
        //    this.Done(new OwnObjectContainer());
        //}

        /// <summary>
        /// 配置完成。
        /// </summary>
        public void Done(IObjectContainer container)
        {
            if (this.Status != ServerStatus.Running) {
                return;
            }
            
            if (_assemblies.Count == 0) {
                this.LoadAssemblies();

                Console.WriteLine("load assemblies completed.");
                Console.WriteLine("[");
                foreach (var assembly in _assemblies) {
                    Console.WriteLine(assembly.FullName);
                }
                Console.WriteLine("]");
            }

            ObjectContainer.Instance = container;

            var nonAbstractTypes = _assemblies.SelectMany(assembly => assembly.GetTypes())
                    .Where(type => type.IsClass && !type.IsAbstract).ToArray();

            this.RegisterComponents(nonAbstractTypes);
            this.RegisterHandlerAndFetcher(nonAbstractTypes);
            this.OnAssembliesLoaded(_assemblies, nonAbstractTypes);
            this.RegisterDefaultComponents();

            _components.ForEach(item => item.Register(container));

            
            _components.Where(item => item.MustbeInitialize())
                .Select(item => item.GetInstance(container))
                .Distinct()
                .Cast<IInitializer>()
                .ForEach(item => item.Initialize(container, _assemblies));

            _assemblies.Clear();
            _components.Clear();

            _assemblies = null;
            _components = null;

            this.Start();

            _stopwatch.Stop();

            Console.WriteLine("system is working, used time:{0}ms.\r\n", _stopwatch.ElapsedMilliseconds);
        }


        /// <summary>
        /// 设置组件
        /// </summary>
        public Bootstrapper SetDefault(Type type, string name, object instance )
        {
            if (this.Status != ServerStatus.Running) {
                throw new ApplicationException("system is working, can not register type, please execute before 'Done' method.");
            }
            type.NotNull("type");

            _components.Add(new Component(type, name, instance));

            return this;
        }

        /// <summary>
        /// 设置组件
        /// </summary>
        public Bootstrapper SetDefault(Type type, string name, Lifecycle lifecycle = Lifecycle.Singleton)
        {
            if (this.Status != ServerStatus.Running) {
                throw new ApplicationException("system is working, can not register type, please execute before 'Done' method.");
            }
            type.NotNull("type");

            _components.Add(new Component(type, name, lifecycle));

            return this;
        }

        /// <summary>
        /// 设置组件
        /// </summary>
        public Bootstrapper SetDefault(Type from, Type to, string name, Lifecycle lifecycle = Lifecycle.Singleton)
        {
            if (this.Status != ServerStatus.Running) {
                throw new ApplicationException("system is working, can not register type, please execute before 'done' method.");
            }
            from.NotNull("from");
            to.NotNull("to");

            _components.Add(new Component(from, to, name, lifecycle));

            return this;
        }

        private void RegisterHandlerAndFetcher(IEnumerable<Type> types)
        {
            foreach(var type in types) {
                var lifecycle = LifeCycleAttribute.GetLifecycle(type);

                var interfaceTypes = type.GetInterfaces();
                foreach(var interfaceType in interfaceTypes.Where(FilterType)) {
                    this.SetDefault(interfaceType, type, type.FullName, lifecycle);
                }
            }
            
            AggregateRootInnerHandlerProvider.Instance.Initialize(types);
        }

        private void RegisterComponents(IEnumerable<Type> types)
        {
            var registionTypes = types.Where(p => p.IsDefined(typeof(RegisterAttribute), false));

            foreach (var type in registionTypes) {
                var lifecycle = LifeCycleAttribute.GetLifecycle(type);

                var attribute = type.GetCustomAttribute<RegisterAttribute>(false);
                if (attribute != null) {
                    var contractType = attribute.ContractType;
                    var contractName = attribute.ContractName;
                    if (attribute.ContractType == null) {
                        this.SetDefault(type, contractName, lifecycle);
                    }
                    else {
                        this.SetDefault(attribute.ContractType, type, contractName, lifecycle);
                    }
                }
            }
        }

        private void RegisterDefaultComponents()
        {
            this.SetDefault<ITextSerializer, DefaultTextSerializer>();
            this.SetDefault<IInterceptorProvider, InterceptorProvider>();
            this.SetDefault<IDataContextFactory, MemoryContextFactory>();
            this.SetDefault<IEventStore, EventStore>();
            this.SetDefault<IEventPublishedVersionStore, EventPublishedVersionStore>();
            this.SetDefault<ISnapshotPolicy, NoneSnapshotPolicy>();
            this.SetDefault<ISnapshotStore, SnapshotStore>();
            this.SetDefault<ICache, LocalCache>();
            this.SetDefault<IRoutingKeyProvider, DefaultRoutingKeyProvider>();
            this.SetDefault<IEventSourcedRepository, EventSourcedRepository>();
            this.SetDefault<IRepository, Repository>();
            this.SetDefault<IMessageBus, MessageBus>();
            this.SetDefault<ICommandService, CommandService>();
            this.SetDefault<ICommandResultNotification, CommandService>();
            this.SetDefault<IQueryService, QueryService>();
            this.SetDefault<IQueryResultNotification, QueryService>();
            this.SetDefault<IMessageHandlerRecordStore, HandlerRecordStore>();
            this.SetDefault<IEnvelopeSender, EnvelopeHub>();
            this.SetDefault<IEnvelopeReceiver, EnvelopeHub>();
            this.SetDefault<IProcessor, Processor>("core");
        }

        private static bool IsRepositoryInterfaceType(Type genericType)
        {
            return genericType == typeof(IRepository<>);
        }

        private static bool IsQueryFetcherInterfaceType(Type genericType)
        {
            return genericType == typeof(IQueryFetcher<,>) ||
                genericType == typeof(IQueryMultipleFetcher<,>) ||
                genericType == typeof(IQueryPageFetcher<,>);
        }

        private static bool IsMessageHandlerInterfaceType(Type genericType)
        {
            return genericType == typeof(IMessageHandler<>) ||
                genericType == typeof(ICommandHandler<>) ||
                genericType == typeof(IEventHandler<>) ||
                genericType == typeof(IEventHandler<,>) ||
                genericType == typeof(IEventHandler<,,>) ||
                genericType == typeof(IEventHandler<,,,>) ||
                genericType == typeof(IEventHandler<,,,,>);
        }

        private static bool FilterType(Type type)
        {
            if(!type.IsGenericType)
                return false;

            var genericType = type.GetGenericTypeDefinition();

            return IsMessageHandlerInterfaceType(genericType) || IsQueryFetcherInterfaceType(genericType);
        }
    }

    /// <summary>
    /// <see cref="Bootstrapper"/> 的扩展类
    /// </summary>
    public static class BootstrapperExtentions
    {
        /// <summary>
        /// 注册类型
        /// </summary>
        public static Bootstrapper SetDefault(this Bootstrapper that, Type type, Lifecycle lifecycle = Lifecycle.Singleton)
        {
            return that.SetDefault(type, (string)null, lifecycle);
        }
        /// <summary>
        /// 注册类型
        /// </summary>
        public static Bootstrapper SetDefault(this Bootstrapper that, Type from, Type to, Lifecycle lifecycle = Lifecycle.Singleton)
        {
            return that.SetDefault(from, to, (string)null, lifecycle);
        }        

        /// <summary>
        /// 注册类型
        /// </summary>
        public static Bootstrapper SetDefault<T>(this Bootstrapper that, Lifecycle lifecycle = Lifecycle.Singleton)
        {
            return that.SetDefault<T>((string)null, lifecycle);
        }
        /// <summary>
        /// 注册类型
        /// </summary>
        public static Bootstrapper SetDefault<T>(this Bootstrapper that, string name, Lifecycle lifecycle = Lifecycle.Singleton)
        {
            return that.SetDefault(typeof(T), name, lifecycle);
        }
        /// <summary>
        /// 注册类型
        /// </summary>
        public static Bootstrapper SetDefault<TFrom, TTo>(this Bootstrapper that, Lifecycle lifecycle = Lifecycle.Singleton)
            where TTo : TFrom
        {
            return that.SetDefault<TFrom, TTo>((string)null, lifecycle);
        }
        /// <summary>
        /// 注册类型
        /// </summary>
        public static Bootstrapper SetDefault<TFrom, TTo>(this Bootstrapper that, string name, Lifecycle lifecycle = Lifecycle.Singleton)
            where TTo : TFrom
        {
            return that.SetDefault(typeof(TFrom), typeof(TTo), name, lifecycle);
        }
    }
}
