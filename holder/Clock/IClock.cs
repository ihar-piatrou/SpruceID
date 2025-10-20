namespace holder.Clock
{
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }
}
