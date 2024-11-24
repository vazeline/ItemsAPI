using System.Collections.Generic;

namespace Common.Models
{
    public interface IOperationResult
    {
        IReadOnlyList<string> Errors { get; }

        IReadOnlyList<(string ErrorMessage, OperationResultErrorType Type)> CategorisedErrors { get; }

        bool IsSuccessful { get; set; }

        void AddError(string error);

        void AddError(string error, OperationResultErrorType type);
    }

    public interface IOperationResult<T> : IOperationResult
    {
        T Data { get; }
    }
}
