using Microsoft.Extensions.Configuration;
using Mollie.Api.Client;
using Mollie.Api.Models;
using Mollie.Api.Models.Customer;
using Mollie.Api.Models.Refund;
using System.Threading.Tasks;

namespace Mollie
{
    public class PaymentManager : IPaymentManager
    {
        private readonly IConfiguration _configuration;

        public PaymentManager(
            IConfiguration configuration
            )
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Creates a Mollie-specific customer from the given customer entity details.
        /// </summary>
        /// <param name="customerName">The customer's name</param>
        /// <param name="customerEmail">The customer's email</param>
        /// <param name="customerLocale">The customer's locale</param>
        /// <returns>A Mollie-specific customer</returns>
        public async Task<CustomerResponse> CreateCustomerAsync(string customerName, string customerEmail, string customerLocale)
        {
            var customerClient = GetCustomerClient();

            var customerRequest = new CustomerRequest
            {
                Name = customerName,
                Email = customerEmail,
                Locale = GetLocale(customerLocale)
            };

            return await customerClient.CreateCustomerAsync(customerRequest);
        }

        /// <summary>
        /// Updates a customer's Mollie-specific customer record.
        /// </summary>
        /// <param name="customerId">The customer's Mollie-specific id</param>
        /// <param name="customerName">The customer's name</param>
        /// <param name="customerEmail">The customer's email</param>
        /// <param name="customerLocale">The customer's locale</param>
        /// <returns></returns>
        public async Task UpdateCustomerAsync(string customerId, string customerName, string customerEmail, string customerLocale)
        {
            var customerClient = GetCustomerClient();

            var customerRequest = new CustomerRequest
            {
                Name = customerName,
                Email = customerEmail,
                Locale = GetLocale(customerLocale)
            };

            if (!string.IsNullOrEmpty(customerId))
            {
                await customerClient.UpdateCustomerAsync(customerId, customerRequest);
            }
        }
























        /// <summary>
        /// Creates a refund.
        /// </summary>
        /// <param name="paymentId">The ID of the payment to refund.</param>
        /// <param name="description">A description/reason for the refund.</param>
        /// <returns>A RefundResponse</returns>
        public async Task<RefundResponse> CreateRefundAsync(string paymentId, string description)
        {
            var paymentClient = GetPaymentClient();
            var refundClient = GetRefundClient();
            var paymentToRefund = await paymentClient.GetPaymentAsync(paymentId);

            var refundRequest = new RefundRequest
            {
                Amount = new Amount(paymentToRefund.Amount.Currency, paymentToRefund.Amount.Value),
                Description = description
            };

            return await refundClient.CreateRefundAsync(paymentId, refundRequest);
        }

        /// <summary>
        /// Cancels a refund.
        /// </summary>
        /// <param name="paymentId">The payment ID of the refund to cancel.</param>
        /// <returns></returns>
        public async Task CancelRefundAsync(string paymentId)
        {
            var refundClient = GetRefundClient();
            var refundList = await refundClient.GetRefundListAsync(paymentId, null);

            if (refundList.Count > 0)
            {
                var refundResponse = refundList.Items.Find(x => x.PaymentId == paymentId);

                if (refundResponse != null)
                {
                    await refundClient.CancelRefundAsync(paymentId, refundResponse.Id);
                }
            }
        }

        /// <summary>
        /// Gets the Mollie customer client.
        /// </summary>
        /// <returns>The Mollie customer client.</returns>
        private CustomerClient GetCustomerClient()
        {
            var mollieSettings = _configuration.GetSection("Mollie");
            var apiKey = mollieSettings.GetValue<string>("APIKey");

            return new CustomerClient(apiKey);
        }

        /// <summary>
        /// Gets the Mollie payment client.
        /// </summary>
        /// <returns>The Mollie payment client.</returns>
        private PaymentClient GetPaymentClient()
        {
            var mollieSettings = _configuration.GetSection("Mollie");
            var apiKey = mollieSettings.GetValue<string>("APIKey");

            return new PaymentClient(apiKey);
        }

        /// <summary>
        /// Gets the Mollie mandate client.
        /// </summary>
        /// <returns>The Mollie mandate client.</returns>
        private MandateClient GetMandateClient()
        {
            var mollieSettings = _configuration.GetSection("Mollie");
            var apiKey = mollieSettings.GetValue<string>("APIKey");

            return new MandateClient(apiKey);
        }

        /// <summary>
        /// Gets the Mollie subscription client.
        /// </summary>
        /// <returns>The Mollie subscription client.</returns>
        private SubscriptionClient GetSubscriptionClient()
        {
            var mollieSettings = _configuration.GetSection("Mollie");
            var apiKey = mollieSettings.GetValue<string>("APIKey");

            return new SubscriptionClient(apiKey);
        }

        /// <summary>
        /// Gets the Mollie refund client.
        /// </summary>
        /// <returns>The Mollie refund client.</returns>
        private RefundClient GetRefundClient()
        {
            var mollieSettings = _configuration.GetSection("Mollie");
            var apiKey = mollieSettings.GetValue<string>("APIKey");

            return new RefundClient(apiKey);
        }

        /// <summary>
        /// Converts a locale to an Mollie-specific locale.
        /// Mollie locale uses underscore, i.e en_US.
        /// </summary>
        /// <param name="locale">The locale to convert.</param>
        /// <returns>A Mollie-specific locale.</returns>
        private string GetLocale(string locale)
        {
            if (string.IsNullOrEmpty(locale))
            {
                var appSettings = _configuration.GetSection("App");
                var defaultLocale = appSettings.GetValue<string>("DefaultLocale");
                locale = defaultLocale;
            }

            switch (locale.ToLower())
            {
                case "nl":
                    return "nl_NL";
                case "fr":
                    return "fr_FR";
                case "de":
                    return "de_DE";
                default:
                    return "en_US";
            }
        }
    }
}
