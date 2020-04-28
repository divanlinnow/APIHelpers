namespace Ingenico.Entities
{
    public class AppConsts
    {
        public static class PaymentStatusCategory
        {
            public const string COMPLETED = "COMPLETED";
        }

        public static class PaymentStatuses
        {
            public const string CREATED = "CREATED";
            public const string CANCELLED = "CANCELLED";
            public const string REJECTED = "REJECTED";
            public const string REJECTED_CAPTURE = "REJECTED_CAPTURE";
            public const string REDIRECTED = "REDIRECTED";
            public const string PENDING_PAYMENT = "PENDING_PAYMENT";
            public const string ACCOUNT_VERIFIED = "ACCOUNT_VERIFIED";
            public const string PENDING_APPROVAL = "PENDING_APPROVAL";
            public const string PENDING_COMPLETION = "PENDING_COMPLETION";
            public const string PENDING_CAPTURE = "PENDING_CAPTURE";
            public const string PENDING_FRAUD_APPROVAL = "PENDING_FRAUD_APPROVAL";
            public const string AUTHORIZATION_REQUESTED = "AUTHORIZATION_REQUESTED";
            public const string CAPTURE_REQUESTED = "CAPTURE_REQUESTED";
            public const string CAPTURED = "CAPTURED";
            public const string PAID = "PAID";
            public const string CHARGEBACK_NOTIFICATION = "CHARGEBACK_NOTIFICATION";
            public const string CHARGEBACKED = "CHARGEBACKED";
            public const string REVERSED = "REVERSED";
            public const string REFUNDED = "REFUNDED";
        };
    }
}
