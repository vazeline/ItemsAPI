using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Common.ExtensionMethods
{
    public static class ReflectionExtensions
    {
        public static async Task<TResult> InvokeAsync<TResult>(this MethodInfo methodInfo, object objectToInvokeMethodOn, params object[] parameters)
        {
            var task = (Task)methodInfo.Invoke(objectToInvokeMethodOn, parameters);
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            return (TResult)resultProperty.GetValue(task);
        }
    }
}
