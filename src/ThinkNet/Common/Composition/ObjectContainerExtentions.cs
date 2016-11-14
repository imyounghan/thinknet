using System;
using System.Collections.Generic;
using System.Linq;

namespace ThinkNet.Common.Composition
{
    /// <summary>
    /// <see cref="IObjectContainer"/> 扩展方法
    /// </summary>
    public static class ObjectContainerExtentions
    {
        /// <summary>
        /// 注册一个实例
        /// </summary>
        /// <param name="that">容器</param>
        /// <param name="type">注册类型</param>
        /// <param name="instance">该类型的实例</param>
        public static void RegisterInstance(this IObjectContainer that, Type type, object instance)
        {
            that.RegisterInstance(type, (string)null, instance);
        }

        /// <summary>
        /// 注册一个实例
        /// </summary>
        /// <typeparam name="T">注册类型</typeparam>
        /// <param name="that">容器</param>
        /// <param name="instance">该类型的实例</param>
        public static void RegisterInstance<T>(this IObjectContainer that, T instance)
        {
            that.RegisterInstance<T>((string)null, instance);
        }

        /// <summary>
        /// 注册一个实例
        /// </summary>
        /// <typeparam name="T">注册类型</typeparam>
        /// <param name="that">容器</param>
        /// <param name="name">注册的名称</param>
        /// <param name="instance">该类型的实例</param>
        public static void RegisterInstance<T>(this IObjectContainer that, string name, T instance)
        {
            that.RegisterInstance(typeof(T), name, instance);
        }

        /// <summary>
        /// 注册一个类型且生命周期为单例
        /// </summary>
        /// <param name="that">容器</param>
        /// <param name="type">注册类型</param>
        public static void RegisterType(this IObjectContainer that, Type type)
        {
            that.RegisterType(type, Lifecycle.Singleton);
        }

        /// <summary>
        /// 注册一个类型
        /// </summary>
        /// <param name="that">容器</param>
        /// <param name="type">注册类型</param>
        /// <param name="lifetime">生命周期</param>
        public static void RegisterType(this IObjectContainer that, Type type, Lifecycle lifetime)
        {
            that.RegisterType(type, (string)null, lifetime);
        }

        /// <summary>
        /// 注册一个类型且生命周期为单例
        /// </summary>
        /// <param name="that">容器</param>
        /// <param name="from">注册类型</param>
        /// <param name="to">目标类型</param>
        public static void RegisterType(this IObjectContainer that, Type from, Type to)
        {
            that.RegisterType(from, to, Lifecycle.Singleton);
        }

        /// <summary>
        /// 注册一个类型
        /// </summary>
        /// <param name="that">容器</param>
        /// <param name="from">注册类型</param>
        /// <param name="to">目标类型</param>
        /// <param name="lifetime">生命周期</param>
        public static void RegisterType(this IObjectContainer that, Type from, Type to, Lifecycle lifetime)
        {
            that.RegisterType(from, to, (string)null, lifetime);
        }

        /// <summary>
        /// 注册一个类型且生命周期为单例
        /// </summary>
        /// <typeparam name="T">注册类型</typeparam>
        /// <param name="that">容器</param>
        public static void RegisterType<T>(this IObjectContainer that)
        {
            that.RegisterType<T>(Lifecycle.Singleton);
        }
        /// <summary>
        /// 注册一个类型
        /// </summary>
        /// <typeparam name="T">注册类型</typeparam>
        /// <param name="that">容器</param>
        /// <param name="lifetime">生命周期</param>
        public static void RegisterType<T>(this IObjectContainer that, Lifecycle lifetime)
        {
            that.RegisterType<T>((string)null, lifetime);
        }
        /// <summary>
        /// 注册一个类型
        /// </summary>
        /// <typeparam name="T">注册类型</typeparam>
        /// <param name="that">容器</param>
        /// <param name="name">注册的名称</param>
        /// <param name="lifetime">生命周期</param>
        public static void RegisterType<T>(this IObjectContainer that, string name, Lifecycle lifetime)
        {
            that.RegisterType(typeof(T), name, lifetime);
        }
        /// <summary>
        /// 注册一个类型且生命周期为单例
        /// </summary>
        /// <typeparam name="TFrom">注册类型</typeparam>
        /// <typeparam name="TTo">目标类型</typeparam>
        /// <param name="that">容器</param>
        public static void RegisterType<TFrom, TTo>(this IObjectContainer that)
            where TTo : TFrom
        {
            that.RegisterType<TFrom, TTo>(Lifecycle.Singleton);
        }
        /// <summary>
        /// 注册一个类型
        /// </summary>
        /// <typeparam name="TFrom">注册类型</typeparam>
        /// <typeparam name="TTo">目标类型</typeparam>
        /// <param name="that">容器</param>
        /// <param name="lifetime">生命周期</param>
        public static void RegisterType<TFrom, TTo>(this IObjectContainer that, Lifecycle lifetime)
            where TTo: TFrom
        {
            that.RegisterType<TFrom, TTo>((string)null, lifetime);
        }
        /// <summary>
        /// 注册一个类型
        /// </summary>
        /// <typeparam name="TFrom">注册类型</typeparam>
        /// <typeparam name="TTo">目标类型</typeparam>
        /// <param name="that">容器</param>
        /// <param name="name">注册的名称</param>
        /// <param name="lifetime">生命周期类型</param>
        public static void RegisterType<TFrom, TTo>(this IObjectContainer that, string name, Lifecycle lifetime)
             where TTo : TFrom
        {
            that.RegisterType(typeof(TFrom), typeof(TTo), name, lifetime);
        }

        /// <summary>
        /// 判断此类型是否已注册
        /// </summary>
        /// <param name="that">容器</param>
        /// <param name="type">类型</param>
        public static bool IsRegistered(this IObjectContainer that, Type type)
        {
            return that.IsRegistered(type, (string)null);
        }
        /// <summary>
        /// 判断此类型是否已注册
        /// </summary>
        /// <typeparam name="T">注册类型</typeparam>
        /// <param name="that">容器</param>
        public static bool IsRegistered<T>(this IObjectContainer that)
        {
            return that.IsRegistered(typeof(T));
        }
        /// <summary>
        /// 判断此类型是否已注册
        /// </summary>
        /// <typeparam name="T">注册类型</typeparam>
        /// <param name="that">容器</param>
        /// <param name="name">注册的名称</param>
        public static bool IsRegistered<T>(this IObjectContainer that, string name)
        {
            return that.IsRegistered(typeof(T), name);
        }

        /// <summary>
        /// 获取类型对应的实例
        /// </summary>
        /// <param name="that">容器</param>
        /// <param name="type">类型</param>
        public static object Resolve(this IObjectContainer that, Type type)
        {
            return that.Resolve(type, (string)null);
        }
        /// <summary>
        /// 获取类型对应的实例
        /// </summary>
        /// <typeparam name="T">注册类型</typeparam>
        /// <param name="that">容器</param>
        public static T Resolve<T>(this IObjectContainer that)
        {
            return (T)that.Resolve(typeof(T));
        }
        /// <summary>
        /// 获取类型对应的实例
        /// </summary>
        /// <typeparam name="T">注册类型</typeparam>
        /// <param name="that">容器</param>
        /// <param name="name">注册的名称</param>
        public static T Resolve<T>(this IObjectContainer that, string name)
        {
            return (T)that.Resolve(typeof(T), name);
        }
        /// <summary>
        /// 获取类型所有的实例
        /// </summary>
        /// <typeparam name="T">注册类型</typeparam>
        /// <param name="that">容器</param>
        public static IEnumerable<T> ResolveAll<T>(this IObjectContainer that)
        {
            return that.ResolveAll(typeof(T)).Cast<T>().ToArray();
        }
    }
}
