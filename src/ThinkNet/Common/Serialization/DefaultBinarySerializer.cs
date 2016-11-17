using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;

namespace ThinkNet.Common.Serialization
{
    /// <summary>
    /// <see cref="IBinarySerializer"/> 的默认实现
    /// </summary>
    public class DefaultBinarySerializer : IBinarySerializer
    {
        ///// <summary>
        ///// <see cref="IBinarySerializer"/> 的一个实例。
        ///// </summary>
        //public static readonly IBinarySerializer Instance = new DefaultBinarySerializer();

        private readonly BinaryFormatter _binaryFormatter;
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public DefaultBinarySerializer()
        {
            this._binaryFormatter = new BinaryFormatter();
        }

        #region IBinarySerializer 成员
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
