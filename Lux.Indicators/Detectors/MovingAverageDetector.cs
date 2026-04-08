
using Lux.Indicators;
using Lux.Indicators.Options;

public class MovingAverageDetector : IDetector<MovingAverageResult>
{
    private readonly Lazy<MovingAverageCalculator> _calculator;
    public MovingAverageDetector(MovingAverageOptions? options = default)
    {
        _calculator = new Lazy<MovingAverageCalculator>(() => new MovingAverageCalculator(options ?? new MovingAverageOptions()));
    }

    public List<Signal> Detect(IReadOnlyList<MovingAverageResult> datas)
    {
        throw new NotImplementedException();
    }

    public List<Signal> Detect(IReadOnlyList<PriceBar> datas)
    {
        return Detect(_calculator.Value.Calculate(datas));
    }
}