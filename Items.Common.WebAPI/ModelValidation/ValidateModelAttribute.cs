using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Common.ExtensionMethods;
using Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Items.Common.WebAPI.ModelValidation
{
    /// <summary>
    /// Checks whether current ModelState is valid and returns BadResult if not.
    /// HOW TO FIX REQUIRED VALUE TYPES IN JSON BINDING - https://thom.ee/blog/clean-way-to-use-required-value-types-in-asp-net-core/#bindrequired.
    /// </summary>
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        private static readonly Regex RgxJsonRequiredFieldFailure =
            new Regex(@"JSON deserialization for type.*missing required properties, including the following:(?:,?\s(?<field>[^,]+))+");

        private readonly string[] additionalRequiredModelPropertyNames;

        public ValidateModelAttribute(params string[] additionalRequiredModelPropertyNames)
        {
            this.additionalRequiredModelPropertyNames = additionalRequiredModelPropertyNames;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var result = new OperationResult
            {
                DoNotAddStackTraceToErrorsInUnitTestMode = true
            };

            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState.Values
                    .SelectMany(e => e.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToArray();

                foreach (var error in errors)
                {
                    var isJsonRequiredFieldFailureMatch = RgxJsonRequiredFieldFailure.Match(error);

                    if (isJsonRequiredFieldFailureMatch.Success)
                    {
                        foreach (Capture field in isJsonRequiredFieldFailureMatch.Groups["field"].Captures.Cast<Capture>())
                        {
                            result.AddError($"The {field.Value.ToTitleCase()} field is required.", OperationResultErrorType.Validation);
                        }
                    }
                    else
                    {
                        result.AddError(error, OperationResultErrorType.Validation);
                    }
                }
            }
            else if (context.ActionArguments?.Count == 1 && this.additionalRequiredModelPropertyNames?.Any() == true)
            {
                var possibleDto = context.ActionArguments.ElementAt(0).Value;

                if (possibleDto?.GetType().IsValueType == false)
                {
                    var propertyInfos = possibleDto.GetType().GetProperties();

                    foreach (var propertyName in this.additionalRequiredModelPropertyNames)
                    {
                        var propertyInfo = propertyInfos.FirstOrDefault(x => x.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

                        if (propertyInfo != null)
                        {
                            var propertyValue = propertyInfo.GetValue(possibleDto);

                            // check if property is a value type, and is set to default value
                            // or is reference type, and is null
                            if ((propertyInfo.PropertyType.IsValueType && propertyValue == Activator.CreateInstance(propertyInfo.PropertyType))
                                || (!propertyInfo.PropertyType.IsValueType && propertyValue == null))
                            {
                                result.AddError($"{propertyName} is required");
                            }
                        }
                    }
                }
            }

            if (!result.IsSuccessful)
            {
                context.Result = (context.Controller as Microsoft.AspNetCore.Mvc.ControllerBase).StatusCode(
                    (int)HttpStatusCode.BadRequest,
                    result);
            }
        }
    }
}
