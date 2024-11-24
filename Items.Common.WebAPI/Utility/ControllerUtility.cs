using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Common.ExtensionMethods;
using Common.Models;
using Common.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Items.Common.WebAPI.Utility
{
    public static class ControllerUtility
    {
        internal const string HeaderKeyResponseSerialisedDtoContent = "response-payload";

        /// <summary>
        /// Should contain the namespace of the Domain Entity assembly in this solution.
        /// Used in HandleResult, to make sure we're not accidentally returning a domain entity to the FE.
        /// </summary>
        public static string DomainEntityNamespace { get; set; }

        public static IActionResult HandleResult(
            Microsoft.AspNetCore.Mvc.ControllerBase controller,
            OperationResult result,
            int? overrideFailureStatusCode = null)
        {
            DomainEntityNamespace.ThrowIfNullOrWhiteSpace();

            // make sure we're just dealing with a raw operation result, we might have an OperationResult<T> passed in due to casting
            result = new OperationResult(result);

            if (result.IsSuccessful)
            {
                return controller.Ok(result);
            }

            return controller.StatusCode(
                GetFailureStatusCode(result, overrideFailureStatusCode),
                result);
        }

        public static IActionResult HandleResult<T>(
            Microsoft.AspNetCore.Mvc.ControllerBase controller,
            OperationResult<T> result,
            int? overrideFailureStatusCode = null)
        {
            DomainEntityNamespace.ThrowIfNullOrWhiteSpace();
            ValidateOperationResultDataIsNotDomainEntity(result);

            if (result.IsSuccessful)
            {
                return controller.Ok(result);
            }

            return controller.StatusCode(
                GetFailureStatusCode(result, overrideFailureStatusCode),
                result);
        }

        /// <summary>
        /// Returns the given fileResult, serialising the OperationResult into the response header.
        /// This enables both an OperationResult and a file to be returned simultaneously.
        /// </summary>
        public static IActionResult HandleResultWithFile(
            Microsoft.AspNetCore.Mvc.ControllerBase controller,
            FileResult fileResult,
            OperationResult result)
        {
            DomainEntityNamespace.ThrowIfNullOrWhiteSpace();

            fileResult.ThrowIfNull();

            if (!string.IsNullOrWhiteSpace(fileResult.FileDownloadName))
            {
                controller.Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition");
            }

            controller.Response.Headers[HeaderKeyResponseSerialisedDtoContent] = JsonSerializer.Serialize(
                value: result,
                inputType: result.GetType(),
                options: new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            // return the file directly without the status helpers like controller.Ok()
            // by default anything returned through the status helpers will be serialized to JSON
            return fileResult;
        }

        /// <summary>
        /// Returns the given fileResult, serialising the OperationResult into the response header.
        /// This enables both an OperationResult and a file to be returned simultaneously.
        /// </summary>
        public static IActionResult HandleResultWithFile<T>(
            Microsoft.AspNetCore.Mvc.ControllerBase controller,
            FileResult fileResult,
            OperationResult<T> result,
            int? overrideFailureStatusCode = null)
        {
            DomainEntityNamespace.ThrowIfNullOrWhiteSpace();
            ValidateOperationResultDataIsNotDomainEntity(result);

            if (!string.IsNullOrWhiteSpace(fileResult.FileDownloadName))
            {
                controller.Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition");
            }

            controller.Response.Headers[HeaderKeyResponseSerialisedDtoContent] = JsonSerializer.Serialize(
                value: result,
                inputType: result.GetType(),
                options: new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            if (result.IsSuccessful)
            {
                return controller.Ok(result);
            }

            return controller.StatusCode(
                GetFailureStatusCode(result, overrideFailureStatusCode),
                result);
        }

        public static int GetFailureStatusCode(OperationResultErrorType errorType) =>
            errorType switch
            {
                OperationResultErrorType.Validation => StatusCodes.Status400BadRequest,
                OperationResultErrorType.EntityNotFoundOrUnauthorized => StatusCodes.Status404NotFound,
                OperationResultErrorType.Critical => StatusCodes.Status500InternalServerError,
                OperationResultErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            };

        internal static int GetFailureStatusCode(OperationResult result, int? overrideFailureStatusCode)
        {
            if (overrideFailureStatusCode == null)
            {
                if (result.CategorisedErrors.Any())
                {
                    return GetFailureStatusCode(result.CategorisedErrors[result.CategorisedErrors.Count - 1].Type);
                }

                return StatusCodes.Status500InternalServerError;
            }

            return overrideFailureStatusCode.Value;
        }

        private static void ValidateOperationResultDataIsNotDomainEntity<T>(OperationResult<T> result)
        {
            // ensure we are only allowed to return DTO classes
            if (result.Data != null)
            {
                var resultType = result.Data.GetType();

                // if we're returning a collection, get the generic type
                // TODO: 2023.01.11 SW - this might need extending to Dictionary in the future
                var genericEnumerableInterfaces = TypeUtility.GetGenericInterfaceImplementations(result.Data.GetType(), typeof(IEnumerable<>));
                if (genericEnumerableInterfaces.Any())
                {
                    resultType = genericEnumerableInterfaces[0].GetGenericArguments()[0];
                }

                if (resultType.Namespace.StartsWith(DomainEntityNamespace))
                {
                    throw new InvalidOperationException($"Invalid return type {resultType.Name} - do not return domain entity types from controllers");
                }
            }
        }
    }
}
