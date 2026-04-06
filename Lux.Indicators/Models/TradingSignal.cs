using System;

namespace Lux.Indicators.Models
{
    /// <summary>
    /// 交易信号类型
    /// </summary>
    public enum TradingSignal
    {
        None,   // 无信号
        Buy,    // 买入信号
        Sell    // 卖出信号
    }
    
    /// <summary>
    /// 带原因的交易信号
    /// </summary>
    public class SignalWithReason
    {
        public TradingSignal Signal { get; set; }
        public string Reason { get; set; }  // 信号产生的原因
        public List<string> ContributingFactors { get; set; } = new List<string>(); // 影响因素
        public decimal Confidence { get; set; } // 信号置信度
    }
}