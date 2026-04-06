using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lux.Indicators.Models;

namespace Lux.Indicators.Demo.Aggregation
{
    /// <summary>
    /// 数据聚合源接口
    /// </summary>
    public interface IDataAggregator
    {
        string Name { get; }
        string Description { get; }
        Task<IEnumerable<StockData>> GetStockDataAsync(string symbol, DateTime startDate, DateTime endDate);
        Task<StockData> GetRealTimeDataAsync(string symbol);
    }

    /// <summary>
    /// 信号聚合源接口
    /// </summary>
    public interface ISignalAggregator
    {
        string Name { get; }
        string Description { get; }
        Task<IEnumerable<SignalData>> GetSignalsAsync(DateTime fromDate, DateTime toDate);
    }

    /// <summary>
    /// 信号数据模型
    /// </summary>
    public class SignalData
    {
        public string Symbol { get; set; }
        public SignalType Type { get; set; }
        public decimal Confidence { get; set; } // 置信度 0-1
        public string Source { get; set; } // 信号来源
        public DateTime Timestamp { get; set; }
        public string Details { get; set; } // 详细信息
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>(); // 额外元数据
    }

    /// <summary>
    /// 信号类型
    /// </summary>
    public enum SignalType
    {
        Buy,
        Sell,
        Hold,
        StrongBuy,
        StrongSell
    }
    
    /// <summary>
    /// 聚合结果类型
    /// </summary>
    public enum AggregationResultType
    {
        StockData,
        Signals,
        Analysis,
        News
    }
    
    /// <summary>
    /// 聚合结果
    /// </summary>
    public class AggregationResult
    {
        public AggregationResultType ResultType { get; set; }
        public object Data { get; set; }
        public string Source { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal Confidence { get; set; } = 1.0m;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}