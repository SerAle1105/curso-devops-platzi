using System.ComponentModel.DataAnnotations;

namespace BmiApi.Models;

/// <summary>Datos de entrada para el cálculo del Índice de Masa Corporal.</summary>
public class BmiRequest
{
    /// <summary>Peso de la persona en kilogramos.</summary>
    /// <example>70</example>
    [Required(ErrorMessage = "El peso es obligatorio.")]
    [Range(1, 500, ErrorMessage = "El peso debe estar entre 1 y 500 kg.")]
    public double WeightKg { get; set; }

    /// <summary>Estatura de la persona en metros.</summary>
    /// <example>1.75</example>
    [Required(ErrorMessage = "La estatura es obligatoria.")]
    [Range(0.5, 2.5, ErrorMessage = "La estatura debe estar entre 0.5 y 2.5 m.")]
    public double HeightM { get; set; }
}
