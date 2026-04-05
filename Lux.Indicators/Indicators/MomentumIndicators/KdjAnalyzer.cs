using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators.Models;
using Lux.Indicators.Options;

namespace Lux.Indicators.MomentumIndicators
{
    /// <summary>
    /// KDJ指标分析器
    /// </summary>
    public static class KdjAnalyzer
    {
        /// <summary>
        /// 分析KDJ指标 (使用默认参数)
        /// </summary>
        /// <param name="highPrices">最高价序列</param>
        /// <param name="lowPrices">最低价序列</param>
        /// <param name="closePrices">收盘价序列</param>
        /// <returns>KDJ分析结果序列</returns>
        public static List<KdjOutput> Analyze(
            List<decimal> highPrices,
            List<decimal> lowPrices,
            List<decimal> closePrices)
        {
            return Analyze(highPrices, lowPrices, closePrices, new KdjOptions());
        }
        
        /// <summary>
        /// 分析KDJ指标 (纯参数模式)
        /// </summary>
        /// <param name="highPrices">最高价序列</param>
        /// <param name="lowPrices">最低价序列</param>
        /// <param name="closePrices">收盘价序列</param>
        /// <param name="rsvPeriod">RSV周期</param>
        /// <param name="kPeriod">K值平滑周期</param>
        /// <param name="dPeriod">D值平滑周期</param>
        /// <returns>KDJ分析结果序列</returns>
        public static List<KdjOutput> Analyze(
            List<decimal> highPrices,
            List<decimal> lowPrices,
            List<decimal> closePrices,
            int rsvPeriod,
            int kPeriod,
            int dPeriod)
        {
            var results = new List<KdjOutput>();
            
            if (highPrices == null || lowPrices == null || closePrices == null ||
                highPrices.Count != lowPrices.Count || highPrices.Count != closePrices.Count)
            {
                return results;
            }
            
            var count = highPrices.Count;
            
            // 计算RSV (未成熟随机值)
            var rsvValues = new List<decimal>();
            for (int i = 0; i < count; i++)
            {
                if (i < rsvPeriod - 1)
                {
                    rsvValues.Add(50m); // 前期不足周期数的数据设为50
                }
                else
                {
                    // 找到周期内的最高价和最低价
                    var startIndex = i - rsvPeriod + 1;
                    var periodHighs = highPrices.Skip(startIndex).Take(rsvPeriod).ToList();
                    var periodLows = lowPrices.Skip(startIndex).Take(rsvPeriod).ToList();
                    
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
                KdjSignalType signal = KdjSignalType.None;
                
                // 判断超买超卖信号
                if (kValues[i] <= 20 && dValues[i] <= 20)
                {
                    signal = KdjSignalType.OversoldBuy;
                }
                else if (kValues[i] >= 80 && dValues[i] >= 80)
                {
                    signal = KdjSignalType.OverboughtSell;
                }
                
                // 判断金叉死叉信号
                if (i > 0)
                {
                    var prevK = kValues[i - 1];
                    var currK = kValues[i];
                    var prevD = dValues[i - 1];
                    var currD = dValues[i];
                    
                    // K线上穿D线 (金叉)
                    if (prevK <= prevD && currK > currD)
                    {
                        signal = KdjSignalType.GoldenCross;
                    }
                    // K线下穿D线 (死叉)
                    else if (prevK >= prevD && currK < currD)
                    {
                        signal = KdjSignalType.DeathCross;
                    }
                }
                
                results.Add(new KdjOutput
                {
                    K = kValues[i],
                    D = dValues[i],
                    J = jValues[i],
                    Signal = signal
                });
            }
            
            return results;
        }
        
        /// <summary>
        /// 分析KDJ指标
        /// </summary>
        /// <param name="highPrices">最高价序列</param>
        /// <param name="lowPrices">最低价序列</param>
        /// <param name="closePrices">收盘价序列</param>
        /// <param name="options">KDJ配置选项</param>
        /// <returns>KDJ分析结果序列</returns>
        public static List<KdjOutput> Analyze(
            List<decimal> highPrices,
            List<decimal> lowPrices,
            List<decimal> closePrices,
            KdjOptions options)
        {
            options.Validate();
            
            return Analyze(highPrices, lowPrices, closePrices, options.RsvPeriod, options.KPeriod, options.DPeriod);
        }
    }
}