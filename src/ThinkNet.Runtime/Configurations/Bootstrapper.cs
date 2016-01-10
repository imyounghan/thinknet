using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ThinkLib.Utilities;
using ThinkNet.Annotation;
using ThinkNet.Common;
using ThinkNet.Component;
using ThinkNet.Infrastructure;
using ThinkNet.Runtime.Logging;


namespace ThinkNet.Configurations
{
    /// <summary>
    /// 引导程序
    /// </summary>
    public sealed class Bootstrapper
    {
        //public static Configuration Create()
        //{
        //    return new Configuration(new TinyContainer());
        //}
        /// <summary>
        /// 当前配置
        /// </summary>
        public static readonly Bootstrapper Current = new Bootstrapper();


        private readonly List<Assembly> _assemblies;
        private readonly List<IInitializer> _initializers;
        private readonly List<Type> _initializeTypes;
        private readonly HashSet<ComponentRegistration> _registeredComponents;
        private Bootstrapper()
        {
            this._assemblies = new List<Assembly>();
            this._initializers = new List<IInitializer>();
            this._initializeTypes = new List<Type>();
            this._registeredComponents = new HashSet<ComponentRegistration>();
        }


        ///// <summary>
        ///// 对象容器
        ///// </summary>
        //public IObjectContainer Container { get; private set; }


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
        

        private bool _running = false;
        /// <summary>
        /// 配置完成。
        /// </summary>
        public void Done()
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

            //_registeredComponents.ForEach(type => type.Register(ObjectContainer.Instance));
            //_initializeTypes.Select(ObjectContainer.Instance.Resolve).OfType<IInitializer>().Concat(_initializers)
            //    .ForEach(initializer => {
            //        initializer.Initialize(ObjectContainer.Instance, allTypes);
            //    });
            
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
        public Bootstrapper RegisterInstance(Type type, object instance, string name = null)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register instance, please execute before 'done' method.");
            }

            Ensure.NotNull(type, "type");
            Ensure.NotNull(instance, "instance");

            _registeredComponents.Add(new ComponentRegistration(type, instance, name));

            if (IsInitializer(instance)) {
                _initializers.Add((IInitializer)instance);
            }

            return this;
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public Bootstrapper RegisterType(Type type, Lifetime lifetime = Lifetime.Singleton, string name = null)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register type, please execute before 'done' method.");
            }

            Ensure.NotNull(type, "type");

            var result = _registeredComponents.Add(new ComponentRegistration(type, name, lifetime));
            if (lifetime == Lifetime.Singleton && IsInitializerType(type)) {
                _initializeTypes.Add(type);
            }

            return this;
        }
        
        /// <summary>
        /// 注册类型
        /// </summary>
        public Bootstrapper RegisterType(Type from, Type to, Lifetime lifetimeType = Lifetime.Singleton, string name = null)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register type, please execute before 'done' method.");
            }

            Ensure.NotNull(from, "from");
            Ensure.NotNull(to, "to");

            _registeredComponents.Add(new ComponentRegistration(from, to, name, lifetimeType));
            if (lifetimeType == Lifetime.Singleton && IsInitializerType(to)) {
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
                var lifecycle = (Lifetime)LifeCycleAttribute.GetLifecycle(type);
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
                var lifecycle = (Lifetime)LifeCycleAttribute.GetLifecycle(type);
                var serviceType = component.ServiceType;

                if (lifecycle == Lifetime.Singleton) {
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


        public class ComponentRegistration
        {
            private readonly int _hashCode;
            private readonly Type _registerType;
            private readonly string _name;
            private readonly object _instance;
            private readonly Type _serviceType;
            private readonly Lifetime _lifetime;


            public ComponentRegistration(Type type, string name)
            {
                this._registerType = type;
                this._name = name;

                this._hashCode = String.Concat(type.FullName, "|", name).GetHashCode();
            }

            public ComponentRegistration(Type type, string name, Lifetime lifetime)
                : this(type, name)
            {
                this._lifetime = lifetime;
            }

            public ComponentRegistration(Type type, object instance, string name)
                : this(type, name)
            {
                this._instance = instance;
            }

            public ComponentRegistration(Type from, Type to, string name, Lifetime lifetime)
                : this(from, name)
            {
                this._serviceType = to;
                this._lifetime = lifetime;
            }

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
                var typeRegistration = obj as ComponentRegistration;

                if (typeRegistration == null)
                    return false;

                if (_registerType != typeRegistration._registerType)
                    return false;

                if (!String.Equals(_name, typeRegistration._name))
                    return false;

                return true;
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }
        }
    }
}
