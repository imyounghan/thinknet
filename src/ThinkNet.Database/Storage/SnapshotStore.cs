using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.EventSourcing;
using ThinkLib.Common;


namespace ThinkNet.Database.Storage
{
    /// <summary>
    /// 聚合快照存储器
    /// </summary>
    [RegisterComponent(typeof(ISnapshotStore))]
    public class SnapshotStore : ISnapshotStore
    {
        private readonly IDataContextFactory _dbContextFactory;
        private readonly bool _persistent;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public SnapshotStore(IDataContextFactory dbContextFactory)
        {
            this._dbContextFactory = dbContextFactory;

            this._persistent = ConfigurationManager.AppSettings["thinkcfg.snapshot_storage"].Change(false);
        }

        /// <summary>
        /// 是否启用快照存储
        /// </summary>
        public bool StorageEnabled
        {
            get { return this._persistent; }
        }


        public Stream GetLastest(SourceKey sourceKey)
        {
            if (!_persistent)
                return null;

            var aggregateRootTypeName = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            var task = Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    return context.CreateQuery<Snapshot>()
                        .Where(p => p.AggregateRootId == sourceKey.SourceId &&
                            p.AggregateRootTypeCode == aggregateRootTypeCode)
                        .FirstOrDefault();
                }
            });
            task.Wait();

            var snapshot = task.Result;

            if (snapshot == null)
                return null;

            return new Stream {
                Key = sourceKey,
                Payload = snapshot.Data,
                Version = snapshot.Version
            };
            
        }

        public bool Save(Stream snapshot)
        {
            if (!_persistent)
                return false;

            var aggregateRootTypeName = string.Concat(snapshot.Key.Namespace, ".", snapshot.Key.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            var data = new Snapshot {
                AggregateRootId = snapshot.Key.SourceId,
                AggregateRootTypeCode = aggregateRootTypeCode,
                Data = snapshot.Payload,
                Version = snapshot.Version,
                Timestamp = DateTime.UtcNow
            };

            var task = Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    bool exist = context.CreateQuery<Snapshot>()
                        .Any(entity => entity.AggregateRootId == data.AggregateRootId &&
                            entity.AggregateRootTypeCode == data.AggregateRootTypeCode);
                    if (exist) {
                        context.Update(snapshot);
                    }
                    else {
                        context.Save(snapshot);
                    }
                    context.Commit();
                }
            });
            task.Wait();

            return task.Exception == null;
        }

        public bool Remove(SourceKey sourceKey)
        {
            if (!_persistent)
                return false;

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
            task.Wait();

            return task.Exception == null;
        }
    }
}
