using Ingenico.Connect.Sdk;
using Ingenico.Connect.Sdk.Domain.Definitions;
using Ingenico.Connect.Sdk.Domain.Hostedcheckout;
using Ingenico.Connect.Sdk.Domain.Hostedcheckout.Definitions;
using Ingenico.Connect.Sdk.Domain.Mandates.Definitions;
using Ingenico.Connect.Sdk.Domain.Payment.Definitions;
using Ingenico.Entities.Enums;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ingenico
{
    public class PaymentManager
    {
        /// <summary>
        /// Ingenico payment product ids for card payment methods that we include in the hosted checkout
        /// </summary>
        private static readonly IList<int?> PaymentProductIncludeIds = new List<int?>
        {
            1, // VISA
            3, // MasterCard
            771 // Sepa Direct Debit
        };

        private readonly IConfiguration _configuration;

        public PaymentManager(
            IConfiguration configuration
            )
        {
            _configuration = configuration;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="customerPayment"></param>
        /// <param name="returnUrl">The URL that the user will be redirected to after the payment process.</param>
        /// <returns></returns>
        public async Task<string> CreateHostedCheckout(Entities.Customer customer, Entities.CustomerPayment customerPayment, string returnUrl)
        {
            var result = string.Empty;

            using (Client client = GetClient())
            {
                var ingenicoSettings = _configuration.GetSection("Ingenico");
                var merchantId = ingenicoSettings.GetValue<string>("MerchantID");
                var hostedCheckoutVariant = ingenicoSettings.GetValue<string>("HostedCheckoutVariant");
                var currency = string.IsNullOrEmpty(customerPayment.Currency) ? ingenicoSettings.GetValue<string>("DefaultCurrency") : customerPayment.Currency;
                var locale = string.IsNullOrEmpty(customer.Locale) ? ingenicoSettings.GetValue<string>("DefaultLocale") : customer.Locale;

                var hostedCheckoutSpecificInput = new HostedCheckoutSpecificInput
                {
                    IsRecurring = false,
                    Locale = locale,
                    Variant = hostedCheckoutVariant,
                    ReturnCancelState = true,
                    PaymentProductFilters = new PaymentProductFiltersHostedCheckout()
                    {
                        // Only allow certain payment methods
                        RestrictTo = new PaymentProductFilter
                        {
                            Products = PaymentProductIncludeIds
                        }
                    }
                };

                var amountOfMoney = new AmountOfMoney
                {
                    Amount = ConvertToIngenicoAmount(customerPayment.Total == 0 ? 0.01m : customerPayment.Total), // Amount cannot be zero. Amount has to be at least 1 cent. 
                    CurrencyCode = currency
                };

                var order = new Order
                {
                    Customer = CreateIngenicoCustomer(customer),
                    AmountOfMoney = amountOfMoney,
                    References = new OrderReferences
                    {
                        // Update Order with CustomerPayment Id as reference
                        MerchantReference = customerPayment.MerchantReference.ToString()
                    }
                };

                // Create Hosted Checkout for recurring direct debit via credit card or Sepa direct debit

                var cardPaymentMethodSpecificInput = new CardPaymentMethodSpecificInputBase
                {
                    Tokenize = true,
                    Recurring = new CardRecurrenceDetails
                    {
                        RecurringPaymentSequenceIndicator = "first"
                    },
                    AuthorizationMode = "SALE",
                    RequiresApproval = false
                };

                var sepaDirectDebitPaymentMethodSpecificInput = new SepaDirectDebitPaymentMethodSpecificInputBase()
                {
                    PaymentProduct771SpecificInput = new SepaDirectDebitPaymentProduct771SpecificInputBase()
                    {
                        Mandate = new CreateMandateBase()
                        {
                            CustomerReference = order.Customer.MerchantCustomerId,
                            RecurrenceType = "RECURRING",
                            SignatureType = "SMS"
                        },
                    }
                };

                var body = new CreateHostedCheckoutRequest
                {
                    HostedCheckoutSpecificInput = hostedCheckoutSpecificInput,
                    Order = order,
                    CardPaymentMethodSpecificInput = cardPaymentMethodSpecificInput,
                    SepaDirectDebitPaymentMethodSpecificInput = sepaDirectDebitPaymentMethodSpecificInput
                };

                try
                {
                    // Initial call to Ingenico API to create hosted checkout.
                    CreateHostedCheckoutResponse createHostedCheckoutResponse = await client.Merchant(merchantId).Hostedcheckouts().Create(body);

                    if (createHostedCheckoutResponse.MerchantReference == customerPayment.MerchantReference.ToString())
                    {
                        // Do initial call to Ingenico API to retreive status of payment
                        GetHostedCheckoutResponse getHostedCheckoutResponse = await client.Merchant(merchantId).Hostedcheckouts().Get(createHostedCheckoutResponse.HostedCheckoutId);

                        // Update CustomerPayment record
                        customerPayment.Status = getHostedCheckoutResponse.Status;
                        //_customerPaymentRepository.Update(customerPayment);
                    }

                    // Build up redirect URL
                    var subdomain = ingenicoSettings.GetValue<string>("Subdomain");
                    result = $"{subdomain}{createHostedCheckoutResponse.PartialRedirectUrl}";
                }
                catch (Exception ex)
                {
                    //Logger.Error("Exception at : PaymentManager.CreateHostedCheckout\n", ex);

                    // Delete Customer Payment records, since initial payment failed
                    //_customerPaymentRepository.Delete(customerPayment.Id);
                }
            }

            return result;
        }

        private Client GetClient()
        {
            var ingenicoSettings = _configuration.GetSection("Ingenico");
            var apiKeyId = ingenicoSettings.GetValue<string>("APIKeyId");
            var apiKeySecret = ingenicoSettings.GetValue<string>("APIKeySecret");
            var endpoint = ingenicoSettings.GetValue<string>("Endpoint");

            CommunicatorConfiguration configuration = Factory.CreateConfiguration(apiKeyId, apiKeySecret);
            configuration.ApiEndpoint = new Uri(endpoint);
            return Factory.CreateClient(configuration);
        }

        /// <summary>
        /// Converts a decimal amount to cents for Ingenico API.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        private long ConvertToIngenicoAmount(decimal amount)
        {
            var result = amount * 100;
            result = Math.Round(result, 2);
            return (long)result;
        }

        private Customer CreateIngenicoCustomer(Entities.Customer customer)
        {
            var ingenicoCustomer = new Customer
            {
                MerchantCustomerId = customer.Id.ToString(),
                BillingAddress = new Address()
            };

            var address = customer.Addresses.Where(a => a.Type == AddressType.Billing).FirstOrDefault();

            if (address != null)
            {
                ingenicoCustomer.BillingAddress = GetIngenicoBillingAddress(address);
            }

            if (string.IsNullOrEmpty(customer.Locale))
            {
                var ingenicoSettings = _configuration.GetSection("Ingenico");
                var defaultLocale = ingenicoSettings.GetValue<string>("DefaultLocale");
                ingenicoCustomer.Locale = defaultLocale;
            }

            return ingenicoCustomer;
        }

        private Address GetIngenicoBillingAddress(Entities.Address inputAddress)
        {
            var address = new Address
            {
                HouseNumber = string.Empty,
                Street = inputAddress.AddressLine1,
                City = inputAddress.City,
                Zip = inputAddress.PostalCode,
                CountryCode = inputAddress.Country.ISO2
            };

            return address;
        }
    }
}
