using System.Globalization;
using System.Text;

namespace BinTracker.Core;

/// <summary>
/// Provider-neutral identity and request-size rules for authentication inputs.
/// New usernames use a deliberately small ASCII alphabet so their displayed
/// spelling and comparison identity cannot diverge through Unicode scripts.
/// </summary>
public static class SecurityIdentifierPolicy
{
    public const int MinimumUsernameLength = 3;
    public const int MaximumUsernameLength = 64;
    public const int MaximumLegacyUsernameLength = 100;
    public const int MaximumPasswordLength = 256;
    public const int MaximumDisplayNameLength = 150;

    public static string CanonicalizeNewUsername(string username)
    {
        ArgumentNullException.ThrowIfNull(username);

        if (username.Length > MaximumUsernameLength)
            throw new ArgumentException(
                $"Username must be no more than {MaximumUsernameLength} characters.",
                nameof(username));
        if (!string.Equals(username, username.Trim(), StringComparison.Ordinal))
            throw new ArgumentException(
                "Username cannot begin or end with whitespace.",
                nameof(username));

        string normalized;
        try
        {
            normalized = username.Normalize(NormalizationForm.FormKC);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException("Username contains invalid Unicode text.", nameof(username), ex);
        }

        if (normalized.Length is < MinimumUsernameLength or > MaximumUsernameLength)
            throw new ArgumentException(
                $"Username must be between {MinimumUsernameLength} and {MaximumUsernameLength} characters.",
                nameof(username));

        var canonical = normalized.ToLowerInvariant();
        if (!canonical.All(IsAllowedNewUsernameCharacter) ||
            !IsAsciiLetterOrDigit(canonical[0]) ||
            !IsAsciiLetterOrDigit(canonical[^1]))
        {
            throw new ArgumentException(
                "Username may contain only English letters, numbers, '.', '_' or '-', and must begin and end with a letter or number.",
                nameof(username));
        }

        return canonical;
    }

    /// <summary>
    /// Produces the comparison identity for bounded legacy rows and login input.
    /// New creation is stricter; this seam prevents case/normalization drift
    /// without silently rewriting an existing account.
    /// </summary>
    public static bool TryGetUsernameComparisonKey(string? username, out string key)
    {
        key = string.Empty;
        if (string.IsNullOrEmpty(username) ||
            username.Length > MaximumLegacyUsernameLength ||
            !string.Equals(username, username.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        string normalized;
        try
        {
            normalized = username.Normalize(NormalizationForm.FormKC);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (normalized.Length is < MinimumUsernameLength or > MaximumLegacyUsernameLength)
            return false;

        foreach (var character in normalized)
        {
            var category = char.GetUnicodeCategory(character);
            if (char.IsWhiteSpace(character) ||
                char.IsControl(character) ||
                category is UnicodeCategory.Control or
                    UnicodeCategory.Format or
                    UnicodeCategory.Surrogate or
                    UnicodeCategory.PrivateUse or
                    UnicodeCategory.OtherNotAssigned)
            {
                return false;
            }
        }

        key = normalized.ToLowerInvariant();
        return true;
    }

    public static bool IsPasswordWithinRequestBounds(string? password) =>
        password is not null && password.Length <= MaximumPasswordLength;

    private static bool IsAllowedNewUsernameCharacter(char value) =>
        IsAsciiLetterOrDigit(value) || value is '.' or '_' or '-';

    private static bool IsAsciiLetterOrDigit(char value) =>
        value is >= 'a' and <= 'z' or >= '0' and <= '9';
}
