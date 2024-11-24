using System.Collections.Generic;
using System.Threading.Tasks;
using Items.Data.EFCore.Abstraction;
using Items.Domain.DomainRepositories.Actions.Interfaces;
using Items.Domain.DomainRepositories.Interfaces;
using Items.Domain.DomainEntities;

namespace Items.Domain.DomainRepositories.Actions
{
    public class ItemAuditLogRepository : BaseRepository<AuditLog, IItemsUnitOfWork>, IAuditLogRepository
    {
        public ItemAuditLogRepository(IItemsUnitOfWork unitOfWork)
            : base(unitOfWork)
        {
        }
    }
}