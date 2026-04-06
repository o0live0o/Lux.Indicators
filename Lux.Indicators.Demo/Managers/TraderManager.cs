using System;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lux.Indicators.Models;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.TrendIndicators;
using Lux.Indicators.Demo.Interfaces;
using Lux.Indicators.Demo.Providers;
using Lux.Indicators.Demo.Managers;
using Lux.Indicators.Demo.Signals;
using Lux.Indicators.Demo.Aggregation;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 股票数据事件参数
    /// </summary>
    public class StockDataEventArgs : EventArgs
    {
        public StockData Data { get; }
        public string StockCode { get; }
        public MacdOutput Macd { get; }
        public KdjOutput Kdj { get; }
        public MovingAverageOutput Ma { get; }
        public decimal Rsi { get; }

        public StockDataEventArgs(StockData data, string stockCode, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            Data = data;
            StockCode = stockCode;
            Macd = macd;
            Kdj = kdj;
            Ma = ma;
            Rsi = rsi;
        }
    }

    /// <summary>
    /// 股票数据发布器 - 模拟实时数据流
    /// </summary>
    public class StockDataPublisher
    {
        // 使用线程安全的字典存储订阅者
        private readonly ConcurrentDictionary<string, ISubscriber> _subscribers = new ConcurrentDictionary<string, ISubscriber>();
        
        /// <summary>
        /// 订阅事件
        /// </summary>
        public event EventHandler<StockDataEventArgs> OnDataReceived;

        /// <summary>
        /// 添加订阅者
        /// </summary>
        public void Subscribe(string subscriberId, ISubscriber subscriber)
        {
            _subscribers.TryAdd(subscriberId, subscriber);
        }

        /// <summary>
        /// 移除订阅者
        /// </summary>
        public void Unsubscribe(string subscriberId)
        {
            _subscribers.TryRemove(subscriberId, out _);
        }

        /// <summary>
        /// 发布数据到所有订阅者
        /// </summary>
        public void PublishData(StockData data, string stockCode, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            var eventArgs = new StockDataEventArgs(data, stockCode, macd, kdj, ma, rsi);
            
            // 触发事件，通知所有订阅者
            OnDataReceived?.Invoke(this, eventArgs);

            // 同时直接调用订阅者的处理方法（如果需要）
            Parallel.ForEach(_subscribers.Values, subscriber =>
            {
                try
                {
                    subscriber.ProcessData(eventArgs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理数据时发生错误: {ex.Message}");
                }
            });
        }
    }

    /// <summary>
    /// 订阅者接口
    /// </summary>
    public interface ISubscriber
    {
        void ProcessData(StockDataEventArgs eventArgs);
    }

    /// <summary>
    /// 交易员管理系统 - 管理多个交易员的生命周期和数据分发
    /// </summary>
    public class TraderManager
    {
        private readonly Dictionary<string, ITrader> _traders;
        private readonly StockDataPublisher _publisher;
        private readonly IntelligentDataCenter _dataCenter;
        private readonly IDataProvider _dataProvider;
        private readonly object _lock = new object();

        public TraderManager(IDataProvider dataProvider = null, AggregationManager aggregationManager = null)
        {
            _traders = new Dictionary<string, ITrader>();
            _publisher = new StockDataPublisher();
            _dataProvider = dataProvider ?? new FileDataProvider();
            _dataCenter = new IntelligentDataCenter(aggregationManager);
        }

        /// <summary>
        /// 添加交易员
        /// </summary>
        public void AddTrader(string traderId, ITrader trader)
        {
            _traders.TryAdd(traderId, trader);
            _publisher.Subscribe(traderId, new TraderAdapter(trader));
        }

        /// <summary>
        /// 移除交易员
        /// </summary>
        public bool RemoveTrader(string traderId)
        {
            lock(_lock)
            {
                if (_traders.ContainsKey(traderId))
                {
                    _traders.Remove(traderId);
                    _publisher.Unsubscribe(traderId);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 向所有交易员发送数据
        /// </summary>
        public void SendDataToTraders(StockData data, string stockCode, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            _publisher.PublishData(data, stockCode, macd, kdj, ma, rsi);
        }
        
        /// <summary>
        /// 添加交易员对特定股票的订阅
        /// </summary>
        public void SubscribeToSymbol(string traderId, string symbol)
        {
            if (_traders.ContainsKey(traderId))
            {
                var trader = _traders[traderId];
                if (trader is BaseTrader baseTrader)
                {
                    baseTrader.SubscribeToSymbol(symbol);
                }
            }
        }
        
        /// <summary>
        /// 批量添加交易员对多个股票的订阅
        /// </summary>
        public void SubscribeToSymbols(string traderId, params string[] symbols)
        {
            foreach (var symbol in symbols)
            {
                SubscribeToSymbol(traderId, symbol);
            }
        }
        
        /// <summary>
        /// 获取所有被订阅的股票代码
        /// </summary>
        public string[] GetSubscribedSymbols()
        {
            var subscribedSymbols = new HashSet<string>();
            foreach (var trader in _traders.Values)
            {
                if (trader is BaseTrader baseTrader)
                {
                    var symbols = baseTrader.GetTrackedSymbols();
                    foreach (var symbol in symbols)
                    {
                        subscribedSymbols.Add(symbol);
                    }
                }
            }
            return subscribedSymbols.ToArray();
        }
        
        /// <summary>
        /// 获取所有交易员关注的股票代码（包括持仓和测算的）
        /// </summary>
        public string[] GetInterestedSymbols()
        {
            var interestedSymbols = new HashSet<string>();
            foreach (var trader in _traders.Values)
            {
                if (trader is BaseTrader baseTrader)
                {
                    var symbols = baseTrader.GetInterestedSymbols();
                    foreach (var symbol in symbols)
                    {
                        interestedSymbols.Add(symbol);
                    }
                }
            }
            return interestedSymbols.ToArray();
        }
        
        /// <summary>
        /// 获取交易员对特定股票的兴趣状态
        /// </summary>
        public bool IsTraderInterestedInSymbol(string traderId, string symbol)
        {
            if (_traders.TryGetValue(traderId, out var trader) && trader is BaseTrader baseTrader)
            {
                return baseTrader.IsInterestedInSymbol(symbol);
            }
            return false;
        }

        /// <summary>
        /// 从数据源获取数据并发送给交易员
        /// </summary>
        public async Task ProcessDataFromSourceAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            var stockDataList = await _dataCenter.GetStockDataAsync(symbol, startDate, endDate);
            
            foreach (var stockData in stockDataList)
            {
                var indicators = await _dataCenter.GetIndicatorsAsync(symbol, new List<StockData> { stockData });
                SendDataToTraders(stockData, symbol, indicators.macd, indicators.kdj, indicators.ma, indicators.rsi);
            }
        }

        /// <summary>
        /// 获取实时数据并发送给交易员
        /// </summary>
        public async Task ProcessRealTimeDataAsync(string symbol)
        {
            var realTimeData = await _dataCenter.GetRealTimeDataAsync(symbol);
            
            // 计算技术指标
            var indicators = await _dataCenter.GetIndicatorsAsync(symbol, new List<StockData> { realTimeData });
            
            // 发送给所有交易员
            SendDataToTraders(realTimeData, symbol, indicators.macd, indicators.kdj, indicators.ma, indicators.rsi);
        }

        /// <summary>
        /// 获取交易员信息
        /// </summary>
        public ITrader GetTrader(string traderId)
        {
            _traders.TryGetValue(traderId, out var trader);
            return trader;
        }

        /// <summary>
        /// 获取所有交易员
        /// </summary>
        public ITrader[] GetAllTraders()
        {
            return _traders.Values.ToArray();
        }
    }

    /// <summary>
    /// 交易员适配器 - 将ITrader适配到ISubscriber接口
    /// </summary>
    internal class TraderAdapter : ISubscriber
    {
        private readonly ITrader _trader;

        public TraderAdapter(ITrader trader)
        {
            _trader = trader;
        }

        public void ProcessData(StockDataEventArgs eventArgs)
        {
            // 将事件数据转换为交易员可以处理的格式
            ((BaseTrader)_trader).ProcessDataPoint(
                eventArgs.Data, 
                eventArgs.Macd, 
                eventArgs.Kdj, 
                eventArgs.Ma, 
                eventArgs.Rsi, 
                eventArgs.StockCode
            );
        }
    }
}