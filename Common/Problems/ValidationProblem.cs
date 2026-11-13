using System.Net;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OpenShock.Internal.Common.Problems;

namespace OpenShock.Common.Problems;

public sealed class ValidationProblem : OpenShockProblem
{
    public ValidationProblem(ModelStateDictionary state) : base("Validation.Error",
        "One or more validation errors occurred", HttpStatusCode.BadRequest)
    {
        Errors = CreateErrorDictionary(state);
    }

    public IDictionary<string, string[]> Errors { get; init; }

    private static Dictionary<string, string[]> CreateErrorDictionary(ModelStateDictionary modelState)
    {
        ArgumentNullException.ThrowIfNull(modelState);

        var errorDictionary = new Dictionary<string, string[]>(modelState.Count, StringComparer.Ordinal);

        foreach (var (key, value) in modelState)
        {
            var errors = value.Errors;
            if (errors is not { Count: > 0 }) continue;
            if (errors.Count == 1)
            {
                var errorMessage = GetErrorMessage(errors[0]);
                errorDictionary.Add(key, [errorMessage]);
            }
            else
            {
                var errorMessages = new string[errors.Count];
                for (var i = 0; i < errors.Count; i++)
                {
                    errorMessages[i] = GetErrorMessage(errors[i]);
                }

                errorDictionary.Add(key, errorMessages);
            }
        }

        return errorDictionary;

        static string GetErrorMessage(ModelError error)
        {
            return string.IsNullOrEmpty(error.ErrorMessage)
                ? "The input was not valid"
                : error.ErrorMessage;
        }
    }
}
