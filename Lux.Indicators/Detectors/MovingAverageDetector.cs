
using Lux.Indicators;

public class MovingAverageDetector : IDetector<MacdResult>
{
    public void Detect(List<MacdResult> inputs)
    {
        throw new NotImplementedException();
    }

    public void Detect(List<PriceBar> prices)
    {
        MacdCalculator calculator = new MacdCalculator();
        var result = calculator.Calculate(prices);
        Detect(result.ToList());
    }
}