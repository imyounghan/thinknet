using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示一个要发送的信件
    /// </summary>
    public class Envelope
    {
        private readonly Dictionary<string, object> dict;
        public Envelope()
        {
            this.dict = new Dictionary<string, object>();
        }

        public Envelope(object body)
            : this(body, body.GetType())
        { }
        public Envelope(object body, Type type)
        {
            this.Body = body;

            this.dict = new Dictionary<string, object>() {
                { StandardMetadata.Namespace, type.Namespace },
                { StandardMetadata.TypeName, type.Name },
                { StandardMetadata.AssemblyName, Path.GetFileNameWithoutExtension(type.Assembly.ManifestModule.FullyQualifiedName) }
            };
        }
        public object Body { get; set; }
        public IDictionary Metadata
        {
            get { return this.dict; }
        }

        public void Complete(object source)
        {
            EnvelopeCompleted.Invoke(source, this);
        }

        public string GetMetadata(string key)
        {
            object value;
            if (dict.TryGetValue(key, out value)) {
                return value.ToString();
            }

            return string.Empty;
        }

        public static event EventHandler<Envelope> EnvelopeCompleted = (sender, args) => { };


        /// <summary>
        /// 从入队到出队的时间
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// 等待入队的时间
        /// </summary>
        public TimeSpan WaitTime { get; set; }

        /// <summary>
        /// 处理该消息的时长
        /// </summary>
        public TimeSpan ProcessTime { get; set; }        
    }
}
