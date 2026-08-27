using BmiApi.Models;

namespace BmiApi.Services;

/// <summary>Calcula el Índice de Masa Corporal y su clasificación.</summary>
public interface IBmiCalculator
{
    BmiResponse Calculate(double weightKg, double heightM);
}
