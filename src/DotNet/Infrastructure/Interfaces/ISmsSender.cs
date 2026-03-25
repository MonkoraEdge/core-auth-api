namespace MonkoraEdge.Core.DotNet.Infrastructure.Interfaces
{
    /// <summary>
    /// Abstraction for sending SMS messages. Implement per provider (Twilio, True Move, DTAC, etc.).
    /// </summary>
    public interface ISmsSender
    {
        /// <summary>Send an arbitrary text message to a phone number (E.164 format recommended).</summary>
        Task<bool> SendAsync(
            string phoneNumber,
            string message,
            CancellationToken cancellationToken = default);

        /// <summary>Send a one-time password (OTP) SMS.</summary>
        Task<bool> SendOtpAsync(
            string phoneNumber,
            string otp,
            CancellationToken cancellationToken = default);
    }
}
