using Microsoft.Extensions.Configuration;
using Mollie.Api.Models.Customer;

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



    }
}
