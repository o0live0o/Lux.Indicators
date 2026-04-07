using Lux.Indicators;

public interface ICalculator<TResult>
{
    IEnumerable<TResult> Calculate(List<PriceBar> prices);
}