namespace MonkoraEdge.Core.DotNet.Infrastructure.Interfaces
{
    /// <summary>
    /// Abstraction for sending email. Implement per provider (SMTP, SendGrid, SES, etc.).
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>Send a single email.</summary>
        Task SendAsync(
            string to,
            string subject,
            string body,
            bool isHtml = true,
            CancellationToken cancellationToken = default);

        /// <summary>Send email to multiple recipients.</summary>
        Task SendAsync(
            IEnumerable<string> to,
            string subject,
            string body,
            bool isHtml = true,
            CancellationToken cancellationToken = default);

        /// <summary>Send email with a single attachment.</summary>
        Task SendWithAttachmentAsync(
            string to,
            string subject,
            string body,
            Stream attachment,
            string attachmentName,
            string contentType = "application/octet-stream",
            bool isHtml = true,
            CancellationToken cancellationToken = default);
    }
}
