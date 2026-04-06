using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators.Models;

namespace Lux.Indicators.SignalDetectors
{
    /// <summary>
    /// 综合买卖点检测器
    /// </summary>
    public static class ComprehensiveBuySellDetector
    {
        /// <summary>
        /// 检测综合买卖点（结合所有可用指标）
        /// </summary>
        /// <param name="stockData">股票数据序列</param>
        /// <param name="macdResults">MACD分析结果</param>
        /// <param name="kdjResults">KDJ分析结果</param>
        /// <param name="maResults">移动平均线分析结果</param>
        /// <param name="bbResults">布林带分析结果</param>
        /// <param name="rsiPeriod">RSI周期，默认14</param>
        /// <param name="volumeThreshold">成交量阈值倍数，默认1.5</param>
        /// <param name="trendPeriod">趋势周期，默认10</param>
        /// <param name="lookbackPeriod">支撑阻力回看周期，默认20</param>
        /// <returns>综合交易信号序列</returns>
        public static List<TradingSignal> DetectComprehensiveSignals(
            List<StockData> stockData,
            List<MacdOutput> macdResults = null,
            List<KdjOutput> kdjResults = null,
            List<MovingAverageOutput> maResults = null,
            List<BollingerBandsOutput> bbResults = null,
            int rsiPeriod = 14,
            decimal volumeThreshold = 1.5m,
            int trendPeriod = 10,
            int lookbackPeriod = 20)
        {
            var signalsWithReason = DetectComprehensiveSignalsWithReason(
                stockData, macdResults, kdjResults, maResults, bbResults, 
                rsiPeriod, volumeThreshold, trendPeriod, lookbackPeriod);
            
            return signalsWithReason.Select(s => s.Signal).ToList();
        }
        
        /// <summary>
        /// 检测综合买卖点（带原因说明）
        /// </summary>
        /// <param name="stockData">股票数据序列</param>
        /// <param name="macdResults">MACD分析结果</param>
        /// <param name="kdjResults">KDJ分析结果</param>
        /// <param name="maResults">移动平均线分析结果</param>
        /// <param name="bbResults">布林带分析结果</param>
        /// <param name="rsiPeriod">RSI周期，默认14</param>
        /// <param name="volumeThreshold">成交量阈值倍数，默认1.5</param>
        /// <param name="trendPeriod">趋势周期，默认10</param>
        /// <param name="lookbackPeriod">支撑阻力回看周期，默认20</param>
        /// <returns>带原因的综合交易信号序列</returns>
        public static List<SignalWithReason> DetectComprehensiveSignalsWithReason(
            List<StockData> stockData,
            List<MacdOutput> macdResults = null,
            List<KdjOutput> kdjResults = null,
            List<MovingAverageOutput> maResults = null,
            List<BollingerBandsOutput> bbResults = null,
            int rsiPeriod = 14,
            decimal volumeThreshold = 1.5m,
            int trendPeriod = 10,
            int lookbackPeriod = 20)
        {
            var comprehensiveSignals = new List<SignalWithReason>();
            
            if (stockData == null || stockData.Count == 0)
            {
                // 返回空信号列表
                for (int i = 0; i < stockData?.Count; i++)
                {
                    comprehensiveSignals.Add(new SignalWithReason
                    {
                        Signal = TradingSignal.None,
                        Reason = "无数据",
                        Confidence = 0m
                    });
                }
                return comprehensiveSignals;
            }
            
            // 初始化信号计数器及原因追踪
            var signalDetails = new List<SignalDetail>();
            for (int i = 0; i < stockData.Count; i++)
            {
                signalDetails.Add(new SignalDetail());
            }
            
            // MACD信号
            if (macdResults != null)
            {
                var macdSignals = BuySellPointDetector.DetectMacdSignals(macdResults);
                for (int i = 0; i < Math.Min(signalDetails.Count, macdSignals.Count); i++)
                {
                    if (macdSignals[i] == TradingSignal.Buy)
                    {
                        signalDetails[i].BuyCount++;
                        signalDetails[i].ContributingFactors.Add($"MACD金叉: DIF({macdResults[i].Dif:F3})上穿DEA({macdResults[i].Dea:F3})");
                    }
                    else if (macdSignals[i] == TradingSignal.Sell)
                    {
                        signalDetails[i].SellCount++;
                        signalDetails[i].ContributingFactors.Add($"MACD死叉: DIF({macdResults[i].Dif:F3})下穿DEA({macdResults[i].Dea:F3})");
                    }
                }
            }
            
            // KDJ信号
            if (kdjResults != null)
            {
                var kdjSignals = BuySellPointDetector.DetectKdjSignals(kdjResults);
                for (int i = 0; i < Math.Min(signalDetails.Count, kdjSignals.Count); i++)
                {
                    if (kdjSignals[i] == TradingSignal.Buy)
                    {
                        signalDetails[i].BuyCount++;
                        signalDetails[i].ContributingFactors.Add($"KDJ金叉: K({kdjResults[i].K:F2})上穿D({kdjResults[i].D:F2})且处于超卖区");
                    }
                    else if (kdjSignals[i] == TradingSignal.Sell)
                    {
                        signalDetails[i].SellCount++;
                        signalDetails[i].ContributingFactors.Add($"KDJ死叉: K({kdjResults[i].K:F2})下穿D({kdjResults[i].D:F2})且处于超买区");
                    }
                }
            }
            
            // 移动平均线信号
            if (maResults != null)
            {
                var maSignals = BuySellPointDetector.DetectMaSignals(stockData, maResults);
                for (int i = 0; i < Math.Min(signalDetails.Count, maSignals.Count); i++)
                {
                    if (maSignals[i] == TradingSignal.Buy)
                    {
                        signalDetails[i].BuyCount++;
                        signalDetails[i].ContributingFactors.Add($"价格上穿均线: 价格({stockData[i].Close:F2})上穿短期均线({maResults[i].ShortMa:F2})");
                    }
                    else if (maSignals[i] == TradingSignal.Sell)
                    {
                        signalDetails[i].SellCount++;
                        signalDetails[i].ContributingFactors.Add($"价格下穿均线: 价格({stockData[i].Close:F2})下穿短期均线({maResults[i].ShortMa:F2})");
                    }
                }
            }
            
            // RSI信号
            var rsiValues = BuySellPointDetector.CalculateRsi(stockData.Select(x => x.Close).ToList(), rsiPeriod);
            var rsiSignals = BuySellPointDetector.DetectRsiSignals(
                stockData.Select(x => x.Close).ToList(), rsiPeriod);
            for (int i = 0; i < Math.Min(signalDetails.Count, rsiSignals.Count); i++)
            {
                if (rsiSignals[i] == TradingSignal.Buy)
                {
                    signalDetails[i].BuyCount++;
                    signalDetails[i].ContributingFactors.Add($"RSI反转: RSI从{rsiValues[i >= 1 ? i - 1 : 0]:F2}升至{rsiValues[i]:F2}突破30线");
                }
                else if (rsiSignals[i] == TradingSignal.Sell)
                {
                    signalDetails[i].SellCount++;
                    signalDetails[i].ContributingFactors.Add($"RSI反转: RSI从{rsiValues[i >= 1 ? i - 1 : 0]:F2}降至{rsiValues[i]:F2}跌破70线");
                }
            }
            
            // 布林带信号
            if (bbResults != null)
            {
                var bbSignals = AdvancedBuySellDetector.DetectBollingerBandSignals(stockData, bbResults);
                for (int i = 0; i < Math.Min(signalDetails.Count, bbSignals.Count); i++)
                {
                    if (bbSignals[i] == TradingSignal.Buy)
                    {
                        signalDetails[i].BuyCount++;
                        signalDetails[i].ContributingFactors.Add($"布林带突破: 价格({stockData[i].Close:F2})突破下轨({bbResults[i].LowerBand:F2})");
                    }
                    else if (bbSignals[i] == TradingSignal.Sell)
                    {
                        signalDetails[i].SellCount++;
                        signalDetails[i].ContributingFactors.Add($"布林带突破: 价格({stockData[i].Close:F2})突破上轨({bbResults[i].UpperBand:F2})");
                    }
                }
            }
            
            // 支撑阻力突破信号
            var srSignals = AdvancedBuySellDetector.DetectSupportResistanceBreakout(stockData, lookbackPeriod);
            for (int i = 0; i < Math.Min(signalDetails.Count, srSignals.Count); i++)
            {
                if (srSignals[i] == TradingSignal.Buy)
                {
                    signalDetails[i].BuyCount++;
                    signalDetails[i].ContributingFactors.Add($"支撑突破: 价格({stockData[i].Close:F2})突破前期支撑位");
                }
                else if (srSignals[i] == TradingSignal.Sell)
                {
                    signalDetails[i].SellCount++;
                    signalDetails[i].ContributingFactors.Add($"阻力突破: 价格({stockData[i].Close:F2})突破前期阻力位");
                }
            }
            
            // 均线排列信号
            if (maResults != null)
            {
                var alignmentSignals = AdvancedBuySellDetector.DetectMovingAverageAlignment(maResults);
                for (int i = 0; i < Math.Min(signalDetails.Count, alignmentSignals.Count); i++)
                {
                    if (alignmentSignals[i] == TradingSignal.Buy)
                    {
                        signalDetails[i].BuyCount++;
                        signalDetails[i].ContributingFactors.Add($"均线多头排列: 短期均线上穿长期均线");
                    }
                    else if (alignmentSignals[i] == TradingSignal.Sell)
                    {
                        signalDetails[i].SellCount++;
                        signalDetails[i].ContributingFactors.Add($"均线空头排列: 短期均线下穿长期均线");
                    }
                }
            }
            
            // 成交量异常信号
            var volumeSignals = AdvancedBuySellDetector.DetectVolumeAnomaly(stockData, volumeThreshold);
            for (int i = 0; i < Math.Min(signalDetails.Count, volumeSignals.Count); i++)
            {
                if (volumeSignals[i] == TradingSignal.Buy)
                {
                    signalDetails[i].BuyCount++;
                    signalDetails[i].ContributingFactors.Add($"成交量放大: 成交量({stockData[i].Volume:F0})较平均水平放大");
                }
                else if (volumeSignals[i] == TradingSignal.Sell)
                {
                    signalDetails[i].SellCount++;
                    signalDetails[i].ContributingFactors.Add($"成交量放大: 成交量({stockData[i].Volume:F0})较平均水平放大且价格下跌");
                }
            }
            
            // 趋势强度信号
            var trendSignals = AdvancedBuySellDetector.DetectTrendStrength(stockData, trendPeriod);
            for (int i = 0; i < Math.Min(signalDetails.Count, trendSignals.Count); i++)
            {
                if (trendSignals[i] == TradingSignal.Buy)
                {
                    signalDetails[i].BuyCount++;
                    signalDetails[i].ContributingFactors.Add($"趋势走强: 价格在{trendPeriod}周期内呈现上升趋势");
                }
                else if (trendSignals[i] == TradingSignal.Sell)
                {
                    signalDetails[i].SellCount++;
                    signalDetails[i].ContributingFactors.Add($"趋势走弱: 价格在{trendPeriod}周期内呈现下降趋势");
                }
            }
            
            // 根据信号强度确定最终信号及原因
            for (int i = 0; i < signalDetails.Count; i++)
            {
                var detail = signalDetails[i];
                
                // 设置信号确认阈值，避免过多噪音信号
                int signalThreshold = 2; // 至少2个指标发出相同信号才确认
                
                SignalWithReason resultSignal;
                
                if (detail.BuyCount >= signalThreshold && detail.BuyCount > detail.SellCount)
                {
                    resultSignal = new SignalWithReason
                    {
                        Signal = TradingSignal.Buy,
                        Reason = $"多指标共振买入: {detail.BuyCount}个指标发出买入信号，{detail.SellCount}个指标发出卖出信号",
                        ContributingFactors = detail.ContributingFactors.Where(f => f.Contains("买入")).ToList(),
                        Confidence = Math.Min(1.0m, (decimal)detail.BuyCount / 5.0m) // 最多5个指标参与计算
                    };
                }
                else if (detail.SellCount >= signalThreshold && detail.SellCount > detail.BuyCount)
                {
                    resultSignal = new SignalWithReason
                    {
                        Signal = TradingSignal.Sell,
                        Reason = $"多指标共振卖出: {detail.SellCount}个指标发出卖出信号，{detail.BuyCount}个指标发出买入信号",
                        ContributingFactors = detail.ContributingFactors.Where(f => f.Contains("卖出")).ToList(),
                        Confidence = Math.Min(1.0m, (decimal)detail.SellCount / 5.0m) // 最多5个指标参与计算
                    };
                }
                else
                {
                    resultSignal = new SignalWithReason
                    {
                        Signal = TradingSignal.None,
                        Reason = $"信号不足: 买入信号{detail.BuyCount}个，卖出信号{detail.SellCount}个，未达到阈值{signalThreshold}",
                        ContributingFactors = detail.ContributingFactors,
                        Confidence = 0.1m
                    };
                }
                
                comprehensiveSignals.Add(resultSignal);
            }
            
            return comprehensiveSignals;
        }
        
        /// <summary>
        /// 检测背离增强型综合信号
        /// </summary>
        /// <param name="stockData">股票数据序列</param>
        /// <param name="macdResults">MACD分析结果</param>
        /// <param name="rsiPeriod">RSI周期</param>
        /// <returns>背离增强型综合信号序列</returns>
        public static List<TradingSignal> DetectDivergenceEnhancedSignals(
            List<StockData> stockData,
            List<MacdOutput> macdResults = null,
            int rsiPeriod = 14)
        {
            var basicSignals = DetectComprehensiveSignals(stockData, macdResults: macdResults, rsiPeriod: rsiPeriod);
            var divergenceSignals = new List<TradingSignal>();
            
            if (stockData != null && macdResults != null && 
                stockData.Count == macdResults.Count && stockData.Count >= 5)
            {
                // 使用MACD作为背离检测的指标
                var macdHistogramValues = macdResults.Select(x => x.Histogram).ToList();
                divergenceSignals = AdvancedBuySellDetector.DetectDivergenceSignals(stockData, macdHistogramValues);
            }
            else if (stockData != null)
            {
                // 使用RSI作为背离检测的指标
                var rsiValues = BuySellPointDetector.DetectRsiSignals(stockData.Select(x => x.Close).ToList(), rsiPeriod)
                    .Select((signal, index) => signal == TradingSignal.Buy ? 30m : (signal == TradingSignal.Sell ? 70m : 50m)).ToList();
                divergenceSignals = AdvancedBuySellDetector.DetectDivergenceSignals(stockData, rsiValues);
            }
            
            // 整合基本信号和背离信号
            var enhancedSignals = new List<TradingSignal>();
            var minLength = Math.Min(basicSignals.Count, divergenceSignals.Count);
            
            for (int i = 0; i < Math.Max(basicSignals.Count, divergenceSignals.Count); i++)
            {
                TradingSignal finalSignal = TradingSignal.None;
                
                if (i < minLength)
                {
                    var basicSignal = basicSignals[i];
                    var divSignal = divergenceSignals[i];
                    
                    // 如果基本信号和背离信号一致，则加强该信号
                    if (basicSignal == divSignal && basicSignal != TradingSignal.None)
                    {
                        finalSignal = basicSignal; // 背离确认了基本信号
                    }
                    // 如果基本信号为None但有背离信号，则采用背离信号
                    else if (basicSignal == TradingSignal.None && divSignal != TradingSignal.None)
                    {
                        finalSignal = divSignal; // 背离信号作为主要信号
                    }
                    // 其他情况保持基本信号
                    else
                    {
                        finalSignal = basicSignal;
                    }
                }
                else if (i < basicSignals.Count)
                {
                    finalSignal = basicSignals[i];
                }
                else if (i < divergenceSignals.Count)
                {
                    finalSignal = divergenceSignals[i];
                }
                
                enhancedSignals.Add(finalSignal);
            }
            
            return enhancedSignals;
        }
        
        /// <summary>
        /// 获取信号置信度评分
        /// </summary>
        /// <param name="signals">交易信号序列</param>
        /// <param name="signalStrength">信号强度数组（每个信号对应的强度值）</param>
        /// <returns>带置信度的信号信息</returns>
        public static List<SignalWithConfidence> GetSignalConfidence(
            List<TradingSignal> signals,
            List<int> signalStrength = null)
        {
            var signalWithConfidence = new List<SignalWithConfidence>();
            
            for (int i = 0; i < signals.Count; i++)
            {
                var signal = signals[i];
                decimal confidence = 0.5m; // 默认中等置信度
                
                if (signalStrength != null && i < signalStrength.Count)
                {
                    // 根据信号强度计算置信度 (假设有3个及以上指标支持则为高置信度)
                    var strength = signalStrength[i];
                    if (strength >= 3)
                    {
                        confidence = 0.9m; // 高置信度
                    }
                    else if (strength >= 2)
                    {
                        confidence = 0.7m; // 中等置信度
                    }
                    else if (strength >= 1)
                    {
                        confidence = 0.5m; // 低置信度
                    }
                }
                
                signalWithConfidence.Add(new SignalWithConfidence
                {
                    Signal = signal,
                    Confidence = confidence
                });
            }
            
            return signalWithConfidence;
        }
        
        /// <summary>
        /// 更新信号计数
        /// </summary>
        private static void UpdateSignalCounts(List<(int buys, int sells)> signalCounts, List<TradingSignal> newSignals)
        {
            for (int i = 0; i < Math.Min(signalCounts.Count, newSignals.Count); i++)
            {
                var signal = newSignals[i];
                var (currentBuys, currentSells) = signalCounts[i];
                
                if (signal == TradingSignal.Buy)
                {
                    signalCounts[i] = (currentBuys + 1, currentSells);
                }
                else if (signal == TradingSignal.Sell)
                {
                    signalCounts[i] = (currentBuys, currentSells + 1);
                }
                // TradingSignal.None 不增加任何计数
            }
        }
    }
    
    /// <summary>
    /// 带置信度的信号
    /// </summary>
    public class SignalWithConfidence
    {
        public TradingSignal Signal { get; set; }
        public decimal Confidence { get; set; } // 置信度 0.0-1.0}
    }
    
    /// <summary>
    /// 信号详情
    /// </summary>
    internal class SignalDetail
    {
        public int BuyCount { get; set; } = 0;
        public int SellCount { get; set; } = 0;
        public List<string> ContributingFactors { get; set; } = new List<string>();
    }
}