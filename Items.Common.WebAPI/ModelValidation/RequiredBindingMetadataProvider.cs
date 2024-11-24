using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Items.Common.WebAPI.ModelValidation
{
    /// <summary>
    /// Ensure that anything marked with [Required] behaves as if it was marked with [BindRequired],
    /// which is to say that if a default value for a value type parameter is passed, it will be considered "not supplied" on the request.
    /// </summary>
    public class RequiredBindingMetadataProvider : IBindingMetadataProvider
    {
        public void CreateBindingMetadata(BindingMetadataProviderContext context)
        {
            if (context.PropertyAttributes?.OfType<RequiredAttribute>().Any() == true)
            {
                context.BindingMetadata.IsBindingRequired = true;
            }
        }
    }
}
