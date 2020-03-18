using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ingenico.Entities
{
    public class CustomerPayment : Entity
    {
        [Required]
        public virtual int CustomerId { get; set; }

        /// <summary>
        /// The merchant reference that gets given to Ingenico to help identify the customer payment record on Ingenico's API.
        /// Can't simply use the CustomerPayment Id, since this gets used across all Ingenico environments (dev and test) and Ids could overlap across environments.
        /// </summary>
        public virtual Guid? MerchantReference { get; set; }

        /// <summary>
        /// The Ingenico payment Id.
        /// </summary>
        [MaxLength(100)]
        public virtual string PaymentId { get; set; }

        /// <summary>
        /// The Ingenico payment product Id.
        /// </summary>
        public virtual int? PaymentProductId { get; set; }

        /// <summary>
        /// The Ingenico payment method identifier.
        /// </summary>
        [MaxLength(100)]
        public virtual string PaymenMethod { get; set; }

        [MaxLength(4)]
        public virtual string Currency { get; set; }

        /// <summary>
        /// The Ingenico payment status.
        /// Current high-level status of the payment in a human-readable form.
        /// </summary>
        public virtual string Status { get; set; }

        /// <summary>
        /// The Ingenico status category.
        /// Current high-level status category of the payment in a human-readable form.
        /// </summary>
        public virtual string StatusCategory { get; set; }

        /// <summary>
        /// The token given by Ingenico to identify the payment method.
        /// Token is given for card payment method. 
        /// </summary>
        [MaxLength(100)]
        public virtual string Token { get; set; }

        /// The mandate reference given by Ingenico to identify the payment method.
        /// Mandate reference is given for Sepa Direct Debit payment method. 
        [MaxLength(100)]
        public virtual string MandateReference { get; set; }

        [Required]
        public virtual decimal SubTotal { get; set; }

        [Required]
        public virtual decimal TaxTotal { get; set; }

        [Required]
        public virtual decimal Total { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime CreationDate { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime LastModifiedDate { get; set; }

        #region Navigational Properties

        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; }

        #endregion
    }
}
