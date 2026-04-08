using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators;
using Lux.Indicators.Options;

namespace Lux.Indicators;

/// <summary>
/// 移动平均线分析器
/// </summary>
public class MovingAverageCalculator : IIndicatorCalculator<MovingAverageResult>
{
    private readonly MovingAverageOptions _options;
    public MovingAverageCalculator(MovingAverageOptions? options = null)
    {
        _options = options ?? new MovingAverageOptions();
        _options.Validate();
    }

    public List<MovingAverageResult> Calculate(IReadOnlyList<PriceBar> datas)
    {
        ArgumentNullException.ThrowIfNull(datas);
        if (datas.Count() == 0)
            return [];


        var closePrices = datas.Select(p => p.Close).ToList();
        // 计算短期移动平均线
        var shortMaValues = IndicatorCalculator.CalculateSMA(closePrices, _options.ShortPeriod);
        // 计算长期移动平均线
        var longMaValues = IndicatorCalculator.CalculateSMA(closePrices, _options.LongPeriod);

        // 生成结果
        var results = new List<MovingAverageResult>();
        for (int i = 0; i < closePrices.Count; i++)
        {
            results.Add(new MovingAverageResult
            {
                Date = datas[i].Date,
                ShortMa = shortMaValues[i],
                LongMa = longMaValues[i],
            });
        }

        return results;
    }
}