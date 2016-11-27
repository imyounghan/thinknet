using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using ThinkLib;
using ThinkLib.Serialization;
using ThinkNet.Domain;
using ThinkNet.Domain.EventSourcing;

namespace ThinkNet.Database.Storage
{
    /// <summary>
    /// 聚合快照存储器
    /// </summary>
    public sealed class SnapshotStore : ISnapshotStore
    {
        private readonly IDataContextFactory _dataContextFactory;
        private readonly ITextSerializer _serializer;
        private readonly bool _persistent;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public SnapshotStore(IDataContextFactory dataContextFactory, ITextSerializer serializer)
        {
            this._dataContextFactory = dataContextFactory;
            this._serializer = serializer;

            this._persistent = ConfigurationManager.AppSettings["thinkcfg.snapshot_storage"].ChangeIfError(false);
        }

        /// <summary>
        /// 是否启用快照存储
        /// </summary>
        public bool StorageEnabled
        {
            get { return this._persistent; }
        }

        /// <summary>
        /// 获取最新的快照
        /// </summary>
        public T GetLastest<T>(DataKey sourceKey)
            where T : class, IAggregateRoot
        {
            if (!_persistent)
                return null;
            
            var aggregateRootTypeCode = sourceKey.GetSourceTypeName().GetHashCode();

            var snapshot = Task.Factory.StartNew(() => {
                using (var context = _dataContextFactory.Create()) {
                    return context.CreateQuery<Snapshot>()
                        .Where(p => p.AggregateRootId == sourceKey.Id && 
                            p.AggregateRootTypeCode == aggregateRootTypeCode)
                        .OrderByDescending(p => p.Version)
                        .FirstOrDefault();
                }
            }).Result;
            

            if (snapshot == null)
                return null;

            return (T)_serializer.DeserializeFromBinary(snapshot.Data, sourceKey.GetSourceType());
            
        }

        /// <summary>
        /// 生成聚合快照
        /// </summary>
        public void Save(IAggregateRoot aggregateRoot)
        {
            if (!_persistent)
                return;

            var aggregateRootType = aggregateRoot.GetType();

            var snapshot = new Snapshot(aggregateRootType, aggregateRoot.Id.ToString()) {
                Data = _serializer.SerializeToBinary(aggregateRoot)
            };

            var eventSourced = aggregateRoot as IEventSourced;
            if (eventSourced != null) {
                snapshot.Version = eventSourced.Version;
            }

            Task.Factory.StartNew(() => {
                using (var context = _dataContextFactory.Create()) {
                    context.Save(snapshot);
                    context.Commit();
                }
            }).ContinueWith(task => {
                if(task.Status == TaskStatus.Faulted) {
                    if(LogManager.Default.IsWarnEnabled)
                        LogManager.Default.Warn(task.Exception,
                            "snapshot persistent failed. aggregateRootId:{0},aggregateRootType:{1},version:{2}.",
                            aggregateRoot.Id, aggregateRootType.FullName, snapshot.Version);
                }
                else {
                    if(LogManager.Default.IsDebugEnabled)
                        LogManager.Default.DebugFormat("make snapshot completed. aggregateRootId:{0},aggregateRootType:{1},version:{2}.",
                           aggregateRoot.Id, aggregateRootType.FullName, snapshot.Version);
                }
            });
        }

        /// <summary>
        /// 删除快照
        /// </summary>
        public void Remove(DataKey sourceKey)
        {
            if (!_persistent)
                return;

            var aggregateRootTypeName = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            var task = Task.Factory.StartNew(() => {
                using (var context = _dataContextFactory.Create()) {
                    context.CreateQuery<Snapshot>()
                        .Where(p => p.AggregateRootId == sourceKey.Id &&
                            p.AggregateRootTypeCode == aggregateRootTypeCode)
                        .ToList()
                        .ForEach(context.Delete);
                    context.Commit();
                }
            });
        }
    }
}
