using ThinkNet.Infrastructure;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// <see cref="IEventSourcedRepository"/> 的扩展类
    /// </summary>
    public static class EventSourcedRepositoryExtensions
    {
        /// <summary>
        /// 根据标识id获得聚合实例。如果不存在则会抛异常
        /// </summary>
        public static TEventSourced Get<TEventSourced>(this IEventSourcedRepository repository, object id)
            where TEventSourced : class, IEventSourced
        {
            var eventSourced = repository.Find<TEventSourced>(id);
            if (eventSourced == null)
                throw new EntityNotFoundException(id, typeof(TEventSourced));

            return eventSourced;
        }

        /// <summary>
        /// 根据标识id获得聚合实例。
        /// </summary>
        public static TEventSourced Find<TEventSourced>(this IEventSourcedRepository repository, object id)
            where TEventSourced : class, IEventSourced
        {
            return repository.Find(typeof(TEventSourced), id) as TEventSourced;
        }

        ///// <summary>
        ///// 删除聚合根。
        ///// </summary>
        //public static void Delete<TEventSourced>(this IEventSourcedRepository repository, object id)
        //    where TEventSourced : class, IEventSourced
        //{
        //    repository.Delete(typeof(TEventSourced), id);
        //}

        /// <summary>
        /// 删除聚合根。
        /// </summary>
        public static void Delete(this IEventSourcedRepository repository, IEventSourced eventSourced)
        {
            repository.Delete(eventSourced.GetType(), eventSourced.Id);
        }
    }
}
