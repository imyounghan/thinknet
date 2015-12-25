using System;
using ThinkNet.Infrastructure;


namespace ThinkNet.Annotation
{
    /// <summary>
    /// 表示该特性的类会被注册到IOC容器中
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    public class RegisterComponentAttribute : Attribute
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public RegisterComponentAttribute()
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public RegisterComponentAttribute(string registerName)
        {
            Ensure.NotNullOrWhiteSpace(registerName, "registerName");

            _registerName = registerName;
        }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public RegisterComponentAttribute(Type registerType)
        {
            Ensure.NotNull(registerType, "registerType");

            _registerType = registerType;
        }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public RegisterComponentAttribute(Type registerType, string registerName)
            : this(registerType)
        {
            Ensure.NotNullOrWhiteSpace(registerName, "registerName");

            _registerName = registerName;
        }

        private Type _registerType;
        /// <summary>
        /// 要注册的服务类型。
        /// </summary>
        public Type RegisterType
        {
            get { return _registerType; }
        }

        private string _registerName;
        /// <summary>
        /// 注册的名称
        /// </summary>
        public string RegisterName
        {
            get { return _registerName; }
        }

        private bool _registerTypeName = false;
        /// <summary>
        /// 使用类型名称
        /// </summary>
        public bool RegisterTypeName
        {
            get { return _registerTypeName; }
            set { _registerTypeName = value; }
        }

        /// <summary>
        /// 获取最终的注册名称。
        /// </summary>
        public string GetFinalRegisterName(Type registerType)
        {
            return _registerTypeName ? registerType.FullName : _registerName;
        }


        ///// <summary>
        ///// 注册类型
        ///// </summary>
        //protected virtual void RegisterType(IObjectContainer container, Type type, string name)
        //{
        //    if (string.IsNullOrEmpty(name) ? container.IsRegistered(type) : container.IsRegistered(type, name)) {
        //        throw new ApplicationException(string.Format("This type {0} has been registered.", type.FullName));
        //    }

        //    var lifetimeStyle = LifeCycleAttribute.GetLifetimeStyle(type);

        //        container.RegisterType(type, name, lifetimeStyle);
        //}

        ///// <summary>
        ///// 注册类型
        ///// </summary>
        //protected virtual void RegisterType(IObjectContainer container, Type from, Type to, string name)
        //{
        //    if (string.IsNullOrEmpty(name) ? container.IsRegistered(from) : container.IsRegistered(from, name)) {
        //        throw new ApplicationException(string.Format("This type {0} has been registered.", from.FullName));
        //    }

        //    var lifetimeStyle = LifeCycleAttribute.GetLifetimeStyle(to);

        //    container.RegisterType(from, to, name, lifetimeStyle);
        //}

        //private static bool IgnoredType(Type type)
        //{
        //    return type.IsDefined<NonRegisteredAttribute>(false) || type.FullName.StartsWith("System.");
        //}

        ///// <summary>
        ///// 执行注册
        ///// </summary>
        //public void Register(IObjectContainer container, Type implementationType, Func<Type, bool> hasRegistered)
        //{
        //    bool checkInterface = true;
        //    if (scanningInterface && registerTypes.Length == 0) {
        //        registerTypes = implementationType.GetInterfaces().Where(type => !IgnoredType(type)).ToArray();
        //        checkInterface = false;
        //    }

        //    var implementationName = implementationType.FullName;
        //    if (registerTypes.Length == 0) {
        //        if (!hasRegistered(implementationType)) {
        //            this.RegisterType(container, implementationType, registerTypeName ? implementationName : registerName);
        //        }

        //        return;
        //    }            

            
        //    Array.ForEach(registerTypes, (Type serviceType) => {
        //        if (hasRegistered(serviceType))
        //            return;

        //        if (checkInterface && !serviceType.IsAssignableFrom(implementationType))
        //            throw new ApplicationException(string.Format("Type {0} is not inherited from type {1}.", implementationName, serviceType.FullName));

        //        this.RegisterType(container, serviceType, implementationType, registerTypeName ? implementationName : registerName);
        //    });
        //}
    }
}
