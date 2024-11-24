using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Common.ExtensionMethods;

namespace Common.Utility
{
    public class ExpressionUtility
    {
        private static readonly ConcurrentDictionary<string, object> GetterToSetterExpressionCache = new ConcurrentDictionary<string, object>();

        public static object GetMemberExpressionParentValue(MemberExpression memberExpression)
        {
            MemberExpression prevExpression = null;

            while (memberExpression.Expression.NodeType != ExpressionType.Constant)
            {
                prevExpression = memberExpression;
                memberExpression = (MemberExpression)memberExpression.Expression;
            }

            var value = ((ConstantExpression)memberExpression.Expression).Value;

            // we're dealing with a closure, need to go back a step to get the FieldInfo, and then resolve its value with the closure
            if (value.GetType().IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                value = ((prevExpression.Expression as MemberExpression).Member as FieldInfo).GetValue(value);
            }

            return value;
        }

        public static object GetMemberExpressionValue(MemberExpression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        public static Expression<Func<T, bool>> CombineWithAnd<T>(params Expression<Func<T, bool>>[] expressions)
        {
            Expression<Func<T, bool>> combined = null;

            foreach (var expression in expressions)
            {
                combined = CombineWithAndInternal(combined, expression);
            }

            return combined;
        }

        public static Expression<Func<T, bool>> CombineWithOr<T>(params Expression<Func<T, bool>>[] expressions)
        {
            Expression<Func<T, bool>> combined = null;

            foreach (var expression in expressions)
            {
                combined = CombineWithOrInternal(combined, expression);
            }

            return combined;
        }

        // from https://stackoverflow.com/questions/7723744/expressionfunctmodel-string-to-expressionactiontmodel-getter-to-sette
        // and then a modified version from https://chat.stackoverflow.com/rooms/4169/discussion-between-mvision-and-xanatos
        public static Action<T, TProperty> GetterToSetter<T, TProperty>(Expression<Func<T, TProperty>> getter)
        {
            var cacheKey = $"{typeof(T).Name}-{getter}";

            if (GetterToSetterExpressionCache.TryGetValue(cacheKey, out var objExpr))
            {
                return (Action<T, TProperty>)objExpr;
            }

            var member = GetMemberExpression(getter);

            MethodInfo setter;

            ParameterExpression arg = Expression.Parameter(typeof(T), "x");
            ParameterExpression valArg = Expression.Parameter(typeof(TProperty), "val");

            Expression expr = arg;

            for (int i = member.Count - 1; i > 0; i--)
            {
                PropertyInfo pi = (PropertyInfo)member[i].Member;
                expr = Expression.Property(expr, pi);
            }

            PropertyInfo finalProp = (PropertyInfo)member[0].Member;
            setter = finalProp.GetSetMethod(true);

            // maybe T was an interface which only exposed a getter - try to resolve the property directly from the base type instead
            if (setter == null)
            {
                var instanceProp = typeof(T).GetProperty(finalProp.Name, BindingFlags.Instance | BindingFlags.Public)
                    ?? typeof(T).GetProperty(finalProp.Name, BindingFlags.Instance | BindingFlags.NonPublic);

                if (instanceProp == null || (setter = instanceProp.GetSetMethod(true)) == null)
                {
                    throw new InvalidOperationException($"Could not access set method for property {finalProp.Name} on type {typeof(T).Name}");
                }

                setter ??= finalProp.GetSetMethod(true);
            }

            Expression valArgCasted = Expression.Convert(valArg, setter.GetParameters()[0].ParameterType);

            expr = Expression.Call(expr, setter, valArgCasted);

            var compiledSetter = Expression.Lambda<Action<T, TProperty>>(expr, arg, valArg).Compile();

            GetterToSetterExpressionCache.AddOrUpdate(cacheKey, compiledSetter, (_, _) => compiledSetter);

            return compiledSetter;
        }

        protected static IList<MemberExpression> GetMemberExpression<T, TProperty>(Expression<Func<T, TProperty>> expression)
        {
            MemberExpression current;

            if (expression.Body.NodeType == ExpressionType.Convert)
            {
                var body = (UnaryExpression)expression.Body;
                current = body.Operand as MemberExpression;
            }
            else if (expression.Body.NodeType == ExpressionType.MemberAccess)
            {
                current = expression.Body as MemberExpression;
            }
            else
            {
                throw new ArgumentException("Not a member access", "expression");
            }

            List<MemberExpression> memberExpression = new List<MemberExpression>();

            while (current != null)
            {
                memberExpression.Add(current);
                current = current.Expression as MemberExpression;
            }

            return memberExpression.AsReadOnly();
        }

        private static Expression<Func<T, bool>> CombineWithAndInternal<T>(Expression<Func<T, bool>> a, Expression<Func<T, bool>> b)
        {
            if (a == null && b == null)
            {
                return null;
            }

            if (a != null && b == null)
            {
                return a;
            }

            if (b != null && a == null)
            {
                return b;
            }

            var p = a.Parameters[0];

            var visitor = new SubstExpressionVisitor();
            visitor.Subst[b.Parameters[0]] = p;

            var body = Expression.AndAlso(a.Body, visitor.Visit(b.Body));

            return Expression.Lambda<Func<T, bool>>(body, p);
        }

        private static Expression<Func<T, bool>> CombineWithOrInternal<T>(Expression<Func<T, bool>> a, Expression<Func<T, bool>> b)
        {
            if (a == null && b == null)
            {
                return null;
            }

            if (a != null && b == null)
            {
                return a;
            }

            if (b != null && a == null)
            {
                return b;
            }

            var p = a.Parameters[0];

            var visitor = new SubstExpressionVisitor();
            visitor.Subst[b.Parameters[0]] = p;

            var body = Expression.OrElse(a.Body, visitor.Visit(b.Body));

            return Expression.Lambda<Func<T, bool>>(body, p);
        }

        internal class SubstExpressionVisitor : ExpressionVisitor
        {
            internal Dictionary<Expression, Expression> Subst { get; } = new Dictionary<Expression, Expression>();

            protected override Expression VisitParameter(ParameterExpression node)
            {
                Expression newValue;

                if (this.Subst.TryGetValue(node, out newValue))
                {
                    return newValue;
                }

                return node;
            }
        }
    }
}
