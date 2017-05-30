using System.Data.Common;

namespace ThinkNet.Database
{
    /// <summary>
    /// 创建数据上下文的工厂
    /// </summary>
    public interface IDataContextFactory
    {
        /// <summary>
        /// 当前上下文的数据操作
        /// </summary>
        IDataContext GetCurrent();

        /// <summary>
        /// 创建一个数据上下文
        /// </summary>
        IDataContext Create();
        /// <summary>
        /// 通过一个有效的数据库连接字符串创建一个数据上下文
        /// </summary>
        IDataContext Create(string nameOrConnectionString);
        /// <summary>
        /// 通过一个有效的数据库连接创建一个数据上下文
        /// </summary>
        IDataContext Create(DbConnection connection);
    }
}
