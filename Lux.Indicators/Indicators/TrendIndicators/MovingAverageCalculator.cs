using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators;
using Lux.Indicators.Options;

namespace Lux.Indicators;

/// <summary>
/// 移动平均线分析器
/// </summary>
public class MovingAverageCalculator : ICalculator<MovingAverageResult>
{
    private readonly MovingAverageOptions _options;
    public MovingAverageCalculator(MovingAverageOptions? options = null)
    {
        _options = options ?? new MovingAverageOptions();
        _options.Validate();
    }

    public IEnumerable<MovingAverageResult> Calculate(List<PriceBar> prices)
    {
        if (prices is null || prices.Count() == 0)
            return [];

        var results = new List<MovingAverageResult>();

        var closePrices = prices.Select(p => p.Close).ToList();
        if (closePrices == null || closePrices.Count == 0)
        {
            return [];
        }

        // 计算短期移动平均线
        var shortMaValues = IndicatorCalculator.CalculateSMA(closePrices, _options.ShortPeriod);

        // 计算长期移动平均线
        var longMaValues = IndicatorCalculator.CalculateSMA(closePrices, _options.LongPeriod);

        // 生成结果
        for (int i = 0; i < closePrices.Count; i++)
        {
            var shortMa = shortMaValues[i];
            var longMa = longMaValues[i];

            results.Add(new MovingAverageResult
            {
                Date = prices[i].Date,
                ShortMa = shortMa,
                LongMa = longMa,
            });
        }

        return results;
    }
}