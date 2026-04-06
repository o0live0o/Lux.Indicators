using System;
using System.Collections.Generic;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 交易动作枚举
    /// </summary>
    public enum TradeAction
    {
        Buy,
        Sell
    }

    /// <summary>
    /// 交易记录
    /// </summary>
    public class TradeRecord
    {
        public TradeAction Action { get; set; }
        public string StockCode { get; set; } = string.Empty; // 初始化为空字符串避免null警告
        public DateTime DateTime { get; set; }
        public decimal Price { get; set; }
        public decimal Shares { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string SignalDetails { get; set; } = string.Empty; // 初始化为空字符串避免null警告
        
        // 技术指标值
        public decimal MacdDif { get; set; }
        public decimal MacdDea { get; set; }
        public decimal MacdHistogram { get; set; }
        public decimal KdjK { get; set; }
        public decimal KdjD { get; set; }
        public decimal KdjJ { get; set; }
        public decimal Rsi { get; set; }
        public decimal MaShort { get; set; }
        public decimal MaLong { get; set; }
    }

    /// <summary>
    /// 模拟结果
    /// </summary>
    public class SimulationResult
    {
        public decimal InitialBalance { get; set; }
        public decimal FinalBalance { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitPercentage { get; set; }
        public int TotalTrades { get; set; }
        public List<TradeRecord> Trades { get; set; } = new List<TradeRecord>();
    }
}