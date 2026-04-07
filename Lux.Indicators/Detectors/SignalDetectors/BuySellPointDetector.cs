using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators.Models;

namespace Lux.Indicators.SignalDetectors
{
    /// <summary>
    /// 买卖点检测器
    /// </summary>
    public static class BuySellPointDetector
    {
        /// <summary>
        /// 检测MACD买卖点
        /// </summary>
        /// <param name="macdResults">MACD分析结果序列</param>
        /// <returns>交易信号序列</returns>
        public static List<TradingSignal> DetectMacdSignals(List<MacdOutput> macdResults)
        {
            var signals = new List<TradingSignal>();
            
            if (macdResults == null || macdResults.Count < 2)
            {
                // 如果数据不足，返回空信号
                for (int i = 0; i < (macdResults?.Count ?? 0); i++)
                {
                    signals.Add(TradingSignal.None);
                }
                return signals;
            }
            
            for (int i = 0; i < macdResults.Count; i++)
            {
                if (i == 0)
                {
                    signals.Add(TradingSignal.None);
                    continue;
                }
                
                var current = macdResults[i];
                var previous = macdResults[i - 1];
                
                // 金叉：DIF上穿DEA（DIF从下方穿越DEA）
                if (previous.Dif <= previous.Dea && current.Dif > current.Dea)
                {
                    signals.Add(TradingSignal.Buy);
                    continue;
                }
                
                // 死叉：DIF下穿DEA（DIF从上方穿越DEA）
                if (previous.Dif >= previous.Dea && current.Dif < current.Dea)
                {
                    signals.Add(TradingSignal.Sell);
                    continue;
                }
                
                // 柱状图变化判断
                if (i >= 2)
                {
                    var prev2 = macdResults[i - 2];
                    var prev1 = macdResults[i - 1];
                    
                    // 柱状图由负转正且增长，可能为买入信号
                    if (prev1.Histogram < 0 && current.Histogram > prev1.Histogram && current.Histogram > 0)
                    {
                        signals.Add(TradingSignal.Buy);
                        continue;
                    }
                    
                    // 柱状图由正转负且下降，可能为卖出信号
                    if (prev1.Histogram > 0 && current.Histogram < prev1.Histogram && current.Histogram < 0)
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
        /// 检测KDJ买卖点
        /// </summary>
        /// <param name="kdjResults">KDJ分析结果序列</param>
        /// <returns>交易信号序列</returns>
        public static List<TradingSignal> DetectKdjSignals(List<KdjOutput> kdjResults)
        {
            var signals = new List<TradingSignal>();
            
            if (kdjResults == null || kdjResults.Count < 2)
            {
                // 如果数据不足，返回空信号
                for (int i = 0; i < (kdjResults?.Count ?? 0); i++)
                {
                    signals.Add(TradingSignal.None);
                }
                return signals;
            }
            
            for (int i = 0; i < kdjResults.Count; i++)
            {
                if (i == 0)
                {
                    signals.Add(TradingSignal.None);
                    continue;
                }
                
                var current = kdjResults[i];
                var previous = kdjResults[i - 1];
                
                // K线上穿D线且在低位区（超卖反弹），买入信号
                if (previous.K <= previous.D && current.K > current.D && current.K < 20)
                {
                    signals.Add(TradingSignal.Buy);
                    continue;
                }
                
                // K线下穿D线且在高位区（超买回调），卖出信号
                if (previous.K >= previous.D && current.K < current.D && current.K > 80)
                {
                    signals.Add(TradingSignal.Sell);
                    continue;
                }
                
                // 低位金叉，买入信号
                if (current.K > current.D && current.K < 30 && previous.K <= previous.D && current.K > previous.K)
                {
                    signals.Add(TradingSignal.Buy);
                    continue;
                }
                
                // 高位死叉，卖出信号
                if (current.K < current.D && current.K > 70 && previous.K >= previous.D && current.K < previous.K)
                {
                    signals.Add(TradingSignal.Sell);
                    continue;
                }
                
                signals.Add(TradingSignal.None);
            }
            
            return signals;
        }
        
        /// <summary>
        /// 检测RSI买卖点
        /// </summary>
        /// <param name="closePrices">收盘价序列</param>
        /// <param name="period">RSI周期，默认14</param>
        /// <returns>交易信号序列</returns>
        public static List<TradingSignal> DetectRsiSignals(List<decimal> closePrices, int period = 14)
        {
            var rsiValues = CalculateRsi(closePrices, period);
            var signals = new List<TradingSignal>();
            
            if (rsiValues == null || rsiValues.Count < 2)
            {
                // 如果数据不足，返回空信号
                for (int i = 0; i < (rsiValues?.Count ?? 0); i++)
                {
                    signals.Add(TradingSignal.None);
                }
                return signals;
            }
            
            for (int i = 0; i < rsiValues.Count; i++)
            {
                if (i == 0)
                {
                    signals.Add(TradingSignal.None);
                    continue;
                }
                
                var currentRsi = rsiValues[i];
                var previousRsi = rsiValues[i - 1];
                
                // RSI上穿30线，从超卖区回升，买入信号
                if (previousRsi <= 30 && currentRsi > 30)
                {
                    signals.Add(TradingSignal.Buy);
                    continue;
                }
                
                // RSI下穿70线，从超买区回落，卖出信号
                if (previousRsi >= 70 && currentRsi < 70)
                {
                    signals.Add(TradingSignal.Sell);
                    continue;
                }
                
                signals.Add(TradingSignal.None);
            }
            
            return signals;
        }
        
        /// <summary>
        /// 检测移动平均线买卖点
        /// </summary>
        /// <param name="stockData">股票数据序列</param>
        /// <param name="maResults">移动平均线分析结果序列</param>
        /// <returns>交易信号序列</returns>
        public static List<TradingSignal> DetectMaSignals(List<StockData> stockData, List<MovingAverageOutput> maResults)
        {
            var signals = new List<TradingSignal>();
            
            if (stockData == null || maResults == null || 
                stockData.Count != maResults.Count || stockData.Count < 2)
            {
                // 如果数据不足，返回空信号
                for (int i = 0; i < Math.Min(stockData?.Count ?? 0, maResults?.Count ?? 0); i++)
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
                
                var currentStock = stockData[i];
                var previousStock = stockData[i - 1];
                var currentMa = maResults[i];
                var previousMa = maResults[i - 1];
                
                // 当前价格上穿短期均线，买入信号
                if (previousStock.Close <= previousMa.ShortMa && currentStock.Close > currentMa.ShortMa)
                {
                    signals.Add(TradingSignal.Buy);
                    continue;
                }
                
                // 当前价格下穿短期均线，卖出信号
                if (previousStock.Close >= previousMa.ShortMa && currentStock.Close < currentMa.ShortMa)
                {
                    signals.Add(TradingSignal.Sell);
                    continue;
                }
                
                signals.Add(TradingSignal.None);
            }
            
            return signals;
        }
        
        /// <summary>
        /// 综合检测买卖点（结合多个指标）
        /// </summary>
        /// <param name="stockData">股票数据序列</param>
        /// <param name="macdResults">MACD分析结果</param>
        /// <param name="kdjResults">KDJ分析结果</param>
        /// <param name="maResults">移动平均线分析结果</param>
        /// <returns>综合交易信号序列</returns>
        public static List<TradingSignal> DetectCombinedSignals(
            List<StockData> stockData,
            List<MacdOutput> macdResults,
            List<KdjOutput> kdjResults,
            List<MovingAverageOutput> maResults)
        {
            var combinedSignals = new List<TradingSignal>();
            
            if (stockData == null || stockData.Count == 0)
            {
                return combinedSignals;
            }
            
            // 获取各个指标的信号
            var macdSignals = DetectMacdSignals(macdResults);
            var kdjSignals = DetectKdjSignals(kdjResults);
            var maSignals = DetectMaSignals(stockData, maResults);
            var rsiSignals = DetectRsiSignals(stockData.Select(x => x.Close).ToList());
            
            // 确保所有信号数组长度一致
            var minLength = Math.Min(Math.Min(
                Math.Min(macdSignals.Count, kdjSignals.Count),
                Math.Min(maSignals.Count, rsiSignals.Count)), 
                stockData.Count);
            
            for (int i = 0; i < minLength; i++)
            {
                int buyCount = 0;
                int sellCount = 0;
                
                if (i < macdSignals.Count)
                {
                    if (macdSignals[i] == TradingSignal.Buy) buyCount++;
                    else if (macdSignals[i] == TradingSignal.Sell) sellCount++;
                }
                
                if (i < kdjSignals.Count)
                {
                    if (kdjSignals[i] == TradingSignal.Buy) buyCount++;
                    else if (kdjSignals[i] == TradingSignal.Sell) sellCount++;
                }
                
                if (i < maSignals.Count)
                {
                    if (maSignals[i] == TradingSignal.Buy) buyCount++;
                    else if (maSignals[i] == TradingSignal.Sell) sellCount++;
                }
                
                if (i < rsiSignals.Count)
                {
                    if (rsiSignals[i] == TradingSignal.Buy) buyCount++;
                    else if (rsiSignals[i] == TradingSignal.Sell) sellCount++;
                }
                
                // 根据多数指标信号决定最终信号
                if (buyCount > sellCount && buyCount >= 2) // 至少两个指标发出买入信号
                {
                    combinedSignals.Add(TradingSignal.Buy);
                }
                else if (sellCount > buyCount && sellCount >= 2) // 至少两个指标发出卖出信号
                {
                    combinedSignals.Add(TradingSignal.Sell);
                }
                else
                {
                    combinedSignals.Add(TradingSignal.None);
                }
            }
            
            // 补齐剩余信号
            while (combinedSignals.Count < stockData.Count)
            {
                combinedSignals.Add(TradingSignal.None);
            }
            
            return combinedSignals;
        }
        
        /// <summary>
        /// 计算RSI值
        /// </summary>
        /// <param name="closePrices">收盘价序列</param>
        /// <param name="period">RSI周期</param>
        /// <returns>RSI值序列</returns>
        internal static List<decimal> CalculateRsi(List<decimal> closePrices, int period)
        {
            var rsiValues = new List<decimal>();
            
            if (closePrices == null || closePrices.Count < period + 1)
            {
                for (int i = 0; i < closePrices?.Count; i++)
                {
                    rsiValues.Add(50m); // 默认中间值
                }
                return rsiValues;
            }
            
            var gains = new List<decimal>();
            var losses = new List<decimal>();
            
            // 计算初始值
            for (int i = 1; i < closePrices.Count; i++)
            {
                var change = closePrices[i] - closePrices[i - 1];
                var gain = change > 0 ? change : 0;
                var loss = change < 0 ? -change : 0;
                
                gains.Add(gain);
                losses.Add(loss);
            }
            
            // 计算RSI
            for (int i = 0; i < closePrices.Count; i++)
            {
                if (i < period)
                {
                    rsiValues.Add(50m); // 前几期无法计算准确RSI
                    continue;
                }
                
                // 使用前period个数据计算平均增益和平均损失
                var avgGain = gains.Skip(i - period).Take(period).Average();
                var avgLoss = losses.Skip(i - period).Take(period).Average();
                
                if (avgLoss == 0)
                {
                    rsiValues.Add(100m);
                }
                else
                {
                    var rs = avgGain / avgLoss;
                    var rsi = 100m - (100m / (1m + rs));
                    rsiValues.Add(rsi);
                }
            }
            
            return rsiValues;
        }
    }
}