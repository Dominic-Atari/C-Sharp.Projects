using Microsoft.Extensions.Logging;
using Nile.Database.DataContracts;
using Nile.Utilities.AzureSdk;

namespace Nile.Accessors.Account;

/// <summary>
/// Handles account reads/writes: status + feature limits + shuffle count.
/// </summary>
internal class AccountAccessor : AccessorBase, IAccountAccessor
{
    private readonly IDateUtility _dateUtility;
    private readonly DatabaseContext _db;

    public AccountAccessor(
        ILogger<AccountAccessor> logger,
        IDateUtility dateUtility,
        DatabaseContext db) : base(logger)
    {
        _dateUtility = dateUtility;
        _db = db;
    }

    public async Task<DTO.ResponseBase> Get(DTO.AccountRequest request)
    {
        throw new NotImplementedException($"{nameof(AccountAccessor)}.{nameof(Get)} not implemented in this version.");
    }

    public Task<DTO.ResponseBase> Store(DTO.StoreAccountRequest request)
    {
        throw new NotImplementedException($"{nameof(AccountAccessor)}.{nameof(Store)} not implemented in this version.");
    }
}
