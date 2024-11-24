using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Common.ExtensionMethods
{
    public static class ExceptionExtensions
    {
        public static void ThrowIfNull<T>(this T param, string exceptionMessage = null, [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (param == null)
            {
                throw new ArgumentNullException(paramName, exceptionMessage);
            }
        }

        public static T ThrowIfNullOrReturn<T>(this T param, string exceptionMessage = null, [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            param.ThrowIfNull(exceptionMessage, paramName);
            return param;
        }

        public static void ThrowIfDefault<T>(this T param, string exceptionMessage = null, [CallerArgumentExpression(nameof(param))] string paramName = null)
            where T : struct
        {
            if (param.Equals(default(T)))
            {
                throw new ArgumentNullException(paramName, exceptionMessage);
            }
        }

        public static T ThrowIfDefaultOrReturn<T>(this T param, string exceptionMessage = null, [CallerArgumentExpression(nameof(param))] string paramName = null)
            where T : struct
        {
            param.ThrowIfDefault(exceptionMessage, paramName);
            return param;
        }

        public static void ThrowIfNullOrEmpty(this string param, string exceptionMessage = null, [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (string.IsNullOrEmpty(param))
            {
                throw new ArgumentNullException(paramName, exceptionMessage ?? $"{paramName} is null or empty");
            }
        }

        public static string ThrowIfNullOrEmptyOrReturn(this string param, string exceptionMessage = null, [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            param.ThrowIfNullOrEmpty(exceptionMessage, paramName);
            return param;
        }

        public static void ThrowIfNullOrWhiteSpace(this string param, string exceptionMessage = null, [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (string.IsNullOrWhiteSpace(param))
            {
                throw new ArgumentNullException(paramName, exceptionMessage ?? $"{paramName} is null or empty");
            }
        }

        public static string ThrowIfNullOrWhiteSpaceOrReturn(this string param, string exceptionMessage = null, [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            param.ThrowIfNullOrWhiteSpace(exceptionMessage, paramName);
            return param;
        }

        public static void ThrowIfNullOrEmpty<T>(this IList<T> param, string exceptionMessage = null, [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (param == null || param.Count == 0)
            {
                throw new ArgumentNullException(paramName, exceptionMessage ?? $"{paramName} is null or empty");
            }
        }

        public static IList<T> ThrowIfNullOrEmptyOrReturn<T>(this IList<T> param, string exceptionMessage = null, [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            param.ThrowIfNullOrEmpty(exceptionMessage, paramName);
            return param;
        }

        public static void ThrowIfFalse<T>(
            this T param,
            Func<T, bool> predicate,
            string exceptionMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null,
            [CallerArgumentExpression(nameof(predicate))] string predicateName = null)
        {
            if (!predicate(param))
            {
                throw new Exception(exceptionMessage ?? $"{predicateName} is not true for {paramName}");
            }
        }

        public static T ThrowIfFalseOrReturn<T>(
            this T param,
            Func<T, bool> predicate,
            string exceptionMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null,
            [CallerArgumentExpression(nameof(predicate))] string predicateName = null)
        {
            param.ThrowIfFalse(predicate, exceptionMessage, paramName, predicateName);
            return param;
        }

        public static void ThrowIfTrue<T>(
            this T param,
            Func<T, bool> predicate,
            string exceptionMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null,
            [CallerArgumentExpression(nameof(predicate))] string predicateName = null)
        {
            if (predicate(param))
            {
                throw new Exception(exceptionMessage ?? $"{predicateName} is not false for {paramName}");
            }
        }

        public static T ThrowIfTrueOrReturn<T>(
            this T param,
            Func<T, bool> predicate,
            string exceptionMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null,
            [CallerArgumentExpression(nameof(predicate))] string predicateName = null)
        {
            param.ThrowIfTrue(predicate, exceptionMessage, paramName, predicateName);
            return param;
        }
    }
}
