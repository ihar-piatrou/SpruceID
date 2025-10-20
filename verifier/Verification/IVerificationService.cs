using verifier.Models;

namespace verifier.Verification
{
    public interface IVerificationService
    {
        Task<VerifyOutcome> VerifyAsync(VerifyRequest req);
    }
}
