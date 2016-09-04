using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Database;
using ThinkNet.EventSourcing;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 聚合快照存储器
    /// </summary>
    [Register(typeof(ISnapshotStore))]
    public class SnapshotStore : ISnapshotStore
    {
        private readonly IDataContextFactory _dbContextFactory;
        private readonly ISerializer _serializer;
        private readonly bool _persistent;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public SnapshotStore(IDataContextFactory dbContextFactory, ISerializer serializer)
        {
            this._dbContextFactory = dbContextFactory;
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


        public IEventSourced GetLastest(DataKey sourceKey)
        {
            if (!_persistent)
                return null;
            
            var aggregateRootTypeCode = sourceKey.GetSourceTypeName().GetHashCode();

            var snapshot = Task.Factory.StartNew(() => {
                using(var context = _dbContextFactory.CreateDataContext()) {
                    return context.CreateQuery<Snapshot>()
                        .Where(p => p.AggregateRootId == sourceKey.SourceId && p.AggregateRootTypeCode == aggregateRootTypeCode)
                        .OrderByDescending(p => p.Version)
                        .FirstOrDefault();
                }
            }).Result;
            

            if (snapshot == null)
                return null;

            return (IEventSourced)_serializer.DeserializeFromBinary(snapshot.Data, sourceKey.GetSourceType());
            
        }

        public void Save(IEventSourced aggregateRoot)
        {
            if (!_persistent)
                return;

            var aggregateRootType = aggregateRoot.GetType();

            var snapshot = new Snapshot {
                AggregateRootId = aggregateRoot.Id.ToString(),
                AggregateRootTypeCode = aggregateRootType.FullName.GetHashCode(),
                AggregateRootTypeName = string.Concat(aggregateRootType.FullName, ", ", Path.GetFileNameWithoutExtension(aggregateRootType.Assembly.ManifestModule.FullyQualifiedName)),
                Data = _serializer.SerializeToBinary(aggregateRoot),
                Version = aggregateRoot.Version,
                Timestamp = DateTime.UtcNow
            };

            Task.Factory.StartNew(() => {
                using(var context = _dbContextFactory.CreateDataContext()) {
                    context.Save(snapshot);
                    context.Commit();
                }
            }).ContinueWith(task => {
                if(task.Status == TaskStatus.Faulted) {
                    if(LogManager.Default.IsWarnEnabled)
                        LogManager.Default.Warn(task.Exception,
                            "snapshot persistent failed. aggregateRootId:{0},aggregateRootType:{1},version:{2}.",
                            aggregateRoot.Id, aggregateRootType.FullName, aggregateRoot.Version);
                }
                else {
                    if(LogManager.Default.IsDebugEnabled)
                        LogManager.Default.DebugFormat("make snapshot completed. aggregateRootId:{0},aggregateRootType:{1},version:{2}.",
                           aggregateRoot.Id, aggregateRootType.FullName, aggregateRoot.Version);
                }
            });
        }

        public void Remove(DataKey sourceKey)
        {
            if (!_persistent)
                return;

            var aggregateRootTypeName = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            var task = Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    context.CreateQuery<Snapshot>()
                        .Where(p => p.AggregateRootId == sourceKey.SourceId &&
                            p.AggregateRootTypeCode == aggregateRootTypeCode)
                        .ToList()
                        .ForEach(context.Delete);
                    context.Commit();
                }
            });
        }
    }
}
