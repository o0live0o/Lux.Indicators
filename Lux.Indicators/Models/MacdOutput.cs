using System;

namespace Lux.Indicators.Models
{
    /// <summary>
    /// MACD指标分析结果
    /// </summary>
    public class MacdOutput
    {
        /// <summary>
        /// DIF线 (快速EMA - 慢速EMA)
        /// </summary>
        public decimal Dif { get; set; }
        
        /// <summary>
        /// DEA线 (DIF的平滑移动平均)
        /// </summary>
        public decimal Dea { get; set; }
        
        /// <summary>
        /// MACD柱状图 (2 * (DIF - DEA))
        /// </summary>
        public decimal Histogram { get; set; }
        
        /// <summary>
        /// 信号类型
        /// </summary>
        public MacdSignalType Signal { get; set; }
    }
    
    /// <summary>
    /// MACD信号类型
    /// </summary>
    public enum MacdSignalType
    {
        /// <summary>
        /// 无信号
        /// </summary>
        None,
        
        /// <summary>
        /// 金叉 (DIF上穿DEA)
        /// </summary>
        GoldenCross,
        
        /// <summary>
        /// 死叉 (DIF下穿DEA)
        /// </summary>
        DeathCross
    }
}