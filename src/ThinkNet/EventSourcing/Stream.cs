
namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 表示事件或快照的数据流
    /// </summary>
    public class Stream
    {
        public Stream()
        { }

        public Stream(SourceKey key, int version, byte[] payload)
        {
            this.Key = key;
            this.Version = version;
            this.Payload = payload;
        }

        /// <summary>
        /// 标识
        /// </summary>
        public SourceKey Key { get; set; }

        public int Version { get; set; }

        /// <summary>
        /// 流数据
        /// </summary>
        public byte[] Payload { get; set; }

        //public string GetSourceTypeFullName()
        //{
        //    return string.Concat(Key.Namespace, ".", Key.TypeName);
        //}

        public System.Type GetSourceType()
        {
            string typeFullName = string.Concat(Key.Namespace, ".", Key.TypeName, ", ", Key.AssemblyName);

            return System.Type.GetType(typeFullName);
        }
    }
}
