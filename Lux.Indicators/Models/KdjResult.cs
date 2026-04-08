using System;

namespace Lux.Indicators;

/// <summary>
/// KDJ指标分析结果
/// </summary>
public class KdjResult
{
    public DateTime Date { get; set; }

    /// <summary>
    /// K值
    /// </summary>
    public double K { get; set; }

    /// <summary>
    /// D值
    /// </summary>
    public double D { get; set; }

    /// <summary>
    /// J值
    /// </summary>
    public double J { get; set; }
}

/// <summary>
/// KDJ信号类型
/// </summary>
public enum KdjSignalType
{
    /// <summary>
    /// 无信号
    /// </summary>
    None,

    /// <summary>
    /// 超卖区买入信号
    /// </summary>
    OversoldBuy,

    /// <summary>
    /// 超买区卖出信号
    /// </summary>
    OverboughtSell,

    /// <summary>
    /// K线上穿D线 (金叉)
    /// </summary>
    GoldenCross,

    /// <summary>
    /// K线下穿D线 (死叉)
    /// </summary>
    DeathCross
}