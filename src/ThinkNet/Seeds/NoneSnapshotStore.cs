

namespace ThinkNet.Seeds
{
    using System;

    internal class NoneSnapshotStore : ISnapshotStore
    {

        #region ISnapshotStore 成员

        public IAggregateRoot GetLastest(Type sourceTypeName, object sourceId)
        {
            return null;
        }

        #endregion
    }
}
