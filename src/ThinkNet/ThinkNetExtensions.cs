using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace ThinkNet
{
    public static class ThinkNetExtensions
    {
        /// <summary>
        /// 验证模型的正确性
        /// </summary>
        public static bool IsValid<TModel>(this TModel model, out IEnumerable<ValidationResult> errors)
            where TModel : class
        {
            errors = from property in TypeDescriptor.GetProperties(model).Cast<PropertyDescriptor>()
                     from attribute in property.Attributes.OfType<ValidationAttribute>()
                     where !attribute.IsValid(property.GetValue(model))
                     select new ValidationResult(attribute.FormatErrorMessage(property.DisplayName ?? property.Name));

            return errors != null && errors.Any();
        }

        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的值不能是 null。
        /// </summary>
        public static void NotNull(this object obj, string variableName)
        {
            if (obj == null || obj == DBNull.Value)
                throw new ArgumentNullException(variableName);
        }

        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的字符串不能 <see cref="string.Empty"/> 字符串。
        /// </summary>
        public static void NotEmpty(this string @string, string variableName)
        {
            if (@string != null && string.IsNullOrEmpty(@string))
                throw new ArgumentException(variableName);
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的字符串不能是 null 或 <see cref="string.Empty"/> 字符串。
        /// </summary>
        public static void NotNullOrEmpty(this string @string, string variableName)
        {
            if (string.IsNullOrEmpty(@string))
                throw new ArgumentNullException(variableName);
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的字符串不能是空白字符串。
        /// </summary>
        public static void NotWhiteSpace(this string @string, string variableName)
        {
            if (@string != null && string.IsNullOrWhiteSpace(@string))
                throw new ArgumentException(variableName);
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的字符串不能是 null 或 空白字符串。
        /// </summary>
        public static void NotNullOrWhiteSpace(this string @string, string variableName)
        {
            if (string.IsNullOrWhiteSpace(@string))
                throw new ArgumentNullException(variableName);
        }

        /// <summary>
        /// Returns default value for provided type
        /// </summary>
        public static object GetDefault(this Type t)
        {
            if (!t.IsValueType)
                return null;
            return Activator.CreateInstance(t);
        }

        /// <summary>
        /// 返回<paramref name="provider"/>上定义的<typeparamref name="A"/>特性数组。
        /// </summary>
        /// <typeparam name="A">特性类型</typeparam>
        /// <param name="provider">为支持自定义属性的反映对象提供自定义属性。</param>
        /// <param name="inherit">当为 true 时，查找继承的自定义属性的层次结构链。</param>
        public static A[] GetAttributes<A>(this ICustomAttributeProvider provider, bool inherit)
            where A : Attribute
        {
            return provider
                .GetCustomAttributes(typeof(A), inherit)
                .Cast<A>()
                .ToArray();
        }


        /// <summary>
        /// 返回<paramref name="provider"/>上定义的第一个<typeparamref name="A"/>特性。
        /// </summary>
        /// <typeparam name="A">特性类型</typeparam>
        /// <param name="provider">为支持自定义属性的反映对象提供自定义属性。</param>
        /// <param name="inherit">当为 true 时，查找继承的自定义属性的层次结构链。</param>
        public static A GetAttribute<A>(this ICustomAttributeProvider provider, bool inherit)
            where A : Attribute
        {
            var attributes = provider.GetAttributes<A>(inherit);

            if (attributes != null && attributes.Length > 0)
                return (A)attributes[0];
            return null;
            //return provider.IsDefined<A>(inherit)
            //        ? provider.GetAttributes<A>(inherit)[0]
            //        : (A)null;
        }

        /// <summary>
        /// 如果当前的字符串为空，则返回安全值
        /// </summary>
        public static string IfEmpty(this string str, string defaultValue)
        {
            return string.IsNullOrWhiteSpace(str) ? defaultValue : str;
        }

        /// <summary>
        /// 如果当前的字符串为空，则返回安全值
        /// </summary>
        public static string IfEmpty(this string str, Func<string> valueFactory)
        {
            return string.IsNullOrWhiteSpace(str) ? valueFactory() : str;
        }

        /// <summary>
        /// 将 <param name="str" /> 转换为 <param name="targetType" /> 的值。转换失败会抛异常
        /// </summary>
        private static object Change(string str, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(str))
                return Activator.CreateInstance(targetType);

           
            if (targetType.IsValueType) {
                if (typeof(bool) == targetType) {
                    var lb = str.ToUpper();
                    if (lb == "T" || lb == "F" || lb == "TRUE" || lb == "FALSE" ||
                        lb == "Y" || lb == "N" || lb == "YES" || lb == "NO") {
                        return (lb == "T" || lb == "TRUE" || lb == "Y" || lb == "YES");
                    }
                }

                var method = targetType.GetMethod("Parse", new Type[] { typeof(string) });
                if (method != null) {
                    return method.Invoke(null, new object[] { str });
                }
            }

            if (targetType.IsEnum) {
                return Enum.Parse(targetType, str);
            }

            throw new ArgumentException(string.Format("Unhandled type of '{0}'.", targetType));
        }

        /// <summary>
        /// 将 <param name="str" /> 转换为 <typeparam name="T" /> 的值。
        /// </summary>
        public static T Change<T>(this string str)
            where T : struct
        {
            return (T)Change(str, typeof(T));
        }

        /// <summary>
        /// 将 <param name="string" /> 转换为 <typeparam name="T" /> 的值。如果转换失败则使用 <param name="defaultValue" /> 的值。
        /// </summary>
        public static T Change<T>(this string str, T defaultValue)
            where T : struct
        {
            T result;
            if (str.TryChange<T>(out result)) {
                return result;
            }

            return defaultValue;
        }

        /// <summary>
        /// 将 <param name="string" /> 转换为 <typeparam name="T" /> 的值。一个指示转换是否成功的返回值 <param name="result" />。
        /// </summary>
        public static bool TryChange<T>(this string str, out T result)
            where T : struct
        {
            try {
                result = str.Change<T>();
                return true;
            }
            catch (Exception) {
                result = default(T);
                return false;
            }
        }


        public static T Clone<T>(this T obj)
            where T : class
        {
            var cloneable = obj as ICloneable;
            if (cloneable != null)
                return cloneable as T;

            var type = typeof(T);
            if (!type.IsClass || type.IsAbstract) {
                throw new ArgumentException(string.Format("This type of '{0}' is not a class.", type.FullName));
            }

            var properties = type.GetProperties();
            var cloneObj = (T)FormatterServices.GetUninitializedObject(type);
            Map(properties, obj, cloneObj);

            return cloneObj;
        }

        //internal static object Clone(this object obj)
        //{
        //    var type = obj.GetType();
        //    var newObj = FormatterServices.GetUninitializedObject(type);
        //    var properties = type.GetProperties();

        //    Map(properties, obj, newObj);

        //    return newObj;
        //}

        private static void Map(PropertyInfo[] properties, object source, object target)
        {
            foreach (var property in properties) {
                if (property.PropertyType.IsValueType || property.PropertyType == typeof(string)) {
                    property.SetValue(target, property.GetValue(source, null), null);
                }
                else {
                    var memberProperties = property.PropertyType.GetProperties();
                    var memberClone = FormatterServices.GetUninitializedObject(property.PropertyType);
                    Map(memberProperties, property.GetValue(source, null), memberClone);
                    property.SetValue(target, memberClone, null);
                }
            }
        }


        /// <summary>
        /// 如果 <param name="source" /> 为null，则创建一个空的 <see cref="IEnumerable{T}"/>。
        /// </summary>
        public static IEnumerable<T> IfNull<T>(this IEnumerable<T> source)
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

        private static object GetLockObj(object obj)
        {
            var collection = obj as ICollection;

            if (collection == null) {
                return obj;
            }
            else {
                return collection.SyncRoot;
            }
        }
        /// <summary>
        /// 如果指定的键尚不存在，则将键/值对添加到字典中。
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> valueFactory, bool synchroniztion = false)
        {
            if (synchroniztion)
                return GetOrAdd(dict, key, valueFactory);

            TValue value;
            if (!dict.TryGetValue(key, out value)) {
                value = valueFactory.Invoke();
                dict.Add(key, value);
            }

            return value;
        }

        private static TValue GetOrAdd<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey key, Func<TValue> valueFactory)
        {
            TValue value;
            if (!dict.TryGetValue(key, out value)) {
                lock (GetLockObj(dict)) {
                    value = dict.GetOrAdd(key, valueFactory);
                }
            }

            return value;
        }

        /// <summary>
        /// 如果指定的键尚不存在，则将键/值对添加到 <see cref="IDictionary{TKey, TValue}"/> 中；如果指定的键已存在，则更新 <see cref="Dictionary{TKey, TValue}"/> 中的键/值对。
        /// </summary>
        public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> addValueFactory, Func<TValue, TValue> updateValueFactory, bool synchroniztion = false)
        {
            if (synchroniztion)
                return AddOrUpdate(dict, key, addValueFactory, updateValueFactory);

            TValue value;

            if (dict.TryGetValue(key, out value)) {
                value = updateValueFactory.Invoke(value);
                dict[key] = value;
            }
            else {
                value = addValueFactory.Invoke();
                dict.Add(key, value);
            }

            return value;
        }

        public static TValue AddOrUpdate<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey key, Func<TValue> addValueFactory, Func<TValue, TValue> updateValueFactory)
        {
            lock (GetLockObj(dict)) {
                return dict.AddOrUpdate(key, addValueFactory, updateValueFactory);
            }
        }

        /// <summary>
        /// 获取key的元素
        /// </summary>
        public static TValue Get<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key)
        {
            TValue value;
            if (dict.TryGetValue(key, out value)) {
                return value;
            }

            return default(TValue);
        }

        /// <summary>
        /// 删除存在key的元素
        /// </summary>
        public static TValue Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key)
        {
            if (dict == null || dict.Count == 0)
                return default(TValue);

            TValue value;
            if (dict.TryRemove(key, out value)) {
                return value;
            }

            return default(TValue);
        }

        /// <summary>
        /// 删除存在key的元素
        /// </summary>
        public static bool TryRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key)
        {
            if (dict == null || dict.Count == 0)
                return false;

            TValue value;
            return dict.TryRemove(key, out value);
        }

        /// <summary>
        /// 移除并返回位于队列开始处的元素。
        /// </summary>
        /// <returns>如果成功移除返回队列开始处的元素，否则返回该泛型的默认值。</returns>
        public static T Dequeue<T>(this ConcurrentQueue<T> queue)
        {
            T item;
            if (queue.TryDequeue(out item)) {
                return item;
            }

            return default(T);
        }

        /// <summary>
        /// 写日志
        /// </summary>
        public static void Debug(this LogManager.ILogger log, Exception ex)
        {
            log.Debug(ex.Message, ex);
        }
        /// <summary>
        /// 写日志
        /// </summary>
        public static void Debug(this LogManager.ILogger log, Exception ex, string format, params object[] args)
        {
            log.Debug(string.Format(format, args), ex);
        }

        /// <summary>
        /// 写日志
        /// </summary>
        public static void Info(this LogManager.ILogger log, Exception ex)
        {
            log.Info(ex.Message, ex);
        }
        /// <summary>
        /// 写日志
        /// </summary>
        public static void Info(this LogManager.ILogger log, Exception ex, string format, params object[] args)
        {
            log.Info(string.Format(format, args), ex);
        }

        /// <summary>
        /// 写日志
        /// </summary>
        public static void Warn(this LogManager.ILogger log, Exception ex)
        {
            log.Warn(ex.Message, ex);
        }
        /// <summary>
        /// 写日志
        /// </summary>
        public static void Warn(this LogManager.ILogger log, Exception ex, string format, params object[] args)
        {
            log.Warn(string.Format(format, args), ex);
        }

        /// <summary>
        /// 写日志
        /// </summary>
        public static void Error(this LogManager.ILogger log, Exception ex)
        {
            log.Error(ex.Message, ex);
        }
        /// <summary>
        /// 写日志
        /// </summary>
        public static void Error(this LogManager.ILogger log, Exception ex, string format, params object[] args)
        {
            log.Error(string.Format(format, args), ex);
        }

        /// <summary>
        /// 写日志
        /// </summary>
        public static void Fatal(this LogManager.ILogger log, Exception ex)
        {
            log.Fatal(ex.Message, ex);
        }
        /// <summary>
        /// 写日志
        /// </summary>
        public static void Fatal(this LogManager.ILogger log, Exception ex, string format, params object[] args)
        {
            log.Fatal(string.Format(format, args), ex);
        }
    }
}
