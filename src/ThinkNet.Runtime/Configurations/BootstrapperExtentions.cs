using System;
using System.Collections.Generic;

namespace ThinkNet.Configurations
{
    public static class BootstrapperExtentions
    {
        public static void Register(this Bootstrapper that, Type type, Lifecycle lifecycle)
        {
            that.Register(type, (string)null, lifecycle);
        }

        public static void Register(this Bootstrapper that, Type type, string name)
        {
            that.Register(type, name, Lifecycle.Singleton);
        }

        public static void Registere(this Bootstrapper that, Type type)
        {
            that.Register(type, (string)null, Lifecycle.Singleton);
        }


        public static void Register(this Bootstrapper that, Type from, Type to, Lifecycle lifecycle)
        {
            that.Register(from, to, (string)null, lifecycle);
        }

        public static void Register(this Bootstrapper that, Type from, Type to, string name)
        {
            that.Register(from, to, name, Lifecycle.Singleton);
        }

        public static void Register(this Bootstrapper that, Type from, Type to)
        {
            that.Register(from, to, (string)null, Lifecycle.Singleton);
        }

        public static void RegisterMultiple(this Bootstrapper that, Type registrationType, IEnumerable<Type> implementationTypes)
        {
            that.RegisterMultiple(registrationType, implementationTypes, Lifecycle.Singleton);
        }

        public static void RegisterMultiple(this Bootstrapper that, Type registrationType, IEnumerable<Type> implementationTypes, Lifecycle lifecycle)
        {
            foreach(var implementationType in implementationTypes) {
                that.Register(registrationType, implementationType, implementationType.FullName, lifecycle);
            }
        }
        public static void RegisterMultiple(this Bootstrapper that, IEnumerable<Type> registrationTypes, Type implementationType)
        {
            that.RegisterMultiple(registrationTypes, implementationType, Lifecycle.Singleton);
        }
        public static void RegisterMultiple(this Bootstrapper that, IEnumerable<Type> registrationTypes, Type implementationType, Lifecycle lifecycle)
        {
            foreach(var registrationType in registrationTypes) {
                that.Register(registrationType, implementationType, lifecycle);
            }
        }

        public static void Register<T>(this Bootstrapper that)
        {
            that.Register<T>((string)null, Lifecycle.Singleton);
        }
        public static void Register<T>(this Bootstrapper that, Lifecycle lifecycle)
        {
            that.Register<T>((string)null, lifecycle);
        }
        public static void Register<T>(this Bootstrapper that, string name)
        {
            that.Register<T>(name, Lifecycle.Singleton);
        }
        public static void Register<T>(this Bootstrapper that, string name, Lifecycle lifecycle)
        {
            that.Register(typeof(T), name, lifecycle);
        }

        public static void Register<TFrom, TTo>(this Bootstrapper that)
             where TTo : TFrom
        {
            that.Register<TFrom, TTo>((string)null, Lifecycle.Singleton);
        }

        public static void Register<TFrom, TTo>(this Bootstrapper that, Lifecycle lifecycle)
            where TTo : TFrom
        {
            that.Register<TFrom, TTo>((string)null, lifecycle);
        }
        public static void Register<TFrom, TTo>(this Bootstrapper that, string name)
            where TTo : TFrom
        {
            that.Register<TFrom, TTo>(name, Lifecycle.Singleton);
        }

        public static void Register<TFrom, TTo>(this Bootstrapper that, string name, Lifecycle lifecycle)
            where TTo : TFrom
        {
            that.Register(typeof(TFrom), typeof(TTo), name, lifecycle);
        }
    }
}
