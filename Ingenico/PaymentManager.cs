using Ingenico.Connect.Sdk;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ingenico
{
    public class PaymentManager
    {
        private readonly IConfiguration _configuration;

        public PaymentManager(
            IConfiguration configuration
            )
        {
            _configuration = configuration;
        }

        //public async Task<string> CreateHostedCheckout(PriceCalculation priceCalculation, Subscription subscription, string locale, string purhcaseProcessId)
        //{
        //    var result = string.Empty;

        //    using (Client client = GetClient())
        //    {
        //        var ingenicoSettings = _configuration.GetSection("Ingenico");
        //        var merchantId = ingenicoSettings.GetValue<string>("MerchantID");
        //        var variant = ingenicoSettings.GetValue<string>("HostedCheckoutVariant");
        //        var appConfigurations = _configuration.GetSection("App");
        //        var returnUrl = $"{appConfigurations.GetValue<string>("WebSiteRootAddress")}{PerSafeConsts.SubscriptionPaymentCheckPageUrl}?id={purhcaseProcessId}"; // Subscription Payment Check page
        //        var userLocale = GetLocale(locale);

        //        var hostedCheckoutSpecificInput = new HostedCheckoutSpecificInput
        //        {
        //            IsRecurring = false,
        //            Locale = userLocale,
        //            Variant = variant,
        //            ReturnCancelState = true,
        //            ReturnUrl = returnUrl, // The URL that the user will be redirected to after the payment process.
        //            PaymentProductFilters = new PaymentProductFiltersHostedCheckout()
        //            {
        //                // Exclude certain payment methods
        //                RestrictTo = new PaymentProductFilter
        //                {
        //                    Products = PerSafeConsts.PaymentProductIncludeIds
        //                }
        //            }
        //        };

        //        // Create initial pro-forma invoice
        //        Logger.Info($"PaymentManager.CreateHostedCheckout => Creating invoice\n");
        //        var invoice = await _invoiceManager.CreateInvoice(priceCalculation, subscription, true);

        //        // Create Persafe customer payment record
        //        Logger.Info($"PaymentManager.CreateHostedCheckout => Creating customer payment record\n");
        //        var customerPayment = await CreateCustomerPaymentRecord(priceCalculation, subscription);

        //        // Create many-to-many InvoicePayments record
        //        var invoicePayment = new InvoicePayment()
        //        {
        //            InvoiceId = invoice.Id,
        //            CustomerPaymentId = customerPayment.Id
        //        };

        //        // Update invoice
        //        Logger.Info($"PaymentManager.CreateHostedCheckout => Updating invoice\n");
        //        invoice.InvoicePayments.Add(invoicePayment);
        //        await _invoiceManager.Update(invoice);

        //        // Update customerpayment
        //        Logger.Info($"PaymentManager.CreateHostedCheckout => Updating customerpayment\n");
        //        customerPayment.InvoicePayments.Add(invoicePayment);
        //        await _customerPaymentRepository.UpdateAsync(customerPayment);

        //        var amountOfMoney = new AmountOfMoney
        //        {
        //            Amount = ConvertToIngenicoAmount(customerPayment.Total == 0 ? 0.01m : customerPayment.Total),
        //            CurrencyCode = customerPayment.Currency
        //        };

        //        var order = new Order
        //        {
        //            Customer = CreateProviderCustomer(priceCalculation.Customer, locale),
        //            AmountOfMoney = amountOfMoney,
        //            References = new OrderReferences
        //            {
        //                // Update Order with PerSafe Customer Payment Id as reference
        //                MerchantReference = customerPayment.MerchantReference.ToString()
        //            }
        //        };

        //        var cardPaymentMethodSpecificInput = new CardPaymentMethodSpecificInputBase
        //        {
        //            Tokenize = true,
        //            Recurring = new CardRecurrenceDetails
        //            {
        //                RecurringPaymentSequenceIndicator = "first"
        //            },
        //            AuthorizationMode = "SALE",
        //            RequiresApproval = false
        //        };

        //        var sepaDirectDebitPaymentMethodSpecificInput = new SepaDirectDebitPaymentMethodSpecificInputBase()
        //        {
        //            PaymentProduct771SpecificInput = new SepaDirectDebitPaymentProduct771SpecificInputBase()
        //            {
        //                Mandate = new CreateMandateBase()
        //                {
        //                    CustomerReference = order.Customer.MerchantCustomerId,
        //                    RecurrenceType = "RECURRING",
        //                    SignatureType = "SMS"
        //                },
        //            }
        //        };

        //        var body = new CreateHostedCheckoutRequest
        //        {
        //            HostedCheckoutSpecificInput = hostedCheckoutSpecificInput,
        //            Order = order,
        //            CardPaymentMethodSpecificInput = cardPaymentMethodSpecificInput,
        //            SepaDirectDebitPaymentMethodSpecificInput = sepaDirectDebitPaymentMethodSpecificInput
        //        };

        //        try
        //        {
        //            // Initial call to Ingenico API to create hosted checkout
        //            Logger.Info($"PaymentManager.CreateHostedCheckout => Initial call to Ingenico API to create hosted checkout\n");
        //            CreateHostedCheckoutResponse createHostedCheckoutResponse = await client.Merchant(merchantId).Hostedcheckouts().Create(body);

        //            if (createHostedCheckoutResponse.MerchantReference == customerPayment.MerchantReference.ToString())
        //            {
        //                // Do initial call to Ingenico API to retreive status of payment
        //                GetHostedCheckoutResponse getHostedCheckoutResponse = await client.Merchant(merchantId).Hostedcheckouts().Get(createHostedCheckoutResponse.HostedCheckoutId);

        //                // Update PerSafe Customer Payment record
        //                customerPayment.Status = getHostedCheckoutResponse.Status;
        //                _customerPaymentRepository.Update(customerPayment);
        //            }

        //            var subdomain = ingenicoSettings.GetValue<string>("Subdomain");
        //            result = $"{subdomain}{createHostedCheckoutResponse.PartialRedirectUrl}";
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error("Exception at : PaymentManager.CreateHostedCheckout\n", ex);

        //            // Delete Customer Payment records, since initial payment failed
        //            _customerPaymentRepository.Delete(customerPayment.Id);

        //            // Delete invoice, since initial payment failed
        //            // This should also delete many-to-many invoicepayment record
        //            await _invoiceManager.Delete(invoice.Id);
        //        }
        //    }

        //    return result;
        //}

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


    }
}
