using Nile.Managers.Contract.Client.DataContract.Admin;

namespace Nile.Managers.Admin;

public interface IAdministratorManager
{
    Task<AdminContextResponse> Login(User.LoginRequest request);
}