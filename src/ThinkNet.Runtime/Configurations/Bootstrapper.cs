using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ThinkLib.Common;
using ThinkLib.Logging;
using ThinkLib.Utilities;

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
        public class TypeRegistration
        {
            /// <summary>
            /// Parameterized constructor.
            /// </summary>
            public TypeRegistration(Type type, object instance, string name)
                : this(type, name)
            {
                this.Instance = instance;
            }
            /// <summary>
            /// Parameterized constructor.
            /// </summary>
            public TypeRegistration(Type type, string name)
                : this(type, name, Lifecycle.Singleton)
            { }
            /// <summary>
            /// Parameterized constructor.
            /// </summary>
            public TypeRegistration(Type type, string name, Lifecycle lifecycle)
            {
                this.RegisterType = type;
                this.Name = name ?? string.Empty;
                this.Lifecycle = lifecycle;
            }
            /// <summary>
            /// Parameterized constructor.
            /// </summary>
            public TypeRegistration(Type from, Type to, string name, Lifecycle lifecycle)
                : this(from, name, lifecycle)
            {
                this.ImplementationType = to;
            }

            /// <summary>
            /// 要注册的名称
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// 要注册的类型
            /// </summary>
            public Type RegisterType { get; private set; }
            /// <summary>
            /// 要注册类型的实例
            /// </summary>
            public object Instance { get; private set; }
            /// <summary>
            /// 要注册类型的实现类型
            /// </summary>
            public Type ImplementationType { get; private set; }
            /// <summary>
            /// 生命周期
            /// </summary>
            public Lifecycle Lifecycle { get; private set; }

            /// <summary>
            /// 返回一个值，该值指示此实例是否与指定的对象相等。
            /// </summary>
            public override bool Equals(object obj)
            {
                var other = obj as TypeRegistration;

                if (other == null)
                    return false;

                if (this.RegisterType != other.RegisterType)
                    return false;

                if (!String.Equals(this.Name, other.Name))
                    return false;

                return true;
            }
            /// <summary>
            /// 返回此实例的哈希代码。
            /// </summary>
            public override int GetHashCode()
            {
                return String.Concat(this.RegisterType.FullName, "|", this.Name).GetHashCode();
            }

            internal int GetUniqueCode()
            {
                if (IsInitializer(this.Instance))
                    return this.Instance.GetHashCode();

                if (IsInitializeType(this.ImplementationType))
                    return this.ImplementationType.GetHashCode();

                if (IsInitializeType(this.RegisterType))
                    return this.RegisterType.GetHashCode();


                return this.GetHashCode();
            }

            internal bool NeedInitialize()
            {
                return IsInitializer(this.Instance) || IsInitializeType(this.RegisterType) || IsInitializeType(this.ImplementationType);
            }

            internal object GetInstance(Func<Type, string, object> resolve)
            {
                return this.Instance ?? resolve(this.RegisterType, this.Name);
            }
        }


        /// <summary>
        /// 当前配置
        /// </summary>
        public static readonly Bootstrapper Current = new Bootstrapper();


        private readonly List<Assembly> _assemblies;
        private readonly List<TypeRegistration> _registerComponents;
        private readonly HashSet<int> _registeredObjects;
        private readonly HashSet<int> _initializedObjects;
        private Bootstrapper()
        {
            this._assemblies = new List<Assembly>();
            this._registerComponents = new List<TypeRegistration>();
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
                .ToArray();

            return this.LoadAssemblies(assemblies);
        }



        private bool _running = false;
        /// <summary>
        /// 配置完成。
        /// </summary>
        public void Done(Action<TypeRegistration> typeRegistry, Func<Type, string, object> typeResolve)
        {
            if (_running)
                return;


            if (_assemblies.Count == 0) {
                this.LoadAssemblies();

                LogManager.GetLogger("ThinkNet").InfoFormat("load assemblies[{0}] completed.",
                    string.Join(" | ", _assemblies.Select(item => item.FullName)));
            }


            var allTypes = _assemblies.SelectMany(assembly => assembly.GetTypes()).ToArray();

            foreach (var type in allTypes) {
                type.GetAttributes<ComponentAttribute>(false)
                    .ForEach(attribute => attribute.Register(type, RegisterComponent));
            }            

            while (_registerComponents.Count > 0) {
                _registerComponents.ForEach(typeRegistry);
                
                var initializers = _registerComponents.Where(item => item.NeedInitialize() && _initializedObjects.Add(item.GetUniqueCode())).ToArray();
                _registerComponents.Clear();

                initializers.Select(item => item.GetInstance(typeResolve)).Cast<IInitializer>().ForEach(item => item.Initialize(allTypes));             
            }


            _initializedObjects.Clear();
            _registeredObjects.Clear();
            _assemblies.Clear();


            _running = true;

            LogManager.GetLogger("ThinkNet").Info("system is running.");
        }

        /// <summary>
        /// 注册实例
        /// </summary>
        public Bootstrapper RegisterInstance(object instance, params Type[] types)
        {
            types.NotNull("types");

            foreach (var type in types) {
                this.RegisterInstance(type, instance);
            }

            return this;
        }

        /// <summary>
        /// 注册实例
        /// </summary>
        public Bootstrapper RegisterInstance(Type type, object instance, string name = null)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register instance, please execute before 'done' method.");
            }

            type.NotNull("type");
            instance.NotNull("instance");

            AddComponent(new TypeRegistration(type, instance, name));


            return this;
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public Bootstrapper RegisterType(Type type, Lifecycle lifecycle = Lifecycle.Singleton, string name = null)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register type, please execute before 'done' method.");
            }
            type.NotNull("type");
            if (!type.IsClass || type.IsAbstract) {
                throw new ApplicationException(string.Format("the type of '{0}' must be a class and cannot be abstract.", type.FullName));
            }


            AddComponent(new TypeRegistration(type, name, lifecycle));

            return this;
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public Bootstrapper RegisterType(Type from, Type to, Lifecycle lifecycle = Lifecycle.Singleton, string name = null)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register type, please execute before 'done' method.");
            }
            from.NotNull("from");
            to.NotNull("to");
            if (!to.IsClass || to.IsAbstract) {
                throw new ApplicationException(string.Format("the type of '{0}' must be a class and cannot be abstract.", to.FullName));
            }

            AddComponent(new TypeRegistration(from, to, name, lifecycle));

            return this;
        }

        private void RegisterComponent(ComponentAttribute component)
        {
            Type registerType = component.RegisterType ?? component.ImplementType;
            Type implementType = component.ImplementType ?? component.RegisterType;
            var registerName = component.RegisterTypeName ? implementType.FullName : component.RegisterName;
            var lifecycle = (Lifecycle)LifeCycleAttribute.GetLifecycle(implementType);
            if (lifecycle == Lifecycle.Singleton) { //如果是单例，先考虑搜索当前类的静态实例
                var member = implementType.GetMember("Instance", MemberTypes.Field | MemberTypes.Property,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase).FirstOrDefault();

                if (member != null && registerType.IsAssignableFrom(member.DeclaringType)) {
                    this.RegisterInstance(registerType, member.GetMemberValue(null), registerName);
                    return;
                }

                if (component.CreateInstance) {
                    var instance = component.ConstructorParameters.IsEmpty() ?
                        Activator.CreateInstance(implementType) : Activator.CreateInstance(implementType, component.ConstructorParameters);
                    this.RegisterInstance(registerType, instance, registerName);
                    return;
                }
            }

            if (component.RegisterType != null && component.ImplementType != null) {
                this.RegisterType(component.RegisterType, component.ImplementType, lifecycle, registerName);
            }
            else {
                this.RegisterType(component.RegisterType, lifecycle, registerName);
            }
        }

        private void AddComponent(TypeRegistration registration)
        {
            if (_registeredObjects.Add(registration.GetHashCode()))
                _registerComponents.Add(registration);
        }

        private static bool IsInitializeType(Type type)
        {
            return !type.IsNull() && type.IsClass && !type.IsAbstract && typeof(IInitializer).IsAssignableFrom(type);
        }

        private static bool IsInitializer(object instance)
        {
            return !instance.IsNull() && instance is IInitializer;
        }
    }
}
