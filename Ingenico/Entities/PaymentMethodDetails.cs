using Ingenico.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ingenico.Entities
{
    public class PaymentMethodDetails
    {
        public int SubscriptionId { get; set; }

        public PaymentType Type { get; set; }

        public int? PaymentProductId { get; set; }

        #region Card

        public string CardNumber { get; set; }

        public string CardHolderName { get; set; }

        public string CardExpiryDate { get; set; }

        #endregion

        #region Mandate/SEPA Direct Debit

        public string MandateStatus { get; set; }

        public string MandateRecurrenceType { get; set; }

        public string MandateCustomerReference { get; set; }

        public string MandateBankAccountHolderName { get; set; }

        public string MandateBankAccountIban { get; set; }

        public string MandateCompanyName { get; set; }

        public string MandateContactEmail { get; set; }

        #endregion
    }
}
