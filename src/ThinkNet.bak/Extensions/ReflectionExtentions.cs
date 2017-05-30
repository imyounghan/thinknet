using System;
using System.Linq;
using System.Reflection;

namespace ThinkNet
{
    /// <summary>
    /// <see cref="MemberInfo"/> 的扩展类
    /// </summary>
    public static class ReflectionExtentions
    {
        /// <summary>
        /// Returns the type of the specified member
        /// </summary>
        /// <param name="memberInfo">member to get type from</param>
        /// <returns>Member type</returns>
        public static Type GetMemberType(this MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo)
                return ((FieldInfo)memberInfo).FieldType;
            if (memberInfo is PropertyInfo)
                return ((PropertyInfo)memberInfo).PropertyType;
            if (memberInfo is MethodInfo)
                return ((MethodInfo)memberInfo).ReturnType;
            if (memberInfo is ConstructorInfo)
                return null;
            if (memberInfo is Type)
                return (Type)memberInfo;
            throw new ArgumentException();
        }

        /// <summary>
        /// check the member is static.
        /// </summary>
        /// <param name="memberInfo">member to get type from</param>
        /// <returns>Member type</returns>
        public static bool IsStaticMember(this MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo)
                return ((FieldInfo)memberInfo).IsStatic;
            if (memberInfo is PropertyInfo) {
                MethodInfo propertyMethod;
                PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
                if ((propertyMethod = propertyInfo.GetGetMethod()) != null || (propertyMethod = propertyInfo.GetSetMethod()) != null)
                    return IsStaticMember(propertyMethod);

            }
            if (memberInfo is MethodInfo)
                return ((MethodInfo)memberInfo).IsStatic;
            throw new ArgumentException();
        }

        /// <summary>
        /// Gets a field/property
        /// </summary>
        /// <param name="memberInfo">The memberInfo specifying the object</param>
        /// <param name="o">The object</param>
        public static object GetMemberValue(this MemberInfo memberInfo, object o)
        {
            if (memberInfo is FieldInfo)
                return ((FieldInfo)memberInfo).GetValue(o);
            if (memberInfo is PropertyInfo)
                return ((PropertyInfo)memberInfo).GetGetMethod().Invoke(o, new object[0]);
            throw new ArgumentException();
        }

        /// <summary>
        /// Sets a field/property
        /// </summary>
        /// <param name="memberInfo">The memberInfo specifying the object</param>
        /// <param name="o">The object</param>
        /// <param name="value">The field/property value to assign</param>
        public static void SetMemberValue(this MemberInfo memberInfo, object o, object value)
        {
            if (memberInfo is FieldInfo)
                ((FieldInfo)memberInfo).SetValue(o, value);
            else if (memberInfo is PropertyInfo)
                ((PropertyInfo)memberInfo).GetSetMethod().Invoke(o, new[] { value });
            else throw new ArgumentException();
        }

        /// <summary>
        /// If memberInfo is a method related to a property, returns the PropertyInfo
        /// </summary>
        public static PropertyInfo GetExposingProperty(this MemberInfo memberInfo)
        {
            var reflectedType = memberInfo.ReflectedType;
            foreach (var propertyInfo in reflectedType.GetProperties()) {
                if (propertyInfo.GetGetMethod() == memberInfo || propertyInfo.GetSetMethod() == memberInfo)
                    return propertyInfo;
            }
            return null;
        }

        /// <summary>
        /// This function returns the type that is the "return type" of the member.
        /// If it is a template it returns the first template parameter type.
        /// </summary>
        /// <param name="memberInfo">The member info.</param>
        public static Type GetFirstInnerReturnType(this MemberInfo memberInfo)
        {
            var type = memberInfo.GetMemberType();

            if (type == null)
                return null;

            if (type.IsGenericType) {
                return type.GetGenericArguments()[0];
            }

            return type;
        }


        //private static A GetSingleAttribute<A>(object[] attributes)
        //    where A : Attribute
        //{
        //    if (attributes.Length > 0)
        //        return (A)attributes[0];
        //    return null;
        //}

        ///// <summary>
        ///// Returns a requested attribute for a given assembly
        ///// </summary>
        ///// <typeparam name="A">The requested attribute type</typeparam>
        ///// <param name="assembly">The assembly supposed to provide that attribute</param>
        ///// <returns>An attribute of type A or null if none</returns>
        //public static A GetAttribute<A>(this Assembly assembly)
        //    where A : Attribute
        //{
        //    return GetSingleAttribute<A>(assembly.GetCustomAttributes(typeof(A), true));
        //}

        ///// <summary>
        ///// Returns a requested attribute for a given type
        ///// </summary>
        ///// <typeparam name="A">The requested attribute type</typeparam>
        ///// <param name="type">The class supposed to provide that attribute</param>
        ///// <returns>An attribute of type A or null if none</returns>
        //public static A GetAttribute<A>(this Type type)
        //    where A : Attribute
        //{
        //    return GetSingleAttribute<A>(type.GetCustomAttributes(typeof(A), true));
        //}

        ///// <summary>
        ///// Returns a requested attribute for a given member
        ///// </summary>
        ///// <typeparam name="A">The requested attribute type</typeparam>
        ///// <param name="memberInfo">The member supposed to provide that attribute</param>
        ///// <returns>An attribute of type A or null if none</returns>
        //public static A GetAttribute<A>(this MemberInfo memberInfo)
        //    where A : Attribute
        //{
        //    return GetSingleAttribute<A>(memberInfo.GetCustomAttributes(typeof(A), true));
        //}
        /// <summary>
        /// 返回<paramref name="provider"/>上定义的<typeparamref name="A"/>特性数组。
        /// </summary>
        /// <typeparam name="A">特性类型</typeparam>
        /// <param name="provider">为支持自定义属性的反映对象提供自定义属性。</param>
        /// <param name="inherit">当为 true 时，查找继承的自定义属性的层次结构链。</param>
        public static A[] GetCustomAttributes<A>(this ICustomAttributeProvider provider, bool inherit)
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
        public static A GetCustomAttribute<A>(this ICustomAttributeProvider provider, bool inherit)
            where A : Attribute
        {
            var attributes = provider.GetCustomAttributes<A>(inherit);

            if (attributes != null && attributes.Length > 0)
                return (A)attributes[0];
            return null;
            //return provider.IsDefined<A>(inherit)
            //        ? provider.GetAttributes<A>(inherit)[0]
            //        : (A)null;
        }

        /// <summary>
        /// 判断<paramref name="provider"/>上是否定义<typeparamref name="TAttribute"/>特性
        /// </summary>
        /// <typeparam name="TAttribute">特性类型</typeparam>
        /// <param name="provider">为支持自定义属性的反映对象提供自定义属性。</param>
        /// <param name="inherit">当为 true 时，查找继承的自定义属性的层次结构链。</param>
        public static bool IsDefined<TAttribute>(this ICustomAttributeProvider provider, bool inherit)
            where TAttribute : Attribute
        {
            return provider.IsDefined(typeof(TAttribute), inherit);
        }
    }
}
