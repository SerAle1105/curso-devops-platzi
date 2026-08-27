using System.Net;
using System.Net.Http.Json;
using BmiApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BmiApi.Tests;

/// <summary>Pruebas de integración que levantan la API en memoria.</summary>
public class BmiControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BmiControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_ConDatosValidos_DevuelveElImcCalculado()
    {
        var response = await _client.PostAsJsonAsync("/api/bmi", new BmiRequest { WeightKg = 70, HeightM = 1.75 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<BmiResponse>();

        Assert.NotNull(body);
        Assert.Equal(22.86, body!.Bmi);
        Assert.Equal("Peso normal", body.Category);
        Assert.Equal(70, body.WeightKg);
        Assert.Equal(1.75, body.HeightM);
    }

    [Fact]
    public async Task Get_ConQueryStringValido_DevuelveElImcCalculado()
    {
        var response = await _client.GetAsync("/api/bmi?weightKg=95&heightM=1.75");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<BmiResponse>();

        Assert.NotNull(body);
        Assert.Equal(31.02, body!.Bmi);
        Assert.Equal("Obesidad grado I", body.Category);
    }

    [Theory]
    [InlineData(0, 1.75)]
    [InlineData(-70, 1.75)]
    [InlineData(70, 0)]
    [InlineData(70, 3.5)]
    [InlineData(600, 1.75)]
    public async Task Post_ConDatosFueraDeRango_DevuelveBadRequest(double weightKg, double heightM)
    {
        var response = await _client.PostAsJsonAsync("/api/bmi", new BmiRequest { WeightKg = weightKg, HeightM = heightM });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.NotEmpty(problem!.Errors);
    }

    [Theory]
    [InlineData("/api/bmi?weightKg=0&heightM=1.75")]
    [InlineData("/api/bmi?weightKg=70&heightM=0")]
    public async Task Get_ConDatosFueraDeRango_DevuelveBadRequest(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_SinCuerpo_DevuelveBadRequest()
    {
        var response = await _client.PostAsync("/api/bmi", new StringContent("", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
