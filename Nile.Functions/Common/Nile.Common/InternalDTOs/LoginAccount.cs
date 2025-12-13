namespace Nile.Common.InternalDTOs;

public class LoginAccount
{
    public Guid ExternalCustomerId { get; init; }
    
    public AccountStatus AccountStatus { get; init; }
}