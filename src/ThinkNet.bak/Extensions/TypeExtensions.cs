using System;
using System.IO;
using System.Reflection;

namespace ThinkNet
{
    /// <summary>
    /// <see cref="Type"/> 的扩展类
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Determines if a given type can have a null value
        /// </summary>
        public static bool CanBeNull(this Type t)
        {
            return IsNullable(t) || !t.IsValueType;
        }

        /// <summary>
        /// Returns a unique MemberInfo
        /// </summary>
        /// <param name="t">The declaring type</param>
        /// <param name="name">The member name</param>
        /// <returns>A MemberInfo or null</returns>
        public static MemberInfo GetSingleMember(this Type t, string name)
        {
            return GetSingleMember(t, name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Returns a unique MemberInfo
        /// </summary>
        /// <param name="t">The declaring type</param>
        /// <param name="name">The member name</param>
        /// <param name="bindingFlags">Binding flags</param>
        /// <returns>A MemberInfo or null</returns>
        public static MemberInfo GetSingleMember(this Type t, string name, BindingFlags bindingFlags)
        {
            var members = t.GetMember(name, bindingFlags);
            if (members.Length > 0)
                return members[0];
            return null;
        }

        /// <summary>
        /// Determines if a Type is specified as nullable
        /// </summary>
        public static bool IsNullable(this Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// If the type is nullable, returns the underlying type
        /// Undefined behavior otherwise (it's user responsibility to check for Nullable first)
        /// </summary>
        public static Type GetNullableType(this Type t)
        {
            return Nullable.GetUnderlyingType(t);
        }

        /// <summary>
        /// 获取该类型的默认值
        /// </summary>
        public static object GetDefaultValue(this Type type)
        {
            if (!type.IsValueType)
                return null;
            return Activator.CreateInstance(type);
        }

        /// <summary>
        /// 获取该类型的程序集名称
        /// </summary>
        public static string GetAssemblyName(this Type type)
        {
            return Path.GetFileNameWithoutExtension(type.Assembly.ManifestModule.FullyQualifiedName);
        }

        /// <summary>
        /// 获取该类型的完整名称且包括程序集名称
        /// </summary>
        public static string GetFullName(this Type type)
        {
            return string.Concat(type.FullName, ", ", type.GetAssemblyName());
        }

        /// <summary>
        /// Returns type name without generic specification
        /// </summary>
        public static string GetShortName(this Type t)
        {
            var name = t.Name;
            if (t.IsGenericTypeDefinition)
                return name.Split('`')[0];
            return name;
        }
    }
}
