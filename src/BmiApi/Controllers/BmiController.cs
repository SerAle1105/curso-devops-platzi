using BmiApi.Models;
using BmiApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BmiApi.Controllers;

/// <summary>Cálculo del Índice de Masa Corporal.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("IMC")]
public class BmiController : ControllerBase
{
    private readonly IBmiCalculator _calculator;

    public BmiController(IBmiCalculator calculator)
    {
        _calculator = calculator;
    }

    /// <summary>Calcula el Índice de Masa Corporal a partir del peso y la estatura.</summary>
    /// <remarks>
    /// Petición de ejemplo:
    ///
    ///     POST /api/bmi
    ///     {
    ///        "weightKg": 70,
    ///        "heightM": 1.75
    ///     }
    ///
    /// El IMC se calcula como peso / estatura² y se redondea a dos decimales.
    /// </remarks>
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

    /// <summary>Calcula el IMC por query string.</summary>
    /// <remarks>
    /// Variante pensada para pruebas rápidas desde el navegador o desde Swagger:
    ///
    ///     GET /api/bmi?weightKg=70&amp;heightM=1.75
    ///
    /// </remarks>
    /// <param name="weightKg">Peso en kilogramos. Rango admitido: 1 a 500.</param>
    /// <param name="heightM">Estatura en metros. Rango admitido: 0.5 a 2.5.</param>
    /// <response code="200">IMC calculado con su clasificación.</response>
    /// <response code="400">Los datos enviados no son válidos.</response>
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
