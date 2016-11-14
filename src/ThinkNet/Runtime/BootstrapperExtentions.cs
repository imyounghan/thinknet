using System;
using System.Collections.Generic;
using ThinkNet.Common;

namespace ThinkNet.Runtime
{
    public static class BootstrapperExtentions
    {
        public static Bootstrapper Register(this Bootstrapper that, Type type, Lifecycle lifecycle)
        {
            return that.Register(type, (string)null, lifecycle);
        }

        public static Bootstrapper Register(this Bootstrapper that, Type type, string name)
        {
            return that.Register(type, name, Lifecycle.Singleton);
        }

        public static Bootstrapper Registere(this Bootstrapper that, Type type)
        {
            return that.Register(type, (string)null, Lifecycle.Singleton);
        }


        public static Bootstrapper Register(this Bootstrapper that, Type from, Type to, Lifecycle lifecycle)
        {
            return that.Register(from, to, (string)null, lifecycle);
        }

        public static Bootstrapper Register(this Bootstrapper that, Type from, Type to, string name)
        {
            return that.Register(from, to, name, Lifecycle.Singleton);
        }

        public static Bootstrapper Register(this Bootstrapper that, Type from, Type to)
        {
            return that.Register(from, to, (string)null, Lifecycle.Singleton);
        }

        public static Bootstrapper RegisterMultiple(this Bootstrapper that, Type registrationType, IEnumerable<Type> implementationTypes)
        {
            return that.RegisterMultiple(registrationType, implementationTypes, Lifecycle.Singleton);
        }

        public static Bootstrapper RegisterMultiple(this Bootstrapper that, Type registrationType, IEnumerable<Type> implementationTypes, Lifecycle lifecycle)
        {
            foreach(var implementationType in implementationTypes) {
                that.Register(registrationType, implementationType, implementationType.FullName, lifecycle);
            }

            return that;
        }
        public static Bootstrapper RegisterMultiple(this Bootstrapper that, IEnumerable<Type> registrationTypes, Type implementationType)
        {
            return that.RegisterMultiple(registrationTypes, implementationType, Lifecycle.Singleton);
        }
        public static Bootstrapper RegisterMultiple(this Bootstrapper that, IEnumerable<Type> registrationTypes, Type implementationType, Lifecycle lifecycle)
        {
            foreach(var registrationType in registrationTypes) {
                that.Register(registrationType, implementationType, lifecycle);
            }

            return that;
        }

        public static Bootstrapper Register<T>(this Bootstrapper that)
        {
            return that.Register<T>((string)null, Lifecycle.Singleton);
        }
        public static Bootstrapper Register<T>(this Bootstrapper that, Lifecycle lifecycle)
        {
            return that.Register<T>((string)null, lifecycle);
        }
        public static Bootstrapper Register<T>(this Bootstrapper that, string name)
        {
            return that.Register<T>(name, Lifecycle.Singleton);
        }
        public static Bootstrapper Register<T>(this Bootstrapper that, string name, Lifecycle lifecycle)
        {
            return that.Register(typeof(T), name, lifecycle);
        }

        public static Bootstrapper Register<TFrom, TTo>(this Bootstrapper that)
             where TTo : TFrom
        {
            return that.Register<TFrom, TTo>((string)null, Lifecycle.Singleton);
        }

        public static Bootstrapper Register<TFrom, TTo>(this Bootstrapper that, Lifecycle lifecycle)
            where TTo : TFrom
        {
            return that.Register<TFrom, TTo>((string)null, lifecycle);
        }
        public static Bootstrapper Register<TFrom, TTo>(this Bootstrapper that, string name)
            where TTo : TFrom
        {
            return that.Register<TFrom, TTo>(name, Lifecycle.Singleton);
        }

        public static Bootstrapper Register<TFrom, TTo>(this Bootstrapper that, string name, Lifecycle lifecycle)
            where TTo : TFrom
        {
            return that.Register(typeof(TFrom), typeof(TTo), name, lifecycle);
        }
    }
}
