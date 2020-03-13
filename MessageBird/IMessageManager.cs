namespace MessageBird
{
    public interface IMessageManager
    {
        string SendVerification(string phoneNumber);

        bool VerifyToken(string verifyId, string token);
    }
}
