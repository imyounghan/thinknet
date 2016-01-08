using System;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 标记此特性的类型是运行时必须的组件。
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true)]
    public class RequiredComponentAttribute : Attribute
    {
        /// <summary>
        /// default constructor.
        /// </summary>
        public RequiredComponentAttribute()
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public RequiredComponentAttribute(Type serviceType)
        {
            Ensure.NotNull(serviceType, "serviceType");

            _serviceType = serviceType;
        }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public RequiredComponentAttribute(Type serviceType, string registerName)
            : this(serviceType)
        {
            Ensure.NotNullOrWhiteSpace(registerName, "registerName");

            _registerName = registerName;
        }


        private string _registerName;
        /// <summary>
        /// 注册的名称
        /// </summary>
        public string RegisterName
        {
            get { return _registerName; }
        }

        private Type _serviceType;
        /// <summary>
        /// 要注册的服务类型。
        /// </summary>
        public Type ServiceType
        {
            get { return _serviceType; }
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

        private bool _created = false;
        /// <summary>
        /// 是否创建实例。
        /// </summary>
        public bool CreateInstance
        {
            get { return _created; }
            set { _created = value; }
        }

        private object[] _parameters = new object[0];
        /// <summary>
        /// 构造函数的参数。
        /// </summary>
        public object[] ConstructorParameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        /// <summary>
        /// 获取最终的注册名称。
        /// </summary>
        public string GetFinalRegisterName()
        {
            return _registerTypeName ? _serviceType.FullName : _registerName;
        }
    }
}
