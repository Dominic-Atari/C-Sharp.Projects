namespace Nile.Common.DataContracts;

public class LoginProfileRequest : UserRequestBase
{
    public Guid UserId { get; set; }
}