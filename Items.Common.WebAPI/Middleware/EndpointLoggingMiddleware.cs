using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Items.BusinessLogic.Services.AuditLog.Interfaces;
using Common.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace Items.Common.WebAPI.Middleware
{
    public class EndpointLoggingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly Func<bool> isEnabledFunc;
        private readonly ILogger<EndpointLoggingMiddleware> logger;

        public EndpointLoggingMiddleware(
            RequestDelegate next,
            Func<bool> isEnabledFunc,
            ILogger<EndpointLoggingMiddleware> logger )
        {
            this.next = next;
            this.isEnabledFunc = isEnabledFunc;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogBusinessLogic auditLogBehaviour )
        {
            try
            {
                if (this.isEnabledFunc())
                {
                    this.logger.LogDebug($"{context.Request.Method} {context.Request.GetDisplayUrl()}");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error in {nameof(EndpointLoggingMiddleware)}");
            }

            await this.next(context);

            try
            {
                var absoluteUri = string.Concat(
                        context.Request.Scheme,
                        "://",
                        context.Request.Host.ToUriComponent(),
                        context.Request.PathBase.ToUriComponent(),
                        context.Request.Path.ToUriComponent(),
                        context.Request.QueryString.ToUriComponent() );

                await auditLogBehaviour.AddActionAuditLogEntry(
                    absoluteUri,
                    context.Response.StatusCode,
                    context.Request.Method,
                    EnvironmentUtility.GetClientIpAddressFromHttpContext(context) );
            }
            catch(Exception ex )
            {
                this.logger.LogError(ex, $"Error in {nameof( EndpointLoggingMiddleware )}");
            }
        }
    }
}
