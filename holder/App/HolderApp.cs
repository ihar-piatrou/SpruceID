using holder.Challenge;
using holder.Clock;
using holder.Security;
using holder.Verifier;
using System.Security.Claims;

namespace holder.App
{
    internal class HolderApp : IApp
    {
        private readonly IChallengeService _challenge;
        private readonly IJwtSigner _signer;
        private readonly IVerifierService _verifier;
        private readonly IClock _clock;
        private readonly string _holderDid;

        public HolderApp(IChallengeService challenge, IJwtSigner signer, IVerifierService verifier, IClock clock, string holderDid)
        {
            _challenge = challenge;
            _signer = signer;
            _verifier = verifier;
            _clock = clock;
            _holderDid = holderDid;
        }

        public async Task RunAsync(CancellationToken ct = default)
        {
            var ch = await _challenge.GetChallenge(ct);
            Console.WriteLine($"Got nonce: {ch.Nonce}, expires: {ch.ExpiresAt:O}, aud: {ch.Audience}");

            var now = _clock.UtcNow;
            var exp = now.AddMinutes(2);

            var claims = new List<Claim>
            {
                new("sub", _holderDid),
                new("holder_id", _holderDid),
                new("nonce", ch.Nonce),
                new("method", "POST"),
                new("path", "/verify")
            };

            var jwt = _signer.SignJwt(claims, ch.Audience, iat: now, nbf: now, exp: exp);
            Console.WriteLine($"JWT (kid = {_signer.KeyId}): {jwt}");

            var result = await _verifier.VerifyAsync(jwt, ct);
            Console.WriteLine($"Verify status: {result.StatusCode}\n{result.Body}");
        }
    }
}
