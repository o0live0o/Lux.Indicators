using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lux.Indicators.Models;

namespace Lux.Indicators.Demo.Aggregation
{
    /// <summary>
    /// 数据聚合管理器 - 协调所有聚合源
    /// </summary>
    public class AggregationManager
    {
        private readonly List<IDataAggregator> _dataAggregators;
        private readonly List<ISignalAggregator> _signalAggregators;
        private readonly Dictionary<string, List<StockData>> _dataCache;
        private readonly Dictionary<string, List<SignalData>> _signalCache;
        private readonly object _cacheLock = new object();

        public AggregationManager()
        {
            _dataAggregators = new List<IDataAggregator>();
            _signalAggregators = new List<ISignalAggregator>();
            _dataCache = new Dictionary<string, List<StockData>>();
            _signalCache = new Dictionary<string, List<SignalData>>();
        }

        public AggregationManager(List<IDataAggregator> dataAggregators, List<ISignalAggregator> signalAggregators)
        {
            _dataAggregators = dataAggregators ?? new List<IDataAggregator>();
            _signalAggregators = signalAggregators ?? new List<ISignalAggregator>();
            _dataCache = new Dictionary<string, List<StockData>>();
            _signalCache = new Dictionary<string, List<SignalData>>();
        }

        #region Data Aggregator Management
        
        public void AddDataAggregator(IDataAggregator aggregator)
        {
            if (!_dataAggregators.Contains(aggregator))
            {
                _dataAggregators.Add(aggregator);
            }
        }

        public void RemoveDataAggregator(IDataAggregator aggregator)
        {
            _dataAggregators.Remove(aggregator);
        }

        public void ClearDataAggregators()
        {
            _dataAggregators.Clear();
        }

        public IDataAggregator[] GetDataAggregators()
        {
            return _dataAggregators.ToArray();
        }

        #endregion

        #region Signal Aggregator Management

        public void AddSignalAggregator(ISignalAggregator aggregator)
        {
            if (!_signalAggregators.Contains(aggregator))
            {
                _signalAggregators.Add(aggregator);
            }
        }

        public void RemoveSignalAggregator(ISignalAggregator aggregator)
        {
            _signalAggregators.Remove(aggregator);
        }

        public void ClearSignalAggregators()
        {
            _signalAggregators.Clear();
        }

        public ISignalAggregator[] GetSignalAggregators()
        {
            return _signalAggregators.ToArray();
        }

        #endregion

        #region Data Retrieval

        /// <summary>
        /// 从所有数据聚合源获取股票数据（并行处理）
        /// </summary>
        public async Task<List<StockData>> GetAllStockDataAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            var allData = new List<StockData>();
            
            // 并行获取所有数据源的数据
            var tasks = _dataAggregators.Select(async aggregator =>
            {
                try
                {
                    var data = await aggregator.GetStockDataAsync(symbol, startDate, endDate);
                    return new { Source = aggregator.Name, Data = data.ToList() };
                }
                catch (Exception ex)
                {
                    // 记录错误但不中断整个过程
                    Console.WriteLine($"Error getting data from {aggregator.Name}: {ex.Message}");
                    return new { Source = aggregator.Name, Data = new List<StockData>() };
                }
            }).ToList();

            var results = await Task.WhenAll(tasks);
            
            foreach (var result in results)
            {
                allData.AddRange(result.Data);
            }

            // 按日期排序并去重
            return allData
                .OrderBy(d => d.Date)
                .GroupBy(d => d.Date)
                .Select(g => g.First()) // 取每个日期的第一个数据点
                .ToList();
        }

        /// <summary>
        /// 从指定数据聚合源获取股票数据
        /// </summary>
        public async Task<List<StockData>> GetStockDataFromSourceAsync(string sourceName, string symbol, DateTime startDate, DateTime endDate)
        {
            var aggregator = _dataAggregators.FirstOrDefault(a => a.Name.Equals(sourceName, StringComparison.OrdinalIgnoreCase));
            if (aggregator != null)
            {
                var data = await aggregator.GetStockDataAsync(symbol, startDate, endDate);
                return data.ToList();
            }
            return new List<StockData>();
        }

        /// <summary>
        /// 获取实时数据（从所有源获取并合并）
        /// </summary>
        public async Task<StockData> GetConsolidatedRealTimeDataAsync(string symbol)
        {
            var tasks = _dataAggregators.Select(async aggregator =>
            {
                try
                {
                    return await aggregator.GetRealTimeDataAsync(symbol);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting real-time data from {aggregator.Name}: {ex.Message}");
                    return null;
                }
            }).ToList();

            var results = await Task.WhenAll(tasks);
            
            // 合并结果，这里简单地取第一个有效的数据
            var validResults = results.Where(r => r != null).ToList();
            if (validResults.Any())
            {
                // 可以实现更复杂的合并逻辑，比如加权平均
                return validResults.First();
            }
            
            return null;
        }

        #endregion

        #region Signal Retrieval

        /// <summary>
        /// 从所有信号聚合源获取信号（并行处理）
        /// </summary>
        public async Task<List<SignalData>> GetAllSignalsAsync(DateTime fromDate, DateTime toDate)
        {
            var allSignals = new List<SignalData>();
            
            // 并行获取所有信号源的数据
            var tasks = _signalAggregators.Select(async aggregator =>
            {
                try
                {
                    var signals = await aggregator.GetSignalsAsync(fromDate, toDate);
                    return new { Source = aggregator.Name, Signals = signals.ToList() };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting signals from {aggregator.Name}: {ex.Message}");
                    return new { Source = aggregator.Name, Signals = new List<SignalData>() };
                }
            }).ToList();

            var results = await Task.WhenAll(tasks);
            
            foreach (var result in results)
            {
                allSignals.AddRange(result.Signals);
            }

            return allSignals;
        }

        /// <summary>
        /// 从指定信号聚合源获取信号
        /// </summary>
        public async Task<List<SignalData>> GetSignalsFromSourceAsync(string sourceName, DateTime fromDate, DateTime toDate)
        {
            var aggregator = _signalAggregators.FirstOrDefault(a => a.Name.Equals(sourceName, StringComparison.OrdinalIgnoreCase));
            if (aggregator != null)
            {
                var signals = await aggregator.GetSignalsAsync(fromDate, toDate);
                return signals.ToList();
            }
            return new List<SignalData>();
        }

        /// <summary>
        /// 获取特定股票的信号
        /// </summary>
        public async Task<List<SignalData>> GetSignalsForSymbolAsync(string symbol, DateTime fromDate, DateTime toDate)
        {
            var allSignals = await GetAllSignalsAsync(fromDate, toDate);
            return allSignals.Where(s => s.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// 清除数据缓存
        /// </summary>
        public void ClearDataCache(string symbol = null)
        {
            lock (_cacheLock)
            {
                if (string.IsNullOrEmpty(symbol))
                {
                    _dataCache.Clear();
                }
                else
                {
                    _dataCache.Remove(symbol);
                }
            }
        }

        /// <summary>
        /// 清除信号缓存
        /// </summary>
        public void ClearSignalCache(string symbol = null)
        {
            lock (_cacheLock)
            {
                if (string.IsNullOrEmpty(symbol))
                {
                    _signalCache.Clear();
                }
                else
                {
                    _signalCache.Remove(symbol);
                }
            }
        }

        /// <summary>
        /// 获取缓存的股票数据
        /// </summary>
        public List<StockData> GetCachedData(string symbol)
        {
            lock (_cacheLock)
            {
                if (_dataCache.ContainsKey(symbol))
                {
                    return _dataCache[symbol].ToList();
                }
                return new List<StockData>();
            }
        }

        /// <summary>
        /// 获取缓存的信号数据
        /// </summary>
        public List<SignalData> GetCachedSignals(string symbol)
        {
            lock (_cacheLock)
            {
                if (_signalCache.ContainsKey(symbol))
                {
                    return _signalCache[symbol].ToList();
                }
                return new List<SignalData>();
            }
        }

        #endregion

        #region Advanced Features

        /// <summary>
        /// 获取高置信度信号
        /// </summary>
        public async Task<List<SignalData>> GetHighConfidenceSignalsAsync(decimal minConfidence = 0.7m, DateTime fromDate = default, DateTime toDate = default)
        {
            if (fromDate == default) fromDate = DateTime.Now.AddDays(-30);
            if (toDate == default) toDate = DateTime.Now;

            var allSignals = await GetAllSignalsAsync(fromDate, toDate);
            return allSignals
                .Where(s => s.Confidence >= minConfidence)
                .OrderByDescending(s => s.Confidence)
                .ToList();
        }

        /// <summary>
        /// 获取信号统计信息
        /// </summary>
        public async Task<Dictionary<string, int>> GetSignalStatisticsAsync(DateTime fromDate, DateTime toDate)
        {
            var signals = await GetAllSignalsAsync(fromDate, toDate);
            var stats = new Dictionary<string, int>();

            foreach (var signal in signals)
            {
                var key = $"{signal.Symbol}-{signal.Type}";
                if (stats.ContainsKey(key))
                {
                    stats[key]++;
                }
                else
                {
                    stats[key] = 1;
                }
            }

            return stats;
        }

        #endregion
    }
}