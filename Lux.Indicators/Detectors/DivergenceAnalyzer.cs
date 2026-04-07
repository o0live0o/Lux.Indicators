using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators.Models;

namespace Lux.Indicators
{
    /// <summary>
    /// 背离检测相关的公共类
    /// </summary>
    public static class DivergenceCommon
    {
        /// <summary>
        /// 背离类型
        /// </summary>
        public enum DivergenceType
        {
            /// <summary>
            /// 顶背离 - 价格创新高但指标未创新高
            /// </summary>
            BearishDivergence,
            
            /// <summary>
            /// 底背离 - 价格创新低但指标未创新低
            /// </summary>
            BullishDivergence,
            
            /// <summary>
            /// 无背离
            /// </summary>
            None
        }
        
        /// <summary>
        /// 背离点信息
        /// </summary>
        public class DivergencePoint
        {
            /// <summary>
            /// 背离类型
            /// </summary>
            public DivergenceType Type { get; set; }
            
            /// <summary>
            /// 价格峰值索引
            /// </summary>
            public int PricePeakIndex { get; set; }
            
            /// <summary>
            /// 指标峰值索引
            /// </summary>
            public int IndicatorPeakIndex { get; set; }
            
            /// <summary>
            /// 价格峰值
            /// </summary>
            public decimal PricePeak { get; set; }
            
            /// <summary>
            /// 指标峰值
            /// </summary>
            public decimal IndicatorPeak { get; set; }
            
            /// <summary>
            /// 描述信息
            /// </summary>
            public string Description { get; set; } = string.Empty;
        }
        
        /// <summary>
        /// 查找局部极值点
        /// </summary>
        /// <param name="values">数值序列</param>
        /// <param name="lookbackPeriod">回溯周期</param>
        /// <returns>局部极值点列表</returns>
        public static List<(int Index, decimal Value, bool IsPeak)> FindLocalExtrema(List<decimal> values, int lookbackPeriod)
        {
            var extrema = new List<(int Index, decimal Value, bool IsPeak)>();
            
            if (values == null || values.Count < lookbackPeriod * 2)
            {
                return extrema;
            }
            
            for (int i = lookbackPeriod; i < values.Count - lookbackPeriod; i++)
            {
                bool isPeak = true;
                bool isValley = true;
                
                // 检查是否为峰值
                for (int j = i - lookbackPeriod; j <= i + lookbackPeriod; j++)
                {
                    if (j != i)
                    {
                        if (values[j] >= values[i])
                        {
                            isPeak = false;
                        }
                        if (values[j] <= values[i])
                        {
                            isValley = false;
                        }
                    }
                }
                
                if (isPeak)
                {
                    extrema.Add((i, values[i], true)); // 峰值
                }
                else if (isValley)
                {
                    extrema.Add((i, values[i], false)); // 谷值
                }
            }
            
            return extrema;
        }
        
        /// <summary>
        /// 查找附近的极值点
        /// </summary>
        /// <param name="values">数值序列</param>
        /// <param name="centerIndex">中心索引</param>
        /// <param name="range">范围</param>
        /// <param name="findPeaks">是否查找峰值，否则查找谷值</param>
        /// <returns>附近的极值点</returns>
        public static List<(int Index, decimal Value)> FindNearbyPeaks(List<decimal> values, int centerIndex, int range, bool findPeaks)
        {
            var peaks = new List<(int Index, decimal Value)>();
            var start = Math.Max(0, centerIndex - range);
            var end = Math.Min(values.Count - 1, centerIndex + range);
            
            for (int i = start; i <= end; i++)
            {
                bool isExtreme = true;
                
                // 检查是否为极值
                for (int j = Math.Max(0, i - 1); j <= Math.Min(values.Count - 1, i + 1); j++)
                {
                    if (j != i)
                    {
                        if (findPeaks && values[j] >= values[i])
                        {
                            isExtreme = false;
                            break;
                        }
                        if (!findPeaks && values[j] <= values[i])
                        {
                            isExtreme = false;
                            break;
                        }
                    }
                }
                
                if (isExtreme)
                {
                    peaks.Add((i, values[i]));
                }
            }
            
            return peaks;
        }
        
        /// <summary>
        /// 检查是否形成背离
        /// </summary>
        /// <param name="prices">价格序列</param>
        /// <param name="indicatorValues">指标值序列</param>
        /// <param name="pricePeak">价格极值点</param>
        /// <param name="indicatorPeak">指标极值点</param>
        /// <param name="threshold">确认阈值</param>
        /// <returns>背离点信息</returns>
        public static DivergencePoint CheckDivergence(
            List<decimal> prices,
            List<decimal> indicatorValues,
            (int Index, decimal Value, bool IsPeak) pricePeak,
            (int Index, decimal Value, bool IsPeak) indicatorPeak,
            decimal threshold)
        {
            var divergence = new DivergencePoint
            {
                PricePeakIndex = pricePeak.Index,
                IndicatorPeakIndex = indicatorPeak.Index,
                PricePeak = pricePeak.Value,
                IndicatorPeak = indicatorPeak.Value
            };
            
            // 检查顶背离：价格创更高高点，但指标未创更高高点
            if (pricePeak.IsPeak && indicatorPeak.IsPeak)
            {
                // 查找附近的高点进行比较
                var nearbyPriceHighs = FindNearbyPeaks(prices, pricePeak.Index, 5, true);
                var nearbyIndicatorHighs = FindNearbyPeaks(indicatorValues, indicatorPeak.Index, 5, true);
                
                if (nearbyPriceHighs.Any() && nearbyIndicatorHighs.Any())
                {
                    var maxPrice = nearbyPriceHighs.Max(x => x.Value);
                    var maxIndicator = nearbyIndicatorHighs.Max(x => x.Value);
                    
                    // 如果价格更高但指标更低，则为顶背离
                    if (maxPrice > pricePeak.Value && maxIndicator < indicatorPeak.Value)
                    {
                        divergence.Type = DivergenceCommon.DivergenceType.BearishDivergence;
                        divergence.Description = $"顶背离: 价格创新高({maxPrice:F4})但指标未创新高({maxIndicator:F4})";
                        return divergence;
                    }
                }
            }
            // 检查底背离：价格创更低低点，但指标未创更低低点
            else if (!pricePeak.IsPeak && !indicatorPeak.IsPeak)
            {
                // 查找附近的低点进行比较
                var nearbyPriceLows = FindNearbyPeaks(prices, pricePeak.Index, 5, false);
                var nearbyIndicatorLows = FindNearbyPeaks(indicatorValues, indicatorPeak.Index, 5, false);
                
                if (nearbyPriceLows.Any() && nearbyIndicatorLows.Any())
                {
                    var minPrice = nearbyPriceLows.Min(x => x.Value);
                    var minIndicator = nearbyIndicatorLows.Min(x => x.Value);
                    
                    // 如果价格更低但指标更高，则为底背离
                    if (minPrice < pricePeak.Value && minIndicator > indicatorPeak.Value)
                    {
                        divergence.Type = DivergenceCommon.DivergenceType.BullishDivergence;
                        divergence.Description = $"底背离: 价格创新低({minPrice:F4})但指标未创新低({minIndicator:F4})";
                        return divergence;
                    }
                }
            }
            
            return new DivergencePoint { Type = DivergenceCommon.DivergenceType.None };
        }
    }
}