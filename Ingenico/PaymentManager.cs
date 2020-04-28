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
        public async Task<string> CreateHostedCheckoutAsync(Entities.Customer customer, Entities.CustomerPayment customerPayment, string returnUrl)
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

                // Create Hosted Checkout for recurring monthly direct debit via credit card or Sepa direct debit payment methods for subscription purchase

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

        public async Task ProcessPaymentAsync(Payment payment)
        {
            if (payment.PaymentOutput == null || payment.PaymentOutput.References == null || payment.PaymentOutput.References.MerchantReference == null)
            {
                return;
            }
            else
            {
                // Get customer payment record
                //var customerPayment = await _customerPaymentRepository.GetAll()
                //                                                      .Where(x => x.MerchantReference.HasValue && x.MerchantReference.Value.ToString() == payment.PaymentOutput.References.MerchantReference.ToUpper())
                //                                                      .Include(c => c.InvoicePayments)
                //                                                      .FirstOrDefaultAsync();
                var customerPayment = new Entities.CustomerPayment();

                if (customerPayment == null)
                {
                    return;
                }
                else
                {
                    //var customer = await _customerRepository.FirstOrDefaultAsync(c => c.Id == customerPayment.CustomerId);

                    var customer = new Entities.Customer();

                    if (customer == null || customerPayment == null)
                    {
                        return;
                    }
                    else if ((customer.Token == null || string.IsNullOrEmpty(customer.Token)) && (customer.MandateReference == null || string.IsNullOrEmpty(customer.MandateReference)))
                    {
                        // If the customer was previously invoiced outside the system and has now updated their payment details, then customer.Token and customer.MandateReference will both be null
                        // So the subscription should be set to default to monthly billing and the next billing date should also be set
                        //var subscription = await _subscriptionManager.GetEntityByIdAsync(customerPayment.SubscriptionId);

                        //if (subscription.BillingPeriod == BillingPeriod.DoNotBill)
                        //{
                        //    subscription.BillingPeriod = BillingPeriod.Month;
                        //    subscription.NextPaymentDueDate = CalculateSubscriptionNextPaymentDueDate(Clock.Now, subscription.BillingPeriod);
                        //    subscription.ExpireDate = subscription.NextPaymentDueDate.Value;
                        //    await _subscriptionManager.Update(subscription);
                        //}

                        //// Update customer payment record
                        //customerPayment.PaymentId = payment.Id;
                        //customerPayment.PaymenMethod = payment.PaymentOutput.PaymentMethod;
                        //customerPayment.Status = payment.Status;
                        //customerPayment.StatusCategory = payment.StatusOutput?.StatusCategory;
                        //customerPayment.LastModifiedDate = Clock.Now;

                        //// Card payment
                        //if (payment.PaymentOutput.CardPaymentMethodSpecificOutput != null &&
                        //    payment.PaymentOutput.CardPaymentMethodSpecificOutput.PaymentProductId.HasValue &&
                        //    !string.IsNullOrEmpty(payment.PaymentOutput.CardPaymentMethodSpecificOutput.Token))
                        //{

                        //    customerPayment.PaymentProductId = payment.PaymentOutput.CardPaymentMethodSpecificOutput.PaymentProductId;
                        //    customerPayment.Token = payment.PaymentOutput.CardPaymentMethodSpecificOutput.Token;

                        //    customer.PaymentProductId = payment.PaymentOutput.CardPaymentMethodSpecificOutput.PaymentProductId;
                        //    customer.Token = payment.PaymentOutput.CardPaymentMethodSpecificOutput.Token;
                        //}

                        //// Sepa Direct Debit payment
                        //if (payment.PaymentOutput.SepaDirectDebitPaymentMethodSpecificOutput != null &&
                        //    payment.PaymentOutput.SepaDirectDebitPaymentMethodSpecificOutput.PaymentProductId.HasValue &&
                        //    payment.PaymentOutput.SepaDirectDebitPaymentMethodSpecificOutput.PaymentProduct771SpecificOutput != null &&
                        //    !string.IsNullOrEmpty(payment.PaymentOutput.SepaDirectDebitPaymentMethodSpecificOutput.PaymentProduct771SpecificOutput.MandateReference))
                        //{

                        //    customerPayment.PaymentProductId = payment.PaymentOutput.SepaDirectDebitPaymentMethodSpecificOutput.PaymentProductId;
                        //    customerPayment.MandateReference = payment.PaymentOutput.SepaDirectDebitPaymentMethodSpecificOutput.PaymentProduct771SpecificOutput.MandateReference;

                        //    customer.PaymentProductId = payment.PaymentOutput.CardPaymentMethodSpecificOutput.PaymentProductId;
                        //    customer.MandateReference = payment.PaymentOutput.SepaDirectDebitPaymentMethodSpecificOutput.PaymentProduct771SpecificOutput.MandateReference;
                        //}

                        //// Update CustomerPayment record
                        //Logger.Info($"PaymentManager.ProcessPaymentAsync => Updating CustomerPayment record\n");
                        //await _customerPaymentRepository.InsertOrUpdateAsync(customerPayment);

                        //// Update Customer record
                        //Logger.Info($"PaymentManager.ProcessPaymentAsync => Updating Customer record\n");
                        //await _customerRepository.UpdateAsync(customer);

                        //// Check payment status
                        //if (payment.StatusOutput == null)
                        //{
                        //    Logger.Warn($"PaymentManager.ProcessPaymentAsync => payment.StatusOutput is null\n");
                        //}
                        //else
                        //{
                        //    if (payment.StatusOutput.StatusCategory == AppConsts.PaymentStatusCategory.COMPLETED &&
                        //        (payment.Status == AppConsts.PaymentStatuses.CAPTURED ||
                        //        payment.Status == AppConsts.PaymentStatuses.PAID))
                        //    {
                        //        // Payment succeeded
                        //        await HandlePaymentStatusCompleted(customerPayment);
                        //    }

                        //    else if (payment.Status == AppConsts.PaymentStatuses.CAPTURE_REQUESTED)
                        //    {
                        //        // Sepa Direct Debit capture has been requested
                        //        await HandlePaymentStatusCaptureRequested(customerPayment);
                        //    }

                        //    else if (payment.Status == AppConsts.PaymentStatuses.CANCELLED ||
                        //        payment.Status == AppConsts.PaymentStatuses.REJECTED ||
                        //        payment.Status == AppConsts.PaymentStatuses.REJECTED_CAPTURE)
                        //    {
                        //        // Payment failed
                        //        await HandlePaymentStatusFailed(customerPayment);
                        //    }
                        //}
                    }
                }
            }
        }

        /// <summary>
        /// Get the customer's payment method details from Ingenico.
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <returns></returns>
        public async Task<Entities.PaymentMethodDetails> GetCustomerPaymentDetailsAsync(Entities.Customer customer)
        {
            var result = new Entities.PaymentMethodDetails
            {
                Type = PaymentType.DirectDebit
            };

            using (Client client = GetClient())
            {
                var ingenicoSettings = _configuration.GetSection("Ingenico");
                var merchantId = ingenicoSettings.GetValue<string>("MerchantID");

                try
                {
                    if (customer.PaymentProductId != null)
                    {
                        result.PaymentProductId = customer.PaymentProductId.Value;
                    }

                    if (customer.Token != null)
                    {
                        var tokenResponse = await client.Merchant(merchantId).Tokens().Get(customer.Token);

                        if (tokenResponse.Card != null & tokenResponse.Card.Data != null)
                        {
                            result.Type = PaymentType.Card;
                            result.CardNumber = tokenResponse.Card.Data.CardWithoutCvv.CardNumber;
                            result.CardHolderName = tokenResponse.Card.Data.CardWithoutCvv.CardholderName;
                            result.CardExpiryDate = tokenResponse.Card.Data.CardWithoutCvv.ExpiryDate;
                        }
                    }
                    else if (customer.MandateReference != null)
                    {
                        var mandateResponse = await client.Merchant(merchantId).Mandates().Get(customer.MandateReference);

                        if (mandateResponse.Mandate != null && mandateResponse.Mandate.Customer != null)
                        {
                            result.Type = PaymentType.DirectDebit;
                            result.MandateStatus = mandateResponse.Mandate.Status;
                            result.MandateRecurrenceType = mandateResponse.Mandate.RecurrenceType;
                            result.MandateCustomerReference = mandateResponse.Mandate.CustomerReference;
                            result.MandateBankAccountHolderName = mandateResponse.Mandate.Customer.BankAccountIban.AccountHolderName;
                            result.MandateBankAccountIban = mandateResponse.Mandate.Customer.BankAccountIban.Iban;
                            result.MandateCompanyName = mandateResponse.Mandate.Customer.CompanyName;
                            result.MandateContactEmail = mandateResponse.Mandate.Customer.ContactDetails.EmailAddress;
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Logger.Error("Error while getting payment details", ex);
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
        /// <param name="amount">The decimal monetary amount that needs to be converted.</param>
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
                ingenicoCustomer.BillingAddress = ConvertToIngenicoBillingAddress(address);
            }

            if (string.IsNullOrEmpty(customer.Locale))
            {
                var ingenicoSettings = _configuration.GetSection("Ingenico");
                var defaultLocale = ingenicoSettings.GetValue<string>("DefaultLocale");
                ingenicoCustomer.Locale = defaultLocale;
            }

            return ingenicoCustomer;
        }

        private Address ConvertToIngenicoBillingAddress(Entities.Address inputAddress)
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

        /// <summary>
        /// Payment succeeded.
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="customerPayment"></param>
        /// <returns></returns>
        private async Task HandlePaymentStatusCompleted(Entities.CustomerPayment customerPayment)
        {
            //Logger.Info($"PaymentManager.HandlePaymentStatusCompleted => Payment succeeded\n");
            //await ActivateSubscription(customerPayment.SubscriptionId);

            //// Update invoices status
            //foreach (var invoicePayment in customerPayment.InvoicePayments)
            //{
            //    var invoice = await _invoiceManager.GetEntityByIdAsync(invoicePayment.InvoiceId);
            //    Logger.Info($"PaymentManager.HandlePaymentStatusCompleted => Updating invoice status to 'Paid'\nInvoice Id : {invoice.Id}\n");
            //    invoice.Status = InvoiceStatus.Paid;
            //    await _invoiceManager.Update(invoice);
            //}
        }

        /// <summary>
        /// Sepa Direct Debit capture has been requested
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="customerPayment"></param>
        /// <returns></returns>
        private async Task HandlePaymentStatusCaptureRequested(Entities.CustomerPayment customerPayment)
        {
            //Logger.Info($"PaymentManager.HandlePaymentStatusCaptureRequested => Payment capture requested, awaiting confirmation\n");
            //await ActivateSubscription(customerPayment.SubscriptionId);

            //// Update invoices status
            //foreach (var invoicePayment in customerPayment.InvoicePayments)
            //{
            //    var invoice = await _invoiceManager.GetEntityByIdAsync(invoicePayment.InvoiceId);
            //    Logger.Info($"PaymentManager.HandlePaymentStatusCaptureRequested => Updating invoice status to 'AwaitingPaymentConfirmation'\nInvoice Id : {invoice.Id}\n");
            //    invoice.Status = InvoiceStatus.AwaitingPaymentConfirmation;
            //    await _invoiceManager.Update(invoice);
            //}
        }

        private async Task HandlePaymentStatusFailed(Entities.CustomerPayment customerPayment)
        {
            //Logger.Info($"PaymentManager.HandlePaymentStatusFailed => Payment failed\n");
            //bool notifiy = false;

            //// Update invoices status
            //foreach (var invoicePayment in customerPayment.InvoicePayments)
            //{
            //    var invoice = await _invoiceManager.GetEntityByIdAsync(invoicePayment.InvoiceId);
            //    if (invoice.Status != InvoiceStatus.PaymentFailed)
            //    {
            //        notifiy = true;
            //        Logger.Info($"PaymentManager.HandlePaymentStatusFailed => Updating invoice status to 'PaymentFailed'\nInvoice Id : {invoice.Id}\n");
            //        invoice.Status = InvoiceStatus.PaymentFailed;
            //        await _invoiceManager.Update(invoice);
            //    }
            //}

            //if (notifiy)
            //{
            //    // Notify customer that payment failed and that they must update their payment details
            //    var ingenicoSettings = _configuration.GetSection("Ingenico");
            //    var paymentFailedDays = ingenicoSettings.GetValue<int>("PaymentFailedDays");
            //    var subscription = await _subscriptionManager.GetEntityByIdAsync(customerPayment.SubscriptionId);
            //    var user = _userRepository.FirstOrDefault(u => u.Id == subscription.Customer.OwnerId);

            //    Logger.Info($"PaymentManager.HandlePaymentStatusFailed => Notifying customer of payment failure\n");
            //    await _communicationManager.SendPaymentFailedCustomerNotification(
            //            subscription.Customer.BillingEmail,
            //            $"{user.Name} {user.Surname}",
            //            subscription.Name,
            //            subscription.Number,
            //            user.Id,
            //            paymentFailedDays
            //        );
            //}

        }
    }
}
