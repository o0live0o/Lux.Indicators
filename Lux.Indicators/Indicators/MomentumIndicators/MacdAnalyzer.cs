using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators.Models;
using Lux.Indicators.Options;

namespace Lux.Indicators.MomentumIndicators
{
    /// <summary>
    /// MACD指标分析器
    /// </summary>
    public static class MacdAnalyzer
    {
        /// <summary>
        /// 分析MACD指标 (使用默认参数)
        /// </summary>
        /// <param name="closePrices">收盘价序列</param>
        /// <returns>MACD分析结果序列</returns>
        public static List<MacdOutput> Analyze(List<decimal> closePrices)
        {
            return Analyze(closePrices, new MacdOptions());
        }
        
        /// <summary>
        /// 分析MACD指标 (纯参数模式)
        /// </summary>
        /// <param name="closePrices">收盘价序列</param>
        /// <param name="fastPeriod">快速EMA周期</param>
        /// <param name="slowPeriod">慢速EMA周期</param>
        /// <param name="signalPeriod">信号线周期</param>
        /// <returns>MACD分析结果序列</returns>
        public static List<MacdOutput> Analyze(
            List<decimal> closePrices, 
            int fastPeriod,
            int slowPeriod,
            int signalPeriod)
        {
            var results = new List<MacdOutput>();
            
            if (closePrices == null || closePrices.Count == 0)
            {
                return results;
            }
            
            // 计算快速EMA
            var fastEMA = IndicatorCalculator.CalculateEMA(closePrices, fastPeriod);
            
            // 计算慢速EMA
            var slowEMA = IndicatorCalculator.CalculateEMA(closePrices, slowPeriod);
            
            // 计算DIF线 (快速EMA - 慢速EMA)
            var difValues = new List<decimal>();
            for (int i = 0; i < closePrices.Count; i++)
            {
                if (i >= slowPeriod - 1) // 确保两个EMA都有有效值
                {
                    difValues.Add(fastEMA[i] - slowEMA[i]);
                }
                else
                {
                    difValues.Add(0);
                }
            }
            
            // 计算DEA线 (DIF的EMA)
            var deaValues = IndicatorCalculator.CalculateEMA(difValues, signalPeriod);
            
            // 计算MACD柱状图
            for (int i = 0; i < closePrices.Count; i++)
            {
                var dif = difValues[i];
                var dea = deaValues[i];
                var histogram = 2 * (dif - dea);
                
                MacdSignalType signal = MacdSignalType.None;
                
                // 判断是否产生金叉或死叉信号
                if (i > 0 && deaValues[i-1] != 0 && deaValues[i] != 0)
                {
                    var prevDif = difValues[i - 1];
                    var currDif = dif;
                    var prevDea = deaValues[i - 1];
                    var currDea = deaValues[i];
                    
                    // 金叉：DIF从下方上穿DEA
                    if (prevDif <= prevDea && currDif > currDea)
                    {
                        signal = MacdSignalType.GoldenCross;
                    }
                    // 死叉：DIF从上方下穿DEA
                    else if (prevDif >= prevDea && currDif < currDea)
                    {
                        signal = MacdSignalType.DeathCross;
                    }
                }
                
                results.Add(new MacdOutput
                {
                    Dif = dif,
                    Dea = dea,
                    Histogram = histogram,
                    Signal = signal
                });
            }
            
            return results;
        }
        
        /// <summary>
        /// 分析MACD指标
        /// </summary>
        /// <param name="closePrices">收盘价序列</param>
        /// <param name="options">MACD配置选项</param>
        /// <returns>MACD分析结果序列</returns>
        public static List<MacdOutput> Analyze(
            List<decimal> closePrices, 
            MacdOptions options)
        {
            options.Validate();
            
            return Analyze(closePrices, options.FastPeriod, options.SlowPeriod, options.SignalPeriod);
        }
    }
}