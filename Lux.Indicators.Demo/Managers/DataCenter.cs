using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lux.Indicators.Models;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.TrendIndicators;
using Lux.Indicators.Demo.Providers;

namespace Lux.Indicators.Demo.Managers
{
    /// <summary>
    /// 数据获取中心 - 获取所有股票数据并根据订阅关系分发
    /// </summary>
    public class DataCenter
    {
        private readonly IDataProvider _dataProvider;
        private readonly Dictionary<string, List<StockData>> _stockDataCache;
        private readonly object _cacheLock = new object();

        public DataCenter(IDataProvider dataProvider = null)
        {
            _dataProvider = dataProvider ?? new FileDataProvider();
            _stockDataCache = new Dictionary<string, List<StockData>>();
        }

        /// <summary>
        /// 获取指定股票的历史数据
        /// </summary>
        public async Task<List<StockData>> GetStockDataAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            lock(_cacheLock)
            {
                if (_stockDataCache.ContainsKey(symbol))
                {
                    var cachedData = _stockDataCache[symbol];
                    return cachedData.Where(d => d.Date >= startDate && d.Date <= endDate).ToList();
                }
            }

            var data = await _dataProvider.GetStockDataAsync(symbol, startDate, endDate);
            
            lock(_cacheLock)
            {
                _stockDataCache[symbol] = data;
            }
            
            return data;
        }

        /// <summary>
        /// 获取实时数据
        /// </summary>
        public async Task<StockData> GetRealTimeDataAsync(string symbol)
        {
            return await _dataProvider.GetRealTimeDataAsync(symbol);
        }

        /// <summary>
        /// 批量获取多个股票的数据
        /// </summary>
        public async Task<Dictionary<string, List<StockData>>> GetMultipleStocksDataAsync(
            IEnumerable<string> symbols, 
            DateTime startDate, 
            DateTime endDate)
        {
            var result = new Dictionary<string, List<StockData>>();

            foreach (var symbol in symbols)
            {
                result[symbol] = await GetStockDataAsync(symbol, startDate, endDate);
            }

            return result;
        }

        /// <summary>
        /// 获取指定股票的技术指标
        /// </summary>
        public async Task<(MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)> GetIndicatorsAsync(string symbol, List<StockData> data)
        {
            return await _dataProvider.CalculateIndicatorsAsync(data);
        }

        /// <summary>
        /// 获取所有已缓存的股票代码
        /// </summary>
        public string[] GetCachedSymbols()
        {
            lock(_cacheLock)
            {
                return _stockDataCache.Keys.ToArray();
            }
        }

        /// <summary>
        /// 清除指定股票的缓存
        /// </summary>
        public void ClearCache(string symbol)
        {
            lock(_cacheLock)
            {
                _stockDataCache.Remove(symbol);
            }
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearAllCache()
        {
            lock(_cacheLock)
            {
                _stockDataCache.Clear();
            }
        }
    }
}