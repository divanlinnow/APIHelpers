using System.Collections.Generic;

namespace SendGrid.Templates
{
    public abstract class EmailBase : IEmail
    {
        protected EmailBase()
        {
            Substitutions = new Dictionary<string, string>();
        }

        public EmailBase(
            string senderName,
            string senderAddress,
            string recipientAddress, string recipientName
            )
        {
            RecipientAddress = recipientAddress;
            RecipientName = recipientName;
            SenderName = senderName;
            SenderAddress = senderAddress;
        }

        public string RecipientAddress { get; set; }

        public string RecipientName { get; set; }

        public string SenderAddress { get; set; }

        public string SenderName { get; set; }

        public IDictionary<string, string> Substitutions { get; set; }

        public string TemplateId { get; set; }
    }
}
