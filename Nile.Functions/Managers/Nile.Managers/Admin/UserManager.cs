using Nile.Accessors.User;
using Nile.Managers.Contract.Client.Mapping;

namespace Nile.Managers.Admin;

// This partial focuses only on User account creation flows.
// Other unrelated functionality is commented out per scope reduction request.
internal partial class AdminManager
{
    public Task<Nile.Managers.Contract.Client.DataContract.V1.User.UsernameSuggestionsResponse> GenerateUsernameSuggestions(Nile.Managers.Contract.Client.DataContract.V1.User.UsernameSuggestionsRequest request)
    {
        // Not part of the requested scope
        throw new NotImplementedException();
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

    public Task<Nile.Managers.Contract.Client.DataContract.V1.User.StoreUserResponseBase> Store(Nile.Managers.Contract.Client.DataContract.V1.User.StoreUserProfileImageRequest request)
    {
        // Not part of the requested scope
        throw new NotImplementedException();
    }

    public Task<Nile.Managers.Contract.Client.DataContract.V1.User.StoreUserResponseBase> Store(Nile.Managers.Contract.Client.DataContract.V1.User.DeleteUserProfileImageRequest request)
    {
        // Not part of the requested scope
        throw new NotImplementedException();
    }
}
