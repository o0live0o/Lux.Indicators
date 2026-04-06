using Lux.Indicators.Models;

namespace Lux.Indicators.Demo.Interfaces
{
    /// <summary>
    /// 交易执行器接口
    /// </summary>
    public interface ITradeExecutor
    {
        TradeExecutionResult ExecuteTrade(StockData data, IndicatorResult indicators, 
            TradingSignal signal, string stockCode, ref decimal currentBalance);
    }
}