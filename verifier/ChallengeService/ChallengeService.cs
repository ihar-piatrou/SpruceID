using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using verifier.Models;
using verifier.NonceStore;
using verifier.Options;
using verifier.SystemTime;

namespace verifier.ChallengeService
{
    public sealed class ChallengeService : IChallengeService
    {
        private readonly INonceStore _nonces;
        private readonly ITimeProvider _time;
        private readonly VerifierOptions _opts;

        public ChallengeService(INonceStore nonces, ITimeProvider time, IOptions<VerifierOptions> options)
        {
            _nonces = nonces;
            _time = time;
            _opts = options.Value;
        }

        public async Task<ChallengeResponse> IssueAsync()
        {
            var nonce = CreateNonce();

            var ttl = TimeSpan.FromSeconds(_opts.NonceTtlSeconds);
            var expires = _time.UtcNow.Add(ttl);

            await _nonces.TryAddAsync(nonce, new NonceRecord(expires, false));
            return new ChallengeResponse(nonce, expires, _opts.Audience);
        }

        private static string CreateNonce()
        {
            Span<byte> bytes = stackalloc byte[16];
            RandomNumberGenerator.Fill(bytes);
            return Base64UrlEncoder.Encode(bytes.ToArray());
        }
    }
}