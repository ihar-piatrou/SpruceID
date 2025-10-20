namespace holder.Config
{
    public class AppConfig
    {
        public string HolderId { get; init; } = "did:example:holder-123-test";
        public string VerifierBase { get; init; } = "https://localhost:7262";
        public string ChallengeUrl { get; init; } = "https://localhost:7262/challenge";
        public string VerifyUrl { get; init; } = "https://localhost:7262/verify";

        public static AppConfig LoadFromEnvironment()
        {
            static string GetEnv(string name, string fallback)
            {
                var v = Environment.GetEnvironmentVariable(name);
                return string.IsNullOrWhiteSpace(v) ? fallback : v;
            }

            static string TrimEndSlash(string s) => s.EndsWith("/") ? s.TrimEnd('/') : s;

            var holderId = GetEnv("HOLDER_ID", "did:example:holder-123-test");
            var verifierBaseRaw = GetEnv("VERIFIER_BASE", "https://localhost:7262");
            var verifierBase = TrimEndSlash(verifierBaseRaw);

            // Allow explicit override or derive from base
            var challengeUrl = GetEnv("CHALLENGE_URL", $"{verifierBase}/challenge");
            var verifyUrl = GetEnv("VERIFY_URL", $"{verifierBase}/verify");

            return new AppConfig
            {
                HolderId = holderId,
                VerifierBase = verifierBase,
                ChallengeUrl = challengeUrl,
                VerifyUrl = verifyUrl
            };
        }

        public override string ToString() =>
            $"HolderId={HolderId}, VerifierBase={VerifierBase}, ChallengeUrl={ChallengeUrl}, VerifyUrl={VerifyUrl}";
    }
}
