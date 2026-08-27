namespace BmiApi.Models;

/// <summary>Resultado del cálculo del Índice de Masa Corporal.</summary>
/// <param name="WeightKg">Peso recibido, en kilogramos.</param>
/// <param name="HeightM">Estatura recibida, en metros.</param>
/// <param name="Bmi">IMC calculado, redondeado a dos decimales.</param>
/// <param name="Category">Clasificación del IMC según la OMS.</param>
public record BmiResponse(double WeightKg, double HeightM, double Bmi, string Category);
