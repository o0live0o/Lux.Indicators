using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators;
using Lux.Indicators.Options;

namespace Lux.Indicators;

public class BollingerBandsCalculator : IIndicatorCalculator<BollingerBandsResult>
{
    private readonly BollingerBandsOptions _options;

    public BollingerBandsCalculator(BollingerBandsOptions? options = default)
    {
        _options = options ?? new BollingerBandsOptions();
        _options.Validate();
    }

    public List<BollingerBandsResult> Calculate(IReadOnlyList<PriceBar> datas)
    {
        ArgumentNullException.ThrowIfNull(datas);
        if (datas.Count() == 0)
            return [];


        var closePrices = datas.Select(p => p.Close).ToList();

        // 计算移动平均线 (中轨)
        var maValues = IndicatorCalculator.CalculateSMA(closePrices, _options.Period);

        // 计算标准差
        var stdDevValues = IndicatorCalculator.CalculateStandardDeviation(closePrices, _options.Period);

        var results = new List<BollingerBandsResult>();

        // 计算上下轨
        for (int i = 0; i < closePrices.Count; i++)
        {
            var middleBand = maValues[i];           // 中轨
            var stdDev = stdDevValues[i];           // 标准差
            var upperBand = middleBand + _options.StdDevMultiplier * stdDev; // 上轨
            var lowerBand = middleBand - _options.StdDevMultiplier * stdDev; // 下轨

            // 计算布林带宽度
            var bandWidth = middleBand != 0 ? (upperBand - lowerBand) / middleBand * 100d : 0d; // 以百分比表示，避免除零错误

            results.Add(new BollingerBandsResult
            {
                Date = datas[i].Date,
                MiddleBand = middleBand,
                UpperBand = upperBand,
                LowerBand = lowerBand,
                BandWidth = bandWidth,
            });
        }

        return results;
    }
}