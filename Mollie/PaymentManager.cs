using Microsoft.Extensions.Configuration;
using Mollie.Api.Client;
using Mollie.Api.Models;
using Mollie.Api.Models.Customer;
using Mollie.Api.Models.Mandate;
using Mollie.Api.Models.Payment;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Payment.Response;
using Mollie.Api.Models.Refund;
using Mollie.Api.Models.Subscription;
using System;
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
        /// Creates a payment.
        /// </summary>
        /// <param name="customerId">The customer's Mollie-specific id</param>
        /// <param name="description">The payment description. This will be shown on the user's card or bank statement when possible.</param>
        /// <param name="amount">The payment amount.</param>
        /// <param name="redirectUrl">The URL that the user will be redirected to after the payment process.</param>
        /// <param name="webhookUrl">The url that Mollie calls with the payment id when the payment status changes. 
        /// The webhookUrl must be reachable from Mollie’s point of view, so you cannot use localhost. 
        /// If you want to use webhook during development on localhost, you must use a tool like ngrok to have the webhooks delivered to your local machine.</param>
        /// <returns>A PaymentResponse</returns>
        public async Task<PaymentResponse> CreatePaymentAsync(string customerId, string description, decimal amount, string redirectUrl, string webhookUrl)
        {
            var mollieSettings = _configuration.GetSection("Mollie");
            var apiKey = mollieSettings.GetValue<string>("APIKey");
            var currency = mollieSettings.GetValue<string>("DefaultCurrency");
            var paymentClient = GetPaymentClient();

            var paymentRequest = new PaymentRequest
            {
                CustomerId = customerId,
                Amount = new Amount
                {
                    Currency = currency,
                    Value = amount.ToString()
                },
                Description = description,
                RedirectUrl = redirectUrl,
                WebhookUrl = webhookUrl
            };

            return await paymentClient.CreatePaymentAsync(paymentRequest);
        }

        /// <summary>
        /// Process the response from Mollie API regarding the payment status.
        /// </summary>
        /// <param name="paymentId">The payment Id</param>
        /// <returns></returns>
        public async Task ProcessPaymentWebhookAsync(string paymentId)
        {
            var paymentClient = GetPaymentClient();
            PaymentResponse paymentResponse = await paymentClient.GetPaymentAsync(paymentId);

            // Process customer payment record on your side here. 
            // For example, like such : 



            //// Get our CustomerPayment record
            //CustomerPayment customerPayment = await _customerPaymentRepository.FirstOrDefaultAsync(c => c.ProviderPaymentId == providerPaymentId);

            //if (customerPayment != null)
            //{
            //    // Get payment response from Mollie
            //    var paymentClient = GetPaymentClient();
            //    PaymentResponse paymentResponse = await paymentClient.GetPaymentAsync(paymentId);

            //    // Update your customer payment record
            //    customerPayment.Method = paymentResponse.Method;
            //    customerPayment.Status = paymentResponse.Status;

            //    // Check and set subscription status
            //    if ((customerPayment.Status == PaymentStatus.Expired) || (customerPayment.Status == PaymentStatus.Canceled) || (customerPayment.Status == PaymentStatus.Failed))
            //    {
            //        if (customerPayment.SubscriptionId != null)
            //        {
            //            var subscription = _subscriptionRepository.FirstOrDefault(s => s.Id == customerPayment.SubscriptionId);
            //            subscription.Status = SubscriptionStatus.Deactivated;
            //            await _subscriptionRepository.UpdateAsync(subscription);
            //        }
            //    }

            //    if (customerPayment.Status == PaymentStatus.Paid)
            //    {
            //        if (customerPayment.SubscriptionId != null)
            //        {
            //            var subscription = _subscriptionRepository.FirstOrDefault(s => s.Id == customerPayment.SubscriptionId);
            //            subscription.Status = Entities.SubscriptionStatus.Active;
            //            await _subscriptionRepository.UpdateAsync(subscription);
            //        }
            //    }

            //    await _customerPaymentRepository.UpdateAsync(customerPayment);
            //}

        }

        public async Task<bool> CreateSubscriptionAsync(string customerId, string customerName, string iban, PaymentMethod paymentMethod, string description, DateTime startDate, decimal amount, string interval, int? times, string webhookUrl)
        {
            var result = false;

            var mandateClient = GetMandateClient();

            // Create "mandate" via Mollie to be able to create a customer subscription.
            // Mandates allow you to charge a customer’s credit card or bank account recurrently.
            var mandateRequest = new MandateRequest
            {
                ConsumerName = customerName,
                ConsumerAccount = iban,
                Method = paymentMethod
            };

            MandateResponse mandateResponse = await mandateClient.CreateMandateAsync(customerId, mandateRequest);

            if (mandateResponse.Status == MandateStatus.Valid)
            {

                // Create your customer payment record here

                var mollieSettings = _configuration.GetSection("Mollie");
                var currency = mollieSettings.GetValue<string>("DefaultCurrency");

                var subscriptionRequest = new SubscriptionRequest
                {
                    Amount = new Amount
                    {
                        Currency = currency,
                        Value = amount.ToString()
                    },
                    Description = description,
                    StartDate = startDate,
                    Method = paymentMethod,
                    Interval = interval, // Interval to wait between charges like 1 month(s) or 14 days
                    Times = times, // Total number of charges for the subscription to complete. Leave empty for an on-going subscription
                    WebhookUrl = webhookUrl // The url that Mollie calls with the payment id when the payment status changes. 
                };

                var subscriptionClient = GetSubscriptionClient();

                SubscriptionResponse subscriptionResponse = await subscriptionClient.CreateSubscriptionAsync(customerId, subscriptionRequest);

                if (subscriptionResponse.Status == SubscriptionStatus.Active)
                {
                    // Update your customer payment record here with new subscription Id

                    result = true;
                }
            }

            return result;
        }
        
        public async Task CancelSubscriptionAsync(string customerId, string subscriptionId)
        {
            var subscriptionClient = GetSubscriptionClient();

            await subscriptionClient.CancelSubscriptionAsync(customerId, subscriptionId);
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
