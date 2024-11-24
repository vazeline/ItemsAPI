using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Common.ExtensionMethods;
using Common.Utility;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("Items.Data.EFCore")]

namespace Common.Models
{
    public enum OperationResultErrorType
    {
        General = 1,

        /// <summary>
        /// Typically used when attempting to look up an entity from a repository,
        /// and it either does not exist, or is blocked from current user access by contact filtering.
        /// </summary>
        EntityNotFoundOrUnauthorized = 2,

        Validation = 3,

        Critical = 4,

        /// <summary>
        /// General unauthorized access attempt to a resource.
        /// </summary>
        Unauthorized = 5
    }

    public class OperationResult : IOperationResult
    {
        private List<(string ErrorMessage, OperationResultErrorType Type)> categorisedErrors = new List<(string ErrorMessage, OperationResultErrorType Type)>();
        private bool isRolledBack = false;

        public OperationResult()
            : this(true)
        {
        }

        public OperationResult(bool isSuccessful)
        {
            this.IsSuccessful = isSuccessful;
        }

        public OperationResult(OperationResult otherResult)
        {
            this.IsSuccessful = otherResult.IsSuccessful;
            this.categorisedErrors = otherResult.CategorisedErrors.ToList();
        }

        public static OperationResult Success => new OperationResult(true);

        public bool IsSuccessful { get; set; }

        [JsonIgnore]
        public bool DoNotAddStackTraceToErrorsInUnitTestMode { get; set; }

        /// <summary>
        /// Sets an action used by GenericBusinessLogic - if committing changes to the context fails, and this property is not null, it will be executed.
        /// Use it to roll back non-related options (such as writing a file to disk, and then deleting it on error) which are transactional along with updating an entity.
        /// </summary>
        [JsonIgnore]
        public Func<OperationResult, Task> OnErrorRollbackFuncAsync { private get; set; }

        [JsonInclude] // required to get System.Text.Json to deserialize private setter
        public IReadOnlyList<string> Errors
        {
            get => this.categorisedErrors.Select(x => x.ErrorMessage).ToList().AsReadOnly();
            set => this.categorisedErrors = value.Select(x => (x, OperationResultErrorType.General)).ToList();
        }

        [JsonIgnore] // required to get System.Text.Json to deserialize private setter
        public IReadOnlyList<(string ErrorMessage, OperationResultErrorType Type)> CategorisedErrors
        {
            get => this.categorisedErrors.AsReadOnly();
        }

        [JsonIgnore]
        internal bool IsValidationHalted { get; set; }

        [JsonIgnore]
        internal string LastValidationError { get; set; }

        public static OperationResult Failure(string error)
        {
            var result = new OperationResult(false);
            result.AddError(error);
            return result;
        }

        public static OperationResult Failure(string error, OperationResultErrorType type)
        {
            var result = new OperationResult(false);
            result.AddError(error, type);
            return result;
        }

        public static OperationResult Failure(string[] errors)
        {
            var result = new OperationResult(false);
            result.AddErrors(errors);
            return result;
        }

        public static OperationResult Failure(IOperationResult otherResult)
        {
            var result = new OperationResult(false);
            result.AddErrors(otherResult);
            return result;
        }

        public async Task RollbackAsync(OperationResult resultToAddErrorsTo)
        {
            try
            {
                if (this.OnErrorRollbackFuncAsync != null && !this.isRolledBack)
                {
                    await this.OnErrorRollbackFuncAsync(resultToAddErrorsTo);
                }
            }
            finally
            {
                this.isRolledBack = true;
            }
        }

        public OperationResult ThrowIfNotSuccessful(Exception innerException = null)
        {
            if (!this.IsSuccessful)
            {
                throw new InvalidOperationException(string.Join(", ", this.Errors), innerException);
            }

            return this;
        }

        public void ThrowIfNotSuccessful<TException>()
            where TException : Exception, new()
        {
            if (!this.IsSuccessful)
            {
                throw (TException)Activator.CreateInstance(typeof(TException), string.Join(", ", this.Errors));
            }
        }

        public bool AddErrors(IOperationResult otherResult)
        {
            if (otherResult.IsSuccessful)
            {
                return false;
            }

            foreach (var categorisedError in otherResult.CategorisedErrors)
            {
                this.AddError(categorisedError.ErrorMessage, categorisedError.Type);
            }

            return true;
        }

        public bool AddErrors(IOperationResult otherResult, OperationResultErrorType type)
        {
            if (otherResult.IsSuccessful)
            {
                return false;
            }

            this.AddErrors(otherResult.Errors.ToArray(), type);

            return true;
        }

        public bool AddErrors(string prefix, IOperationResult otherResult)
        {
            if (otherResult.IsSuccessful)
            {
                return false;
            }

            this.AddErrors(otherResult.Errors.Select(x => $"{prefix} - {x}").ToArray(), OperationResultErrorType.General);

            return true;
        }

        public bool AddErrors(string prefix, IOperationResult otherResult, OperationResultErrorType type)
        {
            if (otherResult.IsSuccessful)
            {
                return false;
            }

            this.AddErrors(otherResult.Errors.Select(x => $"{prefix} - {x}").ToArray(), type);

            return true;
        }

        public void AddError(string error)
        {
            this.AddErrors(new[] { error }, OperationResultErrorType.General);
        }

        public void AddError(string error, OperationResultErrorType type)
        {
            this.AddErrors(new[] { error }, type);
        }

        public void AddErrors(string[] errors)
        {
            this.AddErrors(errors, OperationResultErrorType.General);
        }

        public void AddErrors(string[] errors, OperationResultErrorType type)
        {
            if (errors.Length > 0)
            {
                this.categorisedErrors.AddRange(errors.Select(x => (this.GetErrorMessage(x), type)));
                this.IsSuccessful = false;
            }
        }

        public void LogErrors(ILogger logger, string errorMessagePrefix = null)
        {
            if (!this.IsSuccessful)
            {
                if (!string.IsNullOrWhiteSpace(errorMessagePrefix))
                {
                    logger.LogError($"{errorMessagePrefix} - {this.Errors.StringJoin(", ")}");
                }
                else
                {
                    logger.LogError(this.Errors.StringJoin(", "));
                }
            }
        }

        public void ResumeValidation()
        {
            this.IsValidationHalted = false;
        }

        public override string ToString()
        {
            if (this.IsSuccessful)
            {
                return $"{nameof(this.IsSuccessful)}: {this.IsSuccessful}";
            }

            return $"{nameof(this.IsSuccessful)}: {this.IsSuccessful}, Errors: {this.Errors.StringJoin(", ").TrimIfTooLong(200)}";
        }

        private string GetErrorMessage(string message)
        {
            // append the current stack trace when we get validation errors in unit testing mode, so we have a clue where they came from
            // we do also trap the logger error output, however for regular operation result validation errors which don't throw exceptions, there won't
            // be any error to log, and therefore won't be any stack trace
            // alternative would be to pass an ILogger into every single operation result and log failures in this class - however that's a big refactor
            if (EnvironmentUtility.IsInUnitTestMode && !this.DoNotAddStackTraceToErrorsInUnitTestMode)
            {
                message = $"{message}\r\n\r\nOperationResult Stack Trace:\r\n{Environment.StackTrace}";
            }

            return message;
        }
    }

    public class OperationResult<T> : OperationResult, IOperationResult<T>
    {
        public OperationResult()
            : base()
        {
        }

        public OperationResult(bool isSuccessful)
            : base(isSuccessful)
        {
        }

        public OperationResult(T data)
            : base(true)
        {
            this.Data = data;
        }

        public OperationResult(bool isSuccessful, T data)
            : base(isSuccessful)
        {
            this.Data = data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationResult{T}"/> class.
        /// Added for legacy compatibility with LogiLease.
        /// </summary>
        public OperationResult(T data, bool isSuccessful, params string[] errors)
            : this(isSuccessful, data)
        {
            if (errors?.Any() == true)
            {
                this.AddErrors(errors);
            }
        }

        public OperationResult(OperationResult otherResult)
            : base(otherResult)
        {
        }

        public T Data { get; set; }

        T IOperationResult<T>.Data => this.Data;

        public static new OperationResult<T> Success(T data)
        {
            return new OperationResult<T>(data);
        }

        /// <summary>
        /// Added for legacy compatibility with LogiLease.
        /// </summary>
        public static OperationResult<T> Failure(T data)
        {
            var result = new OperationResult<T>(data)
            {
                IsSuccessful = false
            };

            return result;
        }

        public static new OperationResult<T> Failure(string error)
        {
            var result = new OperationResult<T>(false);
            result.AddError(error);
            return result;
        }

        public static new OperationResult<T> Failure(string error, OperationResultErrorType type)
        {
            var result = new OperationResult<T>(false);
            result.AddError(error, type);
            return result;
        }

        public static new OperationResult<T> Failure(string[] errors)
        {
            var result = new OperationResult<T>(false);
            result.AddErrors(errors);
            return result;
        }

        public static new OperationResult<T> Failure(IOperationResult otherResult)
        {
            var result = new OperationResult<T>(false);
            result.AddErrors(otherResult);
            return result;
        }

        public OperationResult<T> ThrowIfNotSuccessful()
        {
            this.ThrowIfNotSuccessful<InvalidOperationException>();
            return this;
        }

        public T ThrowIfNotSuccessfulAndReturn()
        {
            this.ThrowIfNotSuccessful<InvalidOperationException>();
            return this.Data;
        }

        public override string ToString()
        {
            var baseToString = base.ToString();

            if (!this.IsSuccessful || this.Data == null)
            {
                return baseToString;
            }

            if (this.Data != null)
            {
                return $"{baseToString}, Data: {this.Data.ToString().TrimIfTooLong(200)}";
            }

            return baseToString;
        }
    }
}
