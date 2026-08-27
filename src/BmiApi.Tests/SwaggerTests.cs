using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BmiApi.Tests;

/// <summary>Verifica que la documentación OpenAPI se genere y describa los endpoints.</summary>
public class SwaggerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SwaggerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SwaggerJson_EstaDisponible()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerJson_DescribeLosDosEndpointsDeImc()
    {
        using var document = JsonDocument.Parse(await _client.GetStringAsync("/swagger/v1/swagger.json"));

        var path = document.RootElement.GetProperty("paths").GetProperty("/api/Bmi");

        Assert.True(path.TryGetProperty("get", out _));
        Assert.True(path.TryGetProperty("post", out _));
    }

    [Fact]
    public async Task SwaggerJson_IncluyeElTituloYLaVersion()
    {
        using var document = JsonDocument.Parse(await _client.GetStringAsync("/swagger/v1/swagger.json"));

        var info = document.RootElement.GetProperty("info");

        Assert.Equal("API de Índice de Masa Corporal", info.GetProperty("title").GetString());
        Assert.Equal("v1", info.GetProperty("version").GetString());
    }

    [Fact]
    public async Task SwaggerJson_DocumentaLosEsquemasConSusDescripciones()
    {
        using var document = JsonDocument.Parse(await _client.GetStringAsync("/swagger/v1/swagger.json"));

        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        var request = schemas.GetProperty("BmiRequest").GetProperty("properties");
        Assert.Equal(70, request.GetProperty("weightKg").GetProperty("example").GetDouble());
        Assert.Contains("kilogramos", request.GetProperty("weightKg").GetProperty("description").GetString());

        var response = schemas.GetProperty("BmiResponse").GetProperty("properties");
        Assert.Contains("OMS", response.GetProperty("category").GetProperty("description").GetString());
    }

    [Fact]
    public async Task SwaggerUi_SeSirveEnLaRaiz()
    {
        var response = await _client.GetAsync("/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("swagger", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }
}
