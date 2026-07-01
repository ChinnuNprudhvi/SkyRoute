namespace SkyRoute.Application.Interfaces;

public interface IPricingStrategy
{
    decimal CalculatePrice(decimal baseFare);
}
