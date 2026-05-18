using Nile.Common.Errors;
namespace Nile.Managers.Contract.Client.DataContract;

public class ResponseBase
{
    public ErrorBase? Error { get; set; }
}