namespace verifier.Models;
public record NonceRecord(DateTimeOffset ExpiresAt, bool Used);
