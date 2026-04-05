using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators.Models;

namespace Lux.Indicators
{
    /// <summary>
    /// 技术指标计算工具类
    /// </summary>
    public static class IndicatorCalculator
    {
        /// <summary>
        /// 计算简单移动平均线
        /// </summary>
        /// <param name="values">数值序列</param>
        /// <param name="period">周期</param>
        /// <returns>移动平均值序列</returns>
        public static List<decimal> CalculateSMA(List<decimal> values, int period)
        {
            var result = new List<decimal>();
            
            if (values == null || values.Count < period)
            {
                return result;
            }
            
            for (int i = 0; i < values.Count; i++)
            {
                if (i < period - 1)
                {
                    result.Add(0); // 前期不足周期数的数据设为0
                }
                else
                {
                    var sum = 0m;
                    for (int j = i - period + 1; j <= i; j++)
                    {
                        sum += values[j];
                    }
                    result.Add(sum / period);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 计算指数移动平均线
        /// </summary>
        /// <param name="values">数值序列</param>
        /// <param name="period">周期</param>
        /// <returns>指数移动平均值序列</returns>
        public static List<decimal> CalculateEMA(List<decimal> values, int period)
        {
            var result = new List<decimal>();
            
            if (values == null || values.Count == 0)
            {
                return result;
            }
            
            decimal multiplier = 2m / (period + 1);
            decimal ema = values[0]; // 第一个EMA值等于第一个数据值
            
            result.Add(ema);
            
            for (int i = 1; i < values.Count; i++)
            {
                ema = (values[i] * multiplier) + (ema * (1 - multiplier));
                result.Add(ema);
            }
            
            return result;
        }
        
        /// <summary>
        /// 计算标准差
        /// </summary>
        /// <param name="values">数值序列</param>
        /// <param name="period">周期</param>
        /// <returns>标准差序列</returns>
        public static List<decimal> CalculateStandardDeviation(List<decimal> values, int period)
        {
            var result = new List<decimal>();
            
            if (values == null || values.Count < period)
            {
                return result;
            }
            
            for (int i = 0; i < values.Count; i++)
            {
                if (i < period - 1)
                {
                    result.Add(0);
                }
                else
                {
                    var subset = values.Skip(i - period + 1).Take(period).ToList();
                    var mean = subset.Average();
                    
                    var sumOfSquaredDifferences = 0m;
                    foreach (var value in subset)
                    {
                        var difference = value - mean;
                        sumOfSquaredDifferences += difference * difference;
                    }
                    
                    var variance = sumOfSquaredDifferences / period;
                    var stdDev = (decimal)Math.Sqrt((double)variance);
                    
                    result.Add(stdDev);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取指定周期内的最大值
        /// </summary>
        /// <param name="values">数值序列</param>
        /// <param name="period">周期</param>
        /// <returns>最大值序列</returns>
        public static List<decimal> CalculateMax(List<decimal> values, int period)
        {
            var result = new List<decimal>();
            
            if (values == null || values.Count == 0)
            {
                return result;
            }
            
            for (int i = 0; i < values.Count; i++)
            {
                var startIndex = Math.Max(0, i - period + 1);
                var subset = values.Skip(startIndex).Take(i - startIndex + 1).ToList();
                
                result.Add(subset.Max());
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取指定周期内的最小值
        /// </summary>
        /// <param name="values">数值序列</param>
        /// <param name="period">周期</param>
        /// <returns>最小值序列</returns>
        public static List<decimal> CalculateMin(List<decimal> values, int period)
        {
            var result = new List<decimal>();
            
            if (values == null || values.Count == 0)
            {
                return result;
            }
            
            for (int i = 0; i < values.Count; i++)
            {
                var startIndex = Math.Max(0, i - period + 1);
                var subset = values.Skip(startIndex).Take(i - startIndex + 1).ToList();
                
                result.Add(subset.Min());
            }
            
            return result;
        }
    }
}