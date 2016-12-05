using System.Collections.Generic;
using NHibernate;

namespace ThinkNet.Database
{
    public interface INHibernateContext : IDataContext
    {
        ISession Session { get; }

        /// <summary>
        /// 根据提供的sql获取一组实体
        /// </summary>
        IEnumerable<TEntity> SqlQuery<TEntity>(string sql, params object[] parameters) where TEntity : class;

        /// <summary>
        /// 执行sql命令
        /// </summary>
        /// <returns>受影响的行数。</returns>
        int SqlExecute(string sql, params object[] parameters);
    }
}
