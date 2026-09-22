using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HarnessDemo.Api.Tests;

public class RegisterEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RegisterEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static string UniqueUsername(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    [Fact]
    public async Task Register_WithValidRequest_ReturnsCreatedWithExpectedShape()
    {
        var client = _factory.CreateClient();
        var username = UniqueUsername("valid-user");
        var request = new { username, email = "valid-user@example.com", password = "supersecret" };

        var response = await client.PostAsJsonAsync("/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("id", out var idProperty));
        var id = idProperty.GetGuid();
        Assert.NotEqual(Guid.Empty, id);

        Assert.True(root.TryGetProperty("username", out var usernameProperty));
        Assert.Equal(username, usernameProperty.GetString());

        Assert.True(root.TryGetProperty("email", out var emailProperty));
        Assert.Equal("valid-user@example.com", emailProperty.GetString());

        Assert.False(root.TryGetProperty("password", out _));

        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/register/{id}", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Register_WithMissingUsername_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var request = new { username = "   ", email = "missing-username@example.com", password = "supersecret" };

        var response = await client.PostAsJsonAsync("/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("error", out var errorProperty));
        Assert.False(string.IsNullOrWhiteSpace(errorProperty.GetString()));
    }

    [Fact]
    public async Task Register_WithMissingEmail_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var username = UniqueUsername("missing-email");
        var request = new { username, email = "", password = "supersecret" };

        var response = await client.PostAsJsonAsync("/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("error", out var errorProperty));
        Assert.False(string.IsNullOrWhiteSpace(errorProperty.GetString()));
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var username = UniqueUsername("invalid-email");
        var request = new { username, email = "not-an-email", password = "supersecret" };

        var response = await client.PostAsJsonAsync("/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("error", out var errorProperty));
        Assert.False(string.IsNullOrWhiteSpace(errorProperty.GetString()));
    }

    [Fact]
    public async Task Register_WithMissingPassword_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var username = UniqueUsername("missing-password");
        var request = new { username, email = "missing-password@example.com", password = "" };

        var response = await client.PostAsJsonAsync("/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("error", out var errorProperty));
        Assert.False(string.IsNullOrWhiteSpace(errorProperty.GetString()));
    }

    [Fact]
    public async Task Register_WithTooShortPassword_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var username = UniqueUsername("short-password");
        var request = new { username, email = "short-password@example.com", password = "short1" };

        var response = await client.PostAsJsonAsync("/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("error", out var errorProperty));
        Assert.False(string.IsNullOrWhiteSpace(errorProperty.GetString()));
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var username = UniqueUsername("duplicate-user");
        var firstRequest = new { username, email = "duplicate-user-1@example.com", password = "supersecret" };
        var secondRequest = new { username, email = "duplicate-user-2@example.com", password = "anothersecret" };

        var firstResponse = await client.PostAsJsonAsync("/register", firstRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync("/register", secondRequest);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var body = await secondResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("error", out var errorProperty));
        Assert.False(string.IsNullOrWhiteSpace(errorProperty.GetString()));
    }
}
