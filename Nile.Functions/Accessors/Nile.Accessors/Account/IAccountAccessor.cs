namespace Nile.Accessors.Account;

public interface IAccountAccessor
{
    Task<DTO.ResponseBase> Get(DTO.AccountRequest request);
    
    //Task<DTO.ResponseBase> StoreTransaction(DTO.StoreAccountTransactionRequest request);
    
    Task<DTO.ResponseBase> Store(DTO.StoreAccountRequest request);
}