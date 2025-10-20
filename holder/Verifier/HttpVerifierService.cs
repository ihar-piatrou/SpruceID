using System.Net.Http.Json;

namespace holder.Verifier
{
    public class HttpVerifierService : IVerifierService
    {
        private readonly HttpClient _http;
        private readonly string _verifyUrl;

        public HttpVerifierService(HttpClient http, string verifyUrl)
        {
            _http = http;
            _verifyUrl = verifyUrl;
        }

        public async Task<VerifyResponse> VerifyAsync(string jwt, CancellationToken ct = default)
        {
            using var resp = await _http.PostAsJsonAsync(_verifyUrl, new { token = jwt }, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            return new VerifyResponse((int)resp.StatusCode, body);
        }
    }
}
