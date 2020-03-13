using System.Collections.Generic;

namespace SendGrid.Templates
{
    public interface IEmail
    {
        string RecipientName { get; set; }

        string RecipientAddress { get; set; }

        string TemplateId { get; set; }

        string SenderName { get; set; }

        string SenderAddress { get; set; }

        IDictionary<string, string> Substitutions { get; set; }
    }
}
