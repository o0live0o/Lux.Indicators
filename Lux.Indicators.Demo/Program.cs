using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lux.Indicators;
using Lux.Indicators.Models;
using Lux.Indicators.Options;
using Lux.Indicators.TrendIndicators;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.VolatileIndicators;
using Lux.Indicators.DivergenceDetectors;
using System.Text;
using Lux.Indicators.Demo.Examples;
using Lux.Indicators.Demo.Strategies;
using Lux.Indicators.Demo.Management;
using Lux.Indicators.Demo.Traders;
using Lux.Indicators.Demo.Providers;
using Lux.Indicators.Demo.Aggregation;
using Lux.Indicators.Demo.Managers;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 示例程序，演示各种技术指标的使用方法，包括Options模式
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Lux.Indicators - 股票技术指标计算库");
            Console.WriteLine("===================================");
            
            // 运行交易模拟
            // RunTradingSimulation();

            // 运行交易员系统示例
            // await RunTraderSystemExample();
            
            // 运行高级智能交易系统示例
            // await RunAdvancedIntelligentTradingExample();

            // 运行技术指标分析测试
            // RunTechnicalAnalysisTests();
            
            // 运行日内交易模拟 - 使用最终交易中心版本
            await RunIntradayTradingSimulation();

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
            string stockCode = "600001"; // 确保有正确的股票代码

            // 创建不同策略和仓位管理组合的模拟器进行比较
            var simulationConfigs = new[]
            {
                new { Strategy = (ITradingStrategy)new ShortTermTradingStrategy(), PositionManagement = (IPositionManagement)new ConservativePositionManagement(), Name = "短期交易+保守仓位" },
                new { Strategy = (ITradingStrategy)new ShortTermTradingStrategy(), PositionManagement = (IPositionManagement)new AggressivePositionManagement(), Name = "短期交易+积极仓位" },
                new { Strategy = (ITradingStrategy)new ShortTermTradingStrategy(), PositionManagement = (IPositionManagement)new BalancedPositionManagement(), Name = "短期交易+平衡仓位" },
                new { Strategy = (ITradingStrategy)new LongTermInvestmentStrategy(), PositionManagement = (IPositionManagement)new ConservativePositionManagement(), Name = "长期投资+保守仓位" },
                new { Strategy = (ITradingStrategy)new LongTermInvestmentStrategy(), PositionManagement = (IPositionManagement)new AggressivePositionManagement(), Name = "长期投资+积极仓位" },
                new { Strategy = (ITradingStrategy)new LongTermInvestmentStrategy(), PositionManagement = (IPositionManagement)new BalancedPositionManagement(), Name = "长期投资+平衡仓位" },
                new { Strategy = (ITradingStrategy)new SwingTradingStrategy(), PositionManagement = (IPositionManagement)new ConservativePositionManagement(), Name = "波段交易+保守仓位" },
                new { Strategy = (ITradingStrategy)new SwingTradingStrategy(), PositionManagement = (IPositionManagement)new AggressivePositionManagement(), Name = "波段交易+积极仓位" },
                new { Strategy = (ITradingStrategy)new SwingTradingStrategy(), PositionManagement = (IPositionManagement)new BalancedPositionManagement(), Name = "波段交易+平衡仓位" }
            };

            Console.WriteLine("\n12. 交易模拟 (策略+仓位管理组合对比):");
            
            foreach (var config in simulationConfigs)
            {
                Console.WriteLine($"\n--- {config.Name} ---");
                var simulator = new TradingSimulator(100000m, config.Strategy, config.PositionManagement); // 使用指定策略和仓位管理
                Console.WriteLine($"{config.Name}模拟器已创建，开始运行模拟...");
                simulator.RunSimulation(lastMinutes.ToList(), stockCode);
                Console.WriteLine("模拟运行完成，正在获取结果...");

                var tradingResult = simulator.GetResult();
                Console.WriteLine($"  初始资金: {tradingResult.InitialBalance:N2}");
                Console.WriteLine($"  最终资金: {tradingResult.FinalBalance:N2}");
                Console.WriteLine($"  盈亏金额: {tradingResult.Profit:N2}");
                Console.WriteLine($"  盈亏比例: {tradingResult.ProfitPercentage:N2}%");
                Console.WriteLine($"  总交易次数: {tradingResult.TotalTrades}");

                Console.WriteLine("\n  交易记录 (仅显示前5笔):");
                var limitedTrades = tradingResult.Trades.Take(5).ToList();
                foreach (var trade in limitedTrades)
                {
                    string actionText = trade.Action == TradeAction.Buy ? "买入" : "卖出";
                    Console.WriteLine($"    {trade.DateTime:yyyy-MM-dd HH:mm} {trade.StockCode} {actionText} 价格:{trade.Price:F2} 数量:{trade.Shares} 金额:{trade.Amount:N2} 余额:{trade.BalanceAfter:N2}");
                    Console.WriteLine($"      信号详情: {trade.SignalDetails}");
                    Console.WriteLine($"      技术指标: MACD(DIF:{trade.MacdDif:F3}, DEA:{trade.MacdDea:F3}, Histogram:{trade.MacdHistogram:F3}), " +
                                    $"KDJ(K:{trade.KdjK:F2}, D:{trade.KdjD:F2}, J:{trade.KdjJ:F2}), " +
                                    $"RSI:{trade.Rsi:F2}, MA(Short:{trade.MaShort:F2}, Long:{trade.MaLong:F2})");
                }
                
                if (tradingResult.Trades.Count > 5)
                {
                    Console.WriteLine($"    ... 还有 {tradingResult.Trades.Count - 5} 笔交易");
                }
                
                Console.WriteLine($"{config.Name}模拟完成。");
            }
            
            Console.WriteLine("\n所有策略组合模拟完成。");
        }

        /// <summary>
        /// 运行日内交易模拟 - 使用最终交易中心版本
        /// </summary>
        private static async Task RunIntradayTradingSimulation()
        {
            Console.WriteLine("\n运行日内交易模拟 (使用最终交易中心版本)...");

            // 读取TSV数据
            var stockDataList = ReadTsv();
            string stockCode = "600001"; // 使用600001作为股票代码

            // 使用与RunTradingSimulation相同的方式创建模拟器
            var intradayStrategy = new IntradayTradingStrategy();
            var positionManagement = new IntradayPositionManagement(); // 使用日内仓位管理

            // 创建与RunTradingSimulation相同的模拟器
            var simulator = new TradingSimulator(100000m, intradayStrategy, positionManagement);
            
            Console.WriteLine($"已创建日内交易模拟器，初始资金: 100,000.00");
            Console.WriteLine($"股票代码: {stockCode}");
            Console.WriteLine($"数据点数量: {stockDataList.Count}");

            // 运行模拟
            simulator.RunSimulation(stockDataList, stockCode);

            // 获取结果
            var tradingResult = simulator.GetResult();
            
            Console.WriteLine($"\n=== 日内交易结果 ===");
            Console.WriteLine($"初始资金: {tradingResult.InitialBalance:N2}");
            Console.WriteLine($"最终资金: {tradingResult.FinalBalance:N2}");
            Console.WriteLine($"盈亏金额: {tradingResult.Profit:N2}");
            Console.WriteLine($"盈亏比例: {tradingResult.ProfitPercentage:N2}%");
            Console.WriteLine($"总交易次数: {tradingResult.TotalTrades}");

            // 显示交易记录
            if (tradingResult.Trades.Count > 0)
            {
                Console.WriteLine($"\n交易记录 (仅显示前5笔):");
                var limitedTrades = tradingResult.Trades.Take(5).ToList();
                foreach (var trade in limitedTrades)
                {
                    string actionText = trade.Action == TradeAction.Buy ? "买入" : "卖出";
                    Console.WriteLine($"  {trade.DateTime:yyyy-MM-dd HH:mm} {trade.StockCode} {actionText} 价格:{trade.Price:F2} 数量:{trade.Shares} 金额:{trade.Amount:N2} 余额:{trade.BalanceAfter:N2}");
                    Console.WriteLine($"    信号详情: {trade.SignalDetails}");
                }
                
                if (tradingResult.Trades.Count > 5)
                {
                    Console.WriteLine($"    ... 还有 {tradingResult.Trades.Count - 5} 笔交易");
                }
            }

            Console.WriteLine("日内交易模拟完成。");
        }

        /// <summary>
        /// 运行交易员系统示例
        /// </summary>
        private static async Task RunTraderSystemExample()
        {
            Console.WriteLine("\n运行交易员系统示例...");
            await TraderSystemExample.RunAsync();
            Console.WriteLine("交易员系统示例完成。");
        }
        
        /// <summary>
        /// 运行高级智能交易系统示例
        /// </summary>
        private static async Task RunAdvancedIntelligentTradingExample()
        {
            Console.WriteLine("\n运行高级智能交易系统示例...");
            await AdvancedIntelligentTradingExample.RunAsync();
            Console.WriteLine("高级智能交易系统示例完成。");
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

        /// <summary>
        /// 计算MACD指标 - 简化版本以提高性能
        /// </summary>
        private static MacdOutput CalculateMacd(List<StockData> historicalData)
        {
            if (historicalData.Count < 26) // MACD至少需要26个数据点
            {
                return new MacdOutput { Dif = 0, Dea = 0, Histogram = 0, Signal = MacdSignalType.None };
            }

            // 获取最近的数据点用于计算
            var recentData = historicalData.TakeLast(26).ToList();
            var prices = recentData.Select(d => d.Close).ToList();
            
            // 计算EMA12 (快速线) - 使用简化的计算方式
            decimal ema12 = prices.Skip(Math.Max(0, prices.Count - 12)).DefaultIfEmpty(0).Average();
            // 计算EMA26 (慢速线) - 使用简化的计算方式
            decimal ema26 = prices.Skip(Math.Max(0, prices.Count - 26)).DefaultIfEmpty(0).Average();
            
            // DIF = EMA12 - EMA26
            decimal dif = ema12 - ema26;
            
            // 简化DEA计算
            decimal dea = (dif + ema12 - ema26) / 2;
            
            // BAR = (DIF - DEA) * 2
            decimal histogram = (dif - dea) * 2;

            // 计算信号类型
            MacdSignalType signal = MacdSignalType.None;
            if (historicalData.Count >= 27) // 需要至少27个数据点才能比较当前和前一个状态
            {
                // 获取前一个状态的计算结果（使用前26个数据点）
                var prevRecentData = historicalData.TakeLast(27).Take(26).ToList(); // 前一个26个数据点
                var prevPrices = prevRecentData.Select(d => d.Close).ToList();
                
                decimal prevEma12 = prevPrices.Skip(Math.Max(0, prevPrices.Count - 12)).DefaultIfEmpty(0).Average();
                decimal prevEma26 = prevPrices.Skip(Math.Max(0, prevPrices.Count - 26)).DefaultIfEmpty(0).Average();
                
                decimal prevDif = prevEma12 - prevEma26;
                decimal prevDea = (prevDif + prevEma12 - prevEma26) / 2;
                
                // 金叉：DIF从下方穿越DEA
                if (prevDif <= prevDea && dif > dea)
                {
                    signal = MacdSignalType.GoldenCross;
                }
                // 死叉：DIF从上方穿越DEA
                else if (prevDif >= prevDea && dif < dea)
                {
                    signal = MacdSignalType.DeathCross;
                }
            }

            return new MacdOutput { Dif = dif, Dea = dea, Histogram = histogram, Signal = signal };
        }

        /// <summary>
        /// 计算KDJ指标 - 简化版本以提高性能
        /// </summary>
        private static KdjOutput CalculateKdj(List<StockData> historicalData)
        {
            if (historicalData.Count < 9) // KDJ至少需要9个数据点
            {
                return new KdjOutput { K = 50, D = 50, J = 50, Signal = KdjSignalType.None };
            }

            // 取最近9天的数据
            var recentData = historicalData.TakeLast(9).ToList();
            
            // 计算最高价和最低价
            decimal highestHigh = recentData.Max(d => d.High);
            decimal lowestLow = recentData.Min(d => d.Low);
            var currentData = historicalData.Last();

            // 如果最高价等于最低价，则无法计算
            if (highestHigh == lowestLow)
            {
                return new KdjOutput { K = 50, D = 50, J = 50, Signal = KdjSignalType.None };
            }

            // 计算RSV
            decimal rsv = ((currentData.Close - lowestLow) / (highestHigh - lowestLow)) * 100;

            // 使用当前RSV值作为基础，避免递归计算
            decimal k = Math.Max(0, Math.Min(100, 2.0m/3.0m * 50 + 1.0m/3.0m * rsv)); // 简化K值计算
            decimal d = Math.Max(0, Math.Min(100, 2.0m/3.0m * 50 + 1.0m/3.0m * k));   // 简化D值计算
            decimal j = Math.Max(0, Math.Min(100, 3 * k - 2 * d));                      // J值计算

            // 计算信号类型
            KdjSignalType signal = KdjSignalType.None;
            if (historicalData.Count >= 10) // 需要至少10个数据点才能比较当前和前一个状态
            {
                // 计算前一个状态的K、D值
                var prevRecentData = historicalData.TakeLast(10).Take(9).ToList(); // 前一个9个数据点
                decimal prevHighestHigh = prevRecentData.Max(d => d.High);
                decimal prevLowestLow = prevRecentData.Min(d => d.Low);
                var prevCurrentData = historicalData[historicalData.Count - 2]; // 前一个数据点

                if (prevHighestHigh != prevLowestLow) // 确保分母不为0
                {
                    decimal prevRsv = ((prevCurrentData.Close - prevLowestLow) / (prevHighestHigh - prevLowestLow)) * 100;
                    decimal prevK = Math.Max(0, Math.Min(100, 2.0m/3.0m * 50 + 1.0m/3.0m * prevRsv));
                    decimal prevD = Math.Max(0, Math.Min(100, 2.0m/3.0m * 50 + 1.0m/3.0m * prevK));

                    // 金叉：K线从下方穿越D线
                    if (prevK <= prevD && k > d)
                    {
                        signal = KdjSignalType.GoldenCross;
                    }
                    // 死叉：K线从上方穿越D线
                    else if (prevK >= prevD && k < d)
                    {
                        signal = KdjSignalType.DeathCross;
                    }
                }
            }

            return new KdjOutput { K = k, D = d, J = j, Signal = signal };
        }

        /// <summary>
        /// 计算移动平均线
        /// </summary>
        private static MovingAverageOutput CalculateMovingAverage(List<StockData> historicalData)
        {
            if (historicalData.Count < 5) // 至少需要5个数据点来计算有效均线
            {
                var currentClose = historicalData.LastOrDefault()?.Close ?? 0;
                return new MovingAverageOutput { ShortMa = currentClose, LongMa = currentClose, Signal = MovingAverageSignalType.None };
            }

            var prices = historicalData.Select(d => d.Close).ToList();
            
            // 计算短期均线 (5日)
            int shortPeriod = Math.Min(5, prices.Count);
            decimal shortMa = prices.Skip(Math.Max(0, prices.Count - shortPeriod)).DefaultIfEmpty(0).Average();
            
            // 计算长期均线 (20日)
            int longPeriod = Math.Min(20, prices.Count);
            decimal longMa = prices.Skip(Math.Max(0, prices.Count - longPeriod)).DefaultIfEmpty(0).Average();

            // 计算信号类型
            MovingAverageSignalType signal = MovingAverageSignalType.None;
            if (historicalData.Count >= 6) // 需要至少6个数据点才能比较当前和前一个状态
            {
                // 计算前一个状态的均线
                var prevPrices = historicalData.Take(historicalData.Count - 1).Select(d => d.Close).ToList();
                
                int prevShortPeriod = Math.Min(5, prevPrices.Count);
                decimal prevShortMa = prevPrices.Skip(Math.Max(0, prevPrices.Count - prevShortPeriod)).DefaultIfEmpty(0).Average();
                
                int prevLongPeriod = Math.Min(20, prevPrices.Count);
                decimal prevLongMa = prevPrices.Skip(Math.Max(0, prevPrices.Count - prevLongPeriod)).DefaultIfEmpty(0).Average();

                bool currShortAboveLong = shortMa > longMa;
                bool prevShortAboveLong = prevShortMa > prevLongMa;

                // 多头排列：短期均线上穿长期均线
                if (!prevShortAboveLong && currShortAboveLong)
                {
                    signal = MovingAverageSignalType.Bullish;
                }
                // 空头排列：短期均线下穿长期均线
                else if (prevShortAboveLong && !currShortAboveLong)
                {
                    signal = MovingAverageSignalType.Bearish;
                }
            }

            return new MovingAverageOutput { ShortMa = shortMa, LongMa = longMa, Signal = signal };
        }

        /// <summary>
        /// 计算RSI指标 - 简化版本以提高性能
        /// </summary>
        private static decimal CalculateRsi(List<StockData> historicalData)
        {
            if (historicalData.Count < 14) // RSI需要至少14天数据
            {
                return 50; // 返回中性值
            }

            // 取最近14天的数据
            var recentPrices = historicalData.TakeLast(14).Select(d => d.Close).ToList();
            
            decimal gainSum = 0;
            decimal lossSum = 0;
            
            for (int i = 1; i < recentPrices.Count; i++)
            {
                decimal change = recentPrices[i] - recentPrices[i - 1];
                if (change > 0)
                {
                    gainSum += change;
                }
                else
                {
                    lossSum += Math.Abs(change);
                }
            }
            
            int periods = recentPrices.Count - 1;
            if (periods <= 0) return 50;
            
            decimal avgGain = gainSum / periods;
            decimal avgLoss = lossSum / periods;
            
            if (avgLoss == 0) return 100; // 完全上涨
            if (avgGain == 0) return 0;   // 完全下跌
            
            decimal rs = avgGain / avgLoss;
            decimal rsi = 100 - (100 / (1 + rs));

            return Math.Max(0, Math.Min(100, rsi));
        }
    }
}