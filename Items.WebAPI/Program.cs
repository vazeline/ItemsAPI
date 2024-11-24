using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Common.Utility;
using Items.Common.DependencyInjection;
using Items.Common.WebAPI.DependencyInjection;
using Items.Common.WebAPI.ModelValidation;
using Items.Data.EFCore.Entities;
using Items.Domain;
using Items.WebAPI.Filters.Swagger;
using Items.WebAPI.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Items.WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    Args = args,
                    EnvironmentName = EnvironmentUtility.CurrentEnvironmentName
                });

            HostBuilderUtil.ConfigureHostBuilderCommon(builder.Host);

            var mvcBuilder = ConfigureServices(builder.Services, builder.Configuration);

            HostBuilderUtil.ConfigureServicesCommon(builder.Services, builder.Configuration, mvcBuilder);

            var app = builder.Build();

            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            HostBuilderUtil.ConfigureAppCommon<ItemsContext>(app, useLogicanTemplatingEngine: true);

            ConfigureApplication(
                app: app,
                configuration: builder.Configuration,
                serviceProvider: app.Services,
                logger: logger);

            logger.LogDebug("WebAPI Startup complete");

            app.Run();
        }

        private static IMvcBuilder ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options => ConfigureJwtOptions(options, configuration["JwtSettings:Secret"]));

            services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

            services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue;
            });

            var mvcBuilder = services
                .AddControllers(options =>
                {
                    // [Required] attributes will behave the same as [BindRequired]
                    options.ModelMetadataDetailsProviders.Add(new RequiredBindingMetadataProvider());
                });

            services.AddSwaggerGen(ConfigureSwaggerOptions);

            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

            services.AddHttpContextAccessor();

            return mvcBuilder;
        }

        private static void ConfigureApplication(
            WebApplication app,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<Program> logger)
        {
            try
            {
                var webHostEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>();
                var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();

                // configure the domain entity service provider
                DomainEntityBase.SetCurrentServiceProviderFunc(() => httpContextAccessor.HttpContext?.RequestServices);

                // enables the HTTP request stream to be read multiple times - this is required for logging HTTP request details when exceptions are thrown
                // should be the first registered middleware
                app.EnableHttpRequestReReading();

                app.AddEndpointLogging(isEnabledFunc: () => appSettings.Value.EnableDbContextLogging == "true" );

                // this must be registerd before any other middlewares for which you want to catch exceptions
                app.ConfigureExceptionHandler(logger, httpContextAccessor);

                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Items.WebAPI v1");
                    c.DocExpansion(DocExpansion.None); // start all controllers collapsed - there are too many
                    c.DocumentTitle = "Items API - Swagger UI";
                });

                if (string.IsNullOrEmpty(appSettings.Value.AllowedCORSOrigins))
                {
                    throw new Exception($"{nameof(AppSettings)}:{nameof(AppSettings.AllowedCORSOrigins)} is not defined or empty");
                }

                app.UseCors(x => x
                    .WithOrigins(appSettings.Value.AllowedCORSOrigins.Split(';'))
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());

                // WebApplicationFactory for integration testing does not need to use https, and turning on UseHttpsRedirection causes warnings in the log
                // it is possible to turn it on for integration testing, however it's not a 3-second thing as turning it on causes other errors later in the tests
                if (!EnvironmentUtility.IsInUnitTestMode)
                {
                    app.UseHttpsRedirection();
                }
                else
                {
                    logger.LogDebug("Skipped enabling HTTPS redirection because in test mode");
                }

                app.UseRouting();

                app.UseAuthentication();

                app.UseAuthorization();

                app.MapControllers();

                logger.LogInformation($"Using environment: {webHostEnvironment.EnvironmentName}");
                logger.LogInformation($"Allowed CORS origins: {configuration.GetValue<string>("AppSettings:AllowedCORSOrigins")}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Configure");
                throw;
            }
        }

        private static void ConfigureJwtOptions(JwtBearerOptions options, string jwtSecret)
        {
            var key = Encoding.Default.GetBytes(jwtSecret);

            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                RequireExpirationTime = false,
                ClockSkew = TimeSpan.FromSeconds(1)
            };
        }

        private static void ConfigureSwaggerOptions(SwaggerGenOptions options)
        {
            options.SwaggerDoc(
                name: "v1",
                info: new OpenApiInfo
                {
                    Title = "Items.WebAPI",
                    Version = "v1"
                });

            // Define the BearerAuth scheme that's in use
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "Bearer",
                        Name = "Bearer",
                        In = ParameterLocation.Header,
                    },
                    new List<string>()
                }
            });

            options.OperationFilter<SwaggerModifyParametersOperationFilter>();
            options.OperationFilter<PagingSortingOperationFilter>();

            options.IncludeXmlComments(FileUtility.CombinePath(AppDomain.CurrentDomain.BaseDirectory, "Items.WebAPI.xml"));
        }
    }
}