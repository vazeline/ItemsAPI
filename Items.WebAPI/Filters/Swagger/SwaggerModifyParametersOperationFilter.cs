using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Common.ExtensionMethods;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Items.WebAPI.Filters.Swagger
{
    public class SwaggerModifyParametersOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var removeParameterNames = context.MethodInfo.GetCustomAttributes<SwaggerRemoveParameter>()?
                .Select(x => x.ParameterName)
                .Distinct()
                .ToImmutableHashSet();

            var requiredParameterNames = context.MethodInfo.GetCustomAttributes<SwaggerRequiredParameter>()?
                .Select(x => x.ParameterName)
                .Distinct()
                .ToImmutableHashSet();

            if (removeParameterNames.Any()
                && requiredParameterNames.Any()
                && removeParameterNames.Intersect(requiredParameterNames).Any())
            {
                throw new InvalidOperationException();
            }

            if (removeParameterNames.Any() || requiredParameterNames.Any())
            {
                // check if this is a form-bodied request
                if (operation?.RequestBody?.Content?.TryGetValue("multipart/form-data", out var multiPartFormData) == true)
                {
                    foreach (var removeParameterName in removeParameterNames)
                    {
                        if (multiPartFormData?.Encoding?.ContainsKey(removeParameterName) == true)
                        {
                            multiPartFormData.Encoding.Remove(removeParameterName);
                        }

                        if (multiPartFormData?.Schema?.Properties?.ContainsKey(removeParameterName) == true)
                        {
                            multiPartFormData.Schema.Properties.Remove(removeParameterName);
                        }
                    }

                    foreach (var requiredParameterName in requiredParameterNames)
                    {
                        if (multiPartFormData?.Schema?.Properties?.ContainsKey(requiredParameterName) == true
                            && !multiPartFormData.Schema.Required.Contains(requiredParameterName))
                        {
                            multiPartFormData.Schema.Required.Add(requiredParameterName);
                        }
                    }
                }

                // otherwise loop through the accepted request content types, check if they reference a common schema
                // if so, clone it, reassign the schama, and modify as appropriate
                else if (operation?.RequestBody?.Content != null)
                {
                    foreach (var requestContentType in operation?.RequestBody?.Content)
                    {
                        var schemaId = requestContentType.Value?.Schema?.Reference?.Id;

                        if (schemaId != null
                            && context.SchemaRepository.Schemas.TryGetValue(schemaId, out var schema))
                        {
                            // we don't want to alter the schema in the repository - it will affect all instances of the endpoint, we need to clone it instead
                            var schemaClone = JsonSerializer.Deserialize<OpenApiSchema>(JsonSerializer.Serialize(context.SchemaRepository.Schemas[schemaId]));

                            foreach (var removeParameterName in removeParameterNames)
                            {
                                var schemaPropertyName = schema.Properties.Keys.FirstOrDefault(x => x.Equals(removeParameterName, StringComparison.OrdinalIgnoreCase));

                                if (schemaPropertyName != null)
                                {
                                    schemaClone.Properties.Remove(schemaPropertyName);
                                }
                            }

                            foreach (var requiredParameterName in requiredParameterNames)
                            {
                                var schemaPropertyName = schema.Properties.Keys.FirstOrDefault(x => x.Equals(requiredParameterName, StringComparison.OrdinalIgnoreCase));

                                if (schemaPropertyName != null
                                    && !schemaClone.Required.Contains(schemaPropertyName))
                                {
                                    schemaClone.Required.Add(schemaPropertyName);
                                }
                            }

                            requestContentType.Value.Schema = schemaClone;
                        }
                    }
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SwaggerRemoveParameter : Attribute
    {
        public SwaggerRemoveParameter(string parameterName)
        {
            this.ParameterName = parameterName;
        }

        public string ParameterName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SwaggerRequiredParameter : Attribute
    {
        public SwaggerRequiredParameter(string parameterName)
        {
            this.ParameterName = parameterName;
        }

        public string ParameterName { get; set; }
    }
}
