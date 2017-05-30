using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示这是一个可溯源的有序事件流。
    /// </summary>
    public sealed class EventCollection : IEnumerable<Event>, ICollection, IMessage, IUniquelyIdentifiable
    {
        private readonly List<Event> events;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public EventCollection(IEnumerable<Event> events)
        {
            this.events = new List<Event>(events);
        }

        /// <summary>
        /// 源ID
        /// </summary>
        public SourceKey SourceId { get; set; }

        /// <summary>
        /// 产生事件的相关标识(如命令的id)
        /// </summary>
        public string CorrelationId { get; set; }
        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 确定此实例是否与指定的对象相同。
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as EventCollection;
            if (other == null) {
                return false;
            }

            return other.SourceId == this.SourceId && other.Version == this.Version;
        }

        /// <summary>
        /// 返回此实例的哈希代码。
        /// </summary>
        public override int GetHashCode()
        {
            return SourceId.GetHashCode() ^ Version.GetHashCode();
        }

        /// <summary>
        /// 输出领域事件流的字符串格式
        /// </summary>
        public override string ToString()
        {
            //var events = this.events.Select(@event => string.Concat(@event.GetType().FullName, "&", @event.Id));

            return string.Concat(this.SourceId.GetSourceTypeName(), "@", this.SourceId.Id,
                "[", string.Join(",", events), "]", "#", this.CorrelationId);
        }
        

        //string IMessage.GetKey()
        //{
        //    return this.SourceId.Id;
        //}

        /// <summary>
        /// 返回一个循环访问 <see cref="EventCollection"/> 的枚举器。
        /// </summary>
        public IEnumerator<Event> GetEnumerator()
        {
            return events.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach(Event @event in events)
                yield return @event;
        }

        /// <summary>
        /// 从特定的 <see cref="Array"/> 索引处开始，将 <see cref="EventCollection"/> 的元素复制到一个 <see cref="Array"/> 中。
        /// </summary>
        public void CopyTo(Array array, int index)
        {
            int destIndex = 0;
            events.GetRange(index, events.Count - index).ForEach(
                delegate (Event info) {
                    array.SetValue(info, destIndex++);
                });
        }
        string IUniquelyIdentifiable.Id
        {
            get
            {
                return this.CorrelationId;
            }
        }
        /// <summary>
        /// 获取事件数量
        /// </summary>
        public int Count
        {
            get
            {
                return events.Count;
            }
        }

        /// <summary>
        /// 同步对象
        /// </summary>
        public object SyncRoot
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// 非线程安全
        /// </summary>
        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }
    }
}
