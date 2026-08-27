using BmiApi.Models;
using BmiApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BmiApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BmiController : ControllerBase
{
    private readonly IBmiCalculator _calculator;

    public BmiController(IBmiCalculator calculator)
    {
        _calculator = calculator;
    }

    /// <summary>Calcula el Índice de Masa Corporal a partir del peso y la estatura.</summary>
    /// <param name="request">Peso en kilogramos y estatura en metros.</param>
    /// <response code="200">IMC calculado con su clasificación.</response>
    /// <response code="400">Los datos enviados no son válidos.</response>
    [HttpPost]
    [ProducesResponseType(typeof(BmiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<BmiResponse> Calculate([FromBody] BmiRequest request)
    {
        return Ok(_calculator.Calculate(request.WeightKg, request.HeightM));
    }

    /// <summary>Variante por query string, útil para pruebas rápidas desde el navegador.</summary>
    /// <param name="weightKg">Peso en kilogramos.</param>
    /// <param name="heightM">Estatura en metros.</param>
    [HttpGet]
    [ProducesResponseType(typeof(BmiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<BmiResponse> Calculate([FromQuery] double weightKg, [FromQuery] double heightM)
    {
        var request = new BmiRequest { WeightKg = weightKg, HeightM = heightM };

        if (!TryValidateModel(request))
        {
            return ValidationProblem(ModelState);
        }

        return Ok(_calculator.Calculate(request.WeightKg, request.HeightM));
    }
}
