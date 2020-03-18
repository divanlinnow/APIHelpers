using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ingenico.Entities
{
    public class Customer : Entity
    {
        /// <summary>
        /// The Locale used by Ingenico.
        /// Locale has to be in the following format : 
        /// en_US, en_GB, nl_NL, de_DE, fr_FR, etc.  
        /// </summary>
        public string Locale { get; set; }

        [Required]
        [MaxLength(256)]
        [DataType(DataType.EmailAddress)]
        public virtual string Email { get; set; }

        [MaxLength(32)]
        [DataType(DataType.PhoneNumber)]
        public virtual string PhoneNumber { get; set; }

        public virtual List<Address> Addresses { get; set; }
    }
}
