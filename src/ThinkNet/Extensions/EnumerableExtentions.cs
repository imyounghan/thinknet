using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;


namespace ThinkNet
{
    /// <summary>
    /// <see cref="IEnumerable"/> 的扩展类
    /// </summary>
    public static class EnumerableExtentions
    {
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的集合不能是数量为零的空集合。
        /// </summary>
        public static void NotEmpty<T>(this IEnumerable<T> source, string variableName)
        {
            if (source != null && source.IsEmpty())
                throw new ArgumentException(variableName);
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的集合不能是 null 或 数量为零的空集合。
        /// </summary>
        public static void NotNullOrEmpty<T>(this IEnumerable<T> source, string variableName)
        {
            if (source.IsEmpty())
                throw new ArgumentNullException(variableName);
        }

        /// <summary>
        /// 遍历结果集
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source.IsEmpty())
                return;

            foreach (var item in source) {
                action(item);
            }
        }
        /// <summary>
        /// 如果 <param name="source" /> 为null，则创建一个空的 <see cref="IEnumerable{T}"/>。
        /// </summary>
        public static IEnumerable<T> Safe<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }
        /// <summary>
        /// 检查 <param name="source" /> 是否为空。
        /// </summary>
        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            if (source == null)
                return true;
            var collection = source as ICollection;
            if (collection != null)
                return collection.Count == 0;
            return !source.Any();
        }

        /// <summary>
        /// 将字典数据映射成对应的模型
        /// </summary>
        public static T MapTo<T>(this IDictionary dict)
            where T : class
        {
            return dict.MapTo<T>(null);
        }
        /// <summary>
        /// 将字典数据映射成对应的模型
        /// </summary>
        public static T MapTo<T>(this IDictionary dict, IDictionary map)
            where T : class
        {
            return (T)dict.MapTo(typeof(T), map);
        }
        /// <summary>
        /// 将字典数据映射成对应的模型
        /// </summary>
        public static object MapTo(this IDictionary dict, Type type)
        {
            return dict.MapTo(type, null);
        }

        private readonly static ConcurrentDictionary<int, PropertyInfo[]> propertiesCache = new ConcurrentDictionary<int, PropertyInfo[]>();
        /// <summary>
        /// 将字典数据映射成对应的模型
        /// </summary>
        public static object MapTo(this IDictionary dict, Type type, IDictionary map)
        {
            if (dict == null || dict.Count == 0)
                return null;

            var entity = FormatterServices.GetUninitializedObject(type);

            type.GetProperties()
                .ForEach(property => {
                    string name = map != null && map.Contains(property.Name) ? (string)map[property.Name] : property.Name;

                    if (dict[name] != DBNull.Value && dict[name] != null) {
                        property.SetValue(entity, Convert.ChangeType(dict[name], property.PropertyType), null);
                    }
                });

            return entity;
        }
        /// <summary>
        /// 将字典集合数据映射成对应的模型
        /// </summary>
        public static IEnumerable<T> MapTo<T>(this ICollection collection) where T : class
        {
            return collection.Cast<IDictionary>().Select(dict => dict.MapTo<T>()).AsEnumerable();
        }
        /// <summary>
        /// 将字典集合数据映射成对应的模型
        /// </summary>
        public static IEnumerable<T> MapTo<T>(this ICollection collection, IDictionary map) where T : class
        {
            return collection.Cast<IDictionary>().Select(dict => dict.MapTo<T>(map)).AsEnumerable();
        }

    }
}
