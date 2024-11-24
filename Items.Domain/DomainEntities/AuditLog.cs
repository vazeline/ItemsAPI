using System;
using Items.Domain.DomainRepositories.Interfaces;
using Common.Models;
using Items.Data.EFCore.ExtensionMethods;
using Common.ExtensionMethods;

namespace Items.Domain.DomainEntities
{
    public class AuditLog : ItemsDomainEntityBase
    {
        internal AuditLog()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditLog"/> class for <see cref="Create"/>.
        /// </summary>
        private AuditLog(
            string request,
            int status,
            string method,
            string ipAddress)
        {
            this.RequestUri = request;
            this.StatusCode = status;
            this.Method = method;
            this.IpAddress = ipAddress;
            this.EventUtcDateTime = DateTime.UtcNow;
        }

        public DateTime EventUtcDateTime { get; internal set; }

        public string RequestUri { get; internal set; }

        public int StatusCode { get; internal set; }

        public string IpAddress { get; internal set; }

        public string Method { get; internal set; }

        public static OperationResult<AuditLog> Create(
            string uri,
            int status,
            string method,
            string ipAddress = null)
        {
            var result = new OperationResult<AuditLog>();

            result
                .Validate(uri, ValidationExtensions.StringIsNotNullOrWhiteSpace)
                .Validate(status, ValidationExtensions.IsGreaterThanOrEqualTo, 100)
                .Validate(status, ValidationExtensions.IsLessThanOrEqualTo, 599)
                .Validate(method, ValidationExtensions.StringIsNotNullOrWhiteSpace );

            if (!result.IsSuccessful)
            {
                return result;
            }

            result.Data = new AuditLog(
                uri,
                status,
                method,
                ipAddress);

            return result;
        }

        internal void Delete(IItemsUnitOfWork unitOfWork)
        {
            unitOfWork.AuditLogRepository.Remove(this);
        }
    }
}
