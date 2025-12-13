namespace Nile.Managers.Contract.Client.DataContract.V1.User;

public class UsernameSuggestionsResponse : ResponseBase
{
    public string[] UserNameSuggestions { get; set; } = null!;
}