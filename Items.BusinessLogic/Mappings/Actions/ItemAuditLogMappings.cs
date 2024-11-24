using AutoMapper;
using Items.Domain.DomainEntities;
using Items.DTO.Items.ItemAuditLog.Response;

namespace Items.BusinessLogic.Mappings.Actions
{
    public class ItemAuditLogMappings : Profile
    {
        public ItemAuditLogMappings()
        {
            this.CreateMap<AuditLog, AuditLogDTO>();
        }
    }
}