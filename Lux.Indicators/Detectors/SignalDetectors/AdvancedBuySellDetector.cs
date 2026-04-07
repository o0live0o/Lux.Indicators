using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators.Models;

namespace Lux.Indicators.SignalDetectors
{
    /// <summary>
    /// 高级买卖点检测器
    /// </summary>
    public static class AdvancedBuySellDetector
    {
        /// <summary>
        /// 检测布林带买卖点
        /// </summary>
        /// <param name="stockData">股票数据序列</param>
        /// <param name="bbResults">布林带分析结果序列</param>
        /// <returns>交易信号序列</returns>
        public static List<TradingSignal> DetectBollingerBandSignals(
            List<StockData> stockData, 
            List<BollingerBandsOutput> bbResults)
        {
            var signals = new List<TradingSignal>();
            
            if (stockData == null || bbResults == null || 
                stockData.Count != bbResults.Count || stockData.Count < 2)
            {
                for (int i = 0; i < Math.Max(stockData?.Count ?? 0, bbResults?.Count ?? 0); i++)
                {
                    signals.Add(TradingSignal.None);
                }
                return signals;
            }
            
            for (int i = 0; i < stockData.Count; i++)
            {
                if (i == 0)
                {
                    signals.Add(TradingSignal.None);
                    continue;
                }
                
                var currentData = stockData[i];
                var currentBb = bbResults[i];
                var previousData = stockData[i - 1];
                
                // 价格突破上轨，可能超买，卖出信号
                if (currentData.Close > currentBb.UpperBand && previousData.Close <= currentBb.UpperBand)
                {
                    signals.Add(TradingSignal.Sell);
                    continue;
                }
                
                // 价格突破下轨，可能超卖，买入信号
                if (currentData.Close < currentBb.LowerBand && previousData.Close >= currentBb.LowerBand)
                {
                    signals.Add(TradingSignal.Buy);
                    continue;
                }
                
                signals.Add(TradingSignal.None);
            }
            
            return signals;
        }
        
        /// <summary>
        /// 检测背离信号（顶底背离）
        /// </summary>
        /// <param name="stockData">股票数据序列</param>
        /// <param name="indicatorValues">技术指标值序列（如MACD、RSI等）</param>
        /// <returns>背离信号序列</returns>
        public static List<TradingSignal> DetectDivergenceSignals(
            List<StockData> stockData, 
            List<decimal> indicatorValues)
        {
            var signals = new List<TradingSignal>();
            
            if (stockData == null || indicatorValues == null || 
                stockData.Count != indicatorValues.Count || stockData.Count < 5)
            {
                for (int i = 0; i < Math.Max(stockData?.Count ?? 0, indicatorValues?.Count ?? 0); i++)
                {
                    signals.Add(TradingSignal.None);
                }
                return signals;
            }
            
            for (int i = 0; i < stockData.Count; i++)
            {
                signals.Add(TradingSignal.None); // 默认无信号
                
                // 需要至少5个数据点来检测背离
                if (i < 4) continue;
                
                // 检查最近的高低点是否形成背离
                var recentData = stockData.Skip(i - 4).Take(5).ToList();
                var recentIndicators = indicatorValues.Skip(i - 4).Take(5).ToList();
                
                // 检测顶背离（价格创新高但指标未创新高）
                if (IsHigherHigh(recentData, x => x.High) && IsLowerHigh(recentData, recentIndicators))
                {
                    signals[i] = TradingSignal.Sell; // 顶背离，卖出信号
                }
                // 检测底背离（价格创新低但指标未创新低）
                else if (IsLowerLow(recentData, x => x.Low) && IsHigherLow(recentData, recentIndicators))
                {
                    signals[i] = TradingSignal.Buy; // 底背离，买入信号
                }
            }
            
            return signals;
        }
        
        /// <summary>
        /// 检测支撑阻力位突破信号
        /// </summary>
        /// <param name="stockData">股票数据序列</param>
        /// <param name="lookbackPeriod">回看周期</param>
        /// <returns>支撑阻力突破信号序列</returns>
        public static List<TradingSignal> DetectSupportResistanceBreakout(
            List<StockData> stockData, 
            int lookbackPeriod = 20)
        {
            var signals = new List<TradingSignal>();
            
            if (stockData == null || stockData.Count < lookbackPeriod)
            {
                for (int i = 0; i < stockData?.Count; i++)
                {
                    signals.Add(TradingSignal.None);
                }
                return signals;
            }
            
            for (int i = 0; i < stockData.Count; i++)
            {
                if (i < lookbackPeriod)
                {
                    signals.Add(TradingSignal.None);
                    continue;
                }
                
                // 获取回看期内的最高价和最低价作为阻力和支撑
                var lookbackData = stockData.Skip(i - lookbackPeriod).Take(lookbackPeriod).ToList();
                var highestHigh = lookbackData.Max(x => x.High);
                var lowestLow = lookbackData.Min(x => x.Low);
                
                var currentClose = stockData[i].Close;
                var previousClose = stockData[i - 1].Close;
                
                // 向上突破阻力位
                if (previousClose <= highestHigh && currentClose > highestHigh)
                {
                    signals.Add(TradingSignal.Buy);
                    continue;
                }
                
                // 向下突破支撑位
                if (previousClose >= lowestLow && currentClose < lowestLow)
                {
                    signals.Add(TradingSignal.Sell);
                    continue;
                }
                
                signals.Add(TradingSignal.None);
            }
            
            return signals;
        }
        
        /// <summary>
        /// 检测多空排列信号（均线多头/空头排列）
        /// </summary>
        /// <param name="maResults">移动平均线分析结果序列</param>
        /// <returns>多空排列信号序列</returns>
        public static List<TradingSignal> DetectMovingAverageAlignment(
            List<MovingAverageOutput> maResults)
        {
            var signals = new List<TradingSignal>();
            
            if (maResults == null || maResults.Count < 2)
            {
                for (int i = 0; i < maResults?.Count; i++)
                {
                    signals.Add(TradingSignal.None);
                }
                return signals;
            }
            
            for (int i = 0; i < maResults.Count; i++)
            {
                if (i == 0)
                {
                    signals.Add(TradingSignal.None);
                    continue;
                }
                
                var current = maResults[i];
                var previous = maResults[i - 1];
                
                // 注意：目前MovingAverageOutput只包含ShortMa和LongMa，没有MediumMa
                // 多头排列：ShortMa > LongMa
                if (current.ShortMa > current.LongMa)
                {
                    // 从空头排列转为多头排列，买入信号
                    if (previous.ShortMa <= previous.LongMa)
                    {
                        signals.Add(TradingSignal.Buy);
                        continue;
                    }
                }
                
                // 空头排列：ShortMa < LongMa
                if (current.ShortMa < current.LongMa)
                {
                    // 从多头排列转为空头排列，卖出信号
                    if (previous.ShortMa >= previous.LongMa)
                    {
                        signals.Add(TradingSignal.Sell);
                        continue;
                    }
                }
                
                signals.Add(TradingSignal.None);
            }
            
            return signals;
        }
        
        /// <summary>
        /// 检测成交量异常信号
        /// </summary>
        /// <param name="stockData">股票数据序列</param>
        /// <param name="volumeThreshold">成交量阈值倍数（相对于平均成交量）</param>
        /// <returns>成交量信号序列</returns>
        public static List<TradingSignal> DetectVolumeAnomaly(
            List<StockData> stockData, 
            decimal volumeThreshold = 1.5m)
        {
            var signals = new List<TradingSignal>();
            
            if (stockData == null || stockData.Count < 20) // 需要足够数据计算平均成交量
            {
                for (int i = 0; i < stockData?.Count; i++)
                {
                    signals.Add(TradingSignal.None);
                }
                return signals;
            }
            
            for (int i = 0; i < stockData.Count; i++)
            {
                if (i < 20) // 前20个数据点不计算信号
                {
                    signals.Add(TradingSignal.None);
                    continue;
                }
                
                // 计算前20日平均成交量
                var avgVolume = stockData.Skip(i - 20).Take(20).Average(x => x.Volume);
                var currentVolume = stockData[i].Volume;
                var priceChange = Math.Abs(stockData[i].Close - stockData[i - 1].Close) / stockData[i - 1].Close;
                
                // 成交量放大且伴随价格大幅上涨，可能为买入信号
                if (currentVolume > avgVolume * volumeThreshold && 
                    stockData[i].Close > stockData[i - 1].Close && 
                    priceChange > 0.02m) // 涨幅超过2%
                {
                    signals.Add(TradingSignal.Buy);
                    continue;
                }
                
                // 成交量放大且伴随价格大幅下跌，可能为卖出信号
                if (currentVolume > avgVolume * volumeThreshold && 
                    stockData[i].Close < stockData[i - 1].Close && 
                    priceChange > 0.02m) // 跌幅超过2%
                {
                    signals.Add(TradingSignal.Sell);
                    continue;
                }
                
                signals.Add(TradingSignal.None);
            }
            
            return signals;
        }
        
        /// <summary>
        /// 检测趋势强度信号
        /// </summary>
        /// <param name="stockData">股票数据序列</param>
        /// <param name="trendPeriod">趋势周期</param>
        /// <returns>趋势强度信号序列</returns>
        public static List<TradingSignal> DetectTrendStrength(
            List<StockData> stockData, 
            int trendPeriod = 10)
        {
            var signals = new List<TradingSignal>();
            
            if (stockData == null || stockData.Count < trendPeriod)
            {
                for (int i = 0; i < stockData?.Count; i++)
                {
                    signals.Add(TradingSignal.None);
                }
                return signals;
            }
            
            for (int i = 0; i < stockData.Count; i++)
            {
                if (i < trendPeriod)
                {
                    signals.Add(TradingSignal.None);
                    continue;
                }
                
                var startPrice = stockData[i - trendPeriod].Close;
                var endPrice = stockData[i].Close;
                var trendStrength = (endPrice - startPrice) / startPrice;
                
                // 强势上涨趋势，考虑持有或加仓
                if (trendStrength > 0.10m && endPrice > stockData.Take(i + 1).Max(x => x.High)) // 涨幅超过10%且创近期新高
                {
                    signals.Add(TradingSignal.Buy);
                    continue;
                }
                
                // 强势下跌趋势，考虑减仓或卖出
                if (trendStrength < -0.10m && endPrice < stockData.Take(i + 1).Min(x => x.Low)) // 跌幅超过10%且创近期新低
                {
                    signals.Add(TradingSignal.Sell);
                    continue;
                }
                
                signals.Add(TradingSignal.None);
            }
            
            return signals;
        }
        
        #region Private Helper Methods
        
        /// <summary>
        /// 检查是否形成更高高点
        /// </summary>
        private static bool IsHigherHigh(List<StockData> data, Func<StockData, decimal> selector)
        {
            if (data.Count < 2) return false;
            
            var values = data.Select(selector).ToList();
            // 检查最后一个点是否是最高点
            return values.Last() == values.Max() && values.IndexOf(values.Max()) == values.Count - 1;
        }
        
        /// <summary>
        /// 检查指标是否形成更低高点（顶背离条件之一）
        /// </summary>
        private static bool IsLowerHigh(List<StockData> priceData, List<decimal> indicatorData)
        {
            if (priceData.Count != indicatorData.Count || priceData.Count < 2) return false;
            
            // 检查价格创新高但指标未创新高
            var lastPrice = priceData.Last().High;
            var maxPriceBefore = priceData.Take(priceData.Count - 1).Max(x => x.High);
            
            var lastIndicator = indicatorData.Last();
            var maxIndicatorBefore = indicatorData.Take(indicatorData.Count - 1).Max();
            
            return lastPrice > maxPriceBefore && lastIndicator <= maxIndicatorBefore;
        }
        
        /// <summary>
        /// 检查是否形成更低低点
        /// </summary>
        private static bool IsLowerLow(List<StockData> data, Func<StockData, decimal> selector)
        {
            if (data.Count < 2) return false;
            
            var values = data.Select(selector).ToList();
            // 检查最后一个点是否是最低点
            return values.Last() == values.Min() && values.IndexOf(values.Min()) == values.Count - 1;
        }
        
        /// <summary>
        /// 检查指标是否形成更高低点（底背离条件之一）
        /// </summary>
        private static bool IsHigherLow(List<StockData> priceData, List<decimal> indicatorData)
        {
            if (priceData.Count != indicatorData.Count || priceData.Count < 2) return false;
            
            // 检查价格创新低但指标未创新低
            var lastPrice = priceData.Last().Low;
            var minPriceBefore = priceData.Take(priceData.Count - 1).Min(x => x.Low);
            
            var lastIndicator = indicatorData.Last();
            var minIndicatorBefore = indicatorData.Take(indicatorData.Count - 1).Min();
            
            return lastPrice < minPriceBefore && lastIndicator >= minIndicatorBefore;
        }
        
        #endregion
    }
}