using System;

namespace Lux.Indicators.Options
{
    /// <summary>
    /// MACD指标配置选项
    /// </summary>
    public class MacdOptions
    {
        /// <summary>
        /// 快速EMA周期，默认12
        /// </summary>
        public int FastPeriod { get; set; } = 12;
        
        /// <summary>
        /// 慢速EMA周期，默认26
        /// </summary>
        public int SlowPeriod { get; set; } = 26;
        
        /// <summary>
        /// 信号线周期，默认9
        /// </summary>
        public int SignalPeriod { get; set; } = 9;
        
        /// <summary>
        /// 验证配置参数
        /// </summary>
        public void Validate()
        {
            if (FastPeriod <= 0)
                throw new ArgumentException("FastPeriod must be greater than 0", nameof(FastPeriod));
            if (SlowPeriod <= 0)
                throw new ArgumentException("SlowPeriod must be greater than 0", nameof(SlowPeriod));
            if (SignalPeriod <= 0)
                throw new ArgumentException("SignalPeriod must be greater than 0", nameof(SignalPeriod));
            if (FastPeriod >= SlowPeriod)
                throw new ArgumentException("FastPeriod must be less than SlowPeriod");
        }
    }
    
    /// <summary>
    /// KDJ指标配置选项
    /// </summary>
    public class KdjOptions
    {
        /// <summary>
        /// RSV周期，默认9
        /// </summary>
        public int RsvPeriod { get; set; } = 9;
        
        /// <summary>
        /// K值平滑周期，默认3
        /// </summary>
        public int KPeriod { get; set; } = 3;
        
        /// <summary>
        /// D值平滑周期，默认3
        /// </summary>
        public int DPeriod { get; set; } = 3;
        
        /// <summary>
        /// 验证配置参数
        /// </summary>
        public void Validate()
        {
            if (RsvPeriod <= 0)
                throw new ArgumentException("RsvPeriod must be greater than 0", nameof(RsvPeriod));
            if (KPeriod <= 0)
                throw new ArgumentException("KPeriod must be greater than 0", nameof(KPeriod));
            if (DPeriod <= 0)
                throw new ArgumentException("DPeriod must be greater than 0", nameof(DPeriod));
        }
    }
    
    /// <summary>
    /// 布林带指标配置选项
    /// </summary>
    public class BollingerBandsOptions
    {
        /// <summary>
        /// 周期，默认20
        /// </summary>
        public int Period { get; set; } = 20;
        
        /// <summary>
        /// 标准差倍数，默认2
        /// </summary>
        public decimal StdDevMultiplier { get; set; } = 2m;
        
        /// <summary>
        /// 验证配置参数
        /// </summary>
        public void Validate()
        {
            if (Period <= 0)
                throw new ArgumentException("Period must be greater than 0", nameof(Period));
            if (StdDevMultiplier <= 0)
                throw new ArgumentException("StdDevMultiplier must be greater than 0", nameof(StdDevMultiplier));
        }
    }
    
    /// <summary>
    /// 移动平均线指标配置选项
    /// </summary>
    public class MovingAverageOptions
    {
        /// <summary>
        /// 短期均线周期，默认5
        /// </summary>
        public int ShortPeriod { get; set; } = 5;
        
        /// <summary>
        /// 长期均线周期，默认20
        /// </summary>
        public int LongPeriod { get; set; } = 20;
        
        /// <summary>
        /// 验证配置参数
        /// </summary>
        public void Validate()
        {
            if (ShortPeriod <= 0)
                throw new ArgumentException("ShortPeriod must be greater than 0", nameof(ShortPeriod));
            if (LongPeriod <= 0)
                throw new ArgumentException("LongPeriod must be greater than 0", nameof(LongPeriod));
            if (ShortPeriod >= LongPeriod)
                throw new ArgumentException("ShortPeriod must be less than LongPeriod");
        }
    }
}