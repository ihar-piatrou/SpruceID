using System.Collections.Concurrent;
using verifier.Models;

namespace verifier.NonceStore
{
    public class InMemoryNonceStore : INonceStore
    {
        private readonly ConcurrentDictionary<string, NonceRecord> _nonces = new();

        public Task<bool> TryAddAsync(string nonce, NonceRecord record) =>
            Task.FromResult(_nonces.TryAdd(nonce, record));

        public Task<(bool found, NonceRecord record)> TryGetAsync(string nonce)
        {
            var found = _nonces.TryGetValue(nonce, out var rec);
            return Task.FromResult((found, rec!));
        }

        public Task<bool> MarkUsedAsync(string nonce)
        {
            if (!_nonces.TryGetValue(nonce, out var rec)) return Task.FromResult(false);
            _nonces[nonce] = rec with { Used = true };
            return Task.FromResult(true);
        }
    }
}
