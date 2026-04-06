using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators;
using Lux.Indicators.Models;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.TrendIndicators;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 仓位管理接口
    /// </summary>
    public interface IPositionManagement
    {
        /// <summary>
        /// 获取策略名称
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// 计算建议买入仓位大小
        /// </summary>
        decimal CalculateBuyPosition(decimal availableBalance, StockData data, MacdOutput macd, 
                                   KdjOutput kdj, MovingAverageOutput ma, decimal rsi);
        
        /// <summary>
        /// 计算建议卖出仓位大小
        /// </summary>
        decimal CalculateSellPosition(PositionInfo position, StockData data, MacdOutput macd, 
                                    KdjOutput kdj, MovingAverageOutput ma, decimal rsi);
        
        /// <summary>
        /// 是否允许新开仓位
        /// </summary>
        bool AllowNewPosition(decimal totalValue, decimal currentValue);
        
        /// <summary>
        /// 分析买入信号
        /// </summary>
        string AnalyzeBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, 
                               MovingAverageOutput ma, decimal rsi);
        
        /// <summary>
        /// 分析卖出信号
        /// </summary>
        string AnalyzeSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, 
                               MovingAverageOutput ma, decimal rsi);
    }

    /// <summary>
    /// 基础仓位管理类
    /// </summary>
    public abstract class BasePositionManagement : IPositionManagement
    {
        public abstract string Name { get; }

        public virtual decimal CalculateBuyPosition(decimal availableBalance, StockData data, MacdOutput macd, 
                                                  KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            // 默认实现：使用固定比例
            return 0;
        }

        public virtual decimal CalculateSellPosition(PositionInfo position, StockData data, MacdOutput macd, 
                                                   KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            // 默认实现：全卖或不卖
            return 0;
        }

        public virtual bool AllowNewPosition(decimal totalValue, decimal currentValue)
        {
            // 默认实现：不超过80%仓位
            return currentValue / totalValue < 0.8m;
        }

        public virtual string AnalyzeBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, 
                                              MovingAverageOutput ma, decimal rsi)
        {
            return "默认买入信号";
        }

        public virtual string AnalyzeSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, 
                                              MovingAverageOutput ma, decimal rsi)
        {
            return "默认卖出信号";
        }
    }

    /// <summary>
    /// 保守仓位管理
    /// </summary>
    public class ConservativePositionManagement : BasePositionManagement
    {
        public override string Name => "保守仓位管理";

        public override decimal CalculateBuyPosition(decimal availableBalance, StockData data, MacdOutput macd, 
                                                   KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            // 保守策略：每次最多投入可用资金的10%
            decimal maxAmount = availableBalance * 0.1m;
            decimal maxShares = Math.Floor(maxAmount / data.Close);
            return maxShares;
        }

        public override decimal CalculateSellPosition(PositionInfo position, StockData data, MacdOutput macd, 
                                                    KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            // 保守策略：达到止盈或止损时全卖
            decimal profitRatio = (data.Close - position.AvgBuyPrice) / position.AvgBuyPrice;
            
            // 10%止盈或7%止损
            if (profitRatio >= 0.10m || profitRatio <= -0.07m)
            {
                return position.Shares; // 全部卖出
            }
            
            return 0; // 不卖出
        }

        public override string AnalyzeBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, 
                                              MovingAverageOutput ma, decimal rsi)
        {
            var conditions = new List<string>();

            if (macd.Signal == MacdSignalType.GoldenCross)
                conditions.Add("MACD金叉");
            if (kdj.K > kdj.D && kdj.K <= 70)
                conditions.Add("KDJ金叉且未超买");
            if (ma.ShortMa > ma.LongMa)
                conditions.Add("均线多头排列");
            if (rsi > 30 && rsi < 70)
                conditions.Add("RSI在合理区间");

            return string.Join(", ", conditions);
        }

        public override string AnalyzeSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, 
                                               MovingAverageOutput ma, decimal rsi)
        {
            var conditions = new List<string>();

            if (macd.Signal == MacdSignalType.DeathCross)
                conditions.Add("MACD死叉");
            if (kdj.K < kdj.D && kdj.K > 30)
                conditions.Add("KDJ死叉且非超卖");
            if (ma.ShortMa < ma.LongMa)
                conditions.Add("均线空头排列");
            if (rsi < 30 || rsi > 70)
                conditions.Add("RSI极端值");

            return string.Join(", ", conditions);
        }
    }

    /// <summary>
    /// 积极仓位管理
    /// </summary>
    public class AggressivePositionManagement : BasePositionManagement
    {
        public override string Name => "积极仓位管理";

        public override decimal CalculateBuyPosition(decimal availableBalance, StockData data, MacdOutput macd, 
                                                   KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            // 积极策略：每次最多投入可用资金的30%
            decimal maxAmount = availableBalance * 0.3m;
            decimal maxShares = Math.Floor(maxAmount / data.Close);
            return maxShares;
        }

        public override decimal CalculateSellPosition(PositionInfo position, StockData data, MacdOutput macd, 
                                                    KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            // 积极策略：信号强时全卖，否则部分卖出
            decimal profitRatio = (data.Close - position.AvgBuyPrice) / position.AvgBuyPrice;
            
            // 15%止盈或5%止损
            if (profitRatio >= 0.15m || profitRatio <= -0.05m)
            {
                return position.Shares; // 全部卖出
            }
            
            // 技术指标恶化时部分卖出
            if (macd.Signal == MacdSignalType.DeathCross || 
                (kdj.K < kdj.D && kdj.K > 70) ||  // 高位死叉
                (rsi > 70 && macd.Dif < macd.Dea)) // 超买且MACD转弱
            {
                return Math.Ceiling(position.Shares * 0.5m); // 卖出一半
            }
            
            return 0; // 不卖出
        }

        public override string AnalyzeBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, 
                                              MovingAverageOutput ma, decimal rsi)
        {
            var conditions = new List<string>();

            if (macd.Signal == MacdSignalType.GoldenCross || (macd.Dif > macd.Dea && macd.Histogram > 0))
                conditions.Add("MACD强势金叉");
            if (kdj.K > kdj.D && kdj.K <= 80 && kdj.D < 80)
                conditions.Add("KDJ金叉且未严重超买");
            if (ma.ShortMa > ma.LongMa && data.Close > ma.ShortMa)
                conditions.Add("价格突破短期均线");
            if (rsi > 25 && rsi < 75)
                conditions.Add("RSI适中");

            return string.Join(", ", conditions);
        }

        public override string AnalyzeSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, 
                                               MovingAverageOutput ma, decimal rsi)
        {
            var conditions = new List<string>();

            if (macd.Signal == MacdSignalType.DeathCross || (macd.Dif < macd.Dea && macd.Histogram < 0))
                conditions.Add("MACD强势死叉");
            if (kdj.K < kdj.D && kdj.K > 20 && kdj.D > 20)
                conditions.Add("KDJ死叉且非超卖");
            if (ma.ShortMa < ma.LongMa && data.Close < ma.ShortMa)
                conditions.Add("价格跌破短期均线");
            if (rsi < 25 || rsi > 75)
                conditions.Add("RSI极端值");

            return string.Join(", ", conditions);
        }
    }

    /// <summary>
    /// 平衡仓位管理
    /// </summary>
    public class BalancedPositionManagement : BasePositionManagement
    {
        public override string Name => "平衡仓位管理";

        public override decimal CalculateBuyPosition(decimal availableBalance, StockData data, MacdOutput macd, 
                                                   KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            // 平衡策略：根据信号强度决定仓位大小
            int signalStrength = 0;
            
            if (macd.Signal == MacdSignalType.GoldenCross) signalStrength++;
            if (kdj.K > kdj.D) signalStrength++;
            if (ma.ShortMa > ma.LongMa) signalStrength++;
            if (rsi > 30 && rsi < 70) signalStrength++;
            
            // 根据信号强度决定买入比例：1-4个信号分别对应5%, 10%, 15%, 20%
            decimal buyRatio = 0.05m * signalStrength;
            decimal maxAmount = availableBalance * buyRatio;
            decimal maxShares = Math.Floor(maxAmount / data.Close);
            
            return maxShares;
        }

        public override decimal CalculateSellPosition(PositionInfo position, StockData data, MacdOutput macd, 
                                                    KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            // 平衡策略：根据信号强度决定卖出比例
            int signalStrength = 0;
            
            if (macd.Signal == MacdSignalType.DeathCross) signalStrength++;
            if (kdj.K < kdj.D) signalStrength++;
            if (ma.ShortMa < ma.LongMa) signalStrength++;
            if (rsi < 30 || rsi > 70) signalStrength++;
            
            // 根据信号强度决定卖出比例：1-4个信号分别对应25%, 50%, 75%, 100%
            if (signalStrength >= 4)
                return position.Shares; // 全卖
            else if (signalStrength >= 3)
                return Math.Ceiling(position.Shares * 0.75m); // 卖出75%
            else if (signalStrength >= 2)
                return Math.Ceiling(position.Shares * 0.5m); // 卖出50%
            else if (signalStrength >= 1)
                return Math.Ceiling(position.Shares * 0.25m); // 卖出25%
            
            return 0; // 不卖出
        }

        public override string AnalyzeBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, 
                                              MovingAverageOutput ma, decimal rsi)
        {
            var conditions = new List<string>();

            if (macd.Signal == MacdSignalType.GoldenCross)
                conditions.Add("MACD金叉");
            if (kdj.K > kdj.D && kdj.K <= 80)
                conditions.Add("KDJ金叉且未超买");
            if (ma.ShortMa > ma.LongMa && data.Close > ma.LongMa)
                conditions.Add("价格在均线上方");
            if (rsi > 30 && rsi < 65)
                conditions.Add("RSI在安全区间");

            return string.Join(", ", conditions);
        }

        public override string AnalyzeSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, 
                                               MovingAverageOutput ma, decimal rsi)
        {
            var conditions = new List<string>();

            if (macd.Signal == MacdSignalType.DeathCross)
                conditions.Add("MACD死叉");
            if (kdj.K < kdj.D && kdj.K > 25)
                conditions.Add("KDJ死叉且非超卖");
            if (ma.ShortMa < ma.LongMa && data.Close < ma.LongMa)
                conditions.Add("价格跌破均线");
            if (rsi < 30 || rsi > 70)
                conditions.Add("RSI极端值");

            return string.Join(", ", conditions);
        }
    }
}