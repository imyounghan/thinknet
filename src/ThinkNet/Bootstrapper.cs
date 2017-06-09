
namespace ThinkNet
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using ThinkNet.Infrastructure;
    using ThinkNet.Messaging;
    using ThinkNet.Messaging.Handling;
    using ThinkNet.Seeds;

    /// <summary>
    ///     引导程序
    /// </summary>
    public class Bootstrapper
    {
        #region Static Fields

        /// <summary>
        ///     当前配置
        /// </summary>
        public static readonly Bootstrapper Current = new Bootstrapper();

        #endregion

        #region Fields

        private readonly Stopwatch _stopwatch;
        private List<Assembly> _assemblies;
        private HashSet<Component> _components;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Bootstrapper"/> class. 
        /// </summary>
        protected Bootstrapper()
        {
            this._assemblies = new List<Assembly>();
            this._components = new HashSet<Component>();
            this._stopwatch = Stopwatch.StartNew();
            this.Status = ServerStatus.Running;
        }

        #endregion

        #region Enums

        /// <summary>
        ///     服务状态
        /// </summary>
        public enum ServerStatus
        {
            /// <summary>
            ///     运行中
            /// </summary>
            Running, 

            /// <summary>
            ///     已启动
            /// </summary>
            Started, 

            /// <summary>
            ///     已停止
            /// </summary>
            Stopped
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     当前服务器状态
        /// </summary>
        public ServerStatus Status { get; private set; }

        #endregion

        #region Methods and Operators

        /// <summary>
        ///     配置完成。
        /// </summary>
        public void Done()
        {
            this.Done(new DefaultObjectContainer());
        }

        /// <summary>
        /// 配置完成。
        /// </summary>
        public void Done(IObjectContainer container)
        {
            if (this.Status != ServerStatus.Running)
            {
                return;
            }

            if (this._assemblies.Count == 0)
            {
                this.LoadAssemblies();

                Console.WriteLine("load assemblies completed.");
                Console.WriteLine("[");
                foreach (Assembly assembly in this._assemblies)
                {
                    Console.WriteLine(assembly.FullName);
                }

                Console.WriteLine("]");
            }

            ObjectContainer.Instance = container;

            Type[] nonAbstractTypes =
                this._assemblies.SelectMany(assembly => assembly.GetTypes())
                    .Where(type => type.IsClass && !type.IsAbstract)
                    .ToArray();

            this.RegisterComponents(nonAbstractTypes);
            this.RegisterHandlerAndFetcher(nonAbstractTypes);

            // this.OnAssembliesLoaded(_assemblies, nonAbstractTypes);
            this.RegisterDefaultComponents();

            this._components.ForEach(item => item.Register(container));

            this._components.Where(item => item.MustbeInitialize())
                .Select(item => item.GetInstance(container))
                .Distinct()
                .Cast<IInitializer>()
                .ForEach(item => item.Initialize(container, this._assemblies));

            this._assemblies.Clear();
            this._components.Clear();

            this._assemblies = null;
            this._components = null;

            this.Start();

            this._stopwatch.Stop();

            Console.WriteLine("system is working, used time:{0}ms.\r\n", this._stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// 加载程序集
        /// </summary>
        public Bootstrapper LoadAssemblies(Assembly[] assemblies)
        {
            this._assemblies.Clear();
            this._assemblies.AddRange(assemblies);

            return this;
        }

        /// <summary>
        /// 加载程序集
        /// </summary>
        public Bootstrapper LoadAssemblies(string[] assemblyNames)
        {
            Assembly[] assemblies = assemblyNames.Select(Assembly.Load).ToArray();

            return this.LoadAssemblies(assemblies);
        }

        /// <summary>
        /// 扫描bin目录的程序集
        /// </summary>
        public Bootstrapper LoadAssemblies()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string relativeSearchPath = AppDomain.CurrentDomain.RelativeSearchPath;
            string binPath = string.IsNullOrEmpty(relativeSearchPath)
                                 ? baseDir
                                 : Path.Combine(baseDir, relativeSearchPath);

            // string applicationAssemblyDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bin");
            // if (!FileUtils.DirectoryExists(applicationAssemblyDirectory)) {
            // applicationAssemblyDirectory = AppDomain.CurrentDomain.BaseDirectory;
            // }
            Assembly[] assemblies = Directory.GetFiles(binPath).Where(
                file =>
                    {
                        string ext = Path.GetExtension(file).ToLower();
                        return ext.EndsWith(".dll") || ext.EndsWith(".exe");
                    }).Select(Assembly.LoadFrom).ToArray();

            return this.LoadAssemblies(assemblies);
        }

        /// <summary>
        /// 设置组件
        /// </summary>
        public Bootstrapper SetDefault(Type type, object instance, string name = null)
        {
            if (this.Status != ServerStatus.Running)
            {
                throw new ApplicationException(
                    "system is working, can not register type, please execute before 'Done' method.");
            }

            type.NotNull("type");

            this._components.Add(new Component(type, name, instance));

            return this;
        }

        /// <summary>
        /// 设置组件
        /// </summary>
        public Bootstrapper SetDefault(Type type, string name, Lifecycle lifecycle = Lifecycle.Singleton)
        {
            if (this.Status != ServerStatus.Running)
            {
                throw new ApplicationException(
                    "system is working, can not register type, please execute before 'Done' method.");
            }

            type.NotNull("type");

            this._components.Add(new Component(type, name, lifecycle));

            return this;
        }

        /// <summary>
        /// 设置组件
        /// </summary>
        public Bootstrapper SetDefault(Type from, Type to, string name, Lifecycle lifecycle = Lifecycle.Singleton)
        {
            if (this.Status != ServerStatus.Running)
            {
                throw new ApplicationException(
                    "system is working, can not register type, please execute before 'done' method.");
            }

            from.NotNull("from");
            to.NotNull("to");

            this._components.Add(new Component(from, to, name, lifecycle));

            return this;
        }

        /// <summary>
        ///     启动相关Processes
        /// </summary>
        public virtual void Start()
        {
            if (this.Status == ServerStatus.Started)
            {
                return;
            }

            ObjectContainer.Instance.ResolveAll<IProcessor>().ForEach(p => p.Start());

            this.Status = ServerStatus.Started;
        }

        /// <summary>
        ///     停止相关Processes
        /// </summary>
        public virtual void Stop()
        {
            if (this.Status == ServerStatus.Stopped)
            {
                return;
            }

            ObjectContainer.Instance.ResolveAll<IProcessor>().ForEach(p => p.Stop());

            this.Status = ServerStatus.Stopped;
        }


        private static bool FilterType(Type type)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            Type genericType = type.GetGenericTypeDefinition();

            return IsMessageHandlerInterfaceType(genericType) || IsQueryHandlerInterfaceType(genericType);
        }

        private static bool IsQueryHandlerInterfaceType(Type genericType)
        {
            return genericType == typeof(IQueryHandler<,>);
        }

        private static bool IsMessageHandlerInterfaceType(Type genericType)
        {
            return genericType == typeof(IMessageHandler<>) || genericType == typeof(IEnvelopedMessageHandler<>)
                   || genericType == typeof(ICommandHandler<>) || genericType == typeof(IEventHandler<>)
                   || genericType == typeof(IEventHandler<,>) || genericType == typeof(IEventHandler<,,>)
                   || genericType == typeof(IEventHandler<,,,>) || genericType == typeof(IEventHandler<,,,,>);
        }

        private void RegisterComponents(IEnumerable<Type> types)
        {
            IEnumerable<Type> registionTypes = types.Where(p => p.IsDefined(typeof(RegisterAttribute), false));

            foreach (Type type in registionTypes)
            {
                Lifecycle lifecycle = LifecycleAttribute.GetLifecycle(type);

                var attribute = type.GetSingleAttribute<RegisterAttribute>(false);
                if (attribute != null)
                {
                    Type contractType = attribute.ServiceType;
                    string contractName = attribute.Name;
                    if (contractType == null)
                    {
                        this.SetDefault(type, contractName, lifecycle);
                    }
                    else
                    {
                        this.SetDefault(contractType, type, contractName, lifecycle);
                    }
                }
            }
        }


        private void RegisterDefaultComponents()
        {
            this.SetDefault<ITextSerializer, DefaultTextSerializer>();
            this.SetDefault<ILoggerFactory, DefaultLoggerFactory>();
            this.SetDefault<IEventStore, EventStoreInMemory>();
            this.SetDefault<IEventPublishedVersionStore, EventPublishedVersionInMemory>();
            this.SetDefault<ISnapshotStore, NoneSnapshotStore>();
            this.SetDefault<ICache, LocalCache>();
            this.SetDefault<IRepository, MemoryRepository>();
            this.SetDefault<ICommandBus, CommandProducer>();
            this.SetDefault<IMessageReceiver<Envelope<ICommand>>, CommandProducer>();
            this.SetDefault<IEventBus, EventProducer>();
            this.SetDefault<IMessageReceiver<Envelope<EventCollection>>, EventProducer>();
            this.SetDefault<IMessageBus<IEvent>, MessageProducer<IEvent>>();
            this.SetDefault<IMessageReceiver<Envelope<IEvent>>, MessageProducer<IEvent>>();
            this.SetDefault<IMessageBus<IPublishableException>, MessageProducer<IPublishableException>>();
            this.SetDefault<IMessageReceiver<Envelope<IPublishableException>>, MessageProducer<IPublishableException>>();
            this.SetDefault<IQueryBus, QueryProducer>();
            this.SetDefault<IMessageReceiver<Envelope<IQuery>>, QueryProducer>();

            this.SetDefault<ICommandService, CentralService>();
            this.SetDefault<IQueryService, CentralService>();
            this.SetDefault<ISendReplyService, CentralService>();
            
            this.SetDefault<IProcessor, CommandConsumer>("command");
            this.SetDefault<IProcessor, EventConsumer>("evtcore");
            this.SetDefault<IProcessor, MessageConsumer<IEvent>>("event");
            this.SetDefault<IProcessor, MessageConsumer<IPublishableException>>("publish");
            this.SetDefault<IProcessor, QueryConsumer>("query");
        }

        private void RegisterHandlerAndFetcher(IEnumerable<Type> types)
        {
            foreach (Type type in types)
            {
                Lifecycle lifecycle = LifecycleAttribute.GetLifecycle(type);

                Type[] interfaceTypes = type.GetInterfaces();
                foreach (Type interfaceType in interfaceTypes.Where(FilterType))
                {
                    this.SetDefault(interfaceType, type, type.FullName, lifecycle);
                }
            }

            InnerHandlerProvider.Instance.Initialize(types);
        }

        #endregion


        private class Component
        {
            #region Constructors and Destructors

            public Component(Type type, string name, object instance)
            {
                this.ContractKey = new ObjectContainer.TypeRegistration(type, name);
                this.Instance = instance;
                this.Lifecycle = Lifecycle.Singleton;
            }

            public Component(Type type, string name, Lifecycle lifecycle)
                : this(type, type, name, lifecycle)
            {
            }

            public Component(Type from, Type to, string name, Lifecycle lifecycle)
            {
                this.ContractKey = new ObjectContainer.TypeRegistration(from, name);
                this.ImplementationType = to;
                this.Lifecycle = lifecycle;
            }

            #endregion

            #region Public Properties

            /// <summary>
            ///     要注册的类型
            /// </summary>
            public ObjectContainer.TypeRegistration ContractKey { get; set; }

            /// <summary>
            ///     要注册类型的实现类型
            /// </summary>
            public Type ImplementationType { get; set; }

            /// <summary>
            ///     要注册类型的实例
            /// </summary>
            public object Instance { get; private set; }

            /// <summary>
            ///     生命周期
            /// </summary>
            public Lifecycle Lifecycle { get; private set; }

            #endregion

            #region Methods and Operators

            /// <summary>
            /// 返回一个值，该值指示此实例是否与指定的对象相等。
            /// </summary>
            public override bool Equals(object obj)
            {
                var other = obj as Component;

                if (other == null)
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return this.ContractKey.Equals(other.ContractKey);
            }

            /// <summary>
            /// 返回此实例的哈希代码。
            /// </summary>
            public override int GetHashCode()
            {
                return this.ContractKey.GetHashCode();
            }

            public override string ToString()
            {
                return this.ContractKey.ToString();
            }

            internal object GetInstance(IObjectContainer container)
            {
                if (this.Instance != null)
                {
                    return this.Instance;
                }

                return container.Resolve(this.ContractKey);
            }

            internal bool MustbeInitialize()
            {
                return this.Lifecycle == Lifecycle.Singleton
                       && (IsInitializeType(this.ImplementationType) || (this.Instance is IInitializer));
            }

            internal void Register(IObjectContainer container)
            {
                if (container.IsRegistered(this.ContractKey))
                {
                    return;
                }

                if (this.Instance != null)
                {
                    container.RegisterInstance(this.ContractKey, this.Instance);
                    return;
                }

                container.RegisterType(this.ContractKey, this.ImplementationType, this.Lifecycle);
            }

            private static bool IsInitializeType(Type type)
            {
                return type != null && type.IsClass && !type.IsAbstract && typeof(IInitializer).IsAssignableFrom(type);
            }

            #endregion
        }
    }

    /// <summary>
    ///     <see cref="Bootstrapper" /> 的扩展类
    /// </summary>
    public static class BootstrapperExtentions
    {
        #region Public Methods and Operators

        /// <summary>
        /// 注册类型
        /// </summary>
        public static Bootstrapper SetDefault(
            this Bootstrapper that, 
            Type type, 
            Lifecycle lifecycle = Lifecycle.Singleton)
        {
            return that.SetDefault(type, null, lifecycle);
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public static Bootstrapper SetDefault(
            this Bootstrapper that, 
            Type from, 
            Type to, 
            Lifecycle lifecycle = Lifecycle.Singleton)
        {
            return that.SetDefault(from, to, null, lifecycle);
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public static Bootstrapper SetDefault<T>(this Bootstrapper that, T instance, string name = null)
        {
            return that.SetDefault(typeof(T), instance, name);
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
        public static Bootstrapper SetDefault<T>(
            this Bootstrapper that, 
            string name, 
            Lifecycle lifecycle = Lifecycle.Singleton)
        {
            return that.SetDefault(typeof(T), name, lifecycle);
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public static Bootstrapper SetDefault<TFrom, TTo>(
            this Bootstrapper that, 
            Lifecycle lifecycle = Lifecycle.Singleton) where TTo : TFrom
        {
            return that.SetDefault<TFrom, TTo>((string)null, lifecycle);
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public static Bootstrapper SetDefault<TFrom, TTo>(
            this Bootstrapper that, 
            string name, 
            Lifecycle lifecycle = Lifecycle.Singleton) where TTo : TFrom
        {
            return that.SetDefault(typeof(TFrom), typeof(TTo), name, lifecycle);
        }

        #endregion
    }
}