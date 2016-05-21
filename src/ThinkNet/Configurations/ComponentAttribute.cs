using System;

namespace ThinkNet.Configurations
{
    /// <summary>
    /// 标记此特性的类型将要注册到容器中。
    /// </summary>
    public abstract class ComponentAttribute : Attribute
    {
        private string _registerName;
        /// <summary>
        /// 注册的名称
        /// </summary>
        public string RegisterName
        {
            get { return _registerName; }
            set { _registerName = value; }
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

        private Type _implementType;
        /// <summary>
        /// 要注册类型的实现类。如果为null则表示实现类是注册类型本身。
        /// </summary>
        public Type ImplementType
        {
            get { return _implementType; }
            set { _implementType = value; }
        }

        private Type _registerType;
        /// <summary>
        /// 要注册的类型。
        /// </summary>
        public Type RegisterType
        {
            get { return _registerType; }
            set { _registerType = value; }
        }

        ///// <summary>
        ///// 注册顺序
        ///// </summary>
        //public int Order { get; set; }

        /// <summary>
        /// 类型注册
        /// </summary>
        public virtual void Register(Type sourceType, Action<ComponentAttribute> action)
        {
            action(this);
        }
        ///// <summary>
        ///// 类型注册
        ///// </summary>
        //protected void Register(Bootstrapper container)
        //{
        //    Type registerType = _registerType ?? _implementType;
        //    Type implementType = _implementType ?? _registerType;
        //    var registerName = _registerTypeName ? implementType.FullName : _registerName; ;
        //    var lifecycle = (Lifecycle)LifeCycleAttribute.GetLifecycle(implementType);
        //    if (lifecycle == Lifecycle.Singleton) { //如果是单例，先考虑搜索当前类的静态实例
        //        var member = implementType.GetMember("Instance", MemberTypes.Field | MemberTypes.Property,
        //            BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase).FirstOrDefault();

        //        if (member != null && registerType.IsAssignableFrom(member.DeclaringType)) {
        //            container.RegisterInstance(registerType, member.GetMemberValue(null), registerName);
        //            return;
        //        }

        //        if (this.CreateInstance) {
        //            var instance = this.ConstructorParameters.IsEmpty() ?
        //                Activator.CreateInstance(implementType) : Activator.CreateInstance(implementType, this.ConstructorParameters);
        //            container.RegisterInstance(registerType, instance, registerName);
        //            return;
        //        }
        //    }

        //    if (_implementType != null && _registerType != null) {
        //        container.RegisterType(_registerType, _implementType, lifecycle, registerName);
        //    }
        //    else {
        //        container.RegisterType(_registerType, lifecycle, registerName);
        //    }
        //}
    }
}
