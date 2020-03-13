using SendGrid.Templates;
using System.Collections.Generic;

namespace SendGrid
{
    public interface IEmailManager
    {
        void Send(EmailBase message);

        void Send(IEnumerable<EmailBase> messages);
    }
}
