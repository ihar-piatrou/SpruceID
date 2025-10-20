using holder.App;
using holder.Challenge;
using holder.Clock;
using holder.Config;
using holder.Security;
using holder.Verifier;

namespace holder;

class Program
{
    static async Task Main(string[] args)
    {
        var cfg = AppConfig.LoadFromEnvironment();
        Console.WriteLine($"Config: {cfg}");

        // DEV ONLY: accept self-signed TLS for local verifier
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true
        };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(150) };

        IChallengeService challenge = new HttpChallengeService(http, cfg.ChallengeUrl);
        IVerifierService verifier = new HttpVerifierService(http, cfg.VerifyUrl);
        using IJwtSigner signer = new EcdsaJwtSigner();
        IClock clock = new SystemClock();

        IApp app = new HolderApp(challenge, signer, verifier, clock, cfg.HolderId);
        await app.RunAsync();
    } 
}
