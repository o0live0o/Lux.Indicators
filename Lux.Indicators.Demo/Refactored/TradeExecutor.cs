using System;
using System.Collections.Generic;
using Lux.Indicators.Models;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.TrendIndicators;
using Lux.Indicators.Demo.Interfaces;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 交易执行器 - 负责执行买入和卖出操作
    /// </summary>
    public class TradeExecutor : ITradeExecutor
    {
        private readonly PositionManager _positionManager;
        private readonly List<TradeRecord> _tradeRecords;
        private readonly ITradingStrategy _strategy;
        private readonly IPositionManagement _positionManagement;

        public TradeExecutor(PositionManager positionManager, List<TradeRecord> tradeRecords,
            ITradingStrategy strategy, IPositionManagement positionManagement)
        {
            _positionManager = positionManager ?? throw new ArgumentNullException(nameof(positionManager));
            _tradeRecords = tradeRecords ?? throw new ArgumentNullException(nameof(tradeRecords));
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _positionManagement = positionManagement ?? throw new ArgumentNullException(nameof(positionManagement));
        }

        public TradeExecutionResult ExecuteTrade(StockData data, IndicatorResult indicators, 
            TradingSignal signal, string stockCode, ref decimal currentBalance)
        {
            switch (signal)
            {
                case TradingSignal.Buy:
                    return ExecuteBuy(data, indicators, stockCode, ref currentBalance);
                case TradingSignal.Sell:
                    return ExecuteSell(data, indicators, stockCode, ref currentBalance);
                default:
                    return new TradeExecutionResult { Success = false };
            }
        }

        private TradeExecutionResult ExecuteBuy(StockData data, IndicatorResult indicators, 
            string stockCode, ref decimal currentBalance)
        {
            // 计算买入仓位大小
            decimal sharesToBuy = _positionManagement.CalculateBuyPosition(
                currentBalance, data, indicators.Macd, indicators.Kdj, indicators.Ma, indicators.Rsi);

            if (sharesToBuy > 0)
            {
                decimal cost = sharesToBuy * data.Close;
                if (cost <= currentBalance)
                {
                    currentBalance -= cost;
                    _positionManager.UpdatePosition(stockCode, sharesToBuy, data.Close);

                    var signalDetails = _strategy.AnalyzeBuySignal(0, data, indicators.Macd, 
                        indicators.Kdj, indicators.Ma, indicators.Rsi);

                    var tradeRecord = new TradeRecord
                    {
                        Action = TradeAction.Buy,
                        StockCode = stockCode,
                        DateTime = data.Date,
                        Price = data.Close,
                        Shares = sharesToBuy,
                        Amount = cost,
                        BalanceAfter = currentBalance,
                        SignalDetails = signalDetails,
                        MacdDif = indicators.Macd.Dif,
                        MacdDea = indicators.Macd.Dea,
                        MacdHistogram = indicators.Macd.Histogram,
                        KdjK = indicators.Kdj.K,
                        KdjD = indicators.Kdj.D,
                        KdjJ = indicators.Kdj.J,
                        Rsi = indicators.Rsi,
                        MaShort = indicators.Ma.ShortMa,
                        MaLong = indicators.Ma.LongMa
                    };

                    return new TradeExecutionResult { Success = true, TradeRecord = tradeRecord };
                }
            }

            return new TradeExecutionResult { Success = false };
        }

        private TradeExecutionResult ExecuteSell(StockData data, IndicatorResult indicators, 
            string stockCode, ref decimal currentBalance)
        {
            if (!_positionManager.HasPosition(stockCode))
                return new TradeExecutionResult { Success = false };

            var position = _positionManager.GetPosition(stockCode);
            decimal sharesToSell = _positionManagement.CalculateSellPosition(
                position, data, indicators.Macd, indicators.Kdj, indicators.Ma, indicators.Rsi);

            if (sharesToSell > 0 && sharesToSell <= position.Shares)
            {
                decimal revenue = sharesToSell * data.Close;
                currentBalance += revenue;
                _positionManager.ReducePosition(stockCode, sharesToSell);

                var signalDetails = _strategy.AnalyzeSellSignal(0, data, indicators.Macd, 
                    indicators.Kdj, indicators.Ma, indicators.Rsi);

                var tradeRecord = new TradeRecord
                {
                    Action = TradeAction.Sell,
                    StockCode = stockCode,
                    DateTime = data.Date,
                    Price = data.Close,
                    Shares = sharesToSell,
                    Amount = revenue,
                    BalanceAfter = currentBalance,
                    SignalDetails = signalDetails,
                    MacdDif = indicators.Macd.Dif,
                    MacdDea = indicators.Macd.Dea,
                    MacdHistogram = indicators.Macd.Histogram,
                    KdjK = indicators.Kdj.K,
                    KdjD = indicators.Kdj.D,
                    KdjJ = indicators.Kdj.J,
                    Rsi = indicators.Rsi,
                    MaShort = indicators.Ma.ShortMa,
                    MaLong = indicators.Ma.LongMa
                };

                return new TradeExecutionResult { Success = true, TradeRecord = tradeRecord };
            }

            return new TradeExecutionResult { Success = false };
        }
    }
}