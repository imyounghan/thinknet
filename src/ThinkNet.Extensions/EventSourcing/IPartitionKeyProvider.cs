using System;
using System.Reflection;
using ThinkNet.Annotation;


namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 提供存储事件表的名称的提供程序
    /// </summary>
    //[RequiredComponent(typeof(DefaultEventTableNameProvider))]
    public interface IPartitionKeyProvider
    {
        /// <summary>
        /// 获取给定的聚合根的分区键
        /// </summary>
        string GetPartitionKey(Type type);
        ///// <summary>
        ///// 获取所有存储事件的表名称。
        ///// </summary>
        //IEnumerable<string> GetAllTables();
    }

    //internal class DefaultEventTableNameProvider : IEventTableNameProvider
    //{
    //    /// <summary>
    //    /// 获取给定的聚合根类型的表名称
    //    /// </summary>
    //    public string GetTableName(Type aggregateRootType)
    //    {
    //        var attribute = aggregateRootType.GetAttribute<EventTableNameAttribute>(false);

    //        if (attribute == null) {
    //            return "Events";
    //        }

    //        return attribute.PerAggregate ? string.Concat("Events_", aggregateRootType.Name) : attribute.TableName;
    //    }
    //}
}
