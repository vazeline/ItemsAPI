using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Common.ExtensionMethods;
using Common.Utility.Classes;
using Microsoft.AspNetCore.Http;

namespace Common.Utility
{
    public static class LoggingUtility
    {
        public const string SerilogAdditionalLogInfoContextProperty = "AdditionalInfo";

        public static async Task<string> HttpRequestToLogStringAsync(HttpContext httpContext)
        {
            const string leftPadding = "  ";

            var request = httpContext.Request;

            var sb = new StringBuilder();

            // url and Method
            sb.AppendLine(leftPadding + $"{request.Method} {request.Scheme}://{request.Host}{request.Path}{request.QueryString}");

            sb.AppendLine();
            sb.AppendLine(leftPadding + "HEADERS:");

            // headers
            foreach (var header in request.Headers)
            {
                if (header.Key == "Authorization")
                {
                    sb.AppendLine(leftPadding + $"{header.Key}: (value omitted from log)");
                }
                else
                {
                    sb.AppendLine(leftPadding + $"{header.Key}: {header.Value.ToString().TrimIfTooLong(200)}");
                }
            }

            // body
            var originalBodyPosition = request.Body.Position;
            request.Body.Position = 0;

            var bodyStream = new StreamReader(request.Body);

            string bodyText;

            if (request.Body.Length <= 5000)
            {
                bodyText = await bodyStream.ReadToEndAsync();
            }
            else
            {
                var buffer = new char[5000];
                await bodyStream.ReadAsync(buffer, 0, 5000);
                bodyText = buffer.ToString() + "... (truncated)";
            }

            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                sb.AppendLine();
                sb.AppendLine(leftPadding + "BODY:");

                // pretty format JSON is body is JSON
                if (JsonUtility.IsStringValidJSON(bodyText, out var parsed))
                {
                    bodyText = JsonSerializer.Serialize(parsed, new JsonSerializerOptions() { WriteIndented = true });
                }

                sb.AppendLine(leftPadding + bodyText);
            }

            request.Body.Position = originalBodyPosition;

            sb.AppendLine();
            sb.AppendLine(leftPadding + "USER DETAILS:");

            // user
            var strUserId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
            sb.AppendLine(leftPadding + $"UserId: {strUserId}");
            sb.Append(leftPadding + $"IP Address: {EnvironmentUtility.GetClientIpAddressFromHttpContext(httpContext)}");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
