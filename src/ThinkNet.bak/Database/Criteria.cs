using System;
using System.Linq;
using System.Linq.Expressions;

namespace ThinkNet.Database
{
    /// <summary>
    /// 查询表达式
    /// </summary>
    public class Criteria<T> : ICriteria<T>
        where T : class
    {
        /// <summary>
        /// 表达式运算
        /// </summary>
        public static Criteria<T> Eval(Expression<Func<T, bool>> expression)
        {
            expression.NotNull("expression");

            return new Criteria<T>(expression);
        }

        /// <summary>
        /// 空的查询表达式
        /// </summary>
        public readonly static Criteria<T> Empty = new Criteria<T>();


        private Expression<Func<T, bool>> expressions = null;

        private Criteria()
        { }

        private Criteria(Expression<Func<T, bool>> expression)
        {
            this.expressions = expression;
        }

        /// <summary>
        /// And
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public Criteria<T> And(Expression<Func<T, bool>> expression)
        {
            expression.NotNull("expression");

            expressions = expressions.And(expression);

            return this;
        }
        ///// <summary>
        ///// And
        ///// </summary>
        ///// <param name="expression"></param>
        ///// <returns></returns>
        //public Criteria<T> AndAlso(Expression<Func<T, bool>> expression)
        //{
        //    if (expression == null) {
        //        throw new ArgumentNullException("expression");
        //    }

        //    expressions = expressions.AndAlso(expression);

        //    return this;
        //}
        /// <summary>
        /// 否定And
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public Criteria<T> AndNot(Expression<Func<T, bool>> expression)
        {
            expression.NotNull("expression");

            expressions = expressions.And(expression.Not());

            return this;
        }
        /// <summary>
        /// Or
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public Criteria<T> Or(Expression<Func<T, bool>> expression)
        {
            expression.NotNull("expression");

            expressions = expressions.Or(expression);

            return this;
        }

        /// <summary>
        /// 否定查询
        /// </summary>
        /// <returns></returns>
        public Criteria<T> Not()
        {
            if (expressions != null) {
                expressions = expressions.Not();
            }

            return this;
        }

        #region Criteria<T> 成员

        Expression<Func<T, bool>> ICriteria<T>.Expression
        {
            get { return expressions; }
        }

        IQueryable<T> ICriteria<T>.Filtered(IQueryable<T> enumerable)
        {
            if (expressions == null)
                return enumerable;

            return enumerable.Where(expressions);
        }

        #endregion
    }
}
