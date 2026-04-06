using System;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 交易决策
    /// </summary>
    public class TradingDecision
    {
        public TradeAction Action { get; set; }
        public string Reason { get; set; } = string.Empty;
        public decimal Confidence { get; set; } = 0;
    }
}