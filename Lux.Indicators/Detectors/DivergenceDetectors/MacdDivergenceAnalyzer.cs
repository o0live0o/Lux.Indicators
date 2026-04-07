using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators.Models;

namespace Lux.Indicators.DivergenceDetectors
{
    /// <summary>
    /// MACD背离分析器
    /// </summary>
    public static class MacdDivergenceAnalyzer
    {
        /// <summary>
        /// 查找MACD背离点
        /// </summary>
        /// <param name="closePrices">收盘价序列</param>
        /// <param name="macdOutputs">MACD分析结果序列</param>
        /// <param name="indicatorSelector">选择用于背离检测的指标值的函数</param>
        /// <param name="lookbackPeriod">回溯周期，默认10</param>
        /// <param name="threshold">背离确认阈值，默认0.1</param>
        /// <returns>背离点列表</returns>
        public static List<DivergenceCommon.DivergencePoint> FindDivergences(
            List<decimal> closePrices,
            List<MacdOutput> macdOutputs,
            Func<MacdOutput, decimal> indicatorSelector = null,
            int lookbackPeriod = 10,
            decimal threshold = 0.1m)
        {
            // 默认使用DIF线进行背离检测
            indicatorSelector ??= output => output.Dif;
            
            var divergences = new List<DivergenceCommon.DivergencePoint>();
            
            if (closePrices == null || macdOutputs == null || 
                closePrices.Count != macdOutputs.Count || closePrices.Count < lookbackPeriod)
            {
                return divergences;
            }
            
            // 获取局部极值点
            var pricePeaks = DivergenceCommon.FindLocalExtrema(closePrices, lookbackPeriod);
            var indicatorPeaks = DivergenceCommon.FindLocalExtrema(macdOutputs.Select(indicatorSelector).ToList(), lookbackPeriod);
            
            // 寻找背离点
            foreach (var pricePeak in pricePeaks)
            {
                foreach (var indicatorPeak in indicatorPeaks)
                {
                    // 检查是否在同一时间段附近
                    if (Math.Abs(pricePeak.Index - indicatorPeak.Index) <= lookbackPeriod / 2)
                    {
                        // 检查是否形成背离
                    var divergence = DivergenceCommon.CheckDivergence(
                        closePrices, 
                        macdOutputs.Select(indicatorSelector).ToList(),
                        pricePeak, 
                        indicatorPeak, 
                        threshold);
                    if (divergence != null && divergence.Type != DivergenceCommon.DivergenceType.None)
                    {
                        divergences.Add(divergence);
                    }
                    }
                }
            }
            
            return divergences;
        }
    }
}