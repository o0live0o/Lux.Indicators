using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators;
using Lux.Indicators.Options;

namespace Lux.Indicators;

public class KdjCalculator : IIndicatorCalculator<KdjResult>
{
    private readonly KdjOptions _options;
    public KdjCalculator(KdjOptions? options = default)
    {
        _options = options ?? new KdjOptions();
        _options.Validate();
    }

    public List<KdjResult> Calculate(IReadOnlyList<PriceBar> datas)
    {
        ArgumentNullException.ThrowIfNull(datas);

        if (datas.Count() == 0)
            return [];

        var count = datas.Count;
        var highPrices = datas.Select(p => p.High).ToList();
        var lowPrices = datas.Select(p => p.Low).ToList();
        var closePrices = datas.Select(p => p.Close).ToList();

        // 计算RSV (未成熟随机值)
        var rsvValues = new List<double>();
        for (int i = 0; i < count; i++)
        {
            if (i < _options.RsvPeriod - 1)
            {
                rsvValues.Add(50d); // 前期不足周期数的数据设为50
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
                    rsvValues.Add(50d);
                }
                else
                {
                    var rsv = ((closePrices[i] - lowestLow) / (highestHigh - lowestLow)) * 100d;
                    rsvValues.Add(Math.Min(100d, Math.Max(0d, rsv))); // 限制在0-100之间
                }
            }
        }

        // 计算K值 (RSV的3日移动平均)
        var kValues = new List<double>();
        for (int i = 0; i < count; i++)
        {
            if (i == 0)
            {
                kValues.Add(50d); // 初始K值设为50
            }
            else
            {
                // K = 2/3 * 前一日K值 + 1/3 * 当日RSV
                var kValue = (2d / 3d) * kValues[i - 1] + (1d / 3d) * rsvValues[i];
                kValues.Add(kValue);
            }
        }

        // 计算D值 (K值的3日移动平均)
        var dValues = new List<double>();
        for (int i = 0; i < count; i++)
        {
            if (i == 0)
            {
                dValues.Add(50d); // 初始D值设为50
            }
            else
            {
                // D = 2/3 * 前一日D值 + 1/3 * 当日K值
                var dValue = (2d / 3d) * dValues[i - 1] + (1d / 3d) * kValues[i];
                dValues.Add(dValue);
            }
        }

        // 计算J值 (3 * K - 2 * D)
        var jValues = new List<double>();
        for (int i = 0; i < count; i++)
        {
            var jValue = 3d * kValues[i] - 2d * dValues[i];
            jValues.Add(jValue);
        }

        // 生成结果
        var results = new List<KdjResult>();
        for (int i = 0; i < count; i++)
        {
            results.Add(new KdjResult
            {
                Date = datas[i].Date,
                K = kValues[i],
                D = dValues[i],
                J = jValues[i],
            });
        }

        return results;
    }
}
