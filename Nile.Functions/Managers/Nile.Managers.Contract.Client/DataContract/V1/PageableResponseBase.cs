using Nile.Managers.Contract.Client.DataContract;

namespace Nile.Managers;

public class PageableResponseBase : ResponseBase
{
    public string? NextPageLink { get; set; }
}