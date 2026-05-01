using System.Text.RegularExpressions;

namespace ISDOX.DMS.Application.Common.Behaviors
{
    public static class PasswordPolicy
    {
        public static (bool IsValid, string Message) Validate(string password)
        {
            if (password.Length < 8)
                return (false, "Password must be at least 8 characters.");

            if (!Regex.IsMatch(password, @"[A-Z]"))
                return (false, "Password must contain at least one uppercase letter.");

            if (!Regex.IsMatch(password, @"[0-9]"))
                return (false, "Password must contain at least one number.");

            if (!Regex.IsMatch(password, @"[\!\@\?\*\.]"))
                return (false, "Password must contain a special character (!?*.).");

            return (true, string.Empty);
        }
    }
}
