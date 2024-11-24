using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common.Models;

namespace Common.ExtensionMethods
{
    public static class ValidationExtensions
    {
        private static readonly Regex EmailRegex = new Regex(
            pattern: @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            options: RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #region OperationResult extensions

        /// <summary>
        /// Allows chaining a follow-on action if validation fails for chained validations where halfOnFailure == false.
        /// </summary>
        public static OperationResult IfValidationFailed(this OperationResult result, Action<string> action)
        {
            action.ThrowIfNull();

            if (result.LastValidationError != null)
            {
                action(result.LastValidationError);
            }

            return result;
        }

        /// <summary>
        /// Overload for validation func with no arguments.
        /// </summary>
        public static OperationResult Validate<TParam>(
            this OperationResult result,
            TParam param,
            Func<OperationResult, TParam, string, string, bool> validationFunc,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            return ValidateInternal(
                result,
                () => validationFunc(result, param, customErrorMessage, paramName),
                haltOnFailure);
        }

        /// <summary>
        /// Overload for validation funcs IsTrue/IsFalse.
        /// </summary>
        public static OperationResult Validate<TParam>(
            this OperationResult result,
            TParam param,
            Func<OperationResult, TParam, Func<TParam, bool>, string, string, bool> validationFunc,
            Func<TParam, bool> predicate,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(predicate))] string predicateName = null)
        {
            return ValidateInternal(
                result,
                () => validationFunc(result, param, predicate, customErrorMessage, predicateName),
                haltOnFailure);
        }

        /// <summary>
        /// Overload for validation func ListContains.
        /// </summary>
        public static OperationResult Validate<TParam>(
            this OperationResult result,
            TParam param,
            Func<OperationResult, IList<TParam>, TParam, string, string, bool> validationFunc,
            IList<TParam> list,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            return ValidateInternal(
                result,
                () => validationFunc(result, list, param, customErrorMessage, paramName),
                haltOnFailure);
        }

        /// <summary>
        /// Overload for validation func with 1 argument.
        /// </summary>
        public static OperationResult Validate<TParam, TValidationArg1>(
            this OperationResult result,
            TParam param,
            Func<OperationResult, TParam, TValidationArg1, string, string, bool> validationFunc,
            TValidationArg1 validationArg1,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            return ValidateInternal(
                result,
                () => validationFunc(result, param, validationArg1, customErrorMessage, paramName),
                haltOnFailure);
        }

        /// <summary>
        /// Overload for validation func with 2 arguments.
        /// </summary>
        public static OperationResult Validate<TParam, TValidationArg1, TValidationArg2>(
            this OperationResult result,
            TParam param,
            Func<OperationResult, TParam, TValidationArg1, TValidationArg2, string, string, bool> validationFunc,
            TValidationArg1 validationArg1,
            TValidationArg2 validationArg2,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            return ValidateInternal(
                result,
                () => validationFunc(result, param, validationArg1, validationArg2, customErrorMessage, paramName),
                haltOnFailure);
        }

        /// <summary>
        /// Overload for validation func to check list item existence and return data.
        /// </summary>
        public static OperationResult Validate<T>(
            this OperationResult result,
            IList<T> list,
            Func<OperationResult, IList<T>, Func<T, bool>, Action<T>, string, string, bool> validationFunc,
            Func<T, bool> predicate,
            out T item,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(list))] string paramName = null)
        {
            T tempItem = default;

            var validationResult = ValidateInternal(
                result,
                () => validationFunc(result, list, predicate, x => tempItem = x, customErrorMessage, paramName),
                haltOnFailure);

            item = tempItem;

            return validationResult;
        }

        /// <summary>
        /// Overload for validation func to check dictionary item existence and return data.
        /// </summary>
        public static OperationResult Validate<TKey, TValue>(
            this OperationResult result,
            IDictionary<TKey, TValue> dictionary,
            Func<OperationResult, IDictionary<TKey, TValue>, TKey, Action<TValue>, string, string, bool> validationFunc,
            TKey key,
            out TValue item,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(dictionary))] string paramName = null)
        {
            TValue tempItem = default;

            var validationResult = ValidateInternal(
                result,
                () => validationFunc(result, dictionary, key, x => tempItem = x, customErrorMessage, paramName),
                haltOnFailure);

            item = tempItem;

            return validationResult;
        }

        public static OperationResult ValidateDataAnnotations(
            this OperationResult result,
            object objectToBeValidated)
        {
            var context = new ValidationContext(objectToBeValidated, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(
                objectToBeValidated,
                context,
                validationResults,
                validateAllProperties: true))
            {
                result.AddErrors(validationResults.Select(x => x.ErrorMessage).ToArray());
            }

            return result;
        }

        #endregion

        #region OperationResult<TResult> extensions

        /// <summary>
        /// Overload for validation func with no arguments.
        /// </summary>
        public static OperationResult<TResult> Validate<TResult, TParam>(
            this OperationResult<TResult> result,
            TParam param,
            Func<OperationResult, TParam, string, string, bool> validationFunc,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            return (OperationResult<TResult>)((OperationResult)result).Validate(param, validationFunc, customErrorMessage, haltOnFailure, paramName);
        }

        /// <summary>
        /// Overload for validation funcs IsTrue/IsFalse.
        /// </summary>
        public static OperationResult<TResult> Validate<TResult, TParam>(
            this OperationResult<TResult> result,
            TParam param,
            Func<OperationResult, TParam, Func<TParam, bool>, string, string, bool> validationFunc,
            Func<TParam, bool> predicate,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(predicate))] string predicateName = null)
        {
            return (OperationResult<TResult>)((OperationResult)result).Validate(param, validationFunc, predicate, customErrorMessage, haltOnFailure, predicateName);
        }

        /// <summary>
        /// Overload for validation func ListContains.
        /// </summary>
        public static OperationResult<TResult> Validate<TResult, TParam>(
            this OperationResult result,
            TParam param,
            Func<OperationResult, IList<TParam>, TParam, string, string, bool> validationFunc,
            IList<TParam> list,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            return (OperationResult<TResult>)((OperationResult)result).Validate(param, validationFunc, list, customErrorMessage, haltOnFailure, paramName);
        }

        /// <summary>
        /// Overload for validation func with 1 argument.
        /// </summary>
        public static OperationResult<TResult> Validate<TResult, TParam, TValidationArg1>(
            this OperationResult<TResult> result,
            TParam param,
            Func<OperationResult, TParam, TValidationArg1, string, string, bool> validationFunc,
            TValidationArg1 validationArg1,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            return (OperationResult<TResult>)((OperationResult)result).Validate(param, validationFunc, validationArg1, customErrorMessage, haltOnFailure, paramName);
        }

        /// <summary>
        /// Overload for validation func with 2 arguments.
        /// </summary>
        public static OperationResult<TResult> Validate<TResult, TParam, TValidationArg1, TValidationArg2>(
            this OperationResult<TResult> result,
            TParam param,
            Func<OperationResult, TParam, TValidationArg1, TValidationArg2, string, string, bool> validationFunc,
            TValidationArg1 validationArg1,
            TValidationArg2 validationArg2,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            return (OperationResult<TResult>)((OperationResult)result).Validate(param, validationFunc, validationArg1, validationArg2, customErrorMessage, haltOnFailure, paramName);
        }

        /// <summary>
        /// Overload for validation func to check list item existence and return data.
        /// </summary>
        public static OperationResult<TResult> Validate<TResult, T>(
            this OperationResult<TResult> result,
            IList<T> list,
            Func<OperationResult, IList<T>, Func<T, bool>, Action<T>, string, string, bool> validationFunc,
            Func<T, bool> predicate,
            out T item,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(list))] string paramName = null)
        {
            return (OperationResult<TResult>)((OperationResult)result).Validate(list, validationFunc, predicate, out item, customErrorMessage, haltOnFailure, paramName);
        }

        /// <summary>
        /// Overload for validation func to check dictionary item existence and return data.
        /// </summary>
        public static OperationResult<TResult> Validate<TResult, TKey, TValue>(
            this OperationResult<TResult> result,
            IDictionary<TKey, TValue> dictionary,
            Func<OperationResult, IDictionary<TKey, TValue>, TKey, Action<TValue>, string, string, bool> validationFunc,
            TKey key,
            out TValue item,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(dictionary))] string paramName = null)
        {
            return (OperationResult<TResult>)((OperationResult)result).Validate(dictionary, validationFunc, key, out item, customErrorMessage, haltOnFailure, paramName);
        }

        public static OperationResult<TResult> ValidateDataAnnotations<TResult>(
            this OperationResult<TResult> result,
            object objectToBeValidated)
        {
            return (OperationResult<TResult>)((OperationResult)result).ValidateDataAnnotations(objectToBeValidated);
        }

        #endregion

        #region Validation functions

        public static bool IsNotNull<T>(
            this OperationResult result,
            T param,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (param == null)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} cannot be null", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool IsNull<T>(
            this OperationResult result,
            T param,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (param != null)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} should be null", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool Exists<T>(
            this OperationResult result,
            T param,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (param == null)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} does not exist", OperationResultErrorType.EntityNotFoundOrUnauthorized);
                return false;
            }

            return true;
        }

        public static bool IsValidEmailAddress(
            this OperationResult result,
            string param,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (string.IsNullOrWhiteSpace(param) || !EmailRegex.IsMatch(param))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} is not a valid email address", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool StringIsNotNullOrWhiteSpace(
            this OperationResult result,
            string param,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (string.IsNullOrWhiteSpace(param))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} cannot be null or white space", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool StringContains(
            this OperationResult result,
            string param,
            string contains,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (!param.Contains(contains, StringComparison.OrdinalIgnoreCase))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} does not contain '{contains}'", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool StringDoesNotContain(
            this OperationResult result,
            string param,
            string contains,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (param.Contains(contains, StringComparison.OrdinalIgnoreCase))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} contains '{contains}'", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool StringStartsWith(
            this OperationResult result,
            string param,
            string startsWith,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (!param.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} does not start with '{startsWith}'", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool StringEndsWith(
            this OperationResult result,
            string param,
            string endsWith,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (!param.EndsWith(endsWith, StringComparison.OrdinalIgnoreCase))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} does not end with '{endsWith}'", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool ListIsNotNullOrEmpty<T>(
            this OperationResult result,
            IList<T> param,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (param == null || param.Count == 0)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} cannot be null or empty", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool ListIsEmpty<T>(
            this OperationResult result,
            IList<T> param,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (param == null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (param.Count > 0)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} should be empty", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool ListContains<T>(
            this OperationResult result,
            IList<T> param,
            T value,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            param.ThrowIfNull(paramName: paramName);

            if (!param.Contains(value))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} does not contain expected value {value}", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool ListDoesNotContain<T>(
            this OperationResult result,
            IList<T> param,
            T value,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            param.ThrowIfNull(paramName: paramName);

            if (param.Contains(value))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} contains value {value}", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool ListEquals<T>(
            this OperationResult result,
            IList<T> param,
            IList<T> otherList,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            param.ThrowIfNull(paramName: paramName);

            if (!param.ScrambledEquals(otherList))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} values {param.StringJoin(", ")} do not equal expected values {otherList.StringJoin(", ")}", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool ListIntersects<T>(
            this OperationResult result,
            IList<T> param,
            IList<T> otherList,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            param.ThrowIfNull(paramName: paramName);

            var otherListMembersNotInMainList = otherList.Except(param).ToList();

            if (otherListMembersNotInMainList.Count > 0)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} does not contain values {otherListMembersNotInMainList.StringJoin(", ")}", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool ListDoesNotIntersect<T>(
            this OperationResult result,
            IList<T> param,
            IList<T> otherList,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            param.ThrowIfNull(paramName: paramName);

            var intersection = param.Intersect(otherList).ToList();

            if (intersection.Count > 0)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} already contains values {intersection.StringJoin(", ")}", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool ListContainsAndFetch<T>(
            this OperationResult result,
            IList<T> param,
            Func<T, bool> predicate,
            Action<T> listItemSetter,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            param.ThrowIfNull(paramName: paramName);

            var item = param.SingleOrDefault(predicate);

            listItemSetter(item);

            if (item == null)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} does not contain expected value {predicate}", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool DictionaryContainsAndFetch<TKey, TValue>(
            this OperationResult result,
            IDictionary<TKey, TValue> param,
            TKey key,
            Action<TValue> dictionaryItemSetter,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            param.ThrowIfNull(paramName: paramName);

            if (!param.TryGetValue(key, out var value))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} does not contain expected key {key}", OperationResultErrorType.Validation);
                return false;
            }

            dictionaryItemSetter(value);

            return true;
        }

        public static bool IsTrue<T>(
            this OperationResult result,
            T param,
            Func<T, bool> predicate,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(predicate))] string predicateName = null)
        {
            if (!predicate(param))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{predicateName} must be true", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool IsTrue(
            this OperationResult result,
            bool param,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (!param)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} must be true", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool IsFalse<T>(
            this OperationResult result,
            T param,
            Func<T, bool> predicate,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(predicate))] string predicateName = null)
        {
            if (predicate(param))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{predicateName} must be false", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool IsFalse(
            this OperationResult result,
            bool param,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (param)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} must be true", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool Equals<TParam, TCompareTo>(
            this OperationResult result,
            TParam param,
            TCompareTo compareTo,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if ((param == null && compareTo != null)
                || !param.Equals(compareTo))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} should be equal to {compareTo}, but it was {param}", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool DoesNotEqual<TParam, TCompareTo>(
            this OperationResult result,
            TParam param,
            TCompareTo compareTo,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if ((param == null && compareTo == null)
                || param.Equals(compareTo))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} should not be equal to {compareTo}", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool IsGreaterThan<TParam, TCompareTo>(
            this OperationResult result,
            TParam param,
            TCompareTo compareTo,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
            where TParam : struct, IComparable, IComparable<TParam>, IEquatable<TParam>, IFormattable
            where TCompareTo : struct, IComparable, IComparable<TCompareTo>, IEquatable<TCompareTo>, IFormattable
        {
            TParam typedCompareTo = GetTypedCompareToParameter<TParam, TCompareTo>(compareTo);

            if (param.CompareTo(typedCompareTo) <= 0)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} must be greater than {compareTo}", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool IsGreaterThanOrEqualTo<TParam, TCompareTo>(
            this OperationResult result,
            TParam param,
            TCompareTo compareTo,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
            where TParam : struct, IComparable, IComparable<TParam>, IEquatable<TParam>, IFormattable
            where TCompareTo : struct, IComparable, IComparable<TCompareTo>, IEquatable<TCompareTo>, IFormattable
        {
            TParam typedCompareTo = GetTypedCompareToParameter<TParam, TCompareTo>(compareTo);

            if (param.CompareTo(typedCompareTo) == -1)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} must be greater than {compareTo}", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool IsLessThan<TParam, TCompareTo>(
            this OperationResult result,
            TParam param,
            TCompareTo compareTo,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
            where TParam : struct, IComparable, IComparable<TParam>, IEquatable<TParam>, IFormattable
            where TCompareTo : struct, IComparable, IComparable<TCompareTo>, IEquatable<TCompareTo>, IFormattable
        {
            if (param.CompareTo(compareTo) >= 0)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} must be greater than {compareTo}", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool IsLessThanOrEqualTo<TParam, TCompareTo>(
            this OperationResult result,
            TParam param,
            TCompareTo compareTo,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
            where TParam : struct, IComparable, IComparable<TParam>, IEquatable<TParam>, IFormattable
            where TCompareTo : struct, IComparable, IComparable<TCompareTo>, IEquatable<TCompareTo>, IFormattable
        {
            TParam typedCompareTo = GetTypedCompareToParameter<TParam, TCompareTo>(compareTo);

            if (param.CompareTo(typedCompareTo) == 1)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} must be greater than {compareTo}", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool IsBetween<TParam, TFrom, TTo>(
            this OperationResult result,
            TParam param,
            TFrom from,
            TTo to,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
            where TParam : struct, IComparable, IComparable<TParam>, IEquatable<TParam>, IFormattable
            where TFrom : struct, IComparable, IComparable<TFrom>, IEquatable<TFrom>, IFormattable
            where TTo : struct, IComparable, IComparable<TTo>, IEquatable<TTo>, IFormattable
        {
            TParam typedFrom = GetTypedCompareToParameter<TParam, TFrom>(from);
            TParam typedTo = GetTypedCompareToParameter<TParam, TTo>(to);

            if (param.CompareTo(typedFrom) == -1 || param.CompareTo(typedTo) == 1)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} must be between {from} and {to}", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool IsValidEnumMember<TEnum>(
            this OperationResult result,
            TEnum param,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            param.ThrowIfNull(paramName: paramName);

            if (!Enum.IsDefined(typeof(TEnum), param))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} with value {param} is not a valid member of enum type {typeof(TEnum)}", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool DirectoryExists(
            this OperationResult result,
            string param,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (!Directory.Exists(param))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} directory does not exist", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        public static bool FileExists(
            this OperationResult result,
            string param,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (!File.Exists(param))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{FormatParamName(paramName)} file does not exist", OperationResultErrorType.Validation);
                return false;
            }

            return true;
        }

        #endregion

        #region Helper methods

        public static OperationResult ValidateInternal(
            OperationResult result,
            Func<bool> validationFunc,
            bool haltOnFailure = true)
        {
            if (result.IsValidationHalted)
            {
                return result;
            }

            var validationResult = validationFunc();

            if (!validationResult)
            {
                if (haltOnFailure)
                {
                    result.IsValidationHalted = true;
                }

                result.LastValidationError = result.Errors[result.Errors.Count - 1];
            }
            else
            {
                result.LastValidationError = null;
            }

            return result;
        }

        public static async Task<OperationResult> ValidateInternalAsync(
            OperationResult result,
            Func<Task<bool>> validationFuncAsync,
            bool haltOnFailure = true)
        {
            if (result.IsValidationHalted)
            {
                return result;
            }

            var validationResult = await validationFuncAsync();

            if (!validationResult)
            {
                if (haltOnFailure)
                {
                    result.IsValidationHalted = true;
                }

                result.LastValidationError = result.Errors[result.Errors.Count - 1];
            }
            else
            {
                result.LastValidationError = null;
            }

            return result;
        }

        public static string FormatParamName(string paramName)
        {
            if (paramName == null)
            {
                return null;
            }

            if (paramName.StartsWith("this."))
            {
                paramName = paramName.Substring(5);
            }

            if (paramName.Contains("=>"))
            {
                var expr = paramName.Split("=>").Last();

                if (expr.Contains('.'))
                {
                    expr = expr.Split('.').Last();
                }

                return expr;
            }

            // tidy up null coalescing operators
            paramName = paramName
                .Replace("?.", ".")
                .Replace(" ?? 0", string.Empty);

            return paramName;
        }

        internal static TParam GetTypedCompareToParameter<TParam, TCompareTo>(TCompareTo compareTo)
            where TParam : struct, IComparable, IComparable<TParam>, IEquatable<TParam>, IFormattable
            where TCompareTo : struct, IComparable, IComparable<TCompareTo>, IEquatable<TCompareTo>, IFormattable
        {
            object typedCompareTo = compareTo;

            if (typeof(TParam) != typeof(TCompareTo))
            {
                typedCompareTo = Convert.ChangeType(typedCompareTo, typeof(TParam));
            }

            return (TParam)typedCompareTo;
        }

        #endregion
    }
}
