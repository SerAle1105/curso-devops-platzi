using BmiApi.Services;

namespace BmiApi.Tests;

public class BmiCalculatorTests
{
    private readonly BmiCalculator _calculator = new();

    [Theory]
    [InlineData(70, 1.75, 22.86)]   // 70 / 3.0625
    [InlineData(50, 1.60, 19.53)]
    [InlineData(95.5, 1.80, 29.48)]
    [InlineData(120, 1.70, 41.52)]
    [InlineData(1, 1, 1)]
    public void Calculate_DevuelveElImcRedondeadoADosDecimales(double weightKg, double heightM, double expectedBmi)
    {
        var result = _calculator.Calculate(weightKg, heightM);

        Assert.Equal(expectedBmi, result.Bmi);
    }

    [Fact]
    public void Calculate_ConservaLosDatosDeEntradaEnLaRespuesta()
    {
        var result = _calculator.Calculate(70, 1.75);

        Assert.Equal(70, result.WeightKg);
        Assert.Equal(1.75, result.HeightM);
        Assert.Equal("Peso normal", result.Category);
    }

    [Theory]
    [InlineData(45, 1.75, "Bajo peso")]        // 14.69
    [InlineData(70, 1.75, "Peso normal")]      // 22.86
    [InlineData(85, 1.75, "Sobrepeso")]        // 27.76
    [InlineData(95, 1.75, "Obesidad grado I")] // 31.02
    [InlineData(110, 1.75, "Obesidad grado II")]  // 35.92
    [InlineData(130, 1.75, "Obesidad grado III")] // 42.45
    public void Calculate_ClasificaSegunLasCategoriasDeLaOms(double weightKg, double heightM, string expectedCategory)
    {
        var result = _calculator.Calculate(weightKg, heightM);

        Assert.Equal(expectedCategory, result.Category);
    }

    [Theory]
    [InlineData(18.49, "Bajo peso")]
    [InlineData(18.5, "Peso normal")]
    [InlineData(24.99, "Peso normal")]
    [InlineData(25, "Sobrepeso")]
    [InlineData(29.99, "Sobrepeso")]
    [InlineData(30, "Obesidad grado I")]
    [InlineData(34.99, "Obesidad grado I")]
    [InlineData(35, "Obesidad grado II")]
    [InlineData(39.99, "Obesidad grado II")]
    [InlineData(40, "Obesidad grado III")]
    public void Classify_RespetaLosLimitesDeCadaCategoria(double bmi, string expectedCategory)
    {
        Assert.Equal(expectedCategory, BmiCalculator.Classify(bmi));
    }

    [Theory]
    [InlineData(0, 1.75)]
    [InlineData(-70, 1.75)]
    [InlineData(double.NaN, 1.75)]
    [InlineData(double.PositiveInfinity, 1.75)]
    public void Calculate_LanzaExcepcionCuandoElPesoNoEsValido(double weightKg, double heightM)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Calculate(weightKg, heightM));
    }

    [Theory]
    [InlineData(70, 0)]
    [InlineData(70, -1.75)]
    [InlineData(70, double.NaN)]
    [InlineData(70, double.PositiveInfinity)]
    public void Calculate_LanzaExcepcionCuandoLaEstaturaNoEsValida(double weightKg, double heightM)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Calculate(weightKg, heightM));
    }
}
