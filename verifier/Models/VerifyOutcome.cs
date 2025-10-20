namespace verifier.Models
{
    public class VerifyOutcome
    {
        public bool Success { get; init; }
        public int Status { get; init; }
        public object Body { get; init; } = default!;

        public static VerifyOutcome Ok(object body) => new() { Success = true, Status = StatusCodes.Status200OK, Body = body };
        public static VerifyOutcome Bad(int status, string error, string? detail = null) =>
            new()
            {
                Success = false,
                Status = status,
                Body = detail is null ? new { error } : new { error, detail }
            };

        public IResult ToIResult() =>
            Success
                ? TypedResults.Ok(Body)
                : TypedResults.Json(Body, statusCode: Status);
    }
}
