namespace holder.Verifier
{
    public interface IVerifierService
    {
        Task<VerifyResponse> VerifyAsync(string jwt, CancellationToken ct = default);
    }
}
