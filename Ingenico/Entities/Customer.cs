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

        /// <summary>
        /// Payment Product Id.  
        /// When Ingenico API calls to our webhook with payment response, we need to handle it and populate this value. 
        /// </summary>
        public virtual int? PaymentProductId { get; set; }

        /// <summary>
        /// Token.
        /// When Ingenico API calls to our webhook with payment response, we need to handle it and populate this value. 
        /// If customer paid via credit card, then this will be populated. 
        /// This will be used by Ingenico to make recurring payments. 
        /// </summary>
        [MaxLength(100)]
        public virtual string Token { get; set; }

        /// <summary>
        /// MandateReference.
        /// When Ingenico API calls to our webhook with payment response, we need to handle it and populate this value. 
        /// If customer paid via direct debit, then this will be populated. 
        /// This will be used by Ingenico to make recurring payments. 
        /// </summary>
        [MaxLength(100)]
        public virtual string MandateReference { get; set; }
    }
}
