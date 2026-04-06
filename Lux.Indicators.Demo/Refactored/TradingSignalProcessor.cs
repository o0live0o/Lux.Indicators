using System;
using System.Collections.Generic;
using Lux.Indicators.Models;
using Lux.Indicators.Demo.Interfaces;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 交易信号处理器 - 负责判断买卖信号
    /// </summary>
    public class TradingSignalProcessor : ITradingSignalProcessor
    {
        private readonly ITradingStrategy _strategy;

        public TradingSignalProcessor(ITradingStrategy strategy)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        public TradingSignal GetTradingSignal(StockData data, IndicatorResult indicators, 
            PositionManager positionManager, decimal currentBalance, List<StockData> historicalData = null)
        {
            // 调用带股票代码的方法，使用默认股票代码
            return GetTradingSignal(data, indicators, positionManager, currentBalance, "UNKNOWN", historicalData);
        }
        
        public TradingSignal GetTradingSignal(StockData data, IndicatorResult indicators, 
            PositionManager positionManager, decimal currentBalance, string stockCode, List<StockData> historicalData = null)
        {
            // 使用提供的历史数据，如果未提供则使用单个数据点
            var dataList = historicalData ?? new List<StockData> { data };
            
            var isBuySignal = _strategy.IsBuySignal(dataList.Count - 1, data, indicators.Macd, indicators.Kdj, 
                indicators.Ma, indicators.Rsi, positionManager, currentBalance, dataList);
            
            var isSellSignal = _strategy.IsSellSignal(dataList.Count - 1, data, indicators.Macd, indicators.Kdj, 
                indicators.Ma, indicators.Rsi, positionManager, stockCode);

            if (isBuySignal)
                return TradingSignal.Buy;
            else if (isSellSignal)
                return TradingSignal.Sell;
            else
                return TradingSignal.None;
        }
    }
}