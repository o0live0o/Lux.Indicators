using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators;
using Lux.Indicators.Options;

namespace Lux.Indicators;

public class BollingerBandsCalculator : ICalculator<BollingerBandsResult>
{
    private readonly BollingerBandsOptions _options;

    public BollingerBandsCalculator(BollingerBandsOptions? options = default)
    {
        _options = options ?? new BollingerBandsOptions();
        _options.Validate();
    }

    public IEnumerable<BollingerBandsResult> Calculate(List<PriceBar> prices)
    {
        if (prices is null || prices.Count() == 0)
            return [];
            
        var results = new List<BollingerBandsResult>();

        var closePrices = prices.Select(p => p.Close).ToList();

        // 计算移动平均线 (中轨)
        var maValues = IndicatorCalculator.CalculateSMA(closePrices, _options.Period);

        // 计算标准差
        var stdDevValues = IndicatorCalculator.CalculateStandardDeviation(closePrices, _options.Period);

        // 计算上下轨
        for (int i = 0; i < closePrices.Count; i++)
        {
            var middleBand = maValues[i];           // 中轨
            var stdDev = stdDevValues[i];           // 标准差
            var upperBand = middleBand + _options.StdDevMultiplier * stdDev; // 上轨
            var lowerBand = middleBand - _options.StdDevMultiplier * stdDev; // 下轨

            // 计算布林带宽度
            var bandWidth = middleBand != 0 ? (upperBand - lowerBand) / middleBand * 100m : 0m; // 以百分比表示，避免除零错误

            results.Add(new BollingerBandsResult
            {
                Date = prices[i].Date,
                MiddleBand = middleBand,
                UpperBand = upperBand,
                LowerBand = lowerBand,
                BandWidth = bandWidth,
            });
        }

        return results;
    }
}