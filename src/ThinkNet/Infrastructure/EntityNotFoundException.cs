using System;
using ThinkNet.Common;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// Represents an exception when tring to get a not existing entity.
    /// </summary>
    [Serializable]
    public class EntityNotFoundException : ThinkNetException
    {
        private readonly object entityId;
        private readonly string entityType;

        public EntityNotFoundException()
        { }

        public EntityNotFoundException(object entityId, Type entityType)
        {
            this.entityId = entityId;
            this.entityType = entityType.FullName;
        }

        public override string Message
        {
            get
            {
                return string.Format("Cannot find the entity {0} of id {1}.", entityType, entityId);
            }
        }

        public object EntityId
        {
            get { return this.entityId; }
        }

        public string EntityType
        {
            get { return this.entityType; }
        }
    }
}
