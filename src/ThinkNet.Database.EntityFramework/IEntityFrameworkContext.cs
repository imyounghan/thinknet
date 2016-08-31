using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace ThinkNet.Database.EntityFramework
{
    public interface IEntityFrameworkContext : IDataContext
    {
        DbContext DbContext { get; }

        /// <summary>
        /// 获取对数据类型已知的特定数据源的查询进行计算的功能。
        /// </summary>
        IQueryable<TEntity> CreateQuery<TEntity>(params Expression<Func<TEntity, dynamic>>[] eagerLoadingProperties) where TEntity : class;

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
