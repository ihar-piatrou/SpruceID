namespace verifier.SystemTime
{
    public interface ITimeProvider
    {
        DateTimeOffset UtcNow { get; }
    }
}
