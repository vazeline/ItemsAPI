using Common.Models;
using Items.Domain.DomainEntities;
using Items.Domain.DomainRepositories.Interfaces;
using Items.Domain.DomainRepositories.Actions.Interfaces;
using Items.BusinessLogic.Services.AuditLog.Interfaces;
using System.Threading.Tasks;
using Items.GenericServices;
using AutoMapper;
using Items.BusinessLogic.Services.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Items.Domain.DomainEntityBehaviours.Actions
{
    public class AuditLogBusinessLogic : GenericBusinessLogic<AuditLog, IItemsUnitOfWork, IAuditLogRepository>, IAuditLogBusinessLogic
    {

        private readonly IItemsUnitOfWork unitOfWork;

        public AuditLogBusinessLogic(
            IItemsUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ItemBusinessLogic> logger,
            IHttpContextAccessor httpContextAccessor )
            : base( unitOfWork, mapper, logger, httpContextAccessor )
        {
            this.unitOfWork = unitOfWork;
        }

        public override IAuditLogRepository Repository => this.unitOfWork.AuditLogRepository;

        public async Task<OperationResult> AddActionAuditLogEntry( string requestUri, int status,
            string method, string ipAddress = null )
        {
            var insertResult = await this.CreateSingleEntityAndSaveAsync(
                unitofwork => Task.FromResult(AuditLog.Create( requestUri, status, method, ipAddress )));

            return insertResult;
        }
    }
}
