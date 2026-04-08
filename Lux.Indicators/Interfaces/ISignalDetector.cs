namespace Lux.Indicators;

public interface ISignalDetector
{
    List<Signal> Detect(IReadOnlyList<PriceBar> datas);
}

public interface IDetector<TInput> : ISignalDetector
{
    List<Signal> Detect(IReadOnlyList<TInput> datas);
}