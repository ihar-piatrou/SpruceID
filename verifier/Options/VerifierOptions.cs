namespace verifier.Options
{
    public sealed class VerifierOptions
    {
        public string Audience { get; set; } = "urn:example:verifier";
        public string VerifyMethod { get; set; } = "POST";
        public string VerifyPath { get; set; } = "/verify";
        public int NonceTtlSeconds { get; set; } = 120;
        public int ClockSkewSeconds { get; set; } = 120;
    }
}
