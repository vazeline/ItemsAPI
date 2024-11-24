using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Common.ExtensionMethods;
using Common.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Items.Common.Testing.Utility
{
    public static class AssertionUtility
    {
        public static void AssertIsSuccessful(this OperationResult result, string testName = null, string failureMessage = null)
        {
            AssertOperationResultIsSuccessful(result, testName, failureMessage);
        }

        public static void AssertOperationResultIsSuccessful(OperationResult result, string testName = null, string failureMessage = null)
        {
            Assert.IsTrue(result.IsSuccessful, GetAssertionFailureMessage(result, testName, failureMessage));
        }

        public static string GetAssertionFailureMessage(OperationResult result, string testName = null, string failureMessage = null)
        {
            if (!result.IsSuccessful)
            {
                return $"{testName ?? "Operation"} failed with an unexpected result{Environment.NewLine.Repeat(2)}{(string.IsNullOrWhiteSpace(failureMessage) ? string.Empty : $"{failureMessage}{Environment.NewLine.Repeat(2)}")}{string.Join(Environment.NewLine, result.Errors.Select(x => $" - {x}"))}";
            }

            return null;
        }

        public static string GetAssertionFailureMessage(string testName = null, string failureMessage = null, Exception ex = null)
        {
            return $"{testName ?? "Operation"} failed{Environment.NewLine.Repeat(2)}{failureMessage}{(ex == null ? string.Empty : $"{Environment.NewLine.Repeat(2)}{ex}")}";
        }

        public static async Task AssertHttpResponseStatusCodeAsync(HttpResponseMessage response, int expectedStatusCode = (int)HttpStatusCode.OK)
        {
            if ((int)response.StatusCode != expectedStatusCode)
            {
                var stringResponse = await response.Content.ReadAsStringAsync();

                try
                {
                    // response is not a serialised OperationResult
                    if (!stringResponse.Contains("\"isSuccessful\"", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception();
                    }

                    // if we successfully deserialized an operation result with errors, fail the assertion with a helpful message
                    var result = JsonSerializer.Deserialize<OperationResult>(stringResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    result.IsSuccessful = false;
                    result.AssertIsSuccessful(failureMessage: $"Unexpected response HttpStatus: {response.StatusCode}");
                }
                catch (Exception ex) when (ex is not AssertFailedException)
                {
                    Assert.Fail(GetAssertionFailureMessage(failureMessage: $"Unexpected response HttpStatus: {(int)response.StatusCode} {response.StatusCode}{Environment.NewLine.Repeat(2)}{stringResponse}"));
                }
            }
        }
    }
}
