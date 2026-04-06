using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lux.Indicators;
using Lux.Indicators.Models;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.TrendIndicators;
using Lux.Indicators.Demo.Aggregation;
using Lux.Indicators.Demo.Providers;

namespace Lux.Indicators.Demo.Examples
{
    /// <summary>
    /// 高级智能交易系统示例 - 展示模块化的聚合源架构
    /// </summary>
    public class AdvancedIntelligentTradingExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== 高级智能交易系统示例 ===");

            // 创建数据提供者
            var dataProvider = new FileDataProvider();
            
            // 创建聚合管理器并添加自定义聚合源
            var aggregationManager = new AggregationManager();
            
            // 添加数据聚合源
            aggregationManager.AddDataAggregator(new FileDataAggregator());
            aggregationManager.AddDataAggregator(new ApiDataAggregator());
            aggregationManager.AddDataAggregator(new DatabaseDataAggregator());
            
            // 添加信号聚合源
            aggregationManager.AddSignalAggregator(new QuantitativeSignalAggregator());
            aggregationManager.AddSignalAggregator(new NewsAnalysisSignalAggregator());
            aggregationManager.AddSignalAggregator(new TechnicalIndicatorSignalAggregator());
            aggregationManager.AddSignalAggregator(new SocialMediaSignalAggregator());

            // 创建交易员管理系统
            var traderManager = new TraderManager(dataProvider, aggregationManager);

            // 创建不同类型的交易员
            var activeTrader = new ActiveTrader(
                "激进交易员", 
                100000m, 
                new ShortTermTradingStrategy(), 
                new AggressivePositionManagement()
            );

            var conservativeTrader = new ConservativeTrader(
                "保守交易员", 
                100000m, 
                new LongTermInvestmentStrategy(), 
                new ConservativePositionManagement()
            );

            // 将交易员添加到管理系统
            traderManager.AddTrader("active", activeTrader);
            traderManager.AddTrader("conservative", conservativeTrader);

            Console.WriteLine($"当前管理 {traderManager.GetAllTraders().Length} 个交易员");
            Console.WriteLine($"数据聚合源数量: {aggregationManager.GetDataAggregators().Length}");
            Console.WriteLine($"信号聚合源数量: {aggregationManager.GetSignalAggregators().Length}");

            // 定义要跟踪的股票
            var stockSymbols = new[] { "AAPL", "GOOGL", "MSFT", "TSLA", "NVDA", "AMZN" };
            var startDate = DateTime.Now.AddDays(-10);
            var endDate = DateTime.Now;

            // 使用聚合管理器发现潜在机会
            Console.WriteLine($"\n使用聚合管理器发现潜在投资机会...");
            var investmentSignals = await aggregationManager.GetAllSignalsAsync(startDate, endDate);
            
            Console.WriteLine($"发现 {investmentSignals.Count} 个投资信号:");
            foreach (var signal in investmentSignals.Take(10)) // 只显示前10个
            {
                Console.WriteLine($"  {signal.Symbol}: {signal.Type} (置信度: {signal.Confidence:F2}, 来源: {signal.Source})");
            }
            
            if (investmentSignals.Count > 10)
            {
                Console.WriteLine($"  ... 还有 {investmentSignals.Count - 10} 个信号");
            }

            // 为交易员设置初始关注（模拟已持仓股票）
            activeTrader.SubscribeToSymbol("AAPL"); // 激进交易员关注AAPL
            activeTrader.SubscribeToSymbol("TSLA"); // 和TSLA
            activeTrader.SubscribeToSymbol("NVDA"); // 以及热门科技股
            
            conservativeTrader.SubscribeToSymbol("GOOGL"); // 保守交易员关注GOOGL
            conservativeTrader.SubscribeToSymbol("MSFT");  // 和MSFT
            conservativeTrader.SubscribeToSymbol("AMZN");  // 以及稳定蓝筹股

            Console.WriteLine($"\n激进交易员已关注: AAPL, TSLA, NVDA");
            Console.WriteLine($"保守交易员已关注: GOOGL, MSFT, AMZN");

            // 获取高置信度信号
            var highConfidenceSignals = await aggregationManager.GetHighConfidenceSignalsAsync(0.7m, startDate, endDate);
            Console.WriteLine($"\n高置信度信号 ({highConfidenceSignals.Count} 个):");
            foreach (var signal in highConfidenceSignals)
            {
                Console.WriteLine($"  {signal.Symbol}: {signal.Type} (置信度: {signal.Confidence:F2})");
                
                // 根据信号决定是否让交易员关注该股票
                if (signal.Type == SignalType.Buy && signal.Confidence >= 0.8m)
                {
                    // 对于高置信度的买入信号，可以让相应类型的交易员开始关注
                    if (signal.Symbol == "NVDA" || signal.Symbol == "TSLA" || signal.Symbol == "AAPL")
                    {
                        traderManager.SubscribeToSymbol("active", signal.Symbol);
                        Console.WriteLine($"    -> 激进交易员开始关注 {signal.Symbol}");
                    }
                    else if (signal.Symbol == "GOOGL" || signal.Symbol == "MSFT" || signal.Symbol == "AMZN")
                    {
                        traderManager.SubscribeToSymbol("conservative", signal.Symbol);
                        Console.WriteLine($"    -> 保守交易员开始关注 {signal.Symbol}");
                    }
                }
            }

            Console.WriteLine($"\n开始处理 {stockSymbols.Length} 只股票的数据...");

            // 数据获取中心获取所有相关股票的数据
            foreach (var symbol in stockSymbols)
            {
                Console.WriteLine($"\n处理 {symbol} 的历史数据...");
                
                // 获取指定股票的历史数据
                var stockDataList = await dataProvider.GetStockDataAsync(symbol, startDate, endDate);
                
                foreach (var stockData in stockDataList)
                {
                    // 将数据发送给所有交易员，但只有对该股票感兴趣的交易员才会处理
                    traderManager.SendDataToTraders(stockData, symbol, 
                        new MacdOutput { Dif = 0, Dea = 0, Histogram = 0 }, 
                        new KdjOutput { K = 50, D = 50, J = 50 }, 
                        new MovingAverageOutput { ShortMa = stockData.Close * 0.95m, LongMa = stockData.Close }, 
                        50);
                    
                    Console.WriteLine($"  {symbol}: {stockData.Date:yyyy-MM-dd}, 价格: {stockData.Close:F2}");
                }
            }

            // 模拟获取实时数据
            Console.WriteLine("\n获取各股票实时数据...");
            foreach (var symbol in stockSymbols)
            {
                var realTimeData = await dataProvider.GetRealTimeDataAsync(symbol);
                
                // 发送实时数据
                traderManager.SendDataToTraders(realTimeData, symbol, 
                    new MacdOutput { Dif = 0, Dea = 0, Histogram = 0 }, 
                    new KdjOutput { K = 50, D = 50, J = 50 }, 
                    new MovingAverageOutput { ShortMa = realTimeData.Close * 0.95m, LongMa = realTimeData.Close }, 
                    50);
                
                Console.WriteLine($"实时数据 - {symbol}: 价格 {realTimeData.Close:F2}");
            }

            // 显示最终结果
            Console.WriteLine("\n=== 交易员最终状态 ===");
            foreach (var trader in traderManager.GetAllTraders())
            {
                Console.WriteLine($"{trader.Name}: 余额={trader.Balance:C}, 总价值={trader.TotalValue:C}, 交易次数={trader.Trades.Count}");
            }

            // 计算并显示每个交易员的盈亏
            Console.WriteLine("\n=== 盈亏分析 ===");
            foreach (var trader in traderManager.GetAllTraders())
            {
                var initialBalance = 100000m; // 假设初始资金为10万
                var profit = trader.TotalValue - initialBalance;
                var profitPercent = initialBalance != 0 ? (profit / initialBalance) * 100 : 0;
                
                Console.WriteLine($"{trader.Name}: 盈亏={profit:+0.00;-0.00;0} ({profitPercent:+0.00;-0.00;0}%)");
            }

            Console.WriteLine($"\n所有被关注的股票: {string.Join(", ", traderManager.GetInterestedSymbols())}");
            Console.WriteLine($"所有被订阅的股票: {string.Join(", ", traderManager.GetSubscribedSymbols())}");
            
            // 显示聚合源统计
            Console.WriteLine("\n=== 聚合源统计 ===");
            Console.WriteLine($"数据聚合源: {string.Join(", ", aggregationManager.GetDataAggregators().Select(da => da.Name))}");
            Console.WriteLine($"信号聚合源: {string.Join(", ", aggregationManager.GetSignalAggregators().Select(sa => sa.Name))}");
        }
    }
}