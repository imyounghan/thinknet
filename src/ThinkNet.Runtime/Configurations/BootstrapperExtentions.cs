using System;
using ThinkNet.Infrastructure;

namespace ThinkNet.Configurations
{
    public static class BootstrapperExtentions
    {
        public static void RegisterType(this Bootstrapper that, Type type, Lifecycle lifecycle)
        {
            that.RegisterType(type, (string)null, lifecycle);
        }

        public static void RegisterType(this Bootstrapper that, Type type, string name)
        {
            that.RegisterType(type, name, Lifecycle.Singleton);
        }

        public static void RegisterType(this Bootstrapper that, Type type)
        {
            that.RegisterType(type, (string)null, Lifecycle.Singleton);
        }


        public static void RegisterType(this Bootstrapper that, Type from, Type to, Lifecycle lifecycle)
        {
            that.RegisterType(from, to, (string)null, lifecycle);
        }

        public static void RegisterType(this Bootstrapper that, Type from, Type to, string name)
        {
            that.RegisterType(from, to, name, Lifecycle.Singleton);
        }

        public static void RegisterType(this Bootstrapper that, Type from, Type to)
        {
            that.RegisterType(from, to, (string)null, Lifecycle.Singleton);
        }



        public static void RegisterType<T>(this Bootstrapper that)
        {
            that.RegisterType<T>(null, Lifecycle.Singleton);
        }
        public static void RegisterType<T>(this Bootstrapper that, Lifecycle lifecycle)
        {
            that.RegisterType<T>(null, lifecycle);
        }
        public static void RegisterType<T>(this Bootstrapper that, string name)
        {
            that.RegisterType<T>(name, Lifecycle.Singleton);
        }
        public static void RegisterType<T>(this Bootstrapper that, string name, Lifecycle lifecycle)
        {
            that.RegisterType(typeof(T), name, lifecycle);
        }

        public static void RegisterType<TFrom, TTo>(this Bootstrapper that)
             where TTo : TFrom
        {
            that.RegisterType<TFrom, TTo>(null, Lifecycle.Singleton);
        }

        public static void RegisterType<TFrom, TTo>(this Bootstrapper that, Lifecycle lifecycle)
            where TTo : TFrom
        {
            that.RegisterType<TFrom, TTo>(null, lifecycle);
        }
        public static void RegisterType<TFrom, TTo>(this Bootstrapper that, string name)
            where TTo : TFrom
        {
            that.RegisterType<TFrom, TTo>(name, Lifecycle.Singleton);
        }

        public static void RegisterType<TFrom, TTo>(this Bootstrapper that, string name, Lifecycle lifecycle)
            where TTo : TFrom
        {
            that.RegisterType(typeof(TFrom), typeof(TTo), name, lifecycle);
        }
    }
}
