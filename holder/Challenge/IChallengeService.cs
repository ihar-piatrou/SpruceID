namespace holder.Challenge
{
    public interface IChallengeService
    {
        Task<ChallengeRecord> GetChallenge(CancellationToken ct = default);
    }
}
