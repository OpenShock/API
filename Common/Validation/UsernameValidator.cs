using System.Diagnostics.CodeAnalysis;
using OneOf;
using OneOf.Types;
using OpenShock.Common.Constants;

namespace OpenShock.Common.Validation;

public static class UsernameValidator
{
    public static OneOf<Success, UsernameError> Validate(string username)
    {
        if (username.Length < HardLimits.UsernameMinLength)
        {
            return new UsernameError(UsernameErrorType.TooShort, "Username is too short.");
        }

        if (username.Length > HardLimits.UsernameMaxLength)
        {
            return new UsernameError(UsernameErrorType.TooLong, "Username is too long.");
        }

        if (char.IsWhiteSpace(username, 0) || char.IsWhiteSpace(username, username.Length - 1))
        {
            return new UsernameError(UsernameErrorType.StartOrEndWithWhitespace, "Username cannot start or end with whitespace.");
        }

        if (username.Contains('@'))
        {
            return new UsernameError(UsernameErrorType.ResembleEmail, "Username cannot resemble an email address.");
        }

        // Check if string contains any unwanted characters
        if (CharsetMatchers.ContainsUndesiredUserInterfaceCharacters(username))
            return new UsernameError(UsernameErrorType.ObnoxiousCharacters, "Username must not contain obnoxious characters.");

        return new Success();
    }
}

public readonly struct UsernameError
{
    [SetsRequiredMembers]
    public UsernameError(UsernameErrorType type, string message)
    {
        Message = message;
        Type = type;
    }
    
    public required string Message { get; init; }
    public required UsernameErrorType Type { get; init; }
}

public enum UsernameErrorType
{
    TooShort,
    TooLong,
    StartOrEndWithWhitespace,
    ResembleEmail,
    ObnoxiousCharacters,
    Blacklisted
}