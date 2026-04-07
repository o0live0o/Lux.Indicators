namespace Lux.Indicators;

public interface IDetector
{
    void Detect(List<PriceBar> prices);
}

public interface IDetector<TInput> : IDetector
{
    void Detect(List<TInput> inputs);
}