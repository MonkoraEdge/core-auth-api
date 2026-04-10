namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.ValueObjects;

/// <summary>
/// Email address value object. Immutable and normalised to lower-case on creation.
/// Validates basic RFC 5322 format so invalid addresses never enter the domain model.
/// </summary>
public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>
    /// Create and validate an <see cref="Email"/> from a raw string.
    /// Throws <see cref="ArgumentException"/> when the address is empty or malformed.
    /// </summary>
    public static Email Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Email address cannot be empty.", nameof(raw));

        var normalized = raw.Trim().ToLowerInvariant();
        if (!IsValidFormat(normalized))
            throw new ArgumentException($"'{raw}' is not a valid email address.", nameof(raw));

        return new Email(normalized);
    }

    /// <summary>Try-parse variant — returns <c>false</c> instead of throwing.</summary>
    public static bool TryCreate(string? raw, out Email? email)
    {
        email = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        try { email = Create(raw); return true; }
        catch { return false; }
    }

    public static bool IsValidFormat(string email)
    {
        try { return new System.Net.Mail.MailAddress(email).Address == email; }
        catch { return false; }
    }

    /// <summary>Implicit cast so the value object can be assigned where a plain string is expected.</summary>
    public static implicit operator string(Email email) => email.Value;

    public override string ToString() => Value;

    // ─── Value semantics ──────────────────────────────────────────────────
    public bool Equals(Email? other) => other is not null && other.Value == Value;
    public override bool Equals(object? obj) => obj is Email e && Equals(e);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public static bool operator ==(Email? a, Email? b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(Email? a, Email? b) => !(a == b);
}
