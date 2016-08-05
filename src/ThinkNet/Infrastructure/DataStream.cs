
namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示事件或快照的数据流
    /// </summary>
    public class DataStream
    {
        public DataStream()
        { }

        public DataStream(DataKey key, int version, byte[] payload)
        {
            this.Key = key;
            this.Version = version;
            this.Payload = payload;
        }

        /// <summary>
        /// 标识
        /// </summary>
        public DataKey Key { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
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
