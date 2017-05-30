
namespace ThinkNet
{
    using System;


    /// <summary>
    /// 标记此特性的类型将要注册到容器中。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterAttribute : Attribute
    {
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public RegisterAttribute()
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public RegisterAttribute(string name)
        {
            name.NotNullOrWhiteSpace("name");
            this.Name = name;
        }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public RegisterAttribute(Type serviceType)
        {
            serviceType.NotNull("serviceType");
            this.ServiceType = serviceType;
        }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public RegisterAttribute(string name, Type serviceType)
        {
            name.NotNullOrWhiteSpace("name");
            serviceType.NotNull("serviceType");

            this.Name = name;
            this.ServiceType = serviceType;
        }

        /// <summary>
        /// 注册的名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 注册的类型
        /// </summary>
        public Type ServiceType { get; private set; }
    }
}
