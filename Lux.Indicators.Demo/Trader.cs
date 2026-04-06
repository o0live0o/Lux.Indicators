using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators;
using Lux.Indicators.Models;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.TrendIndicators;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 交易者/投资组合接口
    /// </summary>
    public interface ITrader
    {
        string Name { get; }
        decimal Balance { get; }
        decimal TotalValue { get; }
        List<TradeRecord> Trades { get; }
        Dictionary<string, PositionInfo> Positions { get; }
        
        /// <summary>
        /// 处理单个数据点
        /// </summary>
        void ProcessDataPoint(StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, string stockCode = "UNKNOWN");
        
        /// <summary>
        /// 获取当前持仓价值
        /// </summary>
        decimal GetCurrentPositionValue(List<StockData> dataList);
    }

    /// <summary>
    /// 交易者基类
    /// </summary>
    public abstract class BaseTrader : ITrader
    {
        protected decimal _balance;
        protected Dictionary<string, PositionInfo> _positions;
        protected List<TradeRecord> _trades;
        protected ITradingStrategy _strategy;
        protected IPositionManagement _positionManagement;
        protected List<StockData> _historicalData;
        private readonly HashSet<string> _subscribedSymbols; // 订阅的股票代码集合

        public string Name { get; protected set; }
        public decimal Balance => _balance;
        public List<TradeRecord> Trades => _trades;
        public Dictionary<string, PositionInfo> Positions => _positions;

        public decimal TotalValue => _balance + GetCurrentPositionValue(_historicalData);

        protected BaseTrader(string name, decimal initialBalance, ITradingStrategy strategy, IPositionManagement positionManagement)
        {
            Name = name;
            _balance = initialBalance;
            _positions = new Dictionary<string, PositionInfo>();
            _trades = new List<TradeRecord>();
            _strategy = strategy;
            _positionManagement = positionManagement;
            _historicalData = new List<StockData>();
            _subscribedSymbols = new HashSet<string>();
        }
        
        /// <summary>
        /// 订阅特定股票
        /// </summary>
        public void SubscribeToSymbol(string symbol)
        {
            _subscribedSymbols.Add(symbol);
        }
        
        /// <summary>
        /// 取消订阅特定股票
        /// </summary>
        public void UnsubscribeFromSymbol(string symbol)
        {
            _subscribedSymbols.Remove(symbol);
        }
        
        /// <summary>
        /// 检查是否订阅了特定股票
        /// </summary>
        public bool IsSubscribedToSymbol(string symbol)
        {
            return _subscribedSymbols.Contains(symbol) || _positions.ContainsKey(symbol);
        }
        
        /// <summary>
        /// 获取跟踪的所有股票代码
        /// </summary>
        public string[] GetTrackedSymbols()
        {
            var allSymbols = new HashSet<string>(_subscribedSymbols);
            foreach(var pos in _positions.Keys)
            {
                allSymbols.Add(pos);
            }
            return allSymbols.ToArray();
        }
        
        /// <summary>
        /// 获取交易员关注的股票代码（包括持仓和测算的）
        /// </summary>
        public string[] GetInterestedSymbols()
        {
            var interestedSymbols = new HashSet<string>();
            
            // 已持仓的股票
            foreach(var pos in _positions.Keys)
            {
                interestedSymbols.Add(pos);
            }
            
            // 已订阅的股票
            foreach(var sub in _subscribedSymbols)
            {
                interestedSymbols.Add(sub);
            }
            
            return interestedSymbols.ToArray();
        }
        
        /// <summary>
        /// 检查是否对特定股票有兴趣（持仓或订阅）
        /// </summary>
        public bool IsInterestedInSymbol(string symbol)
        {
            return _positions.ContainsKey(symbol) || _subscribedSymbols.Contains(symbol);
        }

        public abstract void ProcessDataPoint(StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, string stockCode);

        public decimal GetCurrentPositionValue(List<StockData> dataList)
        {
            decimal totalValue = 0;
            foreach (var position in _positions.Values)
            {
                // 因为StockData没有StockCode属性，我们不能直接比较
                // 这里假设我们只处理一种股票或者需要其他方式识别股票
                if (dataList.Any())
                {
                    var latestData = dataList.Last(); // 使用最新的数据点
                    totalValue += position.Shares * latestData.Close;
                }
            }
            return totalValue;
        }

        protected void ExecuteBuy(StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, string stockCode)
        {
            // 使用仓位管理策略计算买入仓位大小
            decimal sharesToBuy = _positionManagement.CalculateBuyPosition(_balance, data, macd, kdj, ma, rsi);

            if (sharesToBuy > 0)
            {
                decimal cost = sharesToBuy * data.Close;
                if (cost <= _balance)
                {
                    _balance -= cost;

                    // 更新持仓信息
                    UpdatePosition(stockCode, sharesToBuy, data.Close);

                    // 记录交易
                    RecordTrade(TradeAction.Buy, data, sharesToBuy, cost, stockCode, macd, kdj, ma, rsi);
                }
            }
        }

        protected void ExecuteSell(StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, string stockCode)
        {
            if (_positions.ContainsKey(stockCode))
            {
                var position = _positions[stockCode];

                // 使用仓位管理策略计算应卖出的仓位大小
                decimal sharesToSell = _positionManagement.CalculateSellPosition(position, data, macd, kdj, ma, rsi);

                if (sharesToSell > 0 && sharesToSell <= position.Shares)
                {
                    decimal revenue = sharesToSell * data.Close;
                    _balance += revenue;

                    // 更新持仓
                    ReducePosition(stockCode, sharesToSell);

                    // 记录交易
                    RecordTrade(TradeAction.Sell, data, sharesToSell, revenue, stockCode, macd, kdj, ma, rsi);
                }
            }
        }

        private void UpdatePosition(string stockCode, decimal shares, decimal price)
        {
            if (_positions.ContainsKey(stockCode))
            {
                var existingPosition = _positions[stockCode];
                decimal totalShares = existingPosition.Shares + shares;
                decimal totalCost = existingPosition.AvgBuyPrice * existingPosition.Shares + price * shares;
                decimal newAvgPrice = totalCost / totalShares;

                _positions[stockCode] = new PositionInfo
                {
                    StockCode = stockCode,
                    Shares = totalShares,
                    AvgBuyPrice = newAvgPrice
                };
            }
            else
            {
                _positions[stockCode] = new PositionInfo
                {
                    StockCode = stockCode,
                    Shares = shares,
                    AvgBuyPrice = price
                };
            }
        }

        private void ReducePosition(string stockCode, decimal sharesToReduce)
        {
            if (_positions.ContainsKey(stockCode))
            {
                var existingPosition = _positions[stockCode];
                decimal newShares = existingPosition.Shares - sharesToReduce;

                if (newShares <= 0)
                {
                    _positions.Remove(stockCode);
                }
                else
                {
                    _positions[stockCode] = new PositionInfo
                    {
                        StockCode = stockCode,
                        Shares = newShares,
                        AvgBuyPrice = existingPosition.AvgBuyPrice
                    };
                }
            }
        }

        private void RecordTrade(TradeAction action, StockData data, decimal shares, decimal amount, string stockCode, 
                                MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            string signalDetails = action == TradeAction.Buy 
                ? _strategy.AnalyzeBuySignal(_historicalData.Count - 1, data, macd, kdj, ma, rsi)
                : _strategy.AnalyzeSellSignal(_historicalData.Count - 1, data, macd, kdj, ma, rsi);

            _trades.Add(new TradeRecord
            {
                Action = action,
                StockCode = stockCode,
                DateTime = data.Date,
                Price = data.Close,
                Shares = shares,
                Amount = amount,
                BalanceAfter = _balance,
                SignalDetails = signalDetails,
                MacdDif = macd.Dif,
                MacdDea = macd.Dea,
                MacdHistogram = macd.Histogram,
                KdjK = kdj.K,
                KdjD = kdj.D,
                KdjJ = kdj.J,
                Rsi = rsi,
                MaShort = ma.ShortMa,
                MaLong = ma.LongMa
            });
        }
    }

    /// <summary>
    /// 主动交易者 - 频繁交易追求短期收益
    /// </summary>
    public class ActiveTrader : BaseTrader
    {
        public ActiveTrader(string name, decimal initialBalance, ITradingStrategy strategy, IPositionManagement positionManagement)
            : base(name, initialBalance, strategy, positionManagement)
        {
        }

        public override void ProcessDataPoint(StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, string stockCode)
        {
            // 只处理订阅的股票
            if (!IsSubscribedToSymbol(stockCode))
            {
                return;
            }

            _historicalData.Add(data);

            // 检查是否满足买入条件
            bool isBuySignal = _strategy.IsBuySignal(_historicalData.Count - 1, data, macd, kdj, ma, rsi, 
                new MockPositionManager(_positions), _balance, _historicalData);

            // 检查是否满足卖出条件
            bool isSellSignal = _strategy.IsSellSignal(_historicalData.Count - 1, data, macd, kdj, ma, rsi, 
                new MockPositionManager(_positions), stockCode);

            if (isBuySignal && _positionManagement.AllowNewPosition(TotalValue, GetCurrentPositionValue(_historicalData)))
            {
                ExecuteBuy(data, macd, kdj, ma, rsi, stockCode);
            }
            else if (isSellSignal)
            {
                ExecuteSell(data, macd, kdj, ma, rsi, stockCode);
            }
        }
    }

    /// <summary>
    /// 保守交易者 - 较少交易，注重风险控制
    /// </summary>
    public class ConservativeTrader : BaseTrader
    {
        public ConservativeTrader(string name, decimal initialBalance, ITradingStrategy strategy, IPositionManagement positionManagement)
            : base(name, initialBalance, strategy, positionManagement)
        {
        }

        public override void ProcessDataPoint(StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, string stockCode)
        {
            // 只处理订阅的股票
            if (!IsSubscribedToSymbol(stockCode))
            {
                return;
            }

            _historicalData.Add(data);

            // 保守策略，对信号要求更高
            bool isStrongBuySignal = _strategy.IsBuySignal(_historicalData.Count - 1, data, macd, kdj, ma, rsi, 
                new MockPositionManager(_positions), _balance, _historicalData) &&
                IsStrongSignal(data, macd, kdj, ma, rsi, true); // 额外的强度验证

            bool isStrongSellSignal = _strategy.IsSellSignal(_historicalData.Count - 1, data, macd, kdj, ma, rsi, 
                new MockPositionManager(_positions), stockCode) &&
                IsStrongSignal(data, macd, kdj, ma, rsi, false); // 额外的强度验证

            if (isStrongBuySignal && _positionManagement.AllowNewPosition(TotalValue, GetCurrentPositionValue(_historicalData)))
            {
                ExecuteBuy(data, macd, kdj, ma, rsi, stockCode);
            }
            else if (isStrongSellSignal)
            {
                ExecuteSell(data, macd, kdj, ma, rsi, stockCode);
            }
        }

        private bool IsStrongSignal(StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, bool isBuy)
        {
            if (isBuy)
            {
                // 对于买入信号，要求更强的技术指标支持
                return macd.Histogram > 0 && macd.Dif > macd.Dea &&  // MACD强势
                       kdj.K > kdj.D && kdj.K < 70 && kdj.D < 70 &&  // KDJ金叉且未严重超买
                       ma.ShortMa > ma.LongMa && data.Close > ma.ShortMa &&  // 均线多头排列且价格在均线上方
                       rsi > 35 && rsi < 65;  // RSI在较安全区间
            }
            else
            {
                // 对于卖出信号，要求更强的卖出信号
                return macd.Histogram < 0 && macd.Dif < macd.Dea &&  // MACD弱势
                       kdj.K < kdj.D && kdj.K > 30 && kdj.D > 30 &&  // KDJ死叉且非超卖
                       ma.ShortMa < ma.LongMa && data.Close < ma.ShortMa;  // 均线空头排列且价格在均线下方
            }
        }
    }

    /// <summary>
    /// 模拟仓位管理器 - 用于策略调用
    /// </summary>
    internal class MockPositionManager : PositionManager
    {
        private readonly Dictionary<string, PositionInfo> _originalPositions;

        public MockPositionManager(Dictionary<string, PositionInfo> positions)
        {
            _originalPositions = positions;
        }

        public new bool HasPosition(string stockCode) => _originalPositions.ContainsKey(stockCode);
        public new PositionInfo GetPosition(string stockCode) => _originalPositions.ContainsKey(stockCode) ? _originalPositions[stockCode] : null;
        public decimal GetTotalPositionValue(List<StockData> dataList) => 0; // 简化实现
    }
}