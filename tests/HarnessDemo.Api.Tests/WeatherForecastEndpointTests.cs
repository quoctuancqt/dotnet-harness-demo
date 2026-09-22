using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HarnessDemo.Api.Tests;

public class WeatherForecastEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WeatherForecastEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/weatherforecast");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsFiveForecastEntries()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/weatherforecast");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.Equal(5, document.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetWeatherForecast_EachEntryHasExpectedShape()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/weatherforecast");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        foreach (var entry in document.RootElement.EnumerateArray())
        {
            Assert.True(entry.TryGetProperty("date", out _));
            Assert.True(entry.TryGetProperty("temperatureC", out var temperatureC));
            Assert.True(entry.TryGetProperty("temperatureF", out _));
            Assert.True(entry.TryGetProperty("summary", out _));

            var temperature = temperatureC.GetInt32();
            Assert.InRange(temperature, -20, 54);
        }
    }
}
