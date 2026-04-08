using System;

namespace Lux.Indicators;

/// <summary>
/// MACD指标分析结果
/// </summary>
public class MacdResult
{
    public DateTime Date { get; set; }

    /// <summary>
    /// DIF线 (快速EMA - 慢速EMA)
    /// </summary>
    public double Dif { get; set; }

    /// <summary>
    /// DEA线 (DIF的平滑移动平均)
    /// </summary>
    public double Dea { get; set; }

    /// <summary>
    /// MACD柱状图 (2 * (DIF - DEA))
    /// </summary>
    public double Histogram { get; set; }

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