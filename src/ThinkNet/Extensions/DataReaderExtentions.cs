using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;


namespace ThinkNet
{
    /// <summary>
    /// 对 <see cref="IDataReader"/> 的扩展
    /// </summary>
    public static class DataReaderExtentions
    {
        private static readonly IDictionary emptyDict = new Hashtable();

        /// <summary>
        /// 转成字典数据
        /// </summary>
        public static IDictionary ToDictionary(this IDataReader reader)
        {
            return reader.ToDictionary(false);
        }
        /// <summary>
        /// 转成字典数据
        /// </summary>
        public static IDictionary ToDictionary(this IDataReader reader, bool closedReader)
        {
            IDictionary dict;

            if (reader.Read()) {
                dict = DataReaderToDictionary(reader);
            }
            else {
                dict = emptyDict;
            }

            if (closedReader) reader.Close();

            return dict;
        }

        private static IDictionary DataReaderToDictionary(IDataReader reader)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);

            for (int i = 0; i < reader.FieldCount; i++) {
                dict.Add(reader.GetName(i), reader.GetValue(i));
            }

            return dict;
        }

        /// <summary>
        /// 转成字典数据集合
        /// </summary>
        public static ICollection ToCollection(this IDataReader reader)
        {
            return reader.ToCollection(false);
        }
        /// <summary>
        /// 转成字典数据集合
        /// </summary>
        public static ICollection ToCollection(this IDataReader reader, bool closedReader)
        {
            ArrayList list = new ArrayList();

            while (reader.Read()) {
                var row = DataReaderToDictionary(reader);
                list.Add(row);
            }

            if (closedReader) reader.Close();

            return list;
        }

        
        /// <summary>
        /// 转成实体集合
        /// </summary>
        public static IList<T> ToList<T>(this IDataReader reader)
            where T : class
        {
            return reader.ToList<T>(false);
        }
        /// <summary>
        /// 转成实体集合
        /// </summary>
        public static IList<T> ToList<T>(this IDataReader reader, IDictionary map)
            where T : class
        {
            return reader.ToList<T>(false, map);
        }
        /// <summary>
        /// 转成实体集合
        /// </summary>
        public static IList<T> ToList<T>(this IDataReader reader, bool closedReader)
            where T : class
        {
            return reader.ToList<T>(closedReader, null);
        }
        /// <summary>
        /// 转成实体集合
        /// </summary>
        public static IList<T> ToList<T>(this IDataReader reader, bool closedReader, IDictionary map)
            where T : class
        {
            List<T> list = new List<T>();

            while (reader.Read()) {
                var row = DataReaderToDictionary(reader).MapTo<T>(map);
                list.Add(row);
            }

            if (closedReader)
                reader.Close();

            return list;
        }
    }
}
