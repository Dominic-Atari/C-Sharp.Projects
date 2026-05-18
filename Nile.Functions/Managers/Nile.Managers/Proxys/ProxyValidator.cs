using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Tokens.Experimental;
using ValidationError = Nile.Common.Errors.ValidationError;

namespace Nile.Managers.Proxys;

public class ProxyValidator : IValidator
{
    public ValidationError? Validate<TRequest>(TRequest request) where TRequest : notnull
    {
        var validationResults = new List<ValidationResult>();

        Validator.TryValidateObject(request, new ValidationContext(request, null, null), validationResults, true);

        if (!validationResults.Any())
        {
            return null;
        }

        // There will only be multiple member names if rules depend on multiple members (e.g., Password & PasswordConfirmation).
        var errors = validationResults.GroupBy(result => string.Join(", ", result.MemberNames)).ToDictionary(
            group => group.Key,
            group => group.Select(result => result.ErrorMessage!).ToArray()
        );

        return new ValidationError { Errors = errors };
    }
}