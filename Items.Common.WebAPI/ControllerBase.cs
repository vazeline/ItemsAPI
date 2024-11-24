using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Models;
using Items.Common.WebAPI.Utility;
using Microsoft.AspNetCore.Mvc;

namespace Items.Common.WebAPI
{
    public class ControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        protected IActionResult HandleResult(OperationResult result, int? overrideFailureStatusCode = null)
        {
            return ControllerUtility.HandleResult(this, result, overrideFailureStatusCode);
        }

        protected IActionResult HandleResult<T>(OperationResult<T> result, int? overrideFailureStatusCode = null)
        {
            return ControllerUtility.HandleResult(this, result, overrideFailureStatusCode);
        }

        protected IActionResult HandleResultWithFile(
            FileResult fileResult,
            OperationResult result)
        {
            return ControllerUtility.HandleResultWithFile(this, fileResult, result);
        }

        protected IActionResult HandleFileResult<T>(
            FileResult fileResult,
            OperationResult<T> result,
            int? overrideFailureStatusCode = null)
        {
            return ControllerUtility.HandleResultWithFile(this, fileResult, result, overrideFailureStatusCode);
        }
    }
}
