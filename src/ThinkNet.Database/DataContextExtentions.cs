using System.Collections.Generic;
using System.Linq;

namespace ThinkNet.Database
{
    /// <summary>
    /// <see cref="IDataContext"/> 的扩展类
    /// </summary>
    public static class DataContextExtentions
    {
        /// <summary>
        /// 获取该类型的实例
        /// </summary>
        public static T Find<T>(this IDataContext dataContext, object id)
            where T : class
        {
            return dataContext.Find(typeof(T), id) as T;
        }

        /// <summary>
        /// 获取该类型的实例
        /// </summary>
        public static T Find<T>(this IDataContext dataContext, params object[] keyValues)
            where T : class
        {
            return dataContext.Find(typeof(T), keyValues) as T;
        }

        ///// <summary>
        ///// 根据标识id获得实例。如果不存在则会抛异常
        ///// </summary>
        //public static object Get(this IDataContext dataContext, Type type, object id)
        //{
        //    var entity = dataContext.Find(type, id);
        //    if (entity == null)
        //        throw new EntityNotFoundException(id, type);

        //    return entity;
        //}

        ///// <summary>
        ///// 根据标识id获得实例。如果不存在则会抛异常
        ///// </summary>
        //public static object Get(this IDataContext dataContext, Type type, params object[] keyValues)
        //{
        //    var entity = dataContext.Find(type, keyValues);
        //    if (entity == null) {
        //        var id = string.Join(":", keyValues.Select(item => item.ToString()));
        //        throw new EntityNotFoundException(id, type);
        //    }

        //    return entity;
        //}

        ///// <summary>
        ///// 根据标识id获得实例。如果不存在则会抛异常
        ///// </summary>
        //public static T Get<T>(this IDataContext dataContext, object id)
        //    where T : class
        //{
        //    return dataContext.Get(typeof(T), id) as T;
        //}

        ///// <summary>
        ///// 根据标识id获得实例。如果不存在则会抛异常
        ///// </summary>
        //public static T Get<T>(this IDataContext dataContext, params object[] keyValues)
        //    where T : class
        //{
        //    return dataContext.Get(typeof(T), keyValues) as T;
        //}

        /// <summary>
        /// 获取符合条件的记录总数
        /// </summary>
        public static int Count<TEntity>(this IDataContext dataContext, ICriteria<TEntity> criteria) where TEntity : class
        {
            var query = dataContext.CreateQuery<TEntity>();

            return (criteria ?? Criteria<TEntity>.Empty).Filtered(query).Count();
        }
        /// <summary>
        /// 根据查询条件是否存在相关数据
        /// </summary>
        public static bool Exists<TEntity>(this IDataContext dataContext, ICriteria<TEntity> criteria) where TEntity : class
        {
            var query = dataContext.CreateQuery<TEntity>();

            return (criteria ?? Criteria<TEntity>.Empty).Filtered(query).Any();
        }

        /// <summary>
        /// 获得单个实体
        /// </summary>
        public static TEntity Single<TEntity>(this IDataContext dataContext, ICriteria<TEntity> criteria) where TEntity : class
        {
            return dataContext.Single<TEntity>(criteria, null);
        }
        /// <summary>
        /// 获得单个实体
        /// </summary>
        public static TEntity Single<TEntity>(this IDataContext dataContext, ISortSet<TEntity> sortset) where TEntity : class
        {
            return dataContext.Single<TEntity>(null, sortset);
        }
        /// <summary>
        /// 获得单个实体
        /// </summary>
        public static TEntity Single<TEntity>(this IDataContext dataContext, ICriteria<TEntity> criteria, ISortSet<TEntity> sortset) where TEntity : class
        {
            return dataContext.FindAll<TEntity>(-1, criteria, sortset).FirstOrDefault();
        }
        /// <summary>
        /// 获得单个实体
        /// </summary>
        public static IEnumerable<TEntity> FindAll<TEntity>(this IDataContext dataContext) where TEntity : class
        {
            return dataContext.FindAll<TEntity>(-1);
        }

        /// <summary>
        /// 获得符合条件的所有实体
        /// </summary>
        public static IEnumerable<TEntity> FindAll<TEntity>(this IDataContext dataContext, int limit) where TEntity : class
        {
            return dataContext.FindAll<TEntity>(limit, null, null);
        }
        /// <summary>
        /// 获得符合条件的所有实体
        /// </summary>
        public static IEnumerable<TEntity> FindAll<TEntity>(this IDataContext dataContext, ISortSet<TEntity> sortset) where TEntity : class
        {
            return dataContext.FindAll<TEntity>(null, sortset);
        }
        /// <summary>
        /// 获得符合条件的所有实体
        /// </summary>
        public static IEnumerable<TEntity> FindAll<TEntity>(this IDataContext dataContext, ICriteria<TEntity> criteria) where TEntity : class
        {
            return dataContext.FindAll<TEntity>(criteria, null);
        }
        /// <summary>
        /// 获得符合条件的所有实体
        /// </summary>
        public static IEnumerable<TEntity> FindAll<TEntity>(this IDataContext dataContext, ICriteria<TEntity> criteria, ISortSet<TEntity> sortset) where TEntity : class
        {
            return dataContext.FindAll<TEntity>(-1, criteria, sortset);
        }
        /// <summary>
        /// 获得符合条件的所有实体
        /// </summary>
        public static IEnumerable<TEntity> FindAll<TEntity>(this IDataContext dataContext, int limit, ISortSet<TEntity> sortset) where TEntity : class
        {
            return dataContext.FindAll<TEntity>(limit, null, sortset);
        }
        /// <summary>
        /// 获得符合条件的所有实体
        /// </summary>
        public static IEnumerable<TEntity> FindAll<TEntity>(this IDataContext dataContext, int limit, ICriteria<TEntity> criteria) where TEntity : class
        {
            return dataContext.FindAll<TEntity>(limit, criteria, null);
        }
        /// <summary>
        /// 获得符合条件的所有实体
        /// </summary>
        public static IEnumerable<TEntity> FindAll<TEntity>(this IDataContext dataContext, int limit, ICriteria<TEntity> criteria, ISortSet<TEntity> sortset) where TEntity : class
        {
            IQueryable<TEntity> query = dataContext.CreateQuery<TEntity>();

            query = (criteria ?? Criteria<TEntity>.Empty).Filtered(query);

            query = (sortset ?? SortSet<TEntity>.Empty).Arranged(query);

            if (limit > 0) {
                query = query.Take(limit);
            }


            return query.ToList();
        }

        /// <summary>
        /// 获得符合条件的所有实体
        /// </summary>
        public static IEnumerable<TEntity> FindAll<TEntity>(this IDataContext dataContext, ICriteria<TEntity> criteria, ISortSet<TEntity> sortset, int pageIndex, int pageSize, out long total) where TEntity : class
        {
            IQueryable<TEntity> query = dataContext.CreateQuery<TEntity>();

            query = (criteria ?? Criteria<TEntity>.Empty).Filtered(query);

            query = (sortset ?? SortSet<TEntity>.Empty).Arranged(query);

            total = query.LongCount();

            if (pageSize > 0) {
                if (pageIndex > 0) {
                    query = query.Skip(pageIndex * pageSize);
                }
                query = query.Take(pageSize);
            }


            return query.ToList();
        }
    }
}
