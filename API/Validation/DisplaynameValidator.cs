using System.Text;

namespace ShockLink.API.Validation;

public static class DisplaynameValidator
{
    public readonly record struct ValidationResult(bool Ok, string ErrorMessage);

    public static ValidationResult Validate(string username)
    {
        if (username.Length < ValidationConstants.UsernameMinLength)
        {
            return new ValidationResult(false, "Username is too short.");
        }
        else if (username.Length > ValidationConstants.UsernameMaxLength)
        {
            return new ValidationResult(false, "Username is too long.");
        }

        if (Char.IsWhiteSpace(username[0]) || Char.IsWhiteSpace(username[^1]))
        {
            return new ValidationResult(false, "Username cannot start or end with whitespace.");
        }

        if (username.Contains('@'))
        {
            return new ValidationResult(false, "Username cannot resemble an email address.");
        }

        // Check if string contains any unwanted characters
        foreach (Rune r in username.EnumerateRunes())
        {
            if (CharsetMatchers.IsUnwantedUserInterfaceRune(r))
            {
                return new ValidationResult(false, "Username must not contain obnoxious characters.");
            }
        }

        return new ValidationResult(true, String.Empty);
    }
}