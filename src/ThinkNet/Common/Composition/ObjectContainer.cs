using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThinkNet.Common.Composition
{
    /// <summary>
    /// <see cref="IObjectContainer"/>抽象实现类
    /// </summary>
    public abstract class ObjectContainer : DisposableObject, IObjectContainer
    {
        /// <summary>
        /// 类型注册
        /// </summary>
        public sealed class TypeRegistration
        {
            private readonly int _hashCode;

            /// <summary>
            /// 类型
            /// </summary>
            public Type Type { get; private set; }
            /// <summary>
            /// 名称
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Parameterized Constructor.
            /// </summary>
            public TypeRegistration(Type type)
                : this(type, string.Empty)
            { }
            /// <summary>
            /// Parameterized Constructor.
            /// </summary>
            public TypeRegistration(Type type, string name)
            {
                this.Type = type;
                this.Name = name;

                if(string.IsNullOrEmpty(name)) {
                    this._hashCode = type.FullName.GetHashCode();
                }
                else {
                    this._hashCode = String.Concat(type.FullName, "|", name).GetHashCode();
                }
            }

            /// <summary>
            /// 判断该实例与当前实例是否相同
            /// </summary>
            public override bool Equals(object obj)
            {
                var other = obj as TypeRegistration;

                if(other == null)
                    return false;

                if(this.Type != other.Type)
                    return false;

                if(!String.Equals(this.Name, other.Name, StringComparison.Ordinal))
                    return false;

                return true;
            }

            /// <summary>
            /// 获取该实例的哈希代码
            /// </summary>
            public override int GetHashCode()
            {
                return this._hashCode;
            }
        }

        /// <summary>
        /// single instance
        /// </summary>
        public static IObjectContainer Instance
        {
            get;
            internal set;
        }

        private readonly List<TypeRegistration> _registeredTypes;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        protected ObjectContainer()
        {
            this._registeredTypes = new List<TypeRegistration>();
        }

        /// <summary>
        /// 获取已注册的类型列表
        /// </summary>
        public IReadOnlyCollection<TypeRegistration> RegisteredTypes
        {
            get
            {
                return new ReadOnlyCollection<TypeRegistration>(_registeredTypes);
            }
        }

        /// <summary>
        /// 获取类型对应的实例
        /// </summary>
        public abstract object Resolve(Type type, string name);
        /// <summary>
        /// 获取类型所有的实例
        /// </summary>
        public abstract IEnumerable<object> ResolveAll(Type type);


        /// <summary>
        /// 注册一个实例
        /// </summary>
        public abstract void RegisterInstance(Type type, string name, object instance);

        /// <summary>
        /// 注册一个类型
        /// </summary>
        public abstract void RegisterType(Type type, string name, Lifecycle lifetime);

        /// <summary>
        /// 注册一个类型
        /// </summary>
        public abstract void RegisterType(Type from, Type to, string name, Lifecycle lifetime);

        /// <summary>
        /// 判断此类型是否已注册
        /// </summary>
        public abstract bool IsRegistered(Type type, string name);


        void IObjectContainer.RegisterInstance(Type type, string name, object instance)
        {
            type.NotNull("type");
            instance.NotNull("instance");

            var typeRegistration = new TypeRegistration(type, name);

            if(_registeredTypes.Contains(typeRegistration) || this.IsRegistered(type, name)) {
                throw new ApplicationException(string.Format("the type of '{0}' as name '{1}' has been registered.", type.FullName, name));
            }

            _registeredTypes.Add(typeRegistration);
            this.RegisterInstance(type, name, instance);
        }

        void IObjectContainer.RegisterType(Type type, string name, Lifecycle lifetime)
        {
            type.NotNull("type");

            if(!type.IsClass || type.IsAbstract) {
                throw new ApplicationException(string.Format("the type of '{0}' must be a class and cannot be abstract.", type.FullName));
            }

            var typeRegistration = new TypeRegistration(type, name);

            if(_registeredTypes.Contains(typeRegistration) || this.IsRegistered(type, name)) {
                throw new ApplicationException(string.Format("the type of '{0}' as name '{1}' has been registered.", type.FullName, name));
            }

            _registeredTypes.Add(typeRegistration);
            this.RegisterType(type, name, lifetime);
        }

        void IObjectContainer.RegisterType(Type from, Type to, string name, Lifecycle lifetime)
        {
            from.NotNull("from");
            to.NotNull("to");
            if(!to.IsClass || to.IsAbstract) {
                throw new ApplicationException(string.Format("the type of '{0}' must be a class and cannot be abstract.", to.FullName));
            }

            if(!from.IsAssignableFrom(to)) {
                throw new ApplicationException(string.Format("'{0}' does not extend '{1}'.", to.FullName, from.FullName));
            }

            var typeRegistration = new TypeRegistration(from, name);

            if(_registeredTypes.Contains(typeRegistration) || this.IsRegistered(from, name)) {
                throw new ApplicationException(string.Format("the type of '{0}' as name '{1}' has been registered.", to.FullName, name));
            }

            _registeredTypes.Add(typeRegistration);
            this.RegisterType(from, to, name, lifetime);
        }

        bool IObjectContainer.IsRegistered(Type type, string name)
        {
            type.NotNull("type");

            return _registeredTypes.Contains(new TypeRegistration(type, name)) || this.IsRegistered(type, name);
        }
    }
}
