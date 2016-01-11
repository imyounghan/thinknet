using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.ServiceLocation;
using ThinkLib.Utilities;
using ThinkNet.Common;
using TinyIoC;


namespace ThinkNet.Configurations
{
    /// <summary>
    /// 引导程序
    /// </summary>
    public sealed class Configuration
    {
        public class ServiceRegistration
        {
            public ServiceRegistration(Type type, object instance, string name)
                : this(type, name)
            {
                this.Instance = instance;
                this.Lifecycle = Configurations.Lifecycle.Singleton;
            }

            public ServiceRegistration(Type type, string name)
                : this(type, type, name, Configurations.Lifecycle.Singleton)
            { }

            public ServiceRegistration(Type type, string name, Lifecycle lifecycle)
                : this(type, type, name, lifecycle)
            { }

            public ServiceRegistration(Type from, Type to, string name, Lifecycle lifecycle)
                : this(from, name, lifecycle)
            {
                this.RegisterType = from;
                this.Name = name;
                this.ImplementationType = to;
                this.Lifecycle = lifecycle;
            }

            public string Name { get; set; }

            public Type RegisterType { get; private set; }

            public object Instance { get; private set; }

            public Type ImplementationType { get; private set; }

            public Lifecycle Lifecycle { get; private set; }

            //public void Register(IContainer container)
            //{
            //    if (_instance != null) {
            //        container.RegisterInstance(_registerType, _instance, _name);
            //        return;
            //    }

            //    if (_serviceType != null) {
            //        container.RegisterType(_registerType, _serviceType, _name, _lifetimeType);
            //        return;
            //    }

            //    container.RegisterType(_registerType, _name, _lifetimeType);
            //}

            public override bool Equals(object obj)
            {
                var other = obj as ServiceRegistration;

                if (other == null)
                    return false;

                if (this.RegisterType != other.RegisterType)
                    return false;

                if (!String.Equals(this.Name, other.Name))
                    return false;

                return true;
            }

            public override int GetHashCode()
            {
                return String.Concat(this.RegisterType.FullName, "|", this.Name).GetHashCode();
            }
        }

        //public static Configuration Create()
        //{
        //    return new Configuration(new TinyContainer());
        //}
        /// <summary>
        /// 当前配置
        /// </summary>
        public static readonly Configuration Current = new Configuration();


        private readonly List<Assembly> _assemblies;
        private readonly List<IInitializer> _initializers;
        private readonly List<Type> _initializeTypes;
        private readonly HashSet<ServiceRegistration> _registeredComponents;
        private Configuration()
        {
            this._assemblies = new List<Assembly>();
            this._initializers = new List<IInitializer>();
            this._initializeTypes = new List<Type>();
            this._registeredComponents = new HashSet<ServiceRegistration>();
        }


        ///// <summary>
        ///// 对象容器
        ///// </summary>
        //public IObjectContainer Container { get; private set; }


        /// <summary>
        /// 加载程序集
        /// </summary>
        public Configuration LoadAssemblies(Assembly[] assemblies)
        {
            _assemblies.Clear();
            _assemblies.AddRange(assemblies);

            return this;
        }

        /// <summary>
        /// 扫描bin目录的程序集
        /// </summary>
        public Configuration LoadAssemblies()
        {
            string applicationAssemblyDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bin");
            if (!FileUtils.DirectoryExists(applicationAssemblyDirectory)) {
                applicationAssemblyDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }


            var assemblies = Directory.GetFiles(applicationAssemblyDirectory)
                .Where(file => {
                    var ext = Path.GetExtension(file).ToLower();
                    return ext.EndsWith(".dll") || ext.EndsWith(".exe");
                })
                .Select(Assembly.LoadFrom)
                //.Where(assembly => assembly.IsDefined<ParticipateInRuntimeAttribute>(false))
                //.OrderBy(assembly => assembly.GetAttribute<ParticipateInRuntimeAttribute>(false).Order)
                .ToArray();

            return this.LoadAssemblies(assemblies);
        }

        public void Done()
        {
            TinyContainer container = new TinyContainer();
            ServiceLocator.SetLocatorProvider(() => new TinyIoCServiceLocator(container));

            Action<ServiceRegistration> action = new Action<ServiceRegistration>(component => {
                if (component.RegisterType == null)
                    return;

                if (component.Instance != null) {
                    container.Register(component.RegisterType, component.Instance, component.Name ?? string.Empty).AsSingleton();
                    return;
                }

                if (component.ImplementationType != null) {
                    var option =container.Register(component.RegisterType, component.ImplementationType, component.Name ?? string.Empty);
                    switch (component.Lifecycle) {
                        case Lifecycle.Singleton:
                            option.AsSingleton();
                            break;
                        case Lifecycle.Transient:
                            option.AsMultiInstance();
                            break;
                        case Lifecycle.PerSession:
                            option.AsPerSession();
                            break;
                        case Lifecycle.PerThread:
                            option.AsPerThread();
                            break;
                    }
                    return;
                }
            });
        }


        private bool _running = false;
        /// <summary>
        /// 配置完成。
        /// </summary>
        public void Done(Action<ServiceRegistration> registration)
        {
            if (_running)
                return;


            if (_assemblies.Count == 0) {
                this.LoadAssemblies();

                //if (logger.IsDebugEnabled) {
                //    logger.Write(LoggingLevel.DEBUG, "load assemblies[{0}] completed.",
                //        string.Join(",", _assemblies.Select(item => item.FullName).ToArray()));
                //}
            }


            var allTypes = _assemblies.SelectMany(assembly => assembly.GetTypes()).ToArray();

            allTypes.Where(IsRegisteredComponent).ForEach(RegisterComponent);
            allTypes.Where(IsRequiredComponent).ForEach(RegisterRequiredComponent);

            _registeredComponents.ForEach(registration);
            _initializeTypes.Select(ServiceLocator.Current.GetInstance).OfType<IInitializer>().Concat(_initializers)
                .ForEach(initializer => initializer.Initialize(allTypes));
            
            _assemblies.Clear();
            _initializers.Clear();
            _initializeTypes.Clear();
            _registeredComponents.Clear();
            

            _running = true;

            //if (logger.IsDebugEnabled) {
            //    logger.Write(LoggingLevel.DEBUG, "system is run.");
            //}
        }

        
        /// <summary>
        /// 注册实例
        /// </summary>
        public Configuration RegisterInstance(Type type, object instance, string name = null)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register instance, please execute before 'done' method.");
            }

            Ensure.NotNull(type, "type");
            Ensure.NotNull(instance, "instance");

            _registeredComponents.Add(new ServiceRegistration(type, instance, name));

            if (IsInitializer(instance)) {
                _initializers.Add((IInitializer)instance);
            }

            return this;
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public Configuration RegisterType(Type type, Lifecycle lifecycle = Lifecycle.Singleton, string name = null)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register type, please execute before 'done' method.");
            }

            Ensure.NotNull(type, "type");

            var result = _registeredComponents.Add(new ServiceRegistration(type, name, lifecycle));
            if (lifecycle == Lifecycle.Singleton && IsInitializerType(type)) {
                _initializeTypes.Add(type);
            }

            return this;
        }
        
        /// <summary>
        /// 注册类型
        /// </summary>
        public Configuration RegisterType(Type from, Type to, Lifecycle lifecycle = Lifecycle.Singleton, string name = null)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register type, please execute before 'done' method.");
            }

            Ensure.NotNull(from, "from");
            Ensure.NotNull(to, "to");

            _registeredComponents.Add(new ServiceRegistration(from, to, name, lifecycle));
            if (lifecycle == Lifecycle.Singleton && IsInitializerType(to)) {
                _initializeTypes.Add(from);
            }

            return this;
        }


        private static bool IsInitializerType(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(IInitializer).IsAssignableFrom(type);
        }

        private static bool IsInitializer(object instance)
        {
            return instance is IInitializer;
        }

        private bool IsRegisteredComponent(Type type)
        {
            return type.IsClass && !type.IsAbstract && type.IsDefined<RegisterComponentAttribute>(false);
        }

        private bool IsRequiredComponent(Type type)
        {
            return type.IsDefined<RequiredComponentAttribute>(false);
        }

        private void RegisterComponent(Type type)
        {
            var components = type.GetAttributes<RegisterComponentAttribute>(false);
            foreach (var component in components) {
                var name = component.GetFinalRegisterName(type);
                var lifecycle = (Lifecycle)LifeCycleAttribute.GetLifecycle(type);
                var registerType = component.RegisterType;

                if (registerType == null) {
                    this.RegisterType(type, lifecycle, name);
                }
                else {
                    this.RegisterType(registerType, type, lifecycle, name);
                }
            }
        }

        private void RegisterRequiredComponent(Type type)
        {
            var components = type.GetAttributes<RequiredComponentAttribute>(false);
            foreach (var component in components) {
                var name = component.GetFinalRegisterName();
                var lifecycle = (Lifecycle)LifeCycleAttribute.GetLifecycle(type);
                var serviceType = component.ServiceType;

                if (lifecycle == Lifecycle.Singleton) {
                    var member = serviceType.GetMember("Instance", MemberTypes.Field | MemberTypes.Property,
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase).FirstOrDefault();

                    if (member != null) {
                        this.RegisterInstance(type, member.GetMemberValue(null), name);
                        continue;
                    }

                    if (component.CreateInstance) {
                        var instance = component.ConstructorParameters == null || component.ConstructorParameters.Length == 0 ?
                            Activator.CreateInstance(serviceType) : Activator.CreateInstance(serviceType, component.ConstructorParameters);
                        this.RegisterInstance(type, instance, name);
                        continue;
                    }
                }

                if (serviceType == null) {
                    this.RegisterType(type, lifecycle, name);
                }
                else {
                    this.RegisterType(type, serviceType, lifecycle, name);
                }
            }
        }


        
    }
}
