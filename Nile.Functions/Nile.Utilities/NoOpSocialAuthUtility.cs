using System.Threading.Tasks;

namespace Nile.Utilities;

// Development placeholder to satisfy ISocialAuthUtility dependencies
public sealed class NoOpSocialAuthUtility : ISocialAuthUtility
{
    public Task<string> Token(DTO.UserTokenRequest request)
    {
        // Return an empty token in non-production environments
        return Task.FromResult(string.Empty);
    }
}
