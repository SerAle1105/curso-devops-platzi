namespace BmiApi.Models;

/// <summary>Resultado del cálculo del Índice de Masa Corporal.</summary>
public class BmiResponse
{
    public BmiResponse(double weightKg, double heightM, double bmi, string category)
    {
        WeightKg = weightKg;
        HeightM = heightM;
        Bmi = bmi;
        Category = category;
    }

    /// <summary>Peso recibido, en kilogramos.</summary>
    /// <example>70</example>
    public double WeightKg { get; }

    /// <summary>Estatura recibida, en metros.</summary>
    /// <example>1.75</example>
    public double HeightM { get; }

    /// <summary>IMC calculado, redondeado a dos decimales.</summary>
    /// <example>22.86</example>
    public double Bmi { get; }

    /// <summary>Clasificación del IMC según la OMS.</summary>
    /// <example>Peso normal</example>
    public string Category { get; }
}
