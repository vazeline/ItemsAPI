using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common.ExtensionMethods;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Items.WebAPI.Filters.Swagger
{
    public class PagingSortingOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var attribute = context.MethodInfo.GetCustomAttribute<PagingSortingOperationFilterAttribute>();

            if (attribute != null)
            {
                operation.Parameters ??= new List<OpenApiParameter>();

                if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor)
                {
                    if (attribute.IncludePaging)
                    {
                        operation.Parameters.Add(new OpenApiParameter()
                        {
                            Name = "pageSize",
                            In = ParameterLocation.Query,
                            Description = "Number of records per page",
                            Required = attribute.IsPagingRequired,
                            Schema = new OpenApiSchema
                            {
                                Type = "integer"
                            }
                        });

                        operation.Parameters.Add(new OpenApiParameter()
                        {
                            Name = "pageIndex",
                            In = ParameterLocation.Query,
                            Description = "Page number (0-indexed)",
                            Schema = new OpenApiSchema
                            {
                                Type = "integer",
                                Default = new OpenApiInteger(0)
                            }
                        });
                    }

                    if (attribute.IncludeSorting)
                    {
                        operation.Parameters.Add(new OpenApiParameter()
                        {
                            Name = "orderBy",
                            In = ParameterLocation.Query,
                            Description = "Sorting parameters in the format field1,field2,field3 desc,field4",
                            Required = attribute.IsSortingRequired,
                            Schema = new OpenApiSchema
                            {
                                Type = "string"
                            }
                        });
                    }
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PagingSortingOperationFilterAttribute : Attribute
    {
        public PagingSortingOperationFilterAttribute(
            bool includePaging = true,
            bool includeSorting = true,
            bool isPagingRequired = false,
            bool isSortingRequired = false)
        {
            if (isPagingRequired && !includePaging)
            {
                throw new ArgumentException($"{nameof(includePaging)} must be true if {nameof(isPagingRequired)} is true");
            }

            if (isSortingRequired && !includeSorting)
            {
                throw new ArgumentException($"{nameof(includeSorting)} must be true if {nameof(isSortingRequired)} is true");
            }

            this.IncludePaging = includePaging;
            this.IncludeSorting = includeSorting;
            this.IsPagingRequired = isPagingRequired;
            this.IsSortingRequired = isSortingRequired;
        }

        public bool IncludePaging { get; }

        public bool IncludeSorting { get; }

        public bool IsPagingRequired { get; }

        public bool IsSortingRequired { get; }
    }
}
