namespace Nile.Common.DataContracts;

public class StoreUserRequestBase : UserRequestBase
{
    public Guid UserId { get; set; }
}