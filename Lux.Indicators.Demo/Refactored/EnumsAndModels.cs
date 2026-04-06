using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators;
using Lux.Indicators.DivergenceDetectors;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.Models;
using Lux.Indicators.TrendIndicators;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 交易信号类型
    /// </summary>
    public enum TradingSignal
    {
        None,
        Buy,
        Sell
    }

    /// <summary>
    /// 技术指标结果包装类
    /// </summary>
    public class IndicatorResult
    {
        public MacdOutput Macd { get; set; }
        public KdjOutput Kdj { get; set; }
        public MovingAverageOutput Ma { get; set; }
        public decimal Rsi { get; set; }
    }

    /// <summary>
    /// 交易执行结果
    /// </summary>
    public class TradeExecutionResult
    {
        public bool Success { get; set; }
        public TradeRecord TradeRecord { get; set; }
    }
}