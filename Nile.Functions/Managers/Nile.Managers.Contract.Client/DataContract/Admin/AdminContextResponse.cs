namespace Nile.Managers.Contract.Client.DataContract.Admin;

public class AdminContextResponse : ResponseBase
{
    public Guid Id { get; init; }
    
    public string EmailAddress { get; init; } = null!;
    
    public string ExternalAuthId { get; init; } = null!;
}