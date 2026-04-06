using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.TrendIndicators;
using Lux.Indicators.VolatileIndicators;
using Lux.Indicators.Models;
using Lux.Indicators.SignalDetectors;

// 为避免命名冲突，明确使用Models命名空间的TradingSignal
using TradingSignal = Lux.Indicators.Models.TradingSignal;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 买卖点检测演示
    /// </summary>
    public static class BuySellDetectionDemo
    {
        public static void Run()
        {
            Console.WriteLine("=== 买卖点检测演示 (使用TSV数据) ===\n");
            
            // 使用TSV文件的真实数据
            var stockData = ReadTsvData();
            if (stockData == null || stockData.Count == 0)
            {
                Console.WriteLine("无法读取TSV数据，使用示例数据进行演示...");
                RunWithSampleData();
                return;
            }
            
            // 只取最近的100个数据点以方便查看
            var recentData = stockData.TakeLast(100).ToList();
            var closePrices = recentData.Select(x => x.Close).ToList();
            
            Console.WriteLine($"从TSV文件加载了 {stockData.Count} 条数据");
            Console.WriteLine($"使用最近 {recentData.Count} 条数据进行分析");
            Console.WriteLine($"价格范围: {recentData.Min(x => x.Close)} - {recentData.Max(x => x.Close)}\n");
            
            // 计算各种技术指标
            var macdResults = MacdAnalyzer.Analyze(closePrices);
            var kdjResults = KdjAnalyzer.Analyze(
                recentData.Select(x => x.High).ToList(), 
                recentData.Select(x => x.Low).ToList(), 
                closePrices);
            var maResults = MovingAverageAnalyzer.Analyze(closePrices, 5, 20);
            var bbResults = BollingerBandsAnalyzer.Analyze(closePrices);
            
            // 基本买卖点检测
            Console.WriteLine("1. 基本买卖点检测:");
            var macdSignals = BuySellPointDetector.DetectMacdSignals(macdResults);
            var kdjSignals = BuySellPointDetector.DetectKdjSignals(kdjResults);
            var maSignals = BuySellPointDetector.DetectMaSignals(recentData, maResults);
            var rsiSignals = BuySellPointDetector.DetectRsiSignals(closePrices);
            
            PrintSignalSummary("MACD", macdSignals);
            PrintSignalSummary("KDJ", kdjSignals);
            PrintSignalSummary("MA", maSignals);
            PrintSignalSummary("RSI", rsiSignals);
            
            Console.WriteLine("\n2. 高级买卖点检测:");
            var bbSignals = AdvancedBuySellDetector.DetectBollingerBandSignals(recentData, bbResults);
            var srSignals = AdvancedBuySellDetector.DetectSupportResistanceBreakout(recentData);
            var alignmentSignals = AdvancedBuySellDetector.DetectMovingAverageAlignment(maResults);
            var volumeSignals = AdvancedBuySellDetector.DetectVolumeAnomaly(recentData);
            var trendSignals = AdvancedBuySellDetector.DetectTrendStrength(recentData);
            
            PrintSignalSummary("布林带", bbSignals);
            PrintSignalSummary("支撑阻力", srSignals);
            PrintSignalSummary("均线排列", alignmentSignals);
            PrintSignalSummary("成交量", volumeSignals);
            PrintSignalSummary("趋势强度", trendSignals);
            
            Console.WriteLine("\n3. 综合买卖点检测:");
            var comprehensiveSignalsRaw = ComprehensiveBuySellDetector.DetectComprehensiveSignals(
                recentData, macdResults, kdjResults, maResults, bbResults);
            PrintSignalSummary("综合信号", comprehensiveSignalsRaw);
            
            // 使用带原因的综合信号检测
            var comprehensiveSignalsWithReason = ComprehensiveBuySellDetector.DetectComprehensiveSignalsWithReason(
                recentData, macdResults, kdjResults, maResults, bbResults);
            
            Console.WriteLine("\n4. 背离增强型综合信号:");
            var divergenceEnhancedSignals = ComprehensiveBuySellDetector.DetectDivergenceEnhancedSignals(
                recentData, macdResults);
            PrintSignalSummary("背离增强信号", divergenceEnhancedSignals);
            
            Console.WriteLine("\n5. 信号详情 (前20个数据点):");
            Console.WriteLine("日期\t\t价格\tMACD\tKDJ\tMA\t综合\t背离增强");
            Console.WriteLine("----\t\t----\t----\t---\t--\t-----\t-------");
            
            for (int i = 0; i < Math.Min(20, recentData.Count); i++)
            {
                Console.WriteLine($"{recentData[i].Date:yyyy-MM-dd}\t{recentData[i].Close:F2}\t" +
                                $"{GetSignalChar(macdSignals[i])}\t{GetSignalChar(kdjSignals[i])}\t" +
                                $"{GetSignalChar(maSignals[i])}\t{GetSignalChar(comprehensiveSignalsRaw[i])}\t" +
                                $"{GetSignalChar(divergenceEnhancedSignals[i])}");
            }
            
            Console.WriteLine("\n6. 信号原因分析 (前10个有信号的数据点):");
            int signalCount = 0;
            for (int i = 0; i < Math.Min(50, comprehensiveSignalsWithReason.Count); i++)
            {
                var signal = comprehensiveSignalsWithReason[i];
                if (signal.Signal != TradingSignal.None && signalCount < 10)
                {
                    Console.WriteLine($"[{recentData[i].Date:yyyy-MM-dd}] {signal.Signal}信号 - {signal.Reason}");
                    Console.WriteLine($"   置信度: {(int)(signal.Confidence * 100)}%");
                    if (signal.ContributingFactors.Any())
                    {
                        Console.WriteLine($"   影响因素:");
                        foreach (var factor in signal.ContributingFactors.Take(3)) // 限制显示前3个因素
                        {
                            Console.WriteLine($"     - {factor}");
                        }
                    }
                    Console.WriteLine();
                    signalCount++;
                }
            }
        }
        
        /// <summary>
        /// 读取TSV数据
        /// </summary>
        private static List<StockData> ReadTsvData()
        {
            var results = new List<StockData>();
            try
            {
                // 读取TSV文件
                var tsvPath = @"E:\project\Lux.Indicators\Lux.Indicators.Demo\minutes.tsv";
                if (!File.Exists(tsvPath))
                {
                    Console.WriteLine($"TSV文件不存在: {tsvPath}");
                    return new List<StockData>();
                }
                
                var lines = File.ReadAllLines(tsvPath);
                if (lines.Length <= 1) // 跳过标题行
                {
                    Console.WriteLine("TSV文件为空或只有标题行");
                    return new List<StockData>();
                }
                
                // 跳过标题行，从第二行开始解析
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    var parts = line.Split('\t');
                    if (parts.Length < 8) continue; // 确保有足够的列
                    
                    var stock = new StockData();
                    
                    // 解析日期 (第1列) - 需要去除中文字符和多余部分
                    var datePart = parts[0];
                    // 按逗号分割并取第一部分，然后去除中文字符
                    var cleanDateStr = datePart.Split(',')[0]; // 先按逗号分割
                    cleanDateStr = cleanDateStr.Replace("浜?", "").Trim(); // 移除可能的乱码字符
                    if (DateTime.TryParse(cleanDateStr, out var date))
                    {
                        stock.Date = date;
                    }
                    
                    // 解析开盘价 (第2列)
                    if (decimal.TryParse(parts[1], out var open))
                        stock.Open = open;
                    
                    // 解析最高价 (第3列)
                    if (decimal.TryParse(parts[2], out var high))
                        stock.High = high;
                    
                    // 解析最低价 (第4列)
                    if (decimal.TryParse(parts[3], out var low))
                        stock.Low = low;
                    
                    // 解析收盘价 (第5列)
                    if (decimal.TryParse(parts[4], out var close))
                        stock.Close = close;
                    
                    // 解析成交量 (第8列)
                    if (parts.Length > 7 && decimal.TryParse(parts[7], out var volume))
                        stock.Volume = volume;
                    
                    results.Add(stock);
                }
                
                Console.WriteLine($"成功从TSV文件读取 {results.Count} 条记录");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取TSV文件时发生错误: {ex.Message}");
                return new List<StockData>();
            }
            
            return results;
        }
        
        /// <summary>
        /// 使用示例数据运行（备用方案）
        /// </summary>
        private static void RunWithSampleData()
        {
            Console.WriteLine("=== 买卖点检测演示 (使用示例数据) ===\n");
            
            // 准备示例数据
            var stockData = GenerateSampleData(100);
            var closePrices = stockData.Select(x => x.Close).ToList();
            
            Console.WriteLine($"生成了 {stockData.Count} 个交易日的数据");
            Console.WriteLine($"价格范围: {stockData.Min(x => x.Close)} - {stockData.Max(x => x.Close)}\n");
            
            // 计算各种技术指标
            var macdResults = MacdAnalyzer.Analyze(closePrices);
            var kdjResults = KdjAnalyzer.Analyze(stockData.Select(x => x.High).ToList(), 
                                                stockData.Select(x => x.Low).ToList(), 
                                                closePrices);
            var maResults = MovingAverageAnalyzer.Analyze(closePrices, 5, 20);
            var bbResults = BollingerBandsAnalyzer.Analyze(closePrices);
            
            // 基本买卖点检测
            Console.WriteLine("1. 基本买卖点检测:");
            var macdSignals = BuySellPointDetector.DetectMacdSignals(macdResults);
            var kdjSignals = BuySellPointDetector.DetectKdjSignals(kdjResults);
            var maSignals = BuySellPointDetector.DetectMaSignals(stockData, maResults);
            var rsiSignals = BuySellPointDetector.DetectRsiSignals(closePrices);
            
            PrintSignalSummary("MACD", macdSignals);
            PrintSignalSummary("KDJ", kdjSignals);
            PrintSignalSummary("MA", maSignals);
            PrintSignalSummary("RSI", rsiSignals);
            
            Console.WriteLine("\n2. 高级买卖点检测:");
            var bbSignals = AdvancedBuySellDetector.DetectBollingerBandSignals(stockData, bbResults);
            var srSignals = AdvancedBuySellDetector.DetectSupportResistanceBreakout(stockData);
            var alignmentSignals = AdvancedBuySellDetector.DetectMovingAverageAlignment(maResults);
            var volumeSignals = AdvancedBuySellDetector.DetectVolumeAnomaly(stockData);
            var trendSignals = AdvancedBuySellDetector.DetectTrendStrength(stockData);
            
            PrintSignalSummary("布林带", bbSignals);
            PrintSignalSummary("支撑阻力", srSignals);
            PrintSignalSummary("均线排列", alignmentSignals);
            PrintSignalSummary("成交量", volumeSignals);
            PrintSignalSummary("趋势强度", trendSignals);
            
            Console.WriteLine("\n3. 综合买卖点检测:");
            var comprehensiveSignalsRaw = ComprehensiveBuySellDetector.DetectComprehensiveSignals(
                stockData, macdResults, kdjResults, maResults, bbResults);
            PrintSignalSummary("综合信号", comprehensiveSignalsRaw);
            
            // 使用带原因的综合信号检测
            var comprehensiveSignalsWithReason = ComprehensiveBuySellDetector.DetectComprehensiveSignalsWithReason(
                stockData, macdResults, kdjResults, maResults, bbResults);
            
            Console.WriteLine("\n4. 背离增强型综合信号:");
            var divergenceEnhancedSignals = ComprehensiveBuySellDetector.DetectDivergenceEnhancedSignals(
                stockData, macdResults);
            PrintSignalSummary("背离增强信号", divergenceEnhancedSignals);
            
            Console.WriteLine("\n5. 信号详情 (前20个数据点):");
            Console.WriteLine("日期\t\t价格\tMACD\tKDJ\tMA\t综合\t背离增强");
            Console.WriteLine("----\t\t----\t----\t---\t--\t-----\t-------");
            
            for (int i = 0; i < Math.Min(20, stockData.Count); i++)
            {
                Console.WriteLine($"{stockData[i].Date:yyyy-MM-dd}\t{stockData[i].Close:F2}\t" +
                                $"{GetSignalChar(macdSignals[i])}\t{GetSignalChar(kdjSignals[i])}\t" +
                                $"{GetSignalChar(maSignals[i])}\t{GetSignalChar(comprehensiveSignalsRaw[i])}\t" +
                                $"{GetSignalChar(divergenceEnhancedSignals[i])}");
            }
            
            Console.WriteLine("\n6. 信号原因分析 (前10个有信号的数据点):");
            int signalCount = 0;
            for (int i = 0; i < Math.Min(50, comprehensiveSignalsWithReason.Count); i++)
            {
                var signal = comprehensiveSignalsWithReason[i];
                if (signal.Signal != TradingSignal.None && signalCount < 10)
                {
                    Console.WriteLine($"[{stockData[i].Date:yyyy-MM-dd}] {signal.Signal}信号 - {signal.Reason}");
                    Console.WriteLine($"   置信度: {(int)(signal.Confidence * 100)}%");
                    if (signal.ContributingFactors.Any())
                    {
                        Console.WriteLine($"   影响因素:");
                        foreach (var factor in signal.ContributingFactors.Take(3)) // 限制显示前3个因素
                        {
                            Console.WriteLine($"     - {factor}");
                        }
                    }
                    Console.WriteLine();
                    signalCount++;
                }
            }
        }
        
        /// <summary>
        /// 生成示例股票数据
        /// </summary>
        private static List<StockData> GenerateSampleData(int count)
        {
            var data = new List<StockData>();
            var random = new Random(42); // 固定种子以获得可重现的结果
            decimal price = 100m; // 初始价格
            
            for (int i = 0; i < count; i++)
            {
                var date = DateTime.Today.AddDays(-count + i + 1);
                
                // 随机价格变动 (-2% 到 +2%)
                var changePercent = (decimal)(random.NextDouble() - 0.5) * 0.04m;
                var changeAmount = price * changePercent;
                var newPrice = price + changeAmount;
                
                // 确保价格不会变成负数
                if (newPrice < 1m) newPrice = 1m;
                
                // 高低价基于当前价格波动
                var high = newPrice * (1 + (decimal)random.NextDouble() * 0.03m);
                var low = newPrice * (1 - (decimal)random.NextDouble() * 0.03m);
                var close = newPrice;
                var open = i > 0 ? data[i - 1].Close : newPrice; // 开盘价通常是前一天收盘价
                
                // 确保高低价合理
                high = Math.Max(high, Math.Max(open, close));
                low = Math.Min(low, Math.Min(open, close));
                
                data.Add(new StockData
                {
                    Date = date,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = 1000000 + (decimal)random.Next(0, 1000000) // 随机成交量
                });
                
                price = newPrice;
            }
            
            return data;
        }
        
        /// <summary>
        /// 打印信号摘要
        /// </summary>
        private static void PrintSignalSummary(string indicatorName, List<TradingSignal> signals)
        {
            var buyCount = signals.Count(s => s == TradingSignal.Buy);
            var sellCount = signals.Count(s => s == TradingSignal.Sell);
            var noneCount = signals.Count(s => s == TradingSignal.None);
            
            Console.WriteLine($"{indicatorName}: 买入信号 {buyCount} 个, 卖出信号 {sellCount} 个, 无信号 {noneCount} 个");
        }
        
        /// <summary>
        /// 获取信号字符表示
        /// </summary>
        private static char GetSignalChar(TradingSignal signal)
        {
            return signal switch
            {
                TradingSignal.Buy => 'B',
                TradingSignal.Sell => 'S',
                _ => '-'
            };
        }
    }
}