namespace Nile.Utilities;

public interface ISocialAuthUtility
{
    Task<string> Token(DTO.UserTokenRequest request);
}