using Nile.Managers.Contract.Client.DataContract.V1;

namespace Nile.Managers.Admin;

public interface IFileManager
{
    Task<SasTokenResponseBase> SasToken(CLI.V1.SasTokenRequestBase request);
}