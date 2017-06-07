

namespace ThinkNet.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public sealed class DefaultObjectContainer : ObjectContainer
    {
        private readonly Dictionary<TypeRegistration, object> instances;

        private readonly Dictionary<TypeRegistration, Type> keyToImpltypeMap;

        private readonly Dictionary<Type, ObjectBuilder> objectBuilders;

        private readonly Dictionary<Type, ICollection<TypeRegistration>> typeToAllMap;

        class ObjectBuilder
        {
            private readonly ConstructorInfo constructorInfo;

            private readonly Lifecycle lifecycle;

            private object instance = null;

            public ObjectBuilder(Type type, Lifecycle lifecycle)
            {
                var constructors = type.GetConstructors();
                if(constructors.Length == 0) {
                    string errorMessage = string.Format("Type '{0}' must have a public constructor.", type.FullName);
                    throw new SystemException(errorMessage);
                }

                if(constructors.Length > 1) {
                    string errorMessage = string.Format("Type '{0}' must have multiple public constructor.", type.FullName);
                    throw new SystemException(errorMessage);
                }

                this.constructorInfo = constructors.First();
                this.lifecycle = lifecycle;
            }

            public object GetInstance()
            {
                if (lifecycle == Lifecycle.Singleton)
                {
                    if (instance == null)
                    {
                        instance = this.CreateInstance();
                    }

                    return instance;
                }

                return instance;
            }

            private object CreateInstance()
            {
                var parameters = constructorInfo.GetParameters();
                if (parameters.Length == 0)
                {
                    return constructorInfo.Invoke(new object[0]);
                }

                var args = parameters.Select(GetParameterValue).ToArray();
                return constructorInfo.Invoke(args);
            }

            private object GetParameterValue(ParameterInfo parameterInfo)
            {
                if(parameterInfo.RawDefaultValue == DBNull.Value) {
                    return Instance.Resolve(parameterInfo.ParameterType, null);
                }

                return parameterInfo.RawDefaultValue;
            }
        }

        public DefaultObjectContainer()
        {
            this.instances = new Dictionary<TypeRegistration, object>();
            this.objectBuilders = new Dictionary<Type, ObjectBuilder>();
            this.keyToImpltypeMap = new Dictionary<TypeRegistration, Type>();
            this.typeToAllMap = new Dictionary<Type, ICollection<TypeRegistration>>();
        }


        protected override void Dispose(bool disposing)
        { }

        public override void RegisterInstance(Type type, string name, object instance)
        {
            var typeRegistration = new TypeRegistration(type, name);
            this.RegisterInstance(typeRegistration, instance);
        }

        public override void RegisterInstance(TypeRegistration typeRegistration, object instance)
        {
            if (instances.TryAdd(typeRegistration, instance)) {
                typeToAllMap.GetOrAdd(typeRegistration.Type, () => new List<TypeRegistration>()).Add(typeRegistration);
            }
        }

        public override void RegisterType(Type type, string name, Lifecycle lifetime)
        {
            var typeRegistration = new TypeRegistration(type, name);
            this.RegisterType(typeRegistration, lifetime);
        }

        public override void RegisterType(TypeRegistration key, Lifecycle lifetime)
        {
            this.RegisterType(key, key.Type, lifetime);
        }

        public override void RegisterType(Type @from, Type to, string name, Lifecycle lifetime)
        {
            var typeRegistration = new TypeRegistration(@from, name);
            this.RegisterType(typeRegistration, to, lifetime);
        }

        public override void RegisterType(TypeRegistration key, Type implType, Lifecycle lifetime)
        {
            if (keyToImpltypeMap.TryAdd(key, implType)) {
                typeToAllMap.GetOrAdd(key.Type, () => new List<TypeRegistration>()).Add(key);
                objectBuilders.TryAdd(implType, new ObjectBuilder(implType, lifetime));
            }
        }

        public override bool IsRegistered(Type type, string name)
        {
            return false;
        }

        public override bool IsRegistered(TypeRegistration key)
        {
            return this.RegisteredTypes.Contains(key);
        }

        public override object Resolve(Type type, string name)
        {
            var key = new TypeRegistration(type, name);

            return Resolve(key);
        }

        public override object Resolve(TypeRegistration typeRegistration)
        {
            object instance = null;
            if (!instances.TryGetValue(typeRegistration, out instance))
            {
                Type implType;
                if (keyToImpltypeMap.TryGetValue(typeRegistration, out implType))
                {
                    ObjectBuilder objectBuilder;
                    if (objectBuilders.TryGetValue(implType, out objectBuilder))
                    {
                        instance = objectBuilder.GetInstance();
                    }
                }
            }

            return instance;
        }

        public override IEnumerable<object> ResolveAll(Type type)
        {
            if (!typeToAllMap.ContainsKey(type))
            {
                return Enumerable.Empty<object>();
            }

            return typeToAllMap[type].Select(this.Resolve).Distinct().ToArray();
        }
    }
}
