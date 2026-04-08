using Lux.Indicators;

public interface IIndicatorCalculator<TResult>
{
    List<TResult> Calculate(IReadOnlyList<PriceBar> datas);
}