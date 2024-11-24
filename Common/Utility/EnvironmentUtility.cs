using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Common.Utility.Classes.Lazy;
using Microsoft.AspNetCore.Http;

namespace Common.Utility
{
    public static class EnvironmentUtility
    {
        public static readonly string CurrentEnvironmentName;

        public static readonly string EnvironmentNameDevelopment = "development";
        public static readonly string EnvironmentNameStaging = "staging";
        public static readonly string EnvironmentNameProduction = "production";

        private static readonly LazyWithTimeout<string> LazyGetInternalIpAddress = new LazyWithTimeout<string>(GetInternalIpAddress, TimeSpan.FromMinutes(5));
        private static readonly LazyWithTimeout<string> LazyGetExternalIpAddress = new LazyWithTimeout<string>(GetExternalIpAddress, TimeSpan.FromMinutes(5));

        static EnvironmentUtility()
        {
            CurrentEnvironmentName = EnvironmentNameInternal;
            IsInUnitTestMode = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.StartsWith("Microsoft.TestPlatform"));

            Environment.SetEnvironmentVariable("BASE_DIR", AppDomain.CurrentDomain.BaseDirectory);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", CurrentEnvironmentName);
        }

        public static bool IsInUnitTestMode { get; }

        public static bool IsDevelopment => CurrentEnvironmentName == EnvironmentNameDevelopment;

        public static string InternalIpAddress => LazyGetInternalIpAddress.Value;

        public static string ExternalIpAddress => LazyGetExternalIpAddress.Value;

        /// <summary>
        /// Gets the base directory for the current application. If the base directory is a UNC path beginning with "file://", it will be stripped.
        /// </summary>
        public static string ApplicationBaseDirectory
        {
            get
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                if (baseDirectory.StartsWith(@"file:\", StringComparison.OrdinalIgnoreCase))
                {
                    baseDirectory = baseDirectory[6..];
                }

                return baseDirectory;
            }
        }

        private static string EnvironmentNameInternal
        {
            get
            {
                string environmentName;
#if DEBUG
                environmentName = EnvironmentNameDevelopment;
#elif STAGING
                environmentName = EnvironmentNameStaging;
#else
                environmentName = EnvironmentNameProduction;
#endif
                return environmentName;
            }
        }

        public static string GetClientIpAddressFromHttpContext(HttpContext httpContext)
        {
            var ip = httpContext.Connection.RemoteIpAddress.ToString();

            // localhost on IPV6
            if (ip == "::1")
            {
                ip = "127.0.0.1";
            }

            return ip;
        }

        private static string GetInternalIpAddress()
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    var endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint.Address.ToString();
                }
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        // yes I know calling into icanhazip.com looks ridiculous, but it was bought by CloudFlare and is about as reliable as anything!
        private static string GetExternalIpAddress()
        {
            try
            {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                return Task.Run(() => new HttpClient().GetStringAsync("https://ipv4.icanhazip.com")).GetAwaiter().GetResult().Trim();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            }
            catch
            {
                return "Not resolvable";
            }
        }
    }
}
