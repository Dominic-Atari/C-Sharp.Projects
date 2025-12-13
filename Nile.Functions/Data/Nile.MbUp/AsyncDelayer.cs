namespace Nile.MbUp;

public class AsyncDelayer : IAsyncDelayer
{
    public async Task Delay(int milliseconds)
    {
        await Task.Delay(milliseconds);
    }
}
