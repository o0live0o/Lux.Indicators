using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators;
using Lux.Indicators.Options;

namespace Lux.Indicators;

/// <summary>
/// MACD指标分析器
/// </summary>
public class MacdCalculator : IIndicatorCalculator<MacdResult>
{
    private readonly MacdOptions _options;
    public MacdCalculator(MacdOptions? options = default)
    {
        _options = options ?? new MacdOptions();
        _options.Validate();
    }

    public List<MacdResult> Calculate(IReadOnlyList<PriceBar> datas)
    {
        ArgumentNullException.ThrowIfNull(datas);
        if (datas.Count() == 0)
            return [];


        var closePrices = datas.Select(p => p.Close).ToList();

        // 计算快速EMA
        var fastEMA = IndicatorCalculator.CalculateEMA(closePrices, _options.FastPeriod);

        // 计算慢速EMA
        var slowEMA = IndicatorCalculator.CalculateEMA(closePrices, _options.SlowPeriod);

        // 计算DIF线 (快速EMA - 慢速EMA)
        var difValues = new List<double>();
        for (int i = 0; i < closePrices.Count; i++)
        {
            // 只有当两个EMA都有有效值时才计算DIF
            if (i >= Math.Max(_options.FastPeriod - 1, _options.SlowPeriod - 1))
            {
                difValues.Add(fastEMA[i] - slowEMA[i]);
            }
            else
            {
                // 在初始阶段，填充0表示暂无有效值
                difValues.Add(0);
            }
        }

        // 计算DEA线 (DIF的EMA)
        var deaValues = IndicatorCalculator.CalculateEMA(difValues, _options.SignalPeriod);

        var results = new List<MacdResult>();
        // 计算MACD柱状图及信号
        for (int i = 0; i < closePrices.Count; i++)
        {
            var dif = difValues[i];
            var dea = deaValues[i];

            // 计算柱状图 (通常为 2*(DIF-DEA)，不同软件可能有不同的倍数)
            var histogram = 2 * (dif - dea);

            results.Add(new MacdResult
            {
                Date = datas[i].Date,
                Dif = dif,
                Dea = dea,
                Histogram = histogram
            });
        }
        return results;
    }
}