using System;
using System.Collections.Generic;
using Lux.Indicators;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.Models;
using Lux.Indicators.TrendIndicators;

namespace Lux.Indicators.Demo.Management
{
    /// <summary>
    /// 日内仓位管理 - 专为日内交易设计的仓位管理策略
    /// 控制单次交易资金比例、止损止盈等
    /// </summary>
    public class IntradayPositionManagement : BasePositionManagement
    {
        private readonly decimal _maxPositionSizePercent; // 最大仓位比例
        private readonly decimal _stopLossPercent;       // 止损百分比
        private readonly decimal _takeProfitPercent;     // 止盈百分比

        public override string Name => "日内仓位管理";

        public IntradayPositionManagement(decimal maxPositionSizePercent = 0.2m, 
                                         decimal stopLossPercent = 0.03m, 
                                         decimal takeProfitPercent = 0.05m)
        {
            _maxPositionSizePercent = maxPositionSizePercent;
            _stopLossPercent = stopLossPercent;
            _takeProfitPercent = takeProfitPercent;
        }

        public override decimal CalculateBuyPosition(decimal availableBalance, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            // 计算最大可购买资金量
            decimal maxInvestment = availableBalance * _maxPositionSizePercent;
            
            // 根据股价计算可购买的股数
            decimal sharesToBuy = maxInvestment / data.Close;
            
            // 确保股数至少为10股（增加单次交易规模）
            if (sharesToBuy < 10)
            {
                sharesToBuy = Math.Min(10, availableBalance / data.Close); // 至少购买10股，但不超过可用资金
            }
            
            // 检查可用余额是否足够
            decimal totalCost = sharesToBuy * data.Close;
            if (totalCost > availableBalance)
            {
                sharesToBuy = availableBalance / data.Close;
            }
            
            return Math.Floor(sharesToBuy); // 返回整数股数
        }

        public override decimal CalculateSellPosition(PositionInfo position, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            if (position.Shares <= 0)
                return 0;

            decimal priceChangePercent = (data.Close - position.AvgBuyPrice) / position.AvgBuyPrice;

            // 触发止损或止盈
            if (priceChangePercent <= -_stopLossPercent || priceChangePercent >= _takeProfitPercent)
            {
                return position.Shares; // 卖出全部持仓
            }

            // 检查是否满足其他卖出条件
            bool shouldSell = ma.ShortMa < ma.LongMa && // 均线下穿
                             macd.Histogram < 0 && macd.Dif < macd.Dea; // MACD死叉

            if (shouldSell)
            {
                return position.Shares; // 卖出全部持仓
            }

            return 0; // 不卖出
        }

        public override bool AllowNewPosition(decimal totalValue, decimal currentValue)
        {
            // 总是允许新仓位，因为我们刚开始时没有持仓
            // 或者如果当前持仓价值占总资产比例小于100%
            return true; // 简单起见，总是允许新开仓
        }

        public override string AnalyzeBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            var signals = new List<string>();

            if (ma.ShortMa > ma.LongMa)
                signals.Add("短期均线上穿长期均线");
            
            if (macd.Histogram > 0 && macd.Dif > macd.Dea)
                signals.Add("MACD金叉");
            
            if (rsi < 70)
                signals.Add("RSI未超买");
            
            if (kdj.K > kdj.D)
                signals.Add("KDJ金叉");
            
            if (kdj.K <= 80 && kdj.D <= 80)
                signals.Add("KDJ未超买");

            return signals.Count > 0 ? string.Join(" + ", signals) : "无明显买入信号";
        }

        public override string AnalyzeSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            var signals = new List<string>();

            if (ma.ShortMa < ma.LongMa)
                signals.Add("短期均线下穿长期均线");
            
            if (macd.Histogram < 0 && macd.Dif < macd.Dea)
                signals.Add("MACD死叉");

            return signals.Count > 0 ? string.Join(" + ", signals) : "无明显卖出信号";
        }
    }
}