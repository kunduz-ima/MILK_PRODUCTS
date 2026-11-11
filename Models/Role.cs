using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MILK_PRODUCTS.Models
{
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public short ID { get; set; }
        [Required]
        [StringLength(50)]
        public string RoleName { get; set; }
        
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
