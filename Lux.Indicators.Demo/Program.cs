using System;
using System.Collections.Generic;
using Lux.Indicators;
using Lux.Indicators.Models;
using Lux.Indicators.Options;
using Lux.Indicators.TrendIndicators;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.VolatileIndicators;
using Lux.Indicators.DivergenceDetectors;
using System.Text;

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
            
            // 运行交易模拟
            RunTradingSimulation();

            // 运行技术指标分析测试
            // RunTechnicalAnalysisTests();

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
                    Open = i == 0 ? closePrice : data[i - 1].Close,
                    High = highPrice,
                    Low = lowPrice,
                    Close = closePrice,
                    Volume = (long)random.Next(100000, 1000000)
                });
            }

            data.Reverse(); // 按时间顺序排列
            return data;
        }

        private static List<StockData> ReadTsv()
        {
            var results = new List<StockData>();
            //时间	开盘	最高	最低	收盘	涨幅	振幅	总手	金额	换手%	成交次数
            var list = ReadTsv(@"E:\project\Lux.Indicators\Lux.Indicators.Demo\minutes.tsv");
            foreach (var item in list)
            {
                StockData stock = new StockData();
                var dateStr = item[0].Split(",")[0];
                if (DateTime.TryParse(dateStr, out var date))
                {
                    stock.Date = date;
                }
                if (decimal.TryParse(item[1], out var open))
                    stock.Open = open;
                if (decimal.TryParse(item[2], out var high))
                    stock.High = high;
                if (decimal.TryParse(item[3], out var low))
                    stock.Low = low;
                if (decimal.TryParse(item[4], out var close))
                    stock.Close = close;
                if (decimal.TryParse(item[7], out var volume))
                    stock.Volume = volume;
                results.Add(stock);
            }
            return results;
        }

        private static IEnumerable<string[]> ReadTsv(string path)
        {
            using var reader = new StreamReader(path, Encoding.UTF8);
            var headerLine = reader.ReadLine();
            if (headerLine == null) yield break;

            int columnCount = 1;
            foreach (var c in headerLine.AsSpan())
            {
                if (c == '\t') columnCount++;
            }
            var reusableBuffer = new string[columnCount];

            string? line = null;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                Array.Clear(reusableBuffer, 0, reusableBuffer.Length);
                line.AsSpan().ParseToBuffer('\t', reusableBuffer);
                yield return reusableBuffer.ToArray();
            }
        }
        private static void WriteTestData()
        {
            var minutes = ReadTsv();
            var lastMinutes = minutes.TakeLast(300).ToArray();
            var lastMinutesClose = lastMinutes.Select(p => p.Close).ToList();
            var macdResultsPure1 = MacdAnalyzer.Analyze(lastMinutesClose, 12, 26, 9);

            // 计算MACD背离，使用更敏感的参数以适应分钟级数据
            var macdDivergences = MacdDivergenceAnalyzer.FindDivergences(lastMinutesClose, macdResultsPure1, lookbackPeriod: 3, threshold: 0.01m);

            // 创建背离字典以便快速查找
            var divergenceDict = new Dictionary<int, DivergenceCommon.DivergencePoint>();
            foreach (var div in macdDivergences)
            {
                divergenceDict[div.PricePeakIndex] = div;
            }

            var lastMinutesCloseArr = lastMinutesClose.ToArray();
            using StreamWriter sw = new StreamWriter("close_macd.tsv");
            sw.WriteLine("Date\tClose\tDIF\tDEA\tHistogram\tSignal\tDivergenceType\tDivergenceDescription"); // 写入表头
            for (int i = 0; i < lastMinutes.Length; i++)
            {
                string divergenceType = "";
                string divergenceDescription = "";

                // 检查是否存在背离点，注意背离分析可能需要一定数据量才开始工作
                if (divergenceDict.ContainsKey(i))
                {
                    var divergence = divergenceDict[i];
                    divergenceType = divergence.Type.ToString();
                    divergenceDescription = divergence.Description.Replace("\t", " ").Replace("\n", " "); // 避免制表符冲突
                }

                sw.WriteLine($"{lastMinutes[i].Date.ToString("yyyy-MM-dd")}\t{lastMinutesCloseArr[i].ToString()}\t{macdResultsPure1[i].Dif.ToString()}\t{macdResultsPure1[i].Dea.ToString()}\t{macdResultsPure1[i].Histogram.ToString()}\t{macdResultsPure1[i].Signal.ToString()}\t{divergenceType}\t{divergenceDescription}");
            }

            // 调试输出
            Console.WriteLine("准备执行交易模拟...");
            Console.WriteLine($"分钟数据总数: {lastMinutes.Length}");
            Console.WriteLine($"第一个数据点: {lastMinutes[0].Date}, 价格: {lastMinutes[0].Close}");
            Console.WriteLine($"最后一个数据点: {lastMinutes[lastMinutes.Length - 1].Date}, 价格: {lastMinutes[lastMinutes.Length - 1].Close}");
        }

        /// <summary>
        /// 运行交易模拟
        /// </summary>
        private static void RunTradingSimulation()
        {
            var minutes = ReadTsv();
            var lastMinutes = minutes.TakeLast(300).ToArray();
            var lastMinutesClose = lastMinutes.Select(p => p.Close).ToList();
            var macdResultsPure1 = MacdAnalyzer.Analyze(lastMinutesClose, 12, 26, 9);
            
            // 计算MACD背离，使用更敏感的参数以适应分钟级数据
            var macdDivergences = MacdDivergenceAnalyzer.FindDivergences(lastMinutesClose, macdResultsPure1, lookbackPeriod: 3, threshold: 0.01m);
            
            // 创建背离字典以便快速查找
            var divergenceDict = new Dictionary<int, DivergenceCommon.DivergencePoint>();
            foreach (var div in macdDivergences)
            {
                divergenceDict[div.PricePeakIndex] = div;
            }
            
            var lastMinutesCloseArr = lastMinutesClose.ToArray();
            using StreamWriter sw = new StreamWriter("close_macd.tsv");
            sw.WriteLine("Date\tClose\tDIF\tDEA\tHistogram\tSignal\tDivergenceType\tDivergenceDescription"); // 写入表头
            for (int i = 0; i < lastMinutes.Length; i++)
            {
                string divergenceType = "";
                string divergenceDescription = "";
                
                // 检查是否存在背离点，注意背离分析可能需要一定数据量才开始工作
                if (divergenceDict.ContainsKey(i))
                {
                    var divergence = divergenceDict[i];
                    divergenceType = divergence.Type.ToString();
                    divergenceDescription = divergence.Description.Replace("\t", " ").Replace("\n", " "); // 避免制表符冲突
                }
                
                sw.WriteLine($"{lastMinutes[i].Date.ToString("yyyy-MM-dd")}\t{lastMinutesCloseArr[i].ToString()}\t{macdResultsPure1[i].Dif.ToString()}\t{macdResultsPure1[i].Dea.ToString()}\t{macdResultsPure1[i].Histogram.ToString()}\t{macdResultsPure1[i].Signal.ToString()}\t{divergenceType}\t{divergenceDescription}");
            }

            // 调试输出
            Console.WriteLine("准备执行交易模拟...");
            Console.WriteLine($"分钟数据总数: {lastMinutes.Length}");
            Console.WriteLine($"第一个数据点: {lastMinutes[0].Date}, 价格: {lastMinutes[0].Close}");
            Console.WriteLine($"最后一个数据点: {lastMinutes[lastMinutes.Length - 1].Date}, 价格: {lastMinutes[lastMinutes.Length - 1].Close}");

            // 运行交易模拟
            Console.WriteLine("\n12. 交易模拟:");
            var simulator = new TradingSimulator(100000m); // 初始资金10万元
            Console.WriteLine("交易模拟器已创建，开始运行模拟...");
            simulator.RunSimulation(lastMinutes.ToList());
            Console.WriteLine("模拟运行完成，正在获取结果...");

            var tradingResult = simulator.GetResult();
            Console.WriteLine($"  初始资金: {tradingResult.InitialBalance:N2}");
            Console.WriteLine($"  最终资金: {tradingResult.FinalBalance:N2}");
            Console.WriteLine($"  盈亏金额: {tradingResult.Profit:N2}");
            Console.WriteLine($"  盈亏比例: {tradingResult.ProfitPercentage:N2}%");
            Console.WriteLine($"  总交易次数: {tradingResult.TotalTrades}");

            Console.WriteLine("\n  交易记录:");
            foreach (var trade in tradingResult.Trades)
            {
                string actionText = trade.Action == TradeAction.Buy ? "买入" : "卖出";
                Console.WriteLine($"    {trade.DateTime:yyyy-MM-dd HH:mm} {actionText} 价格:{trade.Price:F2} 数量:{trade.Shares} 金额:{trade.Amount:N2} 余额:{trade.BalanceAfter:N2}");
            }
            Console.WriteLine("交易模拟完成。");
        }

        /// <summary>
        /// 运行技术指标分析测试
        /// </summary>
        private static void RunTechnicalAnalysisTests()
        {
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
                var macdDivergencesDemo = MacdDivergenceAnalyzer.FindDivergences(closePrices, macdResultsForDivergence);
                if (macdDivergencesDemo.Count > 0)
                {
                    foreach (var divergence in macdDivergencesDemo)
                    {
                        Console.WriteLine($"  背离类型: {divergence.Type}, 位置: {divergence.PricePeakIndex}, 描述: {divergence.Description}");
                    }
                }
                else
                {
                    Console.WriteLine("  未检测到明显的MACD背离");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"计算过程中发生错误: {ex.Message}");
            }
        }
    }
}