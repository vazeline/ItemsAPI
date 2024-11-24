using System.ComponentModel.DataAnnotations;

namespace Items.DTO.Items.Request
{
    public class ItemsInsertRequestDTO
    {
        [Required]
        public ItemDTO[] Items { get; set; }
    }
}
