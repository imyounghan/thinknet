using System;
using ThinkLib.Common;

namespace ThinkNet.Configurations
{
    public static class BootstrapperExtentions
    {
        public static void RegisterType(this Bootstrapper that, Type type, Lifecycle lifecycle)
        {
            that.RegisterType(type, lifecycle, null);
        }

        public static void RegisterType(this Bootstrapper that, Type type, string name)
        {
            that.RegisterType(type, Lifecycle.Singleton, name);
        }

        public static void RegisterType(this Bootstrapper that, Type type)
        {
            that.RegisterType(type, Lifecycle.Singleton, null);
        }


        public static void RegisterType(this Bootstrapper that, Type from, Type to, Lifecycle lifecycle)
        {
            that.RegisterType(from, to, lifecycle, null);
        }

        public static void RegisterType(this Bootstrapper that, Type from, Type to, string name)
        {
            that.RegisterType(from, to, Lifecycle.Singleton, name);
        }

        public static void RegisterType(this Bootstrapper that, Type from, Type to)
        {
            that.RegisterType(from, to, Lifecycle.Singleton, null);
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
            that.RegisterType(typeof(T), lifecycle, name);
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
            that.RegisterType(typeof(TFrom), typeof(TTo), lifecycle, name);
        }
    }
}
