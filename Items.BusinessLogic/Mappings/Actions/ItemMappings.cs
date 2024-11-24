using AutoMapper;
using Item = Items.Domain.DomainEntities.Item;

namespace Items.BusinessLogic.Mappings.Actions
{
    public class ItemMappings : Profile
    {
        public ItemMappings()
        {
            this.CreateMap<Item, DTO.Items.Request.ItemDTO>();
            this.CreateMap<Item, DTO.Items.Response.ItemDTO>();
        }
    }
}