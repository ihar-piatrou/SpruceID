using verifier.Models;

namespace verifier.NonceStore
{
    public interface INonceStore
    {
        Task<bool> TryAddAsync(string nonce, NonceRecord record);
        Task<(bool found, NonceRecord record)> TryGetAsync(string nonce);
        Task<bool> MarkUsedAsync(string nonce);
    }
}
