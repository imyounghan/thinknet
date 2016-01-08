using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using ThinkNet.Annotation;
using ThinkNet.Infrastructure;


namespace ThinkNet.EventSourcing
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

            this._persistent = ConfigurationManager.AppSettings["thinkcfg.snapshot_storage"].Safe("false").ToBoolean();
        }

        /// <summary>
        /// 是否启用快照存储
        /// </summary>
        public bool StorageEnabled
        {
            get { return this._persistent; }
        }


        public Tuple<int, byte[]> GetLastest(SourceKey sourceKey)
        {
            if (!_persistent)
                return null;

            using (var context = _dbContextFactory.CreateDataContext()) {
                var snapshot = context.CreateQuery<Snapshot>()
                    .Where(p => p.AggregateRootId == sourceKey.SourceId &&
                        p.AggregateRootTypeCode == sourceKey.SourceTypeName.GetHashCode())
                    .FirstOrDefault();

                if (snapshot == null)
                    return null;

                return new Tuple<int, byte[]>(snapshot.Version, snapshot.Data);
            }
        }

        public void Save(SourceKey sourceKey, int version, byte[] snapshot)
        {
            if (!_persistent)
                return;
            
            var snapshotData = new Snapshot {
                AggregateRootId = sourceKey.SourceId,
                AggregateRootTypeCode = sourceKey.SourceTypeName.GetHashCode(),
                Data = snapshot,
                Version = version,
                Timestamp = DateTime.UtcNow
            };

            using (var context = _dbContextFactory.CreateDataContext()) {
                bool exist = context.CreateQuery<Snapshot>()
                    .Any(entity => entity.AggregateRootId == snapshotData.AggregateRootId &&
                        entity.AggregateRootTypeCode == snapshotData.AggregateRootTypeCode);
                if (exist) {
                    context.Update(snapshot);
                }
                else {
                    context.Save(snapshot);
                }
                context.Commit();
            }
        }

        public void Remove(SourceKey sourceKey)
        {
            if (!_persistent)
                return;

            using (var context = _dbContextFactory.CreateDataContext()) {
                context.CreateQuery<Snapshot>()
                    .Where(p => p.AggregateRootId == sourceKey.SourceId &&
                        p.AggregateRootTypeCode == sourceKey.SourceTypeName.GetHashCode())
                    .AsEnumerable()
                    .ForEach(context.Delete);
                context.Commit();
            }
        }
    }
}
