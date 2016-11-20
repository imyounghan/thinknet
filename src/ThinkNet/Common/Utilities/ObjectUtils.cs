using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.Script.Serialization;

namespace ThinkNet.Common.Utilities
{
    /// <summary>
    /// Object工具类
    /// </summary>
    public static class ObjectUtils
    {
        private readonly static ConcurrentDictionary<Type, PropertyInfo[]> _entityInfosCache = new ConcurrentDictionary<Type, PropertyInfo[]>();

        private static PropertyInfo[] GetPropertiesFromType(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        /// <summary>
        /// 获取该类型下所有属性
        /// </summary>
        public static PropertyInfo[] GetProperties(Type type)
        {
            return GetProperties(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        /// <summary>
        /// 获取该类型下指定的所有属性
        /// </summary>
        public static PropertyInfo[] GetProperties(Type type, BindingFlags bindingAttr)
        {
            return _entityInfosCache.GetOrAdd(type, key => type.GetProperties(bindingAttr));
        }

        /// <summary>
        /// 查找该类型下的属性
        /// </summary>
        public static PropertyInfo FindProperty(Type entityType, string propertyName)
        {
            var predicate = new Func<PropertyInfo, bool>(property => string.Equals(property.Name, propertyName, StringComparison.CurrentCultureIgnoreCase));

            var properties = GetProperties(entityType);
            if (!properties.Any(predicate))
                return null;

            return properties.First(predicate);
        }


        /// <summary>Create an object from the source object, assign the properties by the same name.
        /// </summary>
        public static T CreateObject<T>(object source) where T : class, new()
        {
            var obj = new T();
            var propertiesFromSource = GetProperties(source.GetType());
            var properties = GetProperties(typeof(T));

            foreach (var property in properties) {
                var sourceProperty = propertiesFromSource.FirstOrDefault(x => x.Name == property.Name);
                if (sourceProperty != null) {
                    property.SetValue(obj, sourceProperty.GetValue(source, null), null);
                }
            }

            return obj;
        }

        /// <summary>Update the target object by the source object, assign the properties by the same name.
        /// </summary>
        public static void UpdateObject<TTarget, TSource>(TTarget target, TSource source, params Expression<Func<TSource, object>>[] propertyExpressionsFromSource)
            where TTarget : class
            where TSource : class
        {
            Ensure.NotNull(target, "target");
            Ensure.NotNull(source, "source");
            Ensure.NotNull(propertyExpressionsFromSource, "propertyExpressionsFromSource");
 
            var properties = GetProperties(typeof(TTarget));// target.GetType().GetProperties();

            foreach (var propertyExpression in propertyExpressionsFromSource) {
                var propertyFromSource = GetProperty<TSource, object>(propertyExpression);
                var propertyFromTarget = properties.SingleOrDefault(x => x.Name == propertyFromSource.Name);
                if (propertyFromTarget != null) {
                    propertyFromTarget.SetValue(target, propertyFromSource.GetValue(source, null), null);
                }
            }
        }

        private static PropertyInfo GetProperty<TSource, TProperty>(Expression<Func<TSource, TProperty>> lambda)
        {
            var type = typeof(TSource);
            MemberExpression memberExpression = null;

            switch (lambda.Body.NodeType) {
                case ExpressionType.Convert:
                    memberExpression = ((UnaryExpression)lambda.Body).Operand as MemberExpression;
                    break;
                case ExpressionType.MemberAccess:
                    memberExpression = lambda.Body as MemberExpression;
                    break;
            }

            if (memberExpression == null) {
                throw new ArgumentException(string.Format("Invalid Lambda Expression '{0}'.", lambda.ToString()));
            }

            var propInfo = memberExpression.Member as PropertyInfo;
            if (propInfo == null) {
                throw new ArgumentException(string.Format("Expression '{0}' refers to a field, not a property.", lambda.ToString()));
            }

            if (type != propInfo.ReflectedType && !type.IsSubclassOf(propInfo.ReflectedType)) {
                throw new ArgumentException(string.Format("Expresion '{0}' refers to a property that is not from type {1}.", lambda.ToString(), type));
            }

            return propInfo;
        }

        ///// <summary>
        ///// 类型转换
        ///// </summary>
        //public static T Convert<T>(object value)
        //{
        //    if (value == null) {
        //        return default(T);
        //    }

        //    return (T)Convert(value, typeof(T));
        //    //var typeConverter1 = TypeDescriptor.GetConverter(typeof(T));
        //    //if (typeConverter1.CanConvertFrom(value.GetType())) {
        //    //    return (T)typeConverter1.ConvertFrom(value);
        //    //}

        //    //var typeConverter2 = TypeDescriptor.GetConverter(value.GetType());
        //    //if (typeConverter2.CanConvertTo(typeof(T))) {
        //    //    return (T)typeConverter2.ConvertTo(value, typeof(T));
        //    //}

        //    //return (T)Convert.ChangeType(value, typeof(T));
        //}

        ///// <summary>
        ///// 类型转换
        ///// </summary>
        //public static object Convert(object value, Type targetType)
        //{
        //    if (value == null) {
        //        return Activator.CreateInstance(targetType);
        //    }

        //    var typeConverter1 = TypeDescriptor.GetConverter(targetType);
        //    if (typeConverter1.CanConvertFrom(value.GetType())) {
        //        return typeConverter1.ConvertFrom(value);
        //    }

        //    var typeConverter2 = TypeDescriptor.GetConverter(value.GetType());
        //    if (typeConverter2.CanConvertTo(targetType)) {
        //        return typeConverter2.ConvertTo(value, targetType);
        //    }

        //    return System.Convert.ChangeType(value, targetType);
        //}

        /// <summary>
        /// 获取对象的字符串格式
        /// </summary>
        public static string GetObjectString(object obj)
        {
            var properties = GetProperties(obj.GetType())
                .Where(prop => !prop.IsDefined<ScriptIgnoreAttribute>(false))
                .Select(prop => string.Concat(prop.Name, "=", prop.GetValue(obj, null))).ToArray();

            return string.Join("|", properties);
        }
    }
}
