using verifier.Models;

namespace verifier.ChallengeService
{
    public interface IChallengeService
    {
        Task<ChallengeResponse> IssueAsync();
    }
}
