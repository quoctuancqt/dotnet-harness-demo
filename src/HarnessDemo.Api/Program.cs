using System.Collections.Concurrent;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<UserStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/health", () => new { status = "ok" })
.WithName("GetHealth");

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapPost("/register", (RegisterRequest request, UserStore userStore) =>
{
    if (string.IsNullOrWhiteSpace(request.Username))
    {
        return Results.BadRequest(new { error = "Username is required." });
    }

    if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
    {
        return Results.BadRequest(new { error = "A valid email is required." });
    }

    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
    {
        return Results.BadRequest(new { error = "Password must be at least 8 characters." });
    }

    var user = userStore.TryRegister(request.Username, request.Email, request.Password);
    if (user is null)
    {
        return Results.Conflict(new { error = "Username is already taken." });
    }

    var response = new RegisterResponse(user.Id, user.Username, user.Email);
    return Results.Created($"/register/{user.Id}", response);
})
.WithName("Register");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record RegisterRequest(string Username, string Email, string Password);

record RegisterResponse(Guid Id, string Username, string Email);

record StoredUser(Guid Id, string Username, string Email, string PasswordHash);

// Simple in-memory user store for demo purposes. Not persisted across restarts
// and not suitable for production use (no external DB is wired up in this repo).
class UserStore
{
    private readonly ConcurrentDictionary<string, StoredUser> _usersByUsername = new(StringComparer.OrdinalIgnoreCase);

    public StoredUser? TryRegister(string username, string email, string password)
    {
        var user = new StoredUser(Guid.NewGuid(), username, email, PasswordHasher.Hash(password));
        return _usersByUsername.TryAdd(username, user) ? user : null;
    }
}

// PBKDF2-based password hashing using only built-in .NET APIs (no extra package dependency).
static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string hashedPassword)
    {
        var parts = hashedPassword.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
}

// Exposes the implicitly-generated Program class as public so it can be
// referenced by WebApplicationFactory<Program> in integration tests.
public partial class Program { }
