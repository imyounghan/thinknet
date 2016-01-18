using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using ThinkLib.Common;

namespace ThinkNet.Database
{
    /// <summary>
    /// 排序表达式实现
    /// </summary>
    public class SortSet<T> : ISortSet<T>
        where T : class
    {
        readonly List<ISortItem<T>> sorts = new List<ISortItem<T>>();

        /// <summary>
        /// 空的排序
        /// </summary>
        public readonly static SortSet<T> Empty = new SortSet<T>();

        private SortSet()
        { }

        private SortSet<T> Add(ISortItem<T> order)
        {
            sorts.Add(order);

            return this;
        }

        /// <summary>
        /// 升序
        /// </summary>
        /// <param name="expression"></param>
        public static SortSet<T> OrderBy(Expression<Func<T, dynamic>> expression)
        {
            Ensure.NotNull(expression, "expression");

            return new SortSet<T>().Add(new SortItem(expression, SortOrder.Ascending));
        }
        /// <summary>
        /// 降序
        /// </summary>
        /// <param name="expression">排序列</param>
        public static SortSet<T> OrderByDescending(Expression<Func<T, dynamic>> expression)
        {
            Ensure.NotNull(expression, "expression");

            return new SortSet<T>().Add(new SortItem(expression, SortOrder.Descending));
        }

        /// <summary>
        /// 升序
        /// </summary>
        /// <param name="expression"></param>
        public SortSet<T> ThenBy(Expression<Func<T, dynamic>> expression)
        {
            Ensure.NotNull(expression, "expression");

            return this.Add(new SortItem(expression, SortOrder.Ascending));
        }
        /// <summary>
        /// 降序
        /// </summary>
        /// <param name="expression">排序列</param>
        public SortSet<T> ThenByDescending(Expression<Func<T, dynamic>> expression)
        {
            Ensure.NotNull(expression, "expression");

            return this.Add(new SortItem(expression, SortOrder.Descending));
        }

        #region ISortSet<T> 成员

        IEnumerable<ISortItem<T>> ISortSet<T>.OrderItems
        {
            get { return sorts; }
        }


        IQueryable<T> ISortSet<T>.Arranged(IQueryable<T> source)
        {
            if (!sorts.Any()) {
                return source;
            }

            string methodAsc = "OrderBy";
            string methodDesc = "OrderByDescending";
            Expression queryExpr = source.Expression;
            foreach (var sort in sorts) {
                MemberExpression selector = (sort.Expression as LambdaExpression).Body.RemoveConvert() as MemberExpression;
                if (selector == null) {
                    throw new InvalidOperationException("不支持的排序类型。");
                }
                Type resultType = selector.Type;

                Expression exp = Expression.Quote(Expression.Lambda(selector, selector.Parameter()));
                if (resultType.IsValueType || resultType == typeof(string)) {
                    queryExpr = Expression.Call(
                    typeof(Queryable), sort.SortOrder == SortOrder.Ascending ? methodAsc : methodDesc,
                    new Type[] { source.ElementType, resultType },
                    queryExpr, exp);
                    methodAsc = "ThenBy";
                    methodDesc = "ThenByDescending";
                }
                else {
                    throw new InvalidOperationException(string.Format("不支持的排序类型：{0}", resultType.FullName));
                }

            }
            return source.Provider.CreateQuery<T>(queryExpr);
            //IOrderedQueryable<T> orderenumerable = null;

            //ISortItem<T> first = orders.First();
            //switch (first.SortOrder) {
            //    case SortOrder.Ascending:
            //        orderenumerable = enumerable.OrderBy(first.Expression);
            //        break;
            //    case SortOrder.Descending:
            //        orderenumerable = enumerable.OrderByDescending(first.Expression);
            //        break;
            //}


            //foreach (ISortItem<T> sort in orders.Skip(1)) {
            //    switch (sort.SortOrder) {
            //        case SortOrder.Ascending:
            //            orderenumerable = orderenumerable.ThenBy(sort.Expression);
            //            break;
            //        case SortOrder.Descending:
            //            orderenumerable = orderenumerable.ThenByDescending(sort.Expression);
            //            break;
            //    }
            //}

            //return orderenumerable;
        }

        #endregion


        internal class SortItem : ISortItem<T>
        {
            Expression<Func<T, dynamic>> sortPredicate = null;
            SortOrder sortOrder = SortOrder.Unspecified;
            public SortItem(Expression<Func<T, dynamic>> sortPredicate, SortOrder sortOrder)
            {
                this.sortPredicate = sortPredicate;
                this.sortOrder = sortOrder;
            }


            Expression<Func<T, dynamic>> ISortItem<T>.Expression
            {
                get { return sortPredicate; }
            }

            SortOrder ISortItem<T>.SortOrder
            {
                get { return sortOrder; }
            }
        }
    }
}
