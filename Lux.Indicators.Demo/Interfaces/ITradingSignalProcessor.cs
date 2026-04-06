using System;
using System.Collections.Generic;
using Lux.Indicators.Models;

namespace Lux.Indicators.Demo.Interfaces
{
    /// <summary>
    /// 交易信号处理器接口
    /// </summary>
    public interface ITradingSignalProcessor
    {
        TradingSignal GetTradingSignal(StockData data, IndicatorResult indicators, 
            PositionManager positionManager, decimal currentBalance, List<StockData> historicalData = null);
        
        /// <summary>
        /// 获取交易信号（指定股票代码）
        /// </summary>
        TradingSignal GetTradingSignal(StockData data, IndicatorResult indicators, 
            PositionManager positionManager, decimal currentBalance, string stockCode, List<StockData> historicalData = null);
    }
}