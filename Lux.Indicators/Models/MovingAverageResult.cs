using System;

namespace Lux.Indicators;

/// <summary>
/// 移动平均线指标分析结果
/// </summary>
public class MovingAverageResult
{
    public DateTime Date { get; set; }

    /// <summary>
    /// 短期移动平均线值
    /// </summary>
    public decimal ShortMa { get; set; }

    /// <summary>
    /// 长期移动平均线值
    /// </summary>
    public decimal LongMa { get; set; }
}

/// <summary>
/// 移动平均线信号类型
/// </summary>
public enum MovingAverageSignalType
{
    /// <summary>
    /// 无信号
    /// </summary>
    None,

    /// <summary>
    /// 短均线上穿长均线 (金叉)
    /// </summary>
    GoldenCross,

    /// <summary>
    /// 短均线下穿长均线 (死叉)
    /// </summary>
    DeathCross,

    /// <summary>
    /// 多头排列 (短均线 > 长均线)
    /// </summary>
    Bullish,

    /// <summary>
    /// 空头排列 (短均线 < 长均线)
    /// </summary>
    Bearish
}