using Microsoft.Extensions.Configuration;
using SendGrid.Helpers.Mail;
using SendGrid.Templates;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SendGrid
{
    public class EmailManager : IEmailManager
    {
        private readonly IConfiguration _configuration;

        public EmailManager(
            IConfiguration configuration
            )
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Sends a single email message.
        /// </summary>
        /// <param name="message">The email message to send.</param>
        public void Send(EmailBase message)
        {
            var task = SendEmail(message);
            task.Wait();
        }

        /// <summary>
        /// Sends multiple email messages.
        /// </summary>
        /// <param name="messages">The email messages to send.</param>
        public void Send(IEnumerable<EmailBase> messages)
        {
            var tasks = new List<Task>();
            messages.ToList().ForEach(x => tasks.Add(SendEmail(x)));

            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// Gets the SendGrid client.
        /// </summary>
        /// <returns>The SendGrid client.</returns>
        private SendGridClient GetClient()
        {
            var sendgridSettings = _configuration.GetSection("Sendgrid");
            return new SendGridClient(sendgridSettings.GetValue<string>("APIKey"));
        }

        /// <summary>
        /// Generates a SendGrid-specific message from the given email message.
        /// </summary>
        /// <param name="message">The relevant email message to be converted to a SendGrid-specific message.</param>
        /// <returns></returns>
        private SendGridMessage GenerateMessage(EmailBase message)
        {
            var sender = new EmailAddress(message.SenderAddress, message.SenderName);
            var recipient = new EmailAddress(message.RecipientAddress, message.RecipientName);
            var sendGridMessage = new SendGridMessage();

            sendGridMessage.SetFrom(sender);
            sendGridMessage.AddTo(recipient);
            sendGridMessage.SetTemplateId(message.TemplateId);

            foreach (var substitution in message.Substitutions)
            {
                sendGridMessage.AddSubstitution(substitution.Key, substitution.Value);
            }

            return sendGridMessage;
        }

        /// <summary>
        /// Asynchronously sends a single email message.
        /// </summary>
        /// <param name="message">The email message to send.</param>
        /// <returns></returns>
        private async Task SendEmail(EmailBase message)
        {
            var sendGridClient = GetClient();
            var mail = GenerateMessage(message);

            var response = await sendGridClient.SendEmailAsync(mail);

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Accepted)
            {
                var errorMessage = $"Error sending email to : {message.RecipientAddress} \n";

                //Logger.Error(errorMessage);
            }
        }
    }
}
