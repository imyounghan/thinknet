using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;


namespace ThinkLib
{
    /// <summary>
    /// Represents the parameter rebinder used for rebinding the parameters for the given expressions. For more information about this solution please refer to http://blogs.msdn.com/b/meek/archive/2008/05/02/linq-to-entities-combining-predicates.aspx.
    /// </summary>
    internal class ParameterRebinder : ExpressionVisitor
    {
        #region Private Fields
        private readonly Dictionary<ParameterExpression, ParameterExpression> map;
        #endregion

        #region Ctor
        internal ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
        {
            this.map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
        }
        #endregion

        #region Internal Static Methods
        internal static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
        {
            return new ParameterRebinder(map).Visit(exp);
        }
        #endregion

        #region Protected Methods
        protected override Expression VisitParameter(ParameterExpression p)
        {
            ParameterExpression replacement;
            if (map.TryGetValue(p, out replacement)) {
                p = replacement;
            }
            return base.VisitParameter(p);
        }
        #endregion
    }

    /// <summary>
    /// Represents the extender for Expression[Func[T, bool]] type. For more information about this solution please refer to http://blogs.msdn.com/b/meek/archive/2008/05/02/linq-to-entities-combining-predicates.aspx.
    /// </summary>
    public static class ExpressionFuncExtender
    {
        private static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            // build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);

            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression 
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        #region Public Methods
        /// <summary>
        /// Combines two given expressions by using the AND semantics.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="first">The first part of the expression.</param>
        /// <param name="second">The second part of the expression.</param>
        /// <returns>The combined expression.</returns>
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.And);

            //var body = Expression.And(first.Body, second.Body);
            //return Expression.Lambda<Func<T, bool>>(body, first.Parameters);
        }

        /// <summary>
        /// Combines two given expressions by using the AND semantics.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="first">The first part of the expression.</param>
        /// <param name="second">The second part of the expression.</param>
        /// <returns>The combined expression.</returns>
        public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {

            return first.Compose(second, Expression.AndAlso);

            //var body = Expression.AndAlso(first.Body, second.Body);
            //return Expression.Lambda<Func<T, bool>>(body, first.Parameters);

        }

        /// <summary>
        /// Combines two given expressions by using the OR semantics.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="first">The first part of the expression.</param>
        /// <param name="second">The second part of the expression.</param>
        /// <returns>The combined expression.</returns>
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.Or);

            //var body = Expression.OrElse(first.Body, second.Body);
            //return Expression.Lambda<Func<T, bool>>(body, first.Parameters);

        }

        /// <summary>
        /// Represents the specification which indicates the semantics opposite to the given specification.
        /// </summary>
        public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expression)
        {
            return Expression.Lambda<Func<T, bool>>(
                Expression.Not(expression.Body),
                expression.Parameters.Single()
            );
//#if NET35
//            var body = Expression.Not(expression.Body);
//            return Expression.Lambda<Func<T, bool>>(body, expression.Parameters);
//#endif
        }
        #endregion

        /// <summary></summary>
        public static Expression RemoveConvert(this Expression expression)
        {
            while (expression != null && (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked)) {
                expression = ((UnaryExpression)expression).Operand.RemoveConvert();
            }
            return expression;
        }

        /// <summary></summary>
        public static ParameterExpression Parameter(this MemberExpression expression)
        {
            ParameterExpression parameter = expression.Expression as ParameterExpression;
            if (parameter != null) {
                return parameter;
            }
            MemberExpression member = expression.Expression as MemberExpression;
            if (member == null) {
                throw new InvalidOperationException("不支持的排序类型。");
            }
            return Parameter(member);
        }
    }
}
