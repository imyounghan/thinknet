using System.IO;
using System.Runtime.Serialization.Formatters.Binary;


namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示一个序列化器。用来序列化对象的字节数组
    /// </summary>
    [RequiredComponent(typeof(DefaultBinarySerializer))]
    public interface IBinarySerializer
    {
        /// <summary>
        /// 序列化一个对象到字节数组。
        /// </summary>
        byte[] Serialize(object obj);
        /// <summary>
        /// 反序列化一个对象从一个字节数组。
        /// </summary>
        object Deserialize(byte[] data, System.Type type);
    }

    internal class DefaultBinarySerializer : IBinarySerializer
    {
        private readonly BinaryFormatter _binaryFormatter = new BinaryFormatter();

        public byte[] Serialize(object obj)
        {
            using (var stream = new MemoryStream()) {
                _binaryFormatter.Serialize(stream, obj);
                return stream.ToArray();
            }
        }

        public object Deserialize(byte[] data, System.Type type)
        {
            using (var stream = new MemoryStream(data)) {
                return _binaryFormatter.Deserialize(stream);
            }
        }
    }
}
