using System;
using System.Collections.Generic;
using Lux.Indicators;
using Lux.Indicators.Models;
using Lux.Indicators.Options;
using Lux.Indicators.TrendIndicators;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.VolatileIndicators;
using Lux.Indicators.DivergenceDetectors;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 示例程序，演示各种技术指标的使用方法，包括Options模式
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Lux.Indicators - 股票技术指标计算库");
            Console.WriteLine("===================================");
            
            // 创建模拟股票数据
            var stockData = GenerateSampleData();
            var closePrices = stockData.Select(s => s.Close).ToList();
            var highPrices = stockData.Select(s => s.High).ToList();
            var lowPrices = stockData.Select(s => s.Low).ToList();

            try
            {
                // 计算MACD (使用默认参数)
                Console.WriteLine("\n1. MACD指标分析 (默认参数):");
                var macdResults = MacdAnalyzer.Analyze(closePrices);
                for (int i = Math.Max(0, macdResults.Count - 5); i < macdResults.Count; i++)
                {
                    var result = macdResults[i];
                    Console.WriteLine($"  [{i}] DIF: {result.Dif:F4}, DEA: {result.Dea:F4}, Histogram: {result.Histogram:F4}, Signal: {result.Signal}");
                }
                
                // 计算MACD (使用纯参数模式)
                Console.WriteLine("\n1. MACD指标分析 (纯参数模式):");
                var macdResultsPure = MacdAnalyzer.Analyze(closePrices, 10, 24, 7);
                for (int i = Math.Max(0, macdResultsPure.Count - 5); i < macdResultsPure.Count; i++)
                {
                    var result = macdResultsPure[i];
                    Console.WriteLine($"  [{i}] DIF: {result.Dif:F4}, DEA: {result.Dea:F4}, Histogram: {result.Histogram:F4}, Signal: {result.Signal}");
                }
                
                // 计算MACD (使用自定义参数)
                Console.WriteLine("\n1. MACD指标分析 (自定义参数):");
                var macdResultsCustom = MacdAnalyzer.Analyze(closePrices, new MacdOptions 
                { 
                    FastPeriod = 10, 
                    SlowPeriod = 24, 
                    SignalPeriod = 7 
                });
                for (int i = Math.Max(0, macdResultsCustom.Count - 5); i < macdResultsCustom.Count; i++)
                {
                    var result = macdResultsCustom[i];
                    Console.WriteLine($"  [{i}] DIF: {result.Dif:F4}, DEA: {result.Dea:F4}, Histogram: {result.Histogram:F4}, Signal: {result.Signal}");
                }

                // 计算KDJ (使用默认参数)
                Console.WriteLine("\n2. KDJ指标分析 (默认参数):");
                var kdjResults = KdjAnalyzer.Analyze(highPrices, lowPrices, closePrices);
                for (int i = Math.Max(0, kdjResults.Count - 5); i < kdjResults.Count; i++)
                {
                    var result = kdjResults[i];
                    Console.WriteLine($"  [{i}] K: {result.K:F4}, D: {result.D:F4}, J: {result.J:F4}, Signal: {result.Signal}");
                }
                
                // 计算KDJ (使用纯参数模式)
                Console.WriteLine("\n2. KDJ指标分析 (纯参数模式):");
                var kdjResultsPure = KdjAnalyzer.Analyze(highPrices, lowPrices, closePrices, 12, 5, 5);
                for (int i = Math.Max(0, kdjResultsPure.Count - 5); i < kdjResultsPure.Count; i++)
                {
                    var result = kdjResultsPure[i];
                    Console.WriteLine($"  [{i}] K: {result.K:F4}, D: {result.D:F4}, J: {result.J:F4}, Signal: {result.Signal}");
                }
                
                // 计算KDJ (使用自定义参数)
                Console.WriteLine("\n2. KDJ指标分析 (自定义参数):");
                var kdjResultsCustom = KdjAnalyzer.Analyze(highPrices, lowPrices, closePrices, new KdjOptions 
                { 
                    RsvPeriod = 12, 
                    KPeriod = 5, 
                    DPeriod = 5 
                });
                for (int i = Math.Max(0, kdjResultsCustom.Count - 5); i < kdjResultsCustom.Count; i++)
                {
                    var result = kdjResultsCustom[i];
                    Console.WriteLine($"  [{i}] K: {result.K:F4}, D: {result.D:F4}, J: {result.J:F4}, Signal: {result.Signal}");
                }

                // 计算布林带 (使用默认参数)
                Console.WriteLine("\n3. 布林带指标分析 (默认参数):");
                var bollResults = BollingerBandsAnalyzer.Analyze(closePrices);
                for (int i = Math.Max(0, bollResults.Count - 5); i < bollResults.Count; i++)
                {
                    var result = bollResults[i];
                    Console.WriteLine($"  [{i}] MiddleBand: {result.MiddleBand:F4}, UpperBand: {result.UpperBand:F4}, LowerBand: {result.LowerBand:F4}, BandWidth: {result.BandWidth:F4}%, Signal: {result.Signal}");
                }
                
                // 计算布林带 (使用纯参数模式)
                Console.WriteLine("\n3. 布林带指标分析 (纯参数模式):");
                var bollResultsPure = BollingerBandsAnalyzer.Analyze(closePrices, 25, 2.5m);
                for (int i = Math.Max(0, bollResultsPure.Count - 5); i < bollResultsPure.Count; i++)
                {
                    var result = bollResultsPure[i];
                    Console.WriteLine($"  [{i}] MiddleBand: {result.MiddleBand:F4}, UpperBand: {result.UpperBand:F4}, LowerBand: {result.LowerBand:F4}, BandWidth: {result.BandWidth:F4}%, Signal: {result.Signal}");
                }
                
                // 计算布林带 (使用自定义参数)
                Console.WriteLine("\n3. 布林带指标分析 (自定义参数):");
                var bollResultsCustom = BollingerBandsAnalyzer.Analyze(closePrices, new BollingerBandsOptions 
                { 
                    Period = 25, 
                    StdDevMultiplier = 2.5m 
                });
                for (int i = Math.Max(0, bollResultsCustom.Count - 5); i < bollResultsCustom.Count; i++)
                {
                    var result = bollResultsCustom[i];
                    Console.WriteLine($"  [{i}] MiddleBand: {result.MiddleBand:F4}, UpperBand: {result.UpperBand:F4}, LowerBand: {result.LowerBand:F4}, BandWidth: {result.BandWidth:F4}%, Signal: {result.Signal}");
                }

                // 计算双均线 (使用默认参数)
                Console.WriteLine("\n4. 移动平均线指标分析 (默认参数):");
                var maResults = MovingAverageAnalyzer.Analyze(closePrices);
                for (int i = Math.Max(0, maResults.Count - 5); i < maResults.Count; i++)
                {
                    var result = maResults[i];
                    Console.WriteLine($"  [{i}] ShortMA: {result.ShortMa:F4}, LongMA: {result.LongMa:F4}, Signal: {result.Signal}");
                }
                
                // 计算双均线 (使用纯参数模式)
                Console.WriteLine("\n4. 移动平均线指标分析 (纯参数模式):");
                var maResultsPure = MovingAverageAnalyzer.Analyze(closePrices, 10, 30);
                for (int i = Math.Max(0, maResultsPure.Count - 5); i < maResultsPure.Count; i++)
                {
                    var result = maResultsPure[i];
                    Console.WriteLine($"  [{i}] ShortMA: {result.ShortMa:F4}, LongMA: {result.LongMa:F4}, Signal: {result.Signal}");
                }
                
                // 计算双均线 (使用自定义参数)
                Console.WriteLine("\n4. 移动平均线指标分析 (自定义参数):");
                var maResultsCustom = MovingAverageAnalyzer.Analyze(closePrices, new MovingAverageOptions 
                { 
                    ShortPeriod = 10, 
                    LongPeriod = 30 
                });
                for (int i = Math.Max(0, maResultsCustom.Count - 5); i < maResultsCustom.Count; i++)
                {
                    var result = maResultsCustom[i];
                    Console.WriteLine($"  [{i}] ShortMA: {result.ShortMa:F4}, LongMA: {result.LongMa:F4}, Signal: {result.Signal}");
                }
                
                // 检测MACD背离
                Console.WriteLine("\n5. MACD背离检测:");
                var macdResultsForDivergence = MacdAnalyzer.Analyze(closePrices);
                var macdDivergences = MacdDivergenceAnalyzer.FindDivergences(closePrices, macdResultsForDivergence);
                if (macdDivergences.Count > 0)
                {
                    foreach (var divergence in macdDivergences)
                    {
                        Console.WriteLine($"  背离类型: {divergence.Type}, 位置: {divergence.PricePeakIndex}, 描述: {divergence.Description}");
                    }
                }
                else
                {
                    Console.WriteLine("  未检测到明显的MACD背离");
                }
                
                // 检测KDJ背离
                Console.WriteLine("\n6. KDJ背离检测:");
                var kdjResultsForDivergence = KdjAnalyzer.Analyze(highPrices, lowPrices, closePrices);
                var kdjDivergences = KdjDivergenceAnalyzer.FindDivergences(closePrices, kdjResultsForDivergence);
                if (kdjDivergences.Count > 0)
                {
                    foreach (var divergence in kdjDivergences)
                    {
                        Console.WriteLine($"  背离类型: {divergence.Type}, 位置: {divergence.PricePeakIndex}, 描述: {divergence.Description}");
                    }
                }
                else
                {
                    Console.WriteLine("  未检测到明显的KDJ背离");
                }
                
                // 检测布林带背离
                Console.WriteLine("\n7. 布林带背离检测:");
                var bollResultsForDivergence = BollingerBandsAnalyzer.Analyze(closePrices);
                var bollDivergences = BollingerBandsDivergenceAnalyzer.FindDivergences(closePrices, bollResultsForDivergence);
                if (bollDivergences.Count > 0)
                {
                    foreach (var divergence in bollDivergences)
                    {
                        Console.WriteLine($"  背离类型: {divergence.Type}, 位置: {divergence.PricePeakIndex}, 描述: {divergence.Description}");
                    }
                }
                else
                {
                    Console.WriteLine("  未检测到明显的布林带背离");
                }
                
                // 检测移动平均线背离
                Console.WriteLine("\n8. 移动平均线背离检测:");
                var maResultsForDivergence = MovingAverageAnalyzer.Analyze(closePrices);
                var maDivergences = MovingAverageDivergenceAnalyzer.FindDivergences(closePrices, maResultsForDivergence);
                if (maDivergences.Count > 0)
                {
                    foreach (var divergence in maDivergences)
                    {
                        Console.WriteLine($"  背离类型: {divergence.Type}, 位置: {divergence.PricePeakIndex}, 描述: {divergence.Description}");
                    }
                }
                else
                {
                    Console.WriteLine("  未检测到明显的移动平均线背离");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"计算过程中发生错误: {ex.Message}");
            }

            Console.WriteLine("\n计算完成！");
        }

        /// <summary>
        /// 生成示例股票数据
        /// </summary>
        /// <returns>股票数据列表</returns>
        private static List<StockData> GenerateSampleData()
        {
            var data = new List<StockData>();
            var random = new Random(42); // 使用固定种子以获得可重现的结果
            
            // 生成50天的示例数据
            for (int i = 0; i < 50; i++)
            {
                var basePrice = 100m + i * 0.5m; // 基础价格趋势
                var fluctuation = (decimal)(random.NextDouble() - 0.5) * 10m; // 随机波动
                
                var closePrice = Math.Max(basePrice + fluctuation, 1m);
                var highPrice = closePrice + (decimal)random.NextDouble() * 5m;
                var lowPrice = Math.Max(closePrice - (decimal)random.NextDouble() * 5m, 1m);
                
                data.Add(new StockData
                {
                    Date = DateTime.Now.AddDays(-i),
                    Open = i == 0 ? closePrice : data[i-1].Close,
                    High = highPrice,
                    Low = lowPrice,
                    Close = closePrice,
                    Volume = (long)random.Next(100000, 1000000)
                });
            }
            
            data.Reverse(); // 按时间顺序排列
            return data;
        }
    }
}