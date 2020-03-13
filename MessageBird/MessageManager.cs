using MessageBird.Exceptions;
using MessageBird.Objects;
using MessageBird.Objects.Common;
using Microsoft.Extensions.Configuration;
using System;

namespace MessageBird
{
    public class MessageManager : IMessageManager
    {
        private readonly IConfiguration _configuration;

        public MessageManager(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Sends a SMS with a confirmation token to the given phone number which is used to verify the phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number that needs to be verified</param>
        /// <returns>A verification ID for the transaction, which must be stored and which will be used later to verify the confirmation token that will be sent from the user.</returns>
        public string SendVerification(string phoneNumber)
        {
            var result = string.Empty;

            if (!string.IsNullOrEmpty(phoneNumber))
            {
                try
                {
                    var messageBirdClient = GetClient();
                    var options = SetupOptionalArguments();
                    var verify = messageBirdClient.CreateVerify(Convert.ToInt64(phoneNumber), options);

                    result = verify.Id;
                }
                catch (ErrorException e)
                {
                    var errorMessage = $"Error sending SMS/message to : {phoneNumber} \n";

                    if (e.HasErrors)
                    {
                        // The request failed with error descriptions from the endpoint.
                        foreach (Error error in e.Errors)
                        {
                            errorMessage += $"Code : {error.Code}\nDescription: {error.Description}\nParameter: {error.Parameter}\n\n";
                        }
                    }

                    if (e.HasReason)
                    {
                        // The request failed without error descriptions from the endpoint, in which case the reason contains a 'best effort' description.
                        errorMessage += $"{e.Reason}\n";
                    }

                    //Logger.Error(errorMessage);
                }
            }

            return result;
        }

        /// <summary>
        /// Verifies the given verification ID and confirmation token, for phone number verification purposes.
        /// </summary>
        /// <param name="verifyId">The verification ID that the confirmation token must be matched with</param>
        /// <param name="token">The confirmation token to be verified</param>
        /// <returns>A boolean indicating if the phone number is verified or not</returns>
        public bool VerifyToken(string verifyId, string token)
        {
            var result = false;

            if (string.IsNullOrEmpty(verifyId) || string.IsNullOrEmpty(token))
            {
                var messageBirdClient = GetClient();
                var verify = messageBirdClient.SendVerifyToken(verifyId, token);

                if (verify.Status == VerifyStatus.Verified)
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the MessageBird client.
        /// </summary>
        /// <returns>The MessageBird client.</returns>
        private Client GetClient()
        {
            var messageBirdSettings = _configuration.GetSection("MessageBird");
            var apiKey = messageBirdSettings.GetValue<string>("APIKey");

            return Client.CreateDefault(apiKey);
        }

        /// <summary>
        /// Gets the configured settings for SMS messages.
        /// </summary>
        /// <returns>The Verify options for SMS messages.</returns>
        private VerifyOptionalArguments SetupOptionalArguments()
        {
            var messageBirdSettings = _configuration.GetSection("MessageBird");
            var originator = messageBirdSettings.GetValue<string>("Originator"); // Company Name, i.e. PerSafe, ShipLogic, etc. 
            var tokenLength = messageBirdSettings.GetValue<int>("TokenLength"); // Length of confirmation token. Either 4 or 6 

            var options = new VerifyOptionalArguments
            {
                Encoding = DataEncoding.Auto,
                Originator = originator,
                Reference = "Verify",
                Type = MessageType.Sms,
                Timeout = 180,
                TokenLength = tokenLength,
                Voice = Voice.Female,
                Language = Language.English,
                Template = $"template"
            };

            return options;
        }
    }
}
