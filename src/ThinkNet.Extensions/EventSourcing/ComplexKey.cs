
namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 溯源用的主键
    /// </summary>
    public struct ComplexKey
    {
        //public ComplexKey() 
        //{ }

        private string sourceId;
        private string sourceType;
        private int version;
 
        public ComplexKey(string sourceId, string sourceType, int version)
        {
            this.sourceId = sourceId;
            this.sourceType = sourceType;
            this.version = version;
        }

        /// <summary>
        /// 源标识。
        /// </summary>
        public string SourceId { get { return this.sourceId; } }
        /// <summary>
        /// 源类型。
        /// </summary>
        public string SourceType { get { return this.sourceType; } }
        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get { return this.version; } }
    }
}
