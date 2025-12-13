namespace Nile.Managers.Engagement;

public interface IContentEngagementManager
{
    Task<CLI.ResponseBase> Get(CLI.RequestBase request);

    Task<CLI.ResponseBase> Interact(CLI.RequestBase request);
}