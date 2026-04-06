using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lux.Indicators.Models;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.TrendIndicators;
using Lux.Indicators.Demo.Aggregation;

namespace Lux.Indicators.Demo.Managers
{
    /// <summary>
    /// 智能数据获取中心 - 整合多种数据和信号聚合源
    /// </summary>
    public class IntelligentDataCenter
    {
        private readonly AggregationManager _aggregationManager;
        private readonly Dictionary<string, List<StockData>> _stockDataCache;
        private readonly Dictionary<string, List<SignalData>> _signalCache;
        private readonly object _cacheLock = new object();

        public IntelligentDataCenter(AggregationManager aggregationManager = null)
        {
            _aggregationManager = aggregationManager ?? new AggregationManager();
            _stockDataCache = new Dictionary<string, List<StockData>>();
            _signalCache = new Dictionary<string, List<SignalData>>();
            
            // 初始化默认的聚合源
            if (!_aggregationManager.GetDataAggregators().Any())
            {
                _aggregationManager.AddDataAggregator(new FileDataAggregator());
                _aggregationManager.AddDataAggregator(new ApiDataAggregator());
                _aggregationManager.AddDataAggregator(new DatabaseDataAggregator());
            }
            
            if (!_aggregationManager.GetSignalAggregators().Any())
            {
                _aggregationManager.AddSignalAggregator(new QuantitativeSignalAggregator());
                _aggregationManager.AddSignalAggregator(new NewsAnalysisSignalAggregator());
                _aggregationManager.AddSignalAggregator(new TechnicalIndicatorSignalAggregator());
                _aggregationManager.AddSignalAggregator(new SocialMediaSignalAggregator());
            }
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

            var data = await _aggregationManager.GetAllStockDataAsync(symbol, startDate, endDate);
            
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
            return await _aggregationManager.GetConsolidatedRealTimeDataAsync(symbol);
        }

        /// <summary>
        /// 获取所有潜在投资机会（通过信号聚合）
        /// </summary>
        public async Task<List<SignalData>> GetInvestmentOpportunitiesAsync(DateTime fromDate, DateTime toDate)
        {
            var signals = await _aggregationManager.GetAllSignalsAsync(fromDate, toDate);
            
            // 缓存信号
            lock(_cacheLock)
            {
                _signalCache.Clear(); // 清除旧信号
                
                foreach (var signal in signals)
                {
                    if (!_signalCache.ContainsKey(signal.Symbol))
                    {
                        _signalCache[signal.Symbol] = new List<SignalData>();
                    }
                    _signalCache[signal.Symbol].Add(signal);
                }
            }
            
            return signals;
        }

        /// <summary>
        /// 获取特定股票的相关信号
        /// </summary>
        public List<SignalData> GetSignalsForSymbol(string symbol)
        {
            lock(_cacheLock)
            {
                if (_signalCache.ContainsKey(symbol))
                {
                    return _signalCache[symbol].ToList();
                }
                return new List<SignalData>();
            }
        }

        /// <summary>
        /// 获取高置信度的投资信号
        /// </summary>
        public List<SignalData> GetHighConfidenceSignals(decimal minConfidence = 0.7m)
        {
            lock(_cacheLock)
            {
                return _signalCache.Values
                    .SelectMany(signals => signals)
                    .Where(signal => signal.Confidence >= minConfidence)
                    .OrderByDescending(signal => signal.Confidence)
                    .ToList();
            }
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
            // 这里需要从Providers命名空间获取IDataProvider实现
            // 为了简化，我们使用一个临时的实现
            var provider = new Lux.Indicators.Demo.Providers.FileDataProvider();
            return await provider.CalculateIndicatorsAsync(data);
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
        /// 获取所有已检测到的股票代码（来自信号）
        /// </summary>
        public string[] GetDetectedSymbols()
        {
            lock(_cacheLock)
            {
                return _signalCache.Keys.ToArray();
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
                _signalCache.Remove(symbol);
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
                _signalCache.Clear();
            }
        }
        
        #region Aggregation Manager Access
        
        /// <summary>
        /// 获取聚合管理器
        /// </summary>
        public AggregationManager GetAggregationManager()
        {
            return _aggregationManager;
        }
        
        /// <summary>
        /// 添加数据聚合源
        /// </summary>
        public void AddDataAggregator(IDataAggregator aggregator)
        {
            _aggregationManager.AddDataAggregator(aggregator);
        }
        
        /// <summary>
        /// 添加信号聚合源
        /// </summary>
        public void AddSignalAggregator(ISignalAggregator aggregator)
        {
            _aggregationManager.AddSignalAggregator(aggregator);
        }
        
        /// <summary>
        /// 获取所有数据聚合源
        /// </summary>
        public IDataAggregator[] GetDataAggregators()
        {
            return _aggregationManager.GetDataAggregators();
        }
        
        /// <summary>
        /// 获取所有信号聚合源
        /// </summary>
        public ISignalAggregator[] GetSignalAggregators()
        {
            return _aggregationManager.GetSignalAggregators();
        }
        
        #endregion
    }
}