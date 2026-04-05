using System;

namespace Lux.Indicators.Models
{
    /// <summary>
    /// 股票基础数据实体
    /// </summary>
    public class StockData
    {
        /// <summary>
        /// 交易日期
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// 开盘价
        /// </summary>
        public decimal Open { get; set; }
        
        /// <summary>
        /// 最高价
        /// </summary>
        public decimal High { get; set; }
        
        /// <summary>
        /// 最低价
        /// </summary>
        public decimal Low { get; set; }
        
        /// <summary>
        /// 收盘价
        /// </summary>
        public decimal Close { get; set; }
        
        /// <summary>
        /// 成交量
        /// </summary>
        public decimal Volume { get; set; }
        
        /// <summary>
        /// 计算典型价格 (最高价+最低价+收盘价)/3
        /// </summary>
        public decimal TypicalPrice => (High + Low + Close) / 3;
        
        /// <summary>
        /// 计算中位价格 (最高价+最低价)/2
        /// </summary>
        public decimal MedianPrice => (High + Low) / 2;
    }
}