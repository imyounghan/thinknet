
namespace ThinkNet.Infrastructure
{
    using System;

    /// <summary>
    /// 表示一个创建日志的工厂
    /// </summary>
    public interface ILoggerFactory
    {
        /// <summary>
        /// 根据给定的名称创建一个日志
        /// </summary>
        ILogger GetOrCreate(string name);
        /// <summary>
        /// 根据给定的类型创建一个日志
        /// </summary>
        ILogger GetOrCreate(Type type);
    }

    /// <summary>
    /// 日志的扩展类
    /// </summary>
    public static class LoggerFactoryExtensions
    {
        /// <summary>
        /// 获取默认的写日志程序
        /// </summary>
        public static ILogger GetDefault(this ILoggerFactory loggerFactory)
        {
            return loggerFactory.GetOrCreate("ThinkNet");
        }
    }
}
