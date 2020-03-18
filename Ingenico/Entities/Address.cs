using Ingenico.Entities.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ingenico.Entities
{
    public class Address : Entity
    {
        public virtual int CustomerId { get; set; }

        public virtual AddressType Type { get; set; }

        [MaxLength(100)]
        public virtual string AddressLine1 { get; set; }

        [MaxLength(100)]
        public virtual string AddressLine2 { get; set; }

        [MaxLength(100)]
        public virtual string AddressLine3 { get; set; }

        [MaxLength(20)]
        [DataType(DataType.PostalCode)]
        public virtual string PostalCode { get; set; }

        [MaxLength(100)]
        public virtual string City { get; set; }

        public virtual int CountryId { get; set; }

        #region Navigational Properties

        [ForeignKey("CountryId")]
        public Country Country { get; set; }

        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; }

        #endregion
    }
}
