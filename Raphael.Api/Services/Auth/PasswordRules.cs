namespace Raphael.Api.Services.Auth
{
    /// <summary>
    /// The password a portal admin may give a user: the same rules the portal checks for a user's own
    /// password (raphael-web passwordRules.ts).
    /// </summary>
    public static class PasswordRules
    {
        public const int MinLength = 8;

        /// <summary>Why the password is not acceptable, or null when it is.</summary>
        public static string? Problem(string? password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < MinLength)
                return $"The password must have at least {MinLength} characters.";
            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
                return "The password must contain letters and digits.";
            return null;
        }
    }
}
