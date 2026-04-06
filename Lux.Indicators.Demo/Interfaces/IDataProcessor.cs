using Lux.Indicators.Models;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.TrendIndicators;

namespace Lux.Indicators.Demo.Interfaces
{
    /// <summary>
    /// 数据处理器接口
    /// </summary>
    public interface IDataProcessor
    {
        IndicatorResult ProcessData(StockData data);
        
        /// <summary>
        /// 处理特定股票的数据
        /// </summary>
        IndicatorResult ProcessData(StockData data, string stockCode);
    }
}