using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators.Models;
using Lux.Indicators.Options;

namespace Lux.Indicators.VolatileIndicators
{
    /// <summary>
    /// 布林带指标分析器
    /// </summary>
    public static class BollingerBandsAnalyzer
    {
        /// <summary>
        /// 分析布林带指标 (使用默认参数)
        /// </summary>
        /// <param name="closePrices">收盘价序列</param>
        /// <returns>布林带分析结果序列</returns>
        public static List<BollingerBandsOutput> Analyze(List<decimal> closePrices)
        {
            return Analyze(closePrices, new BollingerBandsOptions());
        }
        
        /// <summary>
        /// 分析布林带指标 (纯参数模式)
        /// </summary>
        /// <param name="closePrices">收盘价序列</param>
        /// <param name="period">周期</param>
        /// <param name="stdDevMultiplier">标准差倍数</param>
        /// <returns>布林带分析结果序列</returns>
        public static List<BollingerBandsOutput> Analyze(
            List<decimal> closePrices,
            int period,
            decimal stdDevMultiplier)
        {
            var results = new List<BollingerBandsOutput>();
            
            if (closePrices == null || closePrices.Count == 0)
            {
                return results;
            }
            
            // 计算移动平均线 (中轨)
            var maValues = IndicatorCalculator.CalculateSMA(closePrices, period);
            
            // 计算标准差
            var stdDevValues = IndicatorCalculator.CalculateStandardDeviation(closePrices, period);
            
            // 计算上下轨
            for (int i = 0; i < closePrices.Count; i++)
            {
                var middleBand = maValues[i];           // 中轨
                var stdDev = stdDevValues[i];           // 标准差
                var upperBand = middleBand + stdDevMultiplier * stdDev; // 上轨
                var lowerBand = middleBand - stdDevMultiplier * stdDev; // 下轨
                
                BollingerBandsSignalType signal = BollingerBandsSignalType.None;
                
                // 检查股价与布林带的关系
                var currentPrice = closePrices[i];
                
                // 检查是否触及或突破轨道
                if (currentPrice >= upperBand * 0.995m && currentPrice <= upperBand * 1.005m) // 考虑浮点精度
                {
                    signal = BollingerBandsSignalType.TouchUpperBand;
                }
                else if (currentPrice >= lowerBand * 0.995m && currentPrice <= lowerBand * 1.005m)
                {
                    signal = BollingerBandsSignalType.TouchLowerBand;
                }
                else if (currentPrice > upperBand * 1.005m)
                {
                    signal = BollingerBandsSignalType.BreakUpperBand;
                }
                else if (currentPrice < lowerBand * 0.995m)
                {
                    signal = BollingerBandsSignalType.BreakLowerBand;
                }
                
                // 检查布林带收窄（通过比较当前标准差与前几期的平均标准差）
                if (i >= 5) // 至少有5个标准差值才能比较
                {
                    var recentStdDevs = stdDevValues.Skip(i - 5).Take(5).ToList();
                    var avgRecentStdDev = recentStdDevs.Average();
                    
                    // 如果当前标准差明显小于近期平均标准差，则认为是收窄状态
                    if (stdDev < avgRecentStdDev * 0.7m && stdDev > 0)
                    {
                        signal = BollingerBandsSignalType.BandSqueeze;
                    }
                }
                
                // 计算布林带宽度
                var bandWidth = middleBand != 0 ? (upperBand - lowerBand) / middleBand * 100m : 0m; // 以百分比表示，避免除零错误
                
                results.Add(new BollingerBandsOutput
                {
                    MiddleBand = middleBand,
                    UpperBand = upperBand,
                    LowerBand = lowerBand,
                    BandWidth = bandWidth,
                    Signal = signal
                });
            }
            
            return results;
        }
        
        /// <summary>
        /// 分析布林带指标
        /// </summary>
        /// <param name="closePrices">收盘价序列</param>
        /// <param name="options">布林带配置选项</param>
        /// <returns>布林带分析结果序列</returns>
        public static List<BollingerBandsOutput> Analyze(
            List<decimal> closePrices,
            BollingerBandsOptions options)
        {
            options.Validate();
            
            return Analyze(closePrices, options.Period, options.StdDevMultiplier);
        }
    }
}