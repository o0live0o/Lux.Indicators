using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators;
using Lux.Indicators.Options;

namespace Lux.Indicators;

public class KdjCalculator : ICalculator<KdjResult>
{
    private readonly KdjOptions _options;
    public KdjCalculator(KdjOptions? options = default)
    {
        _options = options ?? new KdjOptions();
        _options.Validate();
    }

    public IEnumerable<KdjResult> Calculate(List<PriceBar> prices)
    {
        if (prices is null || prices.Count() == 0)
            return [];

        var results = new List<KdjResult>();
        var count = prices.Count;
        var highPrices = prices.Select(p => p.High).ToList();
        var lowPrices = prices.Select(p => p.Low).ToList();
        var closePrices = prices.Select(p => p.Close).ToList();

        // 计算RSV (未成熟随机值)
        var rsvValues = new List<decimal>();
        for (int i = 0; i < count; i++)
        {
            if (i < _options.RsvPeriod - 1)
            {
                rsvValues.Add(50m); // 前期不足周期数的数据设为50
            }
            else
            {
                // 找到周期内的最高价和最低价
                var startIndex = i - _options.RsvPeriod + 1;
                var periodHighs = highPrices.Skip(startIndex).Take(_options.RsvPeriod).ToList();
                var periodLows = lowPrices.Skip(startIndex).Take(_options.RsvPeriod).ToList();

                var highestHigh = periodHighs.Max();
                var lowestLow = periodLows.Min();

                // 防止除零错误
                if (highestHigh == lowestLow)
                {
                    rsvValues.Add(50m);
                }
                else
                {
                    var rsv = ((closePrices[i] - lowestLow) / (highestHigh - lowestLow)) * 100m;
                    rsvValues.Add(Math.Min(100m, Math.Max(0m, rsv))); // 限制在0-100之间
                }
            }
        }

        // 计算K值 (RSV的3日移动平均)
        var kValues = new List<decimal>();
        for (int i = 0; i < count; i++)
        {
            if (i == 0)
            {
                kValues.Add(50m); // 初始K值设为50
            }
            else
            {
                // K = 2/3 * 前一日K值 + 1/3 * 当日RSV
                var kValue = (2m / 3m) * kValues[i - 1] + (1m / 3m) * rsvValues[i];
                kValues.Add(kValue);
            }
        }

        // 计算D值 (K值的3日移动平均)
        var dValues = new List<decimal>();
        for (int i = 0; i < count; i++)
        {
            if (i == 0)
            {
                dValues.Add(50m); // 初始D值设为50
            }
            else
            {
                // D = 2/3 * 前一日D值 + 1/3 * 当日K值
                var dValue = (2m / 3m) * dValues[i - 1] + (1m / 3m) * kValues[i];
                dValues.Add(dValue);
            }
        }

        // 计算J值 (3 * K - 2 * D)
        var jValues = new List<decimal>();
        for (int i = 0; i < count; i++)
        {
            var jValue = 3m * kValues[i] - 2m * dValues[i];
            jValues.Add(jValue);
        }

        // 生成结果
        for (int i = 0; i < count; i++)
        {
            results.Add(new KdjResult
            {
                Date = prices[i].Date,
                K = kValues[i],
                D = dValues[i],
                J = jValues[i],
            });
        }

        return results;
    }
}
