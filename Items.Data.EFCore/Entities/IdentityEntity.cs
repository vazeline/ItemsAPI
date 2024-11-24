using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Items.Data.EFCore.Entities.Interfaces;

namespace Items.Data.EFCore.Entities
{
    public abstract class IdentityEntity : DomainEntityBase, IIdentity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
    }
}
