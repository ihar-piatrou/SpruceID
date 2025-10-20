namespace holder.App
{
    public interface IApp
    {
        Task RunAsync(CancellationToken ct = default);
    }
}
