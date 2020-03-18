using System.ComponentModel.DataAnnotations;

namespace Ingenico.Entities
{
    public class Country : Entity
    {
        [Required]
        [MaxLength(100)]
        [DataType(DataType.Text)]
        public virtual string Name { get; set; }

        [MaxLength(2)]
        public virtual string ISO2 { get; set; }

        [MaxLength(3)]
        public virtual string ISO3 { get; set; }

    }
}
