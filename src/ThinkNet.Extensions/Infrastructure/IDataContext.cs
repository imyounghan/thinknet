using System;
using System.Collections;
using System.Data;
using System.Linq;


namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 数据上下文
    /// </summary>
    public interface IDataContext : IDisposable
    {
        /// <summary>
        /// 获取跟踪的对象集合
        /// </summary>
        ICollection TrackingObjects { get; }

        /// <summary>
        /// 判断此 <paramref name="entity"/> 是否存在于当前上下文中
        /// </summary>
        bool Contains(object entity);
        /// <summary>
        /// 从当前上下文中分离此 <paramref name="entity"/>
        /// </summary>
        void Detach(object entity);
        /// <summary>
        /// 从当前上下文中附加此 <paramref name="entity"/>
        /// </summary>
        void Attach(object entity);
        /// <summary>
        /// 保存 <paramref name="entity"/> 到数据库(提交时会触发sql-insert)
        /// </summary>
        void Save(object entity);
        /// <summary>
        /// 更新 <paramref name="entity"/> 到数据库(提交时会触发sql-update)
        /// </summary>
        void Update(object entity);
        /// <summary>
        /// 从数据库中删除 <paramref name="entity"/>(提交时会触发sql-delete)
        /// </summary>
        void Delete(object entity);
        /// <summary>
        /// 获取实体信息
        /// </summary>
        object Get(Type type, object id);
        /// <summary>
        /// 从数据库中刷新(触发sql-select)
        /// </summary>
        void Refresh(object entity);

        /// <summary>
        /// 提交所有更改的实体。
        /// </summary>
        void Commit();


        /// <summary>
        /// 获取对数据类型已知的特定数据源的查询进行计算的功能。
        /// </summary>
        IQueryable<TEntity> CreateQuery<TEntity>() where TEntity : class;

        /// <summary>
        /// 获取当前的数据连接
        /// </summary>
        IDbConnection DbConnection { get; }

        /// <summary>
        /// 当数据提交后执行
        /// </summary>
        event EventHandler DataCommitted;
    }    
}
