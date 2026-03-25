using System.Net.Http.Json;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.Extensions.Logging;

namespace MonkoraEdge.Core.Auth.Infrastructure.ExternalApis;

/// <summary>
/// HTTP client adapter that calls the external Notification API.
/// All OTP delivery, email verification and password reset mail
/// is handled by that service; this class only translates domain calls into HTTP.
/// </summary>
public sealed class NotificationApiClient : INotificationApi
{
    private readonly HttpClient _http;
    private readonly ILogger<NotificationApiClient> _logger;

    public NotificationApiClient(HttpClient http, ILogger<NotificationApiClient> logger)
        => (_http, _logger) = (http, logger);

    public async Task SendEmailOtpAsync(
        string toEmail, string otp, string purpose,
        string? displayName = null, string? language = null,
        CancellationToken ct = default)
    {
        var payload = new
        {
            to = toEmail,
            otp,
            purpose,
            displayName,
            language = language ?? "en"
        };

        await PostAsync("/api/notifications/otp/email", payload, ct);
        _logger.LogInformation("Email OTP dispatched to Notification API. Purpose={Purpose}", purpose);
    }

    public async Task SendSmsOtpAsync(
        string toPhone, string otp, string purpose,
        CancellationToken ct = default)
    {
        var payload = new { to = toPhone, otp, purpose };
        await PostAsync("/api/notifications/otp/sms", payload, ct);
        _logger.LogInformation("SMS OTP dispatched to Notification API. Purpose={Purpose}", purpose);
    }

    public async Task SendEmailVerificationAsync(
        string toEmail, string verificationToken,
        string? displayName = null, string? language = null,
        CancellationToken ct = default)
    {
        var payload = new
        {
            to = toEmail,
            token = verificationToken,
            displayName,
            language = language ?? "en"
        };

        await PostAsync("/api/notifications/email/verify", payload, ct);
        _logger.LogInformation("Email verification dispatched to Notification API. Email={Email}", toEmail);
    }

    public async Task SendPasswordResetAsync(
        string toEmail, string resetToken,
        string? displayName = null, string? language = null,
        CancellationToken ct = default)
    {
        var payload = new
        {
            to = toEmail,
            token = resetToken,
            displayName,
            language = language ?? "en"
        };

        await PostAsync("/api/notifications/email/password-reset", payload, ct);
        _logger.LogInformation("Password reset email dispatched to Notification API. Email={Email}", toEmail);
    }

    // ── private ──────────────────────────────────────────────────────────────

    private async Task PostAsync<T>(string path, T payload, CancellationToken ct)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(path, payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Notification API returned non-success. Path={Path} Status={Status}",
                    path, response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            // Non-fatal — notification failure must not break the auth flow.
            // The calling service logs the error; delivery can be retried by Notification API.
            _logger.LogError(ex, "Notification API call failed. Path={Path}", path);
        }
    }
}
