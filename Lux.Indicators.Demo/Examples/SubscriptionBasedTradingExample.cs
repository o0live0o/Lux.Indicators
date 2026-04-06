using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lux.Indicators;
using Lux.Indicators.Models;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.TrendIndicators;
using Lux.Indicators.Demo.Providers;

namespace Lux.Indicators.Demo.Examples
{
    /// <summary>
    /// 订阅式交易系统示例 - 演示数据获取中心与订阅机制
    /// </summary>
    public class SubscriptionBasedTradingExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== 订阅式交易系统示例 ===");

            // 创建数据提供者
            var dataProvider = new FileDataProvider();
            
            // 创建交易员管理系统
            var traderManager = new TraderManager(dataProvider);

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

            // 定义要跟踪的股票
            var stockSymbols = new[] { "AAPL", "GOOGL", "MSFT", "TSLA" };
            var startDate = DateTime.Now.AddDays(-10);
            var endDate = DateTime.Now;

            // 为交易员设置订阅
            traderManager.SubscribeToSymbols("active", "AAPL", "TSLA"); // 激进交易员订阅AAPL和TSLA
            traderManager.SubscribeToSymbols("conservative", "GOOGL", "MSFT"); // 保守交易员订阅GOOGL和MSFT

            Console.WriteLine($"\n激进交易员订阅: AAPL, TSLA");
            Console.WriteLine($"保守交易员订阅: GOOGL, MSFT");

            Console.WriteLine($"\n开始处理 {stockSymbols.Length} 只股票的数据...");

            // 数据获取中心获取所有股票的数据
            foreach (var symbol in stockSymbols)
            {
                Console.WriteLine($"\n处理 {symbol} 的历史数据...");
                
                // 获取指定股票的历史数据
                var stockDataList = await dataProvider.GetStockDataAsync(symbol, startDate, endDate);
                
                foreach (var stockData in stockDataList)
                {
                    // 将数据发送给所有交易员，但只有订阅了该股票的交易员才会处理
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

            Console.WriteLine($"\n所有被订阅的股票: {string.Join(", ", traderManager.GetSubscribedSymbols())}");
        }
    }
}