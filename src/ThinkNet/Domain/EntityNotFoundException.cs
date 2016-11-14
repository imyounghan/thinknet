using System;
using System.Runtime.Serialization;

namespace ThinkNet.Domain
{
    /// <summary>
    /// Represents an exception when tring to get a not existing entity.
    /// </summary>
    [Serializable]
    public class EntityNotFoundException : Exception
    {
        private readonly object entityId;
        private readonly string entityType;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EntityNotFoundException(object entityId, Type entityType)
        {
            this.entityId = entityId;
            this.entityType = entityType.FullName;
        }

        /// <summary>
        /// 获取当前异常信息。
        /// </summary>
        public override string Message
        {
            get
            {
                return string.Format("Cannot find the entity '{0}' of id '{1}'.", entityType, entityId);
            }
        }
        /// <summary>
        /// 获取实体ID
        /// </summary>
        public object EntityId
        {
            get { return this.entityId; }
        }
        /// <summary>
        /// 获取实体类型名称
        /// </summary>
        public string EntityType
        {
            get { return this.entityType; }
        }

        /// <summary>
        /// 重写 System.Runtime.Serialization.SerializationInfo
        /// </summary>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            
            info.AddValue("entityId", this.entityId);
            info.AddValue("entityType", this.entityType);
        }
    }
}
