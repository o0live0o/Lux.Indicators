using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lux.Indicators.Models;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.TrendIndicators;

namespace Lux.Indicators.Demo.Providers
{
    /// <summary>
    /// 数据提供者接口 - 定义数据获取的契约
    /// </summary>
    public interface IDataProvider
    {
        /// <summary>
        /// 获取股票数据
        /// </summary>
        Task<List<StockData>> GetStockDataAsync(string symbol, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// 获取实时股票数据
        /// </summary>
        Task<StockData> GetRealTimeDataAsync(string symbol);
        
        /// <summary>
        /// 计算技术指标
        /// </summary>
        Task<(MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)> CalculateIndicatorsAsync(List<StockData> data);
    }

    /// <summary>
    /// 文件数据提供者 - 从本地文件读取数据
    /// </summary>
    public class FileDataProvider : IDataProvider
    {
        private readonly string _dataDirectory;

        public FileDataProvider(string dataDirectory = "")
        {
            _dataDirectory = dataDirectory;
        }

        public async Task<List<StockData>> GetStockDataAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            // 模拟从文件读取数据
            await Task.Delay(10); // 模拟I/O延迟
            
            // 这里应该实际读取文件，例如CSV或TSV
            // 为演示目的，生成模拟数据
            var data = new List<StockData>();
            var currentDate = startDate;
            
            while (currentDate <= endDate)
            {
                // 生成模拟数据
                var price = 100m + (decimal)(currentDate.Day % 20); // 简单的价格变化
                data.Add(new StockData
                {
                    Date = currentDate,
                    Open = price - 1,
                    High = price + 2,
                    Low = price - 2,
                    Close = price + (currentDate.Hour % 3),
                    Volume = 1000 + currentDate.Day * 100
                });
                
                currentDate = currentDate.AddDays(1);
            }
            
            return data;
        }

        public async Task<StockData> GetRealTimeDataAsync(string symbol)
        {
            await Task.Delay(5); // 模拟I/O延迟
            
            // 返回当前时间的模拟数据
            var price = 100m + (decimal)(DateTime.Now.Second % 10); // 基于秒数的变化
            return new StockData
            {
                Date = DateTime.Now,
                Open = price - 0.5m,
                High = price + 1,
                Low = price - 1,
                Close = price,
                Volume = 500
            };
        }

        public async Task<(MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)> CalculateIndicatorsAsync(List<StockData> data)
        {
            await Task.Delay(1); // 模拟计算延迟
            
            var closePrices = data.Select(s => s.Close).ToList();
            var highPrices = data.Select(s => s.High).ToList();
            var lowPrices = data.Select(s => s.Low).ToList();
            
            // 计算MACD
            MacdOutput macd = closePrices.Count >= 26 
                ? MacdAnalyzer.Analyze(closePrices, 12, 26, 9).Last() 
                : new MacdOutput { Dif = 0, Dea = 0, Histogram = 0, Signal = MacdSignalType.None };
            
            // 计算KDJ
            KdjOutput kdj = highPrices.Count >= 9 
                ? KdjAnalyzer.Analyze(highPrices, lowPrices, closePrices, 9, 3, 3).Last() 
                : new KdjOutput { K = 50, D = 50, J = 50, Signal = KdjSignalType.None };
            
            // 计算移动平均
            MovingAverageOutput ma = closePrices.Count >= 10 
                ? MovingAverageAnalyzer.Analyze(closePrices, 5, 10).Last() 
                : new MovingAverageOutput { ShortMa = 0, LongMa = 0, Signal = MovingAverageSignalType.None };
            
            // 计算RSI
            decimal rsi = closePrices.Count >= 15 
                ? CalculateSimpleRsi(closePrices, 14) 
                : 50;
            
            return (macd, kdj, ma, rsi);
        }
        
        private decimal CalculateSimpleRsi(List<decimal> prices, int period)
        {
            if (prices.Count < 2) return 50;
            
            var gains = 0m;
            var losses = 0m;
            var count = Math.Min(period, prices.Count - 1);
            
            for (int i = prices.Count - count; i < prices.Count; i++)
            {
                if (i > 0)
                {
                    var change = prices[i] - prices[i - 1];
                    if (change > 0)
                        gains += change;
                    else
                        losses += Math.Abs(change);
                }
            }
            
            if (losses == 0) return 100;
            if (gains == 0) return 0;
            
            var rs = gains / losses;
            return 100 - (100 / (1 + rs));
        }
    }

    /// <summary>
    /// API数据提供者 - 从远程API获取数据
    /// </summary>
    public class ApiDataProvider : IDataProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public ApiDataProvider(HttpClient httpClient, string apiBaseUrl)
        {
            _httpClient = httpClient ?? new HttpClient();
            _apiBaseUrl = apiBaseUrl;
        }

        public async Task<List<StockData>> GetStockDataAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            // 模拟API调用
            await Task.Delay(50); // 模拟网络延迟
            
            // 实际实现应该是调用API
            // var response = await _httpClient.GetAsync($"{_apiBaseUrl}/stocks/{symbol}?start={startDate:yyyy-MM-dd}&end={endDate:yyyy-MM-dd}");
            // return ParseResponse(response);
            
            // 为演示目的，生成模拟数据
            var data = new List<StockData>();
            var currentDate = startDate;
            
            while (currentDate <= endDate)
            {
                var price = 100m + (decimal)(new Random().NextDouble() * 10); // 随机价格波动
                data.Add(new StockData
                {
                    Date = currentDate,
                    Open = price - (decimal)(new Random().NextDouble() * 2),
                    High = price + (decimal)(new Random().NextDouble() * 2),
                    Low = price - (decimal)(new Random().NextDouble() * 2),
                    Close = price + (decimal)(new Random().NextDouble() - 0.5),
                    Volume = 1000 + new Random().Next(1000)
                });
                
                currentDate = currentDate.AddDays(1);
            }
            
            return data;
        }

        public async Task<StockData> GetRealTimeDataAsync(string symbol)
        {
            // 模拟实时API调用
            await Task.Delay(20); // 模拟网络延迟
            
            // 实际实现应该是调用实时数据API
            // var response = await _httpClient.GetAsync($"{_apiBaseUrl}/stocks/{symbol}/realtime");
            // return ParseSingleResponse(response);
            
            // 为演示目的，生成随机实时数据
            var price = 100m + (decimal)(new Random().NextDouble() * 5);
            return new StockData
            {
                Date = DateTime.Now,
                Open = price - 0.5m,
                High = price + 1,
                Low = price - 1,
                Close = price + (decimal)(new Random().NextDouble() - 0.5),
                Volume = 800 + new Random().Next(500)
            };
        }

        public async Task<(MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)> CalculateIndicatorsAsync(List<StockData> data)
        {
            // 在实际API实现中，可能会调用服务端的指标计算
            await Task.Delay(10); // 模拟网络延迟
            
            var closePrices = data.Select(s => s.Close).ToList();
            var highPrices = data.Select(s => s.High).ToList();
            var lowPrices = data.Select(s => s.Low).ToList();
            
            // 计算MACD
            MacdOutput macd = closePrices.Count >= 26 
                ? MacdAnalyzer.Analyze(closePrices, 12, 26, 9).Last() 
                : new MacdOutput { Dif = 0, Dea = 0, Histogram = 0, Signal = MacdSignalType.None };
            
            // 计算KDJ
            KdjOutput kdj = highPrices.Count >= 9 
                ? KdjAnalyzer.Analyze(highPrices, lowPrices, closePrices, 9, 3, 3).Last() 
                : new KdjOutput { K = 50, D = 50, J = 50, Signal = KdjSignalType.None };
            
            // 计算移动平均
            MovingAverageOutput ma = closePrices.Count >= 10 
                ? MovingAverageAnalyzer.Analyze(closePrices, 5, 10).Last() 
                : new MovingAverageOutput { ShortMa = 0, LongMa = 0, Signal = MovingAverageSignalType.None };
            
            // 计算RSI
            decimal rsi = closePrices.Count >= 15 
                ? CalculateSimpleRsi(closePrices, 14) 
                : 50;
            
            return (macd, kdj, ma, rsi);
        }
        
        private decimal CalculateSimpleRsi(List<decimal> prices, int period)
        {
            if (prices.Count < 2) return 50;
            
            var gains = 0m;
            var losses = 0m;
            var count = Math.Min(period, prices.Count - 1);
            
            for (int i = prices.Count - count; i < prices.Count; i++)
            {
                if (i > 0)
                {
                    var change = prices[i] - prices[i - 1];
                    if (change > 0)
                        gains += change;
                    else
                        losses += Math.Abs(change);
                }
            }
            
            if (losses == 0) return 100;
            if (gains == 0) return 0;
            
            var rs = gains / losses;
            return 100 - (100 / (1 + rs));
        }
    }

    /// <summary>
    /// 混合数据提供者 - 结合多种数据源的优势
    /// </summary>
    public class HybridDataProvider : IDataProvider
    {
        private readonly IDataProvider _primaryProvider;
        private readonly IDataProvider _fallbackProvider;

        public HybridDataProvider(IDataProvider primaryProvider, IDataProvider fallbackProvider)
        {
            _primaryProvider = primaryProvider ?? throw new ArgumentNullException(nameof(primaryProvider));
            _fallbackProvider = fallbackProvider ?? throw new ArgumentNullException(nameof(fallbackProvider));
        }

        public async Task<List<StockData>> GetStockDataAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            try
            {
                // 首先尝试从主数据源获取
                var data = await _primaryProvider.GetStockDataAsync(symbol, startDate, endDate);
                
                // 如果主数据源为空或异常，使用备用数据源
                if (data == null || !data.Any())
                {
                    data = await _fallbackProvider.GetStockDataAsync(symbol, startDate, endDate);
                }
                
                return data;
            }
            catch
            {
                // 主数据源失败时，使用备用数据源
                return await _fallbackProvider.GetStockDataAsync(symbol, startDate, endDate);
            }
        }

        public async Task<StockData> GetRealTimeDataAsync(string symbol)
        {
            try
            {
                // 首先尝试从主数据源获取
                return await _primaryProvider.GetRealTimeDataAsync(symbol);
            }
            catch
            {
                // 主数据源失败时，使用备用数据源
                return await _fallbackProvider.GetRealTimeDataAsync(symbol);
            }
        }

        public async Task<(MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)> CalculateIndicatorsAsync(List<StockData> data)
        {
            // 使用主数据源的计算方法
            return await _primaryProvider.CalculateIndicatorsAsync(data);
        }
    }
}