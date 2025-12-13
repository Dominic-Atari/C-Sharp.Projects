namespace Nile.Accessors.User
{
    public interface IUserAccessor
    {
        //Task<DTO.UserResponseBase> Get(Nile.Common.UserRequestBase request);

        Task<DTO.UserResponseBase> Store(DTO.UserRequestBase request);
    }
}