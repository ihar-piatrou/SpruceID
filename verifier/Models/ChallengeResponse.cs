using System.Text.Json.Serialization;

namespace verifier.Models;
public sealed record ChallengeResponse(
    [property: JsonPropertyName("nonce")] string Nonce,
    [property: JsonPropertyName("expires_at")] DateTimeOffset ExpiresAt,
    [property: JsonPropertyName("audience")] string Audience);
