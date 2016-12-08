using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ThinkNet
{
    /// <summary>
    /// 对 基础类型 的扩展
    /// </summary>
    public static class ObjectExtentions
    {
        /// <summary>
        /// 返回 HTML 字符串的编码结果
        /// </summary>
        public static string HtmlEncode(this string str)
        {
            if(string.IsNullOrEmpty(str))
                return str;

            return HttpUtility.HtmlEncode(str).Replace("'", "&dot");
        }

        /// <summary>
        /// 返回 HTML 字符串的解码结果
        /// </summary>
        public static string HtmlDecode(this string str)
        {
            if(string.IsNullOrEmpty(str))
                return str;

            return HttpUtility.HtmlDecode(str).Replace("&dot", "'");
        }

        /// <summary>
        /// 返回 URL 字符串的编码结果
        /// </summary>
        public static string UrlEncode(this string str, string charset = "utf-8")
        {
            return str.UrlEncode(Encoding.GetEncoding(charset));
        }
        /// <summary>
        /// 返回 URL 字符串的编码结果
        /// </summary>
        public static string UrlEncode(this string str, Encoding encoding)
        {
            return HttpUtility.UrlEncode(str, encoding);
        }

        /// <summary>
        /// 返回 URL 字符串的解码结果
        /// </summary>
        public static string UrlDecode(this string str, string charset = "utf-8")
        {
            return str.UrlDecode(Encoding.GetEncoding(charset));
        }
        /// <summary>
        /// 返回 URL 字符串的解码结果
        /// </summary>
        public static string UrlDecode(this string str, Encoding encoding)
        {
            return HttpUtility.UrlDecode(str, encoding);
        }

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
            if (obj.IsNull())
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
        /// 参数名称为 <paramref name="variableName"/> 的数值必须是正整数。
        /// </summary>
        public static void MustPositive(this int number, string variableName)
        {
            if (number <= 0)
                throw new ArgumentOutOfRangeException(variableName, string.Concat(variableName, " should be positive."));
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的数值必须是正整数。
        /// </summary>
        public static void MustPositive(this long number, string variableName)
        {
            if (number <= 0L)
                throw new ArgumentOutOfRangeException(variableName, string.Concat(variableName, " should be positive."));
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的数值必须是正整数。
        /// </summary>
        public static void MustPositive(this decimal number, string variableName)
        {
            if (number <= 0m)
                throw new ArgumentOutOfRangeException(variableName, string.Concat(variableName, " should be positive."));
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的数值不能是负数。
        /// </summary>
        public static void NonNegative(this long number, string variableName)
        {
            if (number < 0L)
                throw new ArgumentOutOfRangeException(variableName, string.Concat(variableName, " should be non negative."));
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的数值不能是负数。
        /// </summary>
        public static void NonNegative(this int number, string variableName)
        {
            if (number < 0)
                throw new ArgumentOutOfRangeException(variableName, string.Concat(variableName, " should be non negative."));
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的数值不能是负数。
        /// </summary>
        public static void NonNegative(this decimal number, string variableName)
        {
            if (number < 0m)
                throw new ArgumentOutOfRangeException(variableName, string.Concat(variableName, " should be non negative."));
        }
        /// <summary>
        /// 参数名称为 <paramref name="variableName"/> 的标识符不能是均为零的标识。
        /// </summary>
        public static void NotEmptyGuid(this Guid guid, string variableName)
        {
            if (Guid.Empty == guid)
                throw new ArgumentException(string.Concat(variableName, " shoud be non-empty GUID."), variableName);
        }
        
        /// <summary>
        /// 检查当前对象是否为 null
        /// </summary>
        public static bool IsNull(this object obj)
        {
            return obj == null || obj == DBNull.Value;
        }



        /// <summary>
        /// 如果当前的字符串不为空，则返回加前缀后的字符串
        /// </summary>
        public static string BeforeContact(this string str, string prefix)
        {
            return string.IsNullOrWhiteSpace(str) ? string.Empty : string.Concat(prefix, str);
        }

        /// <summary>
        /// 如果当前的字符串不为空，则返回加后缀后的字符串
        /// </summary>
        public static string AfterContact(this string str, string suffix)
        {
            return string.IsNullOrWhiteSpace(str) ? string.Empty : string.Concat(str, suffix);
        }


        /// <summary>
        /// 返回字符串真实长度, 1个汉字长度为2
        /// </summary>
        public static int TrueLength(this string str, string charset = "utf-8")
        {
            return str.TrueLength(Encoding.GetEncoding(charset));
        }
        /// <summary>
        /// 返回字符串真实长度
        /// </summary>
        public static int TrueLength(this string str, Encoding encoding)
        {
            return encoding.GetByteCount(str);
        }

        /// <summary>
        /// 取指定长度的字符串，超过部分替代
        /// </summary>
        /// <param name="str">原字符串</param>
        /// <param name="len">指定长度</param>
        /// <param name="tail">用于替换的字符串</param>
        public static string Cut(this string str, int len, string tail)
        {
            string result = string.Empty; // 最终返回的结果
            int byteLen = str.TrueLength(); // 单字节字符长度

            if (byteLen > len) {
                int charLen = str.Length; // 把字符平等对待时的字符串长度
                int byteCount = 0; // 记录读取进度
                int pos = 0; // 记录截取位置

                for (int i = 0; i < charLen; i++) {
                    if (Convert.ToInt32(str.ToCharArray()[i]) > 255) {// 按中文字符计算加2
                        byteCount += 2;
                    }
                    else {// 按英文字符计算加1
                        byteCount += 1;
                    }

                    if (byteCount > len) {// 超出时只记下上一个有效位置
                        pos = i;
                        break;
                    }
                    else if (byteCount == len) {// 记下当前位置
                        pos = i + 1;
                        break;
                    }
                }

                if (pos >= 0) {
                    result = string.Concat(str.Substring(0, pos), tail);
                }
            }
            else {
                result = str;
            }

            return result;
        }

        /// <summary>
        /// 分割字符串
        /// </summary>
        public static string[] Split(this string str, string split)
        {
            return (from piece in Regex.Split(str, Regex.Escape(split), RegexOptions.IgnoreCase)
                    let trimmed = piece.Trim()
                    where !string.IsNullOrEmpty(trimmed)
                    select trimmed).ToArray();
        }

        /// <summary>
        /// 判断指定字符串是否属于指定字符串数组中的一个元素
        /// </summary>
        /// <param name="str">要查找的字符串</param>
        /// <param name="array">字符串数组</param>
        /// <param name="caseInsensetive">是否不区分大小写, true为不区分, false为区分</param>
        public static bool InArray(this string str, string[] array, bool caseInsensetive = true)
        {
            return Array.Exists<string>(array, delegate(string element) {
                return string.Equals(element, str,
                        caseInsensetive ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
            });
        }

        /// <summary>
        /// 判断指定字符串在指定字符串数组中的位置
        /// </summary>
        /// <param name="str">要查找的字符串</param>
        /// <param name="array">字符串数组</param>
        /// <param name="caseInsensetive">是否不区分大小写, true为不区分, false为区分</param>
        public static int InArrayIndexOf(this string str, string[] array, bool caseInsensetive = true)
        {
            return Array.FindIndex<string>(array, delegate(string element) {
                return string.Equals(element, str,
                        caseInsensetive ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
            });
        }

        /// <summary>
        /// 判断指定字符串是否属于指定字符串数组中的一个元素
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="strarray">内部以逗号分割单词的字符串</param>
        /// <param name="strsplit">分割字符串</param>
        /// <param name="caseInsensetive">是否不区分大小写, true为不区分, false为区分</param>
        /// <returns>判断结果</returns>
        public static bool InArray(this string str, string strarray, string strsplit = ",", bool caseInsensetive = false)
        {
            return Array.Exists(Split(strarray, strsplit), delegate(string element) {
                return string.Equals(element, str,
                        caseInsensetive ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
            });
        }

        /// <summary>
        /// 判定字符串是不是数值型
        /// </summary>
        public static bool IsNumeric(this string str)
        {
            return Regex.IsMatch(str, @"^[-]?[0-9]*$");
        }

        /// <summary>
        /// 判断字符串是不是yyyy-mm-dd字符串
        /// </summary>
        public static bool IsDate(this string str)
        {
            return Regex.IsMatch(str, @"(\d{4})-(\d{1,2})-(\d{1,2})");
        }

        /// <summary>
        /// 判断字符串是不是时间格式
        /// </summary>
        public static bool IsTime(this string str)
        {
            return Regex.IsMatch(str, @"^((([0-1]?[0-9])|(2[0-3])):([0-5]?[0-9])(:[0-5]?[0-9])?)$");
        }

        /// <summary>
        /// 判断字符串是不是日期模式
        /// </summary>
        public static bool IsDateTime(this string str)
        {
            return Regex.IsMatch(str, @"(\d{4})-(\d{1,2})-(\d{1,2}) ^((([0-1]?[0-9])|(2[0-3])):([0-5]?[0-9])(:[0-5]?[0-9])?)$");
        }

        /// <summary>
        /// 判断字符串是不是小数类型
        /// </summary>
        public static bool IsDecimal(this string str)
        {
            return Regex.IsMatch(str, @"^[-]?[0-9]*[.]?[0-9]*$");
        }

        /// <summary>
        /// 检测是否符合email格式
        /// </summary>
        public static bool IsEmail(this string str)
        {
            return Regex.IsMatch(str, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
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
        public static object Change(this string str, Type targetType)
        {
            str.NotNullOrEmpty("str");

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
        /// 将 <param name="str" /> 转换为 <param name="targetType" /> 的值。如果转换失败则使用 <param name="defaultValue" /> 的值。
        /// </summary>
        public static object ChangeIfError(this string str, Type targetType, object defaultValue)
        {
            if (string.IsNullOrEmpty(str))
                return defaultValue;

            try {
                return str.Change(targetType);
            }
            catch (Exception) {
                return defaultValue;
            }
        }

        /// <summary>
        /// 将 <param name="str" /> 转换为 <param name="targetType" /> 的值。一个指示转换是否成功的返回值 <param name="result" />。
        /// </summary>
        public static bool TryChange(this string str, Type targetType, out object result)
        {
            try {
                result = str.Change(targetType);
                return true;
            }
            catch (Exception) {
                result = targetType.GetDefaultValue();
                return false;
            }
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
        /// 将 <param name="str" /> 转换为 <typeparam name="T" /> 的值。如果转换失败则使用 <param name="defaultValue" /> 的值。
        /// </summary>
        public static T ChangeIfError<T>(this string str, T defaultValue)
            where T : struct
        {
            T result;
            if (str.TryChange<T>(out result)) {
                return result;
            }

            return defaultValue;
        }

        /// <summary>
        /// 将 <param name="str" /> 转换为 <typeparam name="T" /> 的值。一个指示转换是否成功的返回值 <param name="result" />。
        /// </summary>
        public static bool TryChange<T>(this string str, out T result)
            where T : struct
        {
            if(string.IsNullOrEmpty(str)) {
                result = default(T);
                return false;
            }

            try {
                result = str.Change<T>();
                return true;
            }
            catch (Exception) {
                result = default(T);
                return false;
            }
        }

        /// <summary>
        /// 克隆一个 <typeparamref name="T"/> 的副本。
        /// </summary>
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
    }
}
