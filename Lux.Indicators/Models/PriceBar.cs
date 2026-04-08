namespace Lux.Indicators;

public class PriceBar
{
    /// <summary>
    /// 交易日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 开盘价
    /// </summary>
    public double Open { get; set; }

    /// <summary>
    /// 最高价
    /// </summary>
    public double High { get; set; }

    /// <summary>
    /// 最低价
    /// </summary>
    public double Low { get; set; }

    /// <summary>
    /// 收盘价
    /// </summary>
    public double Close { get; set; }

    /// <summary>
    /// 成交量
    /// </summary>
    public double Volume { get; set; }
}