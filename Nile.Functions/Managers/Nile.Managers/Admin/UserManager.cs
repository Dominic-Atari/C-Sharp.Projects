using Nile.Accessors.User;
using Nile.Managers.Contract.Client.Mapping;

namespace Nile.Managers.Admin;

// This partial focuses only on User account creation flows.
// Other unrelated functionality is commented out per scope reduction request.
internal partial class AdminManager
{
    public Task<User.UsernameSuggestionsResponse> GenerateUsernameSuggestions(User.UsernameSuggestionsRequest request)
    {
        // The current request DTO carries no input, so produce non-colliding suggestions
        // by combining a memorable adjective+noun pair with a numeric suffix. Callers can
        // refine later with name-derived suggestions once the DTO carries context.
        var adjectives = new[] { "swift", "calm", "bright", "lively", "kind", "bold", "happy", "merry", "lucky" };
        var nouns = new[] { "river", "forest", "ember", "comet", "harbor", "meadow", "summit", "lyric", "delta" };
        var rng = new Random();
        var suggestions = Enumerable.Range(0, 5)
            .Select(_ => $"{adjectives[rng.Next(adjectives.Length)]}_{nouns[rng.Next(nouns.Length)]}_{rng.Next(100, 9999)}")
            .Distinct()
            .ToArray();

        return Task.FromResult(new User.UsernameSuggestionsResponse
        {
            UserNameSuggestions = suggestions
        });
    }

    public Task<Nile.Managers.Contract.Client.DataContract.V1.User.UserContextResponse> Login(Nile.Managers.Contract.Client.DataContract.V1.User.LoginRequest request)
    {
        // Not part of the requested scope
        throw new NotImplementedException();
    }

    public Task<Nile.Managers.Contract.Client.DataContract.V1.User.StoreUserResponseBase> Store(Nile.Managers.Contract.Client.DataContract.V1.User.StoreUserRequestBase request)
    {
        // Not part of the requested scope
        throw new NotImplementedException();
    }

    public async Task<Nile.Managers.Contract.Client.DataContract.V1.User.StoreUserResponseBase> Store(Nile.Managers.Contract.Client.DataContract.V1.User.CreateUserProfileRequest request)
    {
        // Map client request -> internal DTO (profile-aware)
        var dtoReq = ClientDtoMapper.Map<Nile.Common.InternalDTOs.CreateUserProfileRequest>(request);

        // Persist via accessor
        var dtoResp = await _userAccessor.Store(dtoReq);

        // Map internal DTO response -> client response
        var cliResp = ClientDtoMapper.Map<Nile.Managers.Contract.Client.DataContract.V1.User.StoreUserResponseBase>(dtoResp);
        return cliResp;
    }

    public async Task<User.StoreUserResponseBase> Store(User.UpdateUserProfileRequest request)
    {
        var dtoReq = ClientDtoMapper.Map<DTO.UpdateUserProfileRequest>(request);
        var dtoResp = await _userAccessor.Store(dtoReq);
        return ClientDtoMapper.Map<User.StoreUserResponseBase>(dtoResp);
    }

    public async Task<User.StoreUserResponseBase> Store(User.StoreUserProfileImageRequest request)
    {
        var dtoReq = ClientDtoMapper.Map<DTO.StoreUserProfileImageRequest>(request);
        var dtoResp = await _userAccessor.Store(dtoReq);
        return ClientDtoMapper.Map<User.StoreUserResponseBase>(dtoResp);
    }

    public async Task<User.StoreUserResponseBase> Store(User.DeleteUserProfileImageRequest request)
    {
        var dtoReq = ClientDtoMapper.Map<DTO.DeleteUserProfileImageRequest>(request);
        var dtoResp = await _userAccessor.Store(dtoReq);
        return ClientDtoMapper.Map<User.StoreUserResponseBase>(dtoResp);
    }

    public async Task<User.StoreUserResponseBase> Store(User.StoreNotificationPreferencesRequest request)
    {
        var dtoReq = ClientDtoMapper.Map<DTO.StoreNotificationPreferencesRequest>(request);
        var dtoResp = await _userAccessor.Store(dtoReq);
        return ClientDtoMapper.Map<User.StoreUserResponseBase>(dtoResp);
    }
}
