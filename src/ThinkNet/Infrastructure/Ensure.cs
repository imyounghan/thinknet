using System;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 参数约定
    /// </summary>
    public static class Ensure
    {
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的值不能是 null。
        /// </summary>
        public static void NotNull<T>(T @object, string variableName) where T : class
        {
            if (@object == null)
                throw new ArgumentNullException(variableName);
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的字符串不能 <see cref="string.Empty"/> 字符串。
        /// </summary>
        public static void NotEmpty(string @string, string variableName)
        {
            if (@string != null && string.IsNullOrEmpty(@string))
                throw new ArgumentException(variableName);
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的字符串不能是 null 或 <see cref="string.Empty"/> 字符串。
        /// </summary>
        public static void NotNullOrEmpty(string @string, string variableName)
        {
            if (string.IsNullOrEmpty(@string))
                throw new ArgumentNullException(variableName);
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的字符串不能是空白字符串。
        /// </summary>
        public static void NotWhiteSpace(string @string, string variableName)
        {
            if (@string != null && string.IsNullOrWhiteSpace(@string))
                throw new ArgumentException(variableName);
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的字符串不能是 null 或 空白字符串。
        /// </summary>
        public static void NotNullOrWhiteSpace(string @string, string variableName)
        {
            if (string.IsNullOrWhiteSpace(@string))
                throw new ArgumentNullException(variableName);
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的数值必须是正整数。
        /// </summary>
        public static void Positive(int number, string variableName)
        {
            if (number <= 0)
                throw new ArgumentOutOfRangeException(variableName, string.Concat(variableName, " should be positive."));
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的数值必须是正整数。
        /// </summary>
        public static void Positive(long number, string variableName)
        {
            if (number <= 0)
                throw new ArgumentOutOfRangeException(variableName, string.Concat(variableName, " should be positive."));
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的数值不能是负数。
        /// </summary>
        public static void Nonnegative(long number, string variableName)
        {
            if (number < 0)
                throw new ArgumentOutOfRangeException(variableName,string.Concat(variableName, " should be non negative."));
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的数值不能是负数。
        /// </summary>
        public static void Nonnegative(int number, string variableName)
        {
            if (number < 0)
                throw new ArgumentOutOfRangeException(variableName, string.Concat(variableName, " should be non negative."));
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的标识符不能是均为零的标识。
        /// </summary>
        public static void NotEmptyGuid(Guid guid, string variableName)
        {
            if (Guid.Empty == guid)
                throw new ArgumentException(variableName, variableName + " shoud be non-empty GUID.");
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的变量值 <paramref name="expected" /> 必须与 <paramref name="actual" /> 相等。
        /// </summary>
        public static void Equal(int expected, int actual, string variableName)
        {
            if (expected != actual)
                throw new ArgumentException(string.Format("{0} expected value: {1}, actual value: {2}", variableName, expected, actual));
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的变量值 <paramref name="expected" /> 必须与 <paramref name="actual" /> 相等。
        /// </summary>
        public static void Equal(long expected, long actual, string variableName)
        {
            if (expected != actual)
                throw new ArgumentException(string.Format("{0} expected value: {1}, actual value: {2}", variableName, expected, actual));
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的变量值 <paramref name="expected" /> 必须与 <paramref name="actual" /> 相等。
        /// </summary>
        public static void Equal(bool expected, bool actual, string variableName)
        {
            if (expected != actual)
                throw new ArgumentException(string.Format("{0} expected value: {1}, actual value: {2}", variableName, expected, actual));
        }        
    }
}
