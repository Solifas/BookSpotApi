using System.Globalization;
using System.Text;

namespace BookSpot.Application.Features.Auth;

public static class AuthRules
{
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.Ordinal)
    {
        "passwordpassword",
        "123456789012345",
        "qwertyqwertyqwerty",
        "letmeinletmeinletmein"
    };

    public static string NormalizeEmail(string email) =>
        email.Trim().Normalize(NormalizationForm.FormC).ToLower(CultureInfo.InvariantCulture);

    public static bool IsPasswordAllowed(string password)
    {
        if (password is null)
        {
            return false;
        }

        var scalarCount = password.EnumerateRunes().Count();
        var byteCount = Encoding.UTF8.GetByteCount(password);
        return scalarCount is >= 15 and <= 64
            && byteCount <= 72
            && !CommonPasswords.Contains(password.ToLowerInvariant());
    }
}
