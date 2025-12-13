using Nile.Managers.Contract.Client.DataContract.V1;

namespace Nile.Managers.Admin;

internal partial class AdminManager : IFileManager
{
    public async Task<SasTokenResponseBase> SasToken(SasTokenRequestBase request)
    {
        throw new NotImplementedException();
    }
}