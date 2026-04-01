using System.Security.Cryptography;
using System.Text;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using OtpNet;

namespace MonkoraEdge.Core.Auth.Infrastructure.Services.Security;

public class PasswordService : IPasswordService
{
    private static readonly System.Text.RegularExpressions.Regex PkceVerifierRegex =
        new("^[A-Za-z0-9\\-._~]{43,128}$", System.Text.RegularExpressions.RegexOptions.Compiled);

    // Pre-computed bcrypt hash used solely for timing normalization when a user is not found.
    // Running a full bcrypt verify here ensures the response time matches a real password check,
    // preventing username enumeration via timing side-channel (the comparison always returns false).
    private static readonly string _dummyPasswordHash =
        BCrypt.Net.BCrypt.HashPassword("__timing_normalization_sentinel__", workFactor: 12);

    public string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool VerifyPassword(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);

    public bool MeetsPasswordPolicy(string password, int minLength = 8, bool requireUpper = true, bool requireNumber = true, bool requireSpecial = true)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < minLength)
            return false;
        // Passwords with spaces are rejected — they cause issues with some OAuth clients
        // and are inconsistent with the complexity requirements below.
        if (password.Contains(' '))
            return false;
        if (requireUpper && !password.Any(char.IsUpper))
            return false;
        if (requireNumber && !password.Any(char.IsDigit))
            return false;
        if (requireSpecial && !password.Any(c => !char.IsLetterOrDigit(c)))
            return false;
        return true;
    }

    public string GenerateSecureToken(int length = 64)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    public string GenerateOtpCode(int digits = 6)
    {
        var max = (int)Math.Pow(10, digits);
        return RandomNumberGenerator.GetInt32(max).ToString().PadLeft(digits, '0');
    }

    public string HashSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool VerifyPkceCodeVerifier(string codeVerifier, string codeChallenge, string codeChallengeMethod)
    {
        if (string.IsNullOrEmpty(codeVerifier) || string.IsNullOrEmpty(codeChallenge))
            return false;

        if (!PkceVerifierRegex.IsMatch(codeVerifier))
            return false;

        if (codeChallengeMethod?.ToUpperInvariant() == "PLAIN")
            return codeVerifier == codeChallenge;

        var hashBytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        var base64 = Convert.ToBase64String(hashBytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return base64 == codeChallenge;
    }

    public string GenerateTotpSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public bool VerifyTotpCode(string secret, string code)
    {
        try
        {
            var keyBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(keyBytes);
            return totp.VerifyTotp(code, out _, new VerificationWindow(2, 2));
        }
        catch
        {
            return false;
        }
    }

    public string GetTotpUri(string secret, string email, string issuer)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(email);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
    }

    public string[] GenerateRecoveryCodes(int count = 10)
    {
        var codes = new string[count];
        for (int i = 0; i < count; i++)
        {
            var part1 = RandomNumberGenerator.GetInt32(100000, 999999);
            var part2 = RandomNumberGenerator.GetInt32(100000, 999999);
            codes[i] = $"{part1}-{part2}";
        }
        return codes;
    }

    public void PerformDummyVerify(string password) =>
        BCrypt.Net.BCrypt.Verify(password, _dummyPasswordHash);
}