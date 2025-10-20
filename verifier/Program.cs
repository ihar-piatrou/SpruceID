// Verifier/Program.cs
using Microsoft.AspNetCore.Mvc;
using verifier.Models;
using verifier.ChallengeService;
using verifier.JwkKeyResolver;
using verifier.NonceStore;
using verifier.Options;
using verifier.SystemTime;
using verifier.TokenValidator;
using verifier.Verification;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<VerifierOptions>(builder.Configuration.GetSection("Verifier"));
builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();
builder.Services.AddSingleton<INonceStore, InMemoryNonceStore>();
builder.Services.AddSingleton<IKeyResolver, DidJwkKeyResolver>();
builder.Services.AddSingleton<ITokenValidator, JwtTokenValidator>();
builder.Services.AddSingleton<IChallengeService, ChallengeService>();
builder.Services.AddSingleton<IVerificationService, VerificationService>();

var app = builder.Build();

app.MapPost("/challenge", async ([FromServices] IChallengeService challenge) =>
{
    var res = await challenge.IssueAsync();
    return Results.Json(res);
});


app.MapPost("/verify", async (
    [FromBody] VerifyRequest req,
    [FromServices] IVerificationService verification) =>
{
    var result = await verification.VerifyAsync(req);
    return result.ToIResult();
});


await app.RunAsync();
