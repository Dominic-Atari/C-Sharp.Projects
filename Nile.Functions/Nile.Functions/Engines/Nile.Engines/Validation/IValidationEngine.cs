namespace Nile.Engines.Validation;

public interface IValidationEngine
{
    Task Validate(CLI.RequestBase request);
    
    Task Validate(DTO.StoreUserRequestBase request);
}