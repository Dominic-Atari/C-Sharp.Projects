using Nile.Common.Errors;

namespace Nile.Managers.Proxys;

public interface IValidator
{
    ValidationError? Validate<TRequest>(TRequest request) where TRequest : notnull;
}