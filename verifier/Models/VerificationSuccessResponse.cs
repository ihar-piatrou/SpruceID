namespace verifier.Models
{
    /// <summary>
    /// Response model returned when verification succeeds.
    /// </summary>
    public record VerificationSuccessResponse(
        string Status,
        string HolderId,
        string Kid,
        DateTimeOffset VerifiedAt);
}
