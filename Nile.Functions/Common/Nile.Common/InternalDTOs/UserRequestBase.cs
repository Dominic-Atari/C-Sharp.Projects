namespace Nile.Common.InternalDTOs
{
    public abstract class UserRequestBase : RequestBase
    {
        public int? Limit { get; set; }
        public string? PagingToken { get; set; }

    }   
}