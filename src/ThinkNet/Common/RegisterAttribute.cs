using System;

namespace ThinkNet.Common
{
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
        public RegisterAttribute(string contractName)
        {
            contractName.NotNullOrWhiteSpace("contractName");
            this.ContractName = contractName;
        }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public RegisterAttribute(Type contractType)
        {
            contractType.NotNull("contractType");
            this.ContractType = contractType;
        }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public RegisterAttribute(string contractName, Type contractType)
        {
            contractName.NotNullOrWhiteSpace("contractName");
            contractType.NotNull("contractType");

            this.ContractName = contractName;
            this.ContractType = contractType;
        }

        /// <summary>
        /// 注册的名称
        /// </summary>
        public string ContractName { get; private set; }
        /// <summary>
        /// 注册的类型
        /// </summary>
        public Type ContractType { get; private set; }

        /// <summary>
        /// 是否创建实例。
        /// </summary>
        public bool Created { get; set; }

        /// <summary>
        /// 构造函数的参数。
        /// </summary>
        public object[] ConstructorParameters { get; set; }
    }
}
