using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using ThinkNet.Configurations;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示一个序列化器。用来序列化对象的字节数组
    /// </summary>
    [UnderlyingComponent(typeof(DefaultBinarySerializer))]
    public interface IBinarySerializer
    {
        /// <summary>
        /// 序列化一个对象到 <see cref="Stream"/>。
        /// </summary>
        void Serialize(Stream stream, object obj);
        /// <summary>
        /// 序列化一个对象到 <see cref="Stream"/>。
        /// </summary>
        void Serialize(Stream stream, object obj, bool formatType);
        /// <summary>
        /// 反序列化一个对象从一个 <see cref="Stream"/>。
        /// </summary>
        object Deserialize(Stream stream);
        /// <summary>
        /// 反序列化一个对象从一个 <see cref="Stream"/>。
        /// </summary>
        object Deserialize(Stream stream, Type type);
    }

    /// <summary>
    /// <see cref="IBinarySerializer"/> 的默认实现。
    /// </summary>
    internal class DefaultBinarySerializer : IBinarySerializer
    {
        /// <summary>
        /// <see cref="IBinarySerializer"/> 的一个实例。
        /// </summary>
        public static readonly IBinarySerializer Instance = new DefaultBinarySerializer();

        private readonly BinaryFormatter _binaryFormatter = new BinaryFormatter();
        ///// <summary>
        ///// Default Constructor.
        ///// </summary>
        //public DefaultBinarySerializer()
        //{
        //    this._binaryFormatter = new BinaryFormatter();
        //}

        #region IBinarySerializer 成员
        /// <summary>
        /// 序列化一个对象到 <see cref="Stream"/>。
        /// </summary>
        public void Serialize(Stream stream, object obj)
        {
            this.Serialize(stream, obj, false);
        }
        /// <summary>
        /// 序列化一个对象到 <see cref="Stream"/>。
        /// </summary>
        public void Serialize(Stream stream, object obj, bool formatType)
        {
            if (formatType) {
                _binaryFormatter.Serialize(stream, obj);
            }
            else {
                var serializer = new DataContractJsonSerializer(obj.GetType());
                serializer.WriteObject(stream, obj);
            }
        }
        /// <summary>
        /// 反序列化一个对象从一个 <see cref="Stream"/>。
        /// </summary>
        public object Deserialize(Stream stream)
        {
            return _binaryFormatter.Deserialize(stream);
        }
        /// <summary>
        /// 反序列化一个对象从一个 <see cref="Stream"/>。
        /// </summary>
        public object Deserialize(Stream stream, Type type)
        {
            var serializer = new DataContractJsonSerializer(type);
            return serializer.ReadObject(stream);
        }

        #endregion
    }
}
