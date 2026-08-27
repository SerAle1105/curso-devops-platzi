using BmiApi.Models;

namespace BmiApi.Services;

/// <inheritdoc />
public class BmiCalculator : IBmiCalculator
{
    /// <summary>
    /// Aplica la fórmula IMC = peso / estatura^2 y clasifica el resultado
    /// según las categorías de la Organización Mundial de la Salud.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Si el peso o la estatura no son valores positivos y finitos.
    /// </exception>
    public BmiResponse Calculate(double weightKg, double heightM)
    {
        if (!double.IsFinite(weightKg) || weightKg <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weightKg), "El peso debe ser mayor que cero.");
        }

        if (!double.IsFinite(heightM) || heightM <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(heightM), "La estatura debe ser mayor que cero.");
        }

        var bmi = Math.Round(weightKg / (heightM * heightM), 2, MidpointRounding.AwayFromZero);

        return new BmiResponse(weightKg, heightM, bmi, Classify(bmi));
    }

    /// <summary>Devuelve la categoría de la OMS correspondiente a un IMC.</summary>
    public static string Classify(double bmi) => bmi switch
    {
        < 18.5 => "Bajo peso",
        < 25 => "Peso normal",
        < 30 => "Sobrepeso",
        < 35 => "Obesidad grado I",
        < 40 => "Obesidad grado II",
        _ => "Obesidad grado III",
    };
}
