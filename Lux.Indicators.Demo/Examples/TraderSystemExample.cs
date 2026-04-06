using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lux.Indicators;
using Lux.Indicators.Models;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.TrendIndicators;
using Lux.Indicators.Demo.Providers;

namespace Lux.Indicators.Demo.Examples
{
    /// <summary>
    /// 交易员系统示例
    /// </summary>
    public class TraderSystemExample
    {
        public static async Task RunAsync()
        {
            // 创建数据提供者 - 这里使用文件数据提供者进行演示
            var fileDataProvider = new FileDataProvider();
            
            // 或者使用API数据提供者（需要配置HTTP客户端和API地址）
            // var apiDataProvider = new ApiDataProvider(new HttpClient(), "https://api.example.com");
            
            // 或者使用混合数据提供者（优先使用API，备选文件）
            // var hybridDataProvider = new HybridDataProvider(apiDataProvider, fileDataProvider);
            
            // 创建交易员管理系统示例，传入数据提供者
            var traderManager = new TraderManager(fileDataProvider);

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

            var swingTrader = new ActiveTrader(  // 使用ActiveTrader作为波段交易员
                "波段交易员", 
                100000m, 
                new SwingTradingStrategy(), 
                new BalancedPositionManagement()
            );

            // 将交易员添加到管理系统
            traderManager.AddTrader("active", activeTrader);
            traderManager.AddTrader("conservative", conservativeTrader);
            traderManager.AddTrader("swing", swingTrader);

            Console.WriteLine("交易员系统启动成功！");
            Console.WriteLine($"当前管理 {traderManager.GetAllTraders().Length} 个交易员");

            // 使用数据提供者从数据源获取数据并处理
            Console.WriteLine("\n开始从数据源获取数据并处理...");
            
            var startDate = DateTime.Now.AddDays(-5);
            var endDate = DateTime.Now;
            var stockSymbol = "TEST.STOCK";

            // 从数据源获取数据并自动分发给所有交易员
            await traderManager.ProcessDataFromSourceAsync(stockSymbol, startDate, endDate);

            // 模拟获取实时数据
            Console.WriteLine("\n获取实时数据...");
            await traderManager.ProcessRealTimeDataAsync(stockSymbol);

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
        }
    }
}