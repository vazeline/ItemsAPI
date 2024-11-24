using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Common.Exceptions;
using Common.Models;
using Common.Utility;
using Items.Common.WebAPI.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Items.Common.WebAPI.DependencyInjection
{
    public static class ApplicationBuilderExtensions
    {
        public static void EnableHttpRequestReReading(this IApplicationBuilder app)
        {
            app.Use((context, next) =>
            {
                context.Request.EnableBuffering();
                return next();
            });
        }

        public static void AddEndpointLogging(this IApplicationBuilder app, Func<bool> isEnabledFunc )
        {
            app.UseMiddleware<EndpointLoggingMiddleware>(isEnabledFunc);
        }

        public static void ConfigureExceptionHandler(this IApplicationBuilder app, ILogger logger, IHttpContextAccessor httpContextAccessor)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async httpContext =>
                {
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    httpContext.Response.ContentType = "application/json";

                    var contextFeature = httpContext.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        var logMessage = "Unhandled exception caught in WebAPI";

                        if (httpContextAccessor?.HttpContext != null)
                        {
                            var additionalInfo = Environment.NewLine
                                + "  HTTP REQUEST DETAILS"
                                + Environment.NewLine
                                + "  --------------------"
                                + Environment.NewLine
                                + await LoggingUtility.HttpRequestToLogStringAsync(httpContextAccessor.HttpContext);

                            using (LogContext.PushProperty(LoggingUtility.SerilogAdditionalLogInfoContextProperty, additionalInfo))
                            {
                                logger.LogError(contextFeature.Error, logMessage);
                            }
                        }
                        else
                        {
                            logger.LogError(contextFeature.Error, logMessage);
                        }

                        // by default, hide the exception details from the response message to calling clients, we don't want to leak internal server details
                        var errorMessage = "Unexpected error";

                        var friendlyException = contextFeature.Error as ConsumerFriendlyException;

                        // if we've thrown a friendly exception somewhere, we should always use its error message preferentially
                        if (friendlyException != null)
                        {
                            errorMessage = friendlyException.Message;
                        }

                        // return the entire exception and stack trace in unit test mode so we can see the result in the test logs
                        if (EnvironmentUtility.IsInUnitTestMode)
                        {
                            errorMessage = contextFeature.Error.ToString();
                        }

                        // otherwise if we're in development mode, append the exception message, to at least give ourselves a clue where to look
                        // we don't want the entire exception stack trace because it will just overflow the screen of whatever client is calling the API
                        else if (EnvironmentUtility.IsDevelopment && friendlyException == null)
                        {
                            errorMessage += $" - {contextFeature.Error.Message}";
                        }

                        var result = OperationResult.Failure(errorMessage);

                        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
                    }
                });
            });
        }
    }
}
