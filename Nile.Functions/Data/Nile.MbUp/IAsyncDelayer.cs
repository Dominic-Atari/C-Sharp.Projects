namespace Nile.MbUp;

public interface IAsyncDelayer
{
    Task Delay(int milliseconds);
}
