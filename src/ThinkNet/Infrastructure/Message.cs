using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// <see cref="IMessage"/> 的抽象实现类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class Message : IMessage
    {
        /// <summary>
        /// 消息标识
        /// </summary>
        public string Id { get; private set; }
        /// <summary>
        /// 消息的时间戳
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected Message(string id)
        {
            this.Id = string.IsNullOrWhiteSpace(id) ? GuidUtil.NewSequentialId().ToString() : id;
            this.Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// 返回空的路由key
        /// </summary>
        public virtual string GetRoutingKey()
        {
            return string.Empty;
        }

        /// <summary>
        /// 输出消息的字符串格式
        /// </summary>
        public override string ToString()
        {
            var properties = new string[] {
                string.Concat("Id=", this.Id),
                string.Concat("Timestamp=", this.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            }.Concat(
                this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(prop => !prop.IsDefined(typeof(IgnoreDataMemberAttribute), false))
                .Select(prop => string.Concat(prop.Name, "=", prop.GetValue(this, null)))
                ).ToArray();

            return string.Join("|", properties);
        }

        /// <summary>
        /// Guid工具
        /// </summary>
        internal static class GuidUtil
        {
            private static readonly long epochMilliseconds = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000L;

            /// <summary>
            /// Creates a sequential GUID according to SQL Server's ordering rules.
            /// </summary>
            public static Guid NewSequentialId()
            {
                // This code was not reviewed to guarantee uniqueness under most conditions, nor completely optimize for avoiding
                // page splits in SQL Server when doing inserts from multiple hosts, so do not re-use in production systems.
                var guidBytes = Guid.NewGuid().ToByteArray();

                // get the milliseconds since Jan 1 1970
                byte[] sequential = BitConverter.GetBytes((DateTime.Now.Ticks / 10000L) - epochMilliseconds);

                // discard the 2 most significant bytes, as we only care about the milliseconds increasing, but the highest ones 
                // should be 0 for several thousand years to come (non-issue).
                if (BitConverter.IsLittleEndian) {
                    guidBytes[10] = sequential[5];
                    guidBytes[11] = sequential[4];
                    guidBytes[12] = sequential[3];
                    guidBytes[13] = sequential[2];
                    guidBytes[14] = sequential[1];
                    guidBytes[15] = sequential[0];
                }
                else {
                    Buffer.BlockCopy(sequential, 2, guidBytes, 10, 6);
                }

                return new Guid(guidBytes);
            }
        }
    }
}
