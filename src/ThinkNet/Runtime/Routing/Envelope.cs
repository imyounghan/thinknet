using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;

namespace ThinkNet.Runtime.Routing
{
    /// <summary>
    /// 表示一个要发送的信件
    /// </summary>
    public class Envelope : EventArgs
    {
        private readonly Dictionary<string, string> dict;
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public Envelope()
        {
            this.dict = new Dictionary<string, string>();
        }
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public Envelope(object body)
            : this(body, body.GetType())
        { }
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public Envelope(object body, Type type)
        {
            this.Body = body;

            this.dict = new Dictionary<string, string>() {
                { StandardMetadata.Namespace, type.Namespace },
                { StandardMetadata.TypeName, type.Name },
                { StandardMetadata.AssemblyName, Path.GetFileNameWithoutExtension(type.Assembly.ManifestModule.FullyQualifiedName) }
            };
        }
        /// <summary>
        /// 元数据
        /// </summary>
        public object Body { get; set; }
        /// <summary>
        /// 元数据信息
        /// </summary>
        public IDictionary<string, string> Metadata
        {
            get { return this.dict; }
        }
        /// <summary>
        /// 完成后的操作
        /// </summary>
        public void Complete(object source)
        {
            EnvelopeCompleted.Invoke(source, this);
        }

        /// <summary>
        /// 获取元数据的信息
        /// </summary>
        public string GetMetadata(string key)
        {
            string value;
            if (dict.TryGetValue(key, out value)) {
                return value;
            }

            return string.Empty;
        }

        /// <summary>
        /// 信件完成后的处理方式
        /// </summary>
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
