namespace OpenShock.Common.Validation;

public static class UsernameValidator
{
    public readonly record struct ValidationResult(bool Ok, string ErrorMessage);

    public static ValidationResult Validate(string username)
    {
        if (username.Length < ValidationConstants.UsernameMinLength)
        {
            return new ValidationResult(false, "Username is too short.");
        }

        if (username.Length > ValidationConstants.UsernameMaxLength)
        {
            return new ValidationResult(false, "Username is too long.");
        }

        if (char.IsWhiteSpace(username[0]) || char.IsWhiteSpace(username[^1]))
        {
            return new ValidationResult(false, "Username cannot start or end with whitespace.");
        }

        if (username.Contains('@'))
        {
            return new ValidationResult(false, "Username cannot resemble an email address.");
        }

        // Check if string contains any unwanted characters
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var r in username.EnumerateRunes())
            if (CharsetMatchers.IsUnwantedUserInterfaceRune(r))
                return new ValidationResult(false, "Username must not contain obnoxious characters.");


        return new ValidationResult(true, string.Empty);
    }
}