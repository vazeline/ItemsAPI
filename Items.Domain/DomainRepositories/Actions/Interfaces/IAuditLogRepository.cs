using Items.Domain.DomainEntities;
using Items.Data.EFCore.Abstraction.Interfaces;

namespace Items.Domain.DomainRepositories.Actions.Interfaces
{
    public interface IAuditLogRepository : IBaseRepository<AuditLog>
    {
    }
}