using System.Text.Json;

namespace holder.Challenge
{
    public class HttpChallengeService : IChallengeService
    {
        private readonly HttpClient _http;
        private readonly string _challengeUrl;

        public HttpChallengeService(HttpClient http, string challengeUrl)
        {
            _http = http;
            _challengeUrl = challengeUrl;
        }

        public async Task<ChallengeRecord> GetChallenge(CancellationToken ct = default)
        {
            using var resp = await _http.PostAsync(_challengeUrl, content: null, ct);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;
            var nonce = root.GetProperty("nonce").GetString() ?? throw new InvalidOperationException("nonce missing");
            var aud = root.GetProperty("audience").GetString() ?? throw new InvalidOperationException("aud missing");
            var exp = root.GetProperty("expires_at").GetDateTimeOffset();

            return new ChallengeRecord(nonce, aud, exp);
        }
    }
}
