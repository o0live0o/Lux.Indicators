using System;

namespace Lux.Indicators;

/// <summary>
/// 布林带指标分析结果
/// </summary>
public class BollingerBandsResult
{
    public DateTime Date { get; set; }

    /// <summary>
    /// 中轨线 (MA)
    /// </summary>
    public decimal MiddleBand { get; set; }

    /// <summary>
    /// 上轨线 (MB + k * 标准差)
    /// </summary>
    public decimal UpperBand { get; set; }

    /// <summary>
    /// 下轨线 (MB - k * 标准差)
    /// </summary>
    public decimal LowerBand { get; set; }

    /// <summary>
    /// 布林带宽度
    /// </summary>
    public decimal BandWidth { get; set; }
}

/// <summary>
/// 布林带信号类型
/// </summary>
public enum BollingerBandsSignalType
{
    /// <summary>
    /// 无信号
    /// </summary>
    None,

    /// <summary>
    /// 股价触及上轨线
    /// </summary>
    TouchUpperBand,

    /// <summary>
    /// 股价触及下轨线
    /// </summary>
    TouchLowerBand,

    /// <summary>
    /// 股价突破上轨线
    /// </summary>
    BreakUpperBand,

    /// <summary>
    /// 股价跌破下轨线
    /// </summary>
    BreakLowerBand,

    /// <summary>
    /// 布林带收窄
    /// </summary>
    BandSqueeze
}