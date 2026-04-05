using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators.Models;
using Lux.Indicators.Options;

namespace Lux.Indicators.TrendIndicators
{
    /// <summary>
    /// 移动平均线分析器
    /// </summary>
    public static class MovingAverageAnalyzer
    {
        /// <summary>
        /// 分析移动平均线指标 (使用默认参数)
        /// </summary>
        /// <param name="closePrices">收盘价序列</param>
        /// <returns>移动平均线分析结果序列</returns>
        public static List<MovingAverageOutput> Analyze(List<decimal> closePrices)
        {
            return Analyze(closePrices, new MovingAverageOptions());
        }
        
        /// <summary>
        /// 分析移动平均线指标 (纯参数模式)
        /// </summary>
        /// <param name="closePrices">收盘价序列</param>
        /// <param name="shortPeriod">短期均线周期</param>
        /// <param name="longPeriod">长期均线周期</param>
        /// <returns>移动平均线分析结果序列</returns>
        public static List<MovingAverageOutput> Analyze(
            List<decimal> closePrices,
            int shortPeriod,
            int longPeriod)
        {
            var results = new List<MovingAverageOutput>();
            
            if (closePrices == null || closePrices.Count == 0)
            {
                return results;
            }
            
            // 计算短期移动平均线
            var shortMaValues = IndicatorCalculator.CalculateSMA(closePrices, shortPeriod);
            
            // 计算长期移动平均线
            var longMaValues = IndicatorCalculator.CalculateSMA(closePrices, longPeriod);
            
            // 生成结果
            for (int i = 0; i < closePrices.Count; i++)
            {
                var shortMa = shortMaValues[i];
                var longMa = longMaValues[i];
                
                MovingAverageSignalType signal = MovingAverageSignalType.None;
                
                // 判断均线排列和交叉情况
                if (shortMa > 0 && longMa > 0) // 确保均线值有效
                {
                    if (i > 0)
                    {
                        var prevShortMa = shortMaValues[i - 1];
                        var prevLongMa = longMaValues[i - 1];
                        
                        // 金叉：短期均线上穿长期均线
                        if (prevShortMa <= prevLongMa && shortMa > longMa)
                        {
                            signal = MovingAverageSignalType.GoldenCross;
                        }
                        // 死叉：短期均线下穿长期均线
                        else if (prevShortMa >= prevLongMa && shortMa < longMa)
                        {
                            signal = MovingAverageSignalType.DeathCross;
                        }
                    }
                    
                    // 多头排列：短期均线 > 长期均线
                    if (shortMa > longMa && signal == MovingAverageSignalType.None)
                    {
                        signal = MovingAverageSignalType.Bullish;
                    }
                    // 空头排列：短期均线 < 长期均线
                    else if (shortMa < longMa && signal == MovingAverageSignalType.None)
                    {
                        signal = MovingAverageSignalType.Bearish;
                    }
                }
                
                results.Add(new MovingAverageOutput
                {
                    ShortMa = shortMa,
                    LongMa = longMa,
                    Signal = signal
                });
            }
            
            return results;
        }
        
        /// <summary>
        /// 分析移动平均线指标
        /// </summary>
        /// <param name="closePrices">收盘价序列</param>
        /// <param name="options">移动平均线配置选项</param>
        /// <returns>移动平均线分析结果序列</returns>
        public static List<MovingAverageOutput> Analyze(
            List<decimal> closePrices,
            MovingAverageOptions options)
        {
            options.Validate();
            
            return Analyze(closePrices, options.ShortPeriod, options.LongPeriod);
        }
    }
}