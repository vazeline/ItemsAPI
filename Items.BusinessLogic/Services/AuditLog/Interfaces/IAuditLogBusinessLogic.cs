using Items.Domain.DomainEntities;
using Common.Models;
using System.Threading.Tasks;

namespace Items.BusinessLogic.Services.AuditLog.Interfaces
{
    public interface IAuditLogBusinessLogic
    {
        Task<OperationResult> AddActionAuditLogEntry(
            string requestUri, int statusCode,
            string method, string ipAddress = null);

    }
}
