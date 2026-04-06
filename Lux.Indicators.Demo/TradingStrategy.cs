using System;
using System.Collections.Generic;
using Lux.Indicators;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.Models;
using Lux.Indicators.TrendIndicators;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 交易策略接口
    /// </summary>
    public interface ITradingStrategy
    {
        /// <summary>
        /// 获取策略名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 判断是否为买入信号
        /// </summary>
        bool IsBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, PositionManager positionManager, decimal currentBalance, List<StockData> dataList);

        /// <summary>
        /// 判断是否为卖出信号
        /// </summary>
        bool IsSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, PositionManager positionManager, string stockCode = "UNKNOWN");

        /// <summary>
        /// 分析买入信号详情
        /// </summary>
        string AnalyzeBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi);

        /// <summary>
        /// 分析卖出信号详情
        /// </summary>
        string AnalyzeSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi);
    }

    /// <summary>
    /// 基础交易策略类
    /// </summary>
    public abstract class BaseTradingStrategy : ITradingStrategy
    {
        public abstract string Name { get; }

        public virtual bool IsBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, PositionManager positionManager, decimal currentBalance, List<StockData> dataList)
        {
            // 默认实现，子类可以重写
            return false;
        }

        public virtual bool IsSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, PositionManager positionManager, string stockCode = "UNKNOWN")
        {
            // 默认实现，子类可以重写
            return false;
        }

        public virtual string AnalyzeBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            // 默认实现
            return "默认买入信号";
        }

        public virtual string AnalyzeSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            // 默认实现
            return "默认卖出信号";
        }
    }

    /// <summary>
    /// 短期交易策略
    /// </summary>
    public class ShortTermTradingStrategy : BaseTradingStrategy
    {
        public override string Name => "短期交易策略";

        public override bool IsBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, PositionManager positionManager, decimal currentBalance, List<StockData> dataList)
        {
            // 检查当前总仓位是否已满（不超过80%）
            decimal totalValue = currentBalance + GetTotalPositionValue(positionManager, dataList);
            decimal currentStockValue = GetTotalPositionValue(positionManager, dataList);
            decimal currentPositionRatio = totalValue > 0 ? currentStockValue / totalValue : 0;

            // 允许在不超过80%总仓位的情况下继续买入
            bool canBuyMore = currentPositionRatio < 0.8m;

            // 短线强势买入信号 - 更严格的条件
            bool strongBuySignal = macd.Signal == MacdSignalType.GoldenCross &&     // MACD金叉
                                  kdj.K > kdj.D && kdj.K <= 80 && kdj.D <= 80 &&  // KDJ金叉且未超买
                                  ma.ShortMa > ma.LongMa &&                        // 短期均线上穿长期均线
                                  rsi > 30 && rsi < 65;                           // RSI适中，未超买

            // MACD柱状图增强信号 - 表示动能强劲
            bool macdHistogramGrowing = macd.Histogram > 0 && 
                                       index > 0 && 
                                       macd.Histogram > GetPreviousMacdHistogram(index - 1, dataList.Count);

            // KDJ三线位置优化 - 寻找最佳买入点
            bool kdjOptimalPosition = kdj.K > kdj.D &&     // K线上穿D线
                                     kdj.K < 80 &&        // 未进入超买区
                                     kdj.D < 70 &&        // D线未过高
                                     kdj.K >= kdj.D &&    // K线不低于D线
                                     (kdj.K - kdj.D) > 2; // 金叉后有一定距离，非刚交叉

            // RSI背离或进入合理区域
            bool rsiOptimal = (rsi > 30 && rsi < 65) ||           // RSI在合理区间
                             (rsi >= 65 && rsi < 70 && macd.Dif > macd.Dea); // RSI稍高但MACD仍向好

            // 均线多头排列加强确认
            bool maBullishConfirm = ma.Signal == MovingAverageSignalType.Bullish || 
                                  (ma.ShortMa > ma.LongMa && data.Close > ma.ShortMa); // 价格突破短期均线

            // 组合信号判断
            bool compositeSignal = strongBuySignal && 
                                 kdjOptimalPosition && 
                                 rsiOptimal && 
                                 maBullishConfirm;

            // 动能确认 - MACD柱状图增长
            bool momentumConfirm = macdHistogramGrowing || 
                                 (macd.Dif > macd.Dea && macd.Dif > 0 && macd.Histogram > 0);

            // 超卖反弹机会 - 更谨慎的处理
            bool oversoldOpportunity = rsi < 30 && 
                                     rsi > 15 &&               // 不是极度超卖
                                     macd.Dif > macd.Dea &&    // MACD开始转好
                                     kdj.K > kdj.D &&          // KDJ出现金叉迹象
                                     macd.Histogram > -0.01m;  // MACD柱状图不过度负值

            // 短线精准买点 - 结合多个条件
            bool preciseEntry = compositeSignal && momentumConfirm;

            // 放宽条件但仍保持质量
            bool qualitySignal = preciseEntry || (oversoldOpportunity && canBuyMore);

            return qualitySignal && canBuyMore;
        }

        public override bool IsSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, PositionManager positionManager, string stockCode = "UNKNOWN")
        {
            // 止损和止盈条件需要先确保有持仓和有效的买入价格
            decimal avgBuyPrice = GetAvgBuyPrice(positionManager, stockCode);
            bool stopLoss = false;
            bool takeProfit = false;
            
            if (avgBuyPrice > 0)
            {
                decimal currentProfitRatio = (data.Close - avgBuyPrice) / avgBuyPrice;
                stopLoss = currentProfitRatio < -0.05m; // 5%止损
                takeProfit = currentProfitRatio > 0.10m; // 10%止盈
            }

            // 强势卖出信号：同时满足多项技术指标恶化
            bool strongSellSignal = macd.Signal == MacdSignalType.DeathCross && 
                                   kdj.Signal == KdjSignalType.DeathCross &&
                                   ma.Signal == MovingAverageSignalType.Bearish &&
                                   (rsi < 30 || rsi > 70);

            // 一般卖出信号：至少满足3个条件
            bool macdCondition = macd.Signal == MacdSignalType.DeathCross || 
                                (macd.Dif < macd.Dea && macd.Dif < 0 && macd.Histogram < 0); // 确保柱状图小于0

            bool kdjCondition = kdj.Signal == KdjSignalType.DeathCross || 
                               (kdj.K < kdj.D && kdj.K > 20 && kdj.K < 80); // 在合理区间内的死叉

            bool maCondition = ma.Signal == MovingAverageSignalType.Bearish || 
                              (ma.ShortMa < ma.LongMa && data.Close < ma.LongMa); // 价格跌破长期均线

            bool rsiCondition = rsi < 30 || rsi > 70; // 极端值


            int generalConditions = (macdCondition ? 1 : 0) +
                                  (kdjCondition ? 1 : 0) +
                                  (maCondition ? 1 : 0) +
                                  (rsiCondition ? 1 : 0);

            // 超买回调信号：RSI高于70但其他指标转弱
            bool overboughtPullback = rsi > 70 && 
                                    (macd.Dif < macd.Dea || macd.Signal == MacdSignalType.DeathCross) &&
                                    kdj.K < kdj.D;

            // 返回逻辑，判断是否需要卖出持有的特定股票
            return (stopLoss || takeProfit || strongSellSignal || generalConditions >= 3 || overboughtPullback) && 
                   positionManager.HasPosition(stockCode); // 且当前持有该股票仓位
        }

        public override string AnalyzeBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            var conditions = new List<string>();

            // MACD条件
            if (macd.Signal == MacdSignalType.GoldenCross)
                conditions.Add("MACD金叉");
            else if (macd.Dif > macd.Dea && macd.Dif > 0 && macd.Histogram > 0)
                conditions.Add("DIF>DEA且DIF>0且柱状图>0");

            // KDJ条件
            if (kdj.Signal == KdjSignalType.GoldenCross)
                conditions.Add("KDJ金叉");
            else if (kdj.K > kdj.D && kdj.K <= 80 && kdj.D <= 80)
                conditions.Add("K>D且K在合理区间");

            // 移动平均线条件
            if (ma.Signal == MovingAverageSignalType.Bullish)
                conditions.Add("均线多头排列");
            else if (ma.ShortMa > ma.LongMa && data.Close > ma.LongMa)
                conditions.Add("短期均线上穿长期均线且价格在长期均线上方");

            // RSI条件
            if (rsi > 30 && rsi < 65)
                conditions.Add("RSI在30-65合理区间");

            return string.Join(", ", conditions);
        }

        public override string AnalyzeSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            var conditions = new List<string>();

            // MACD条件
            if (macd.Signal == MacdSignalType.DeathCross)
                conditions.Add("MACD死叉");
            else if (macd.Dif < macd.Dea && macd.Dif < 0 && macd.Histogram < 0)
                conditions.Add("DIF<DEA且DIF<0且柱状图<0");

            // KDJ条件
            if (kdj.Signal == KdjSignalType.DeathCross)
                conditions.Add("KDJ死叉");
            else if (kdj.K < kdj.D && kdj.K > 20 && kdj.K < 80)
                conditions.Add("K<D且K在合理区间");

            // 移动平均线条件
            if (ma.Signal == MovingAverageSignalType.Bearish)
                conditions.Add("均线空头排列");
            else if (ma.ShortMa < ma.LongMa && data.Close < ma.LongMa)
                conditions.Add("短期均线下穿长期均线且价格跌破长期均线");

            // RSI条件
            if (rsi < 30 || rsi > 70)
                conditions.Add("RSI处于极端值");

            // 超买回调条件
            if (rsi > 70 && (macd.Dif < macd.Dea || macd.Signal == MacdSignalType.DeathCross) && kdj.K < kdj.D)
                conditions.Add("RSI超买且其他指标走弱");

            return string.Join(", ", conditions);
        }

        #region 辅助方法
        private decimal GetTotalPositionValue(PositionManager positionManager, List<StockData> dataList)
        {
            decimal totalValue = 0;
            var allPositions = positionManager.GetAllPositions();
            
            foreach (var position in allPositions.Values)
            {
                // 如果提供了数据列表，尝试使用最新价格计算持仓价值
                if (dataList != null && dataList.Count > 0)
                {
                    // 使用最后一个数据点的价格作为当前价格
                    var currentPrice = dataList[dataList.Count - 1].Close;
                    totalValue += position.Shares * currentPrice;
                }
                else
                {
                    // 否则使用平均买入价格估算
                    totalValue += position.Value;
                }
            }
            
            return totalValue;
        }

        private decimal GetAvgBuyPrice(PositionManager positionManager, string stockCode)
        {
            var position = positionManager.GetPosition(stockCode);
            if (position != null)
            {
                return position.AvgBuyPrice;
            }
            return 0;
        }

        private decimal GetPreviousMacdHistogram(int index, int totalCount)
        {
            // 这里应该有实际的MACD历史数据，暂时返回0
            return 0;
        }
        #endregion
    }

    /// <summary>
    /// 长期投资策略
    /// </summary>
    public class LongTermInvestmentStrategy : BaseTradingStrategy
    {
        public override string Name => "长期投资策略";

        public override bool IsBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, PositionManager positionManager, decimal currentBalance, List<StockData> dataList)
        {
            // 长期投资策略：更关注趋势和基本面
            // 检查总仓位
            decimal totalValue = currentBalance + GetTotalPositionValue(positionManager, dataList);
            decimal currentStockValue = GetTotalPositionValue(positionManager, dataList);
            decimal currentPositionRatio = totalValue > 0 ? currentStockValue / totalValue : 0;
            bool canBuyMore = currentPositionRatio < 0.8m;

            // 长期趋势向上：MA50 > MA200 或者短期均线上穿长期均线
            bool longTermTrendUp = ma.ShortMa > ma.LongMa && ma.ShortMa > data.Close * 0.95m; // 短期均线略低于价格

            // MACD形成金叉，且DIF > DEA，表示上涨趋势
            bool macdTrendUp = macd.Signal == MacdSignalType.GoldenCross || 
                              (macd.Dif > macd.Dea && macd.Dif > 0);

            // RSI处于中低位，但有上升趋势
            bool rsiOptimal = rsi > 35 && rsi < 60;

            // KDJ在相对低位形成金叉或K线开始上扬
            bool kdjOptimal = (kdj.Signal == KdjSignalType.GoldenCross || kdj.K > kdj.D) && 
                             kdj.K > 25 && kdj.K < 75;

            // 组合条件
            bool buyCondition = longTermTrendUp && macdTrendUp && rsiOptimal && kdjOptimal;

            return buyCondition && canBuyMore;
        }

        public override bool IsSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, PositionManager positionManager, string stockCode = "UNKNOWN")
        {
            // 长期投资的卖出条件：趋势反转
            decimal avgBuyPrice = GetAvgBuyPrice(positionManager, stockCode);
            bool stopLoss = false;
            bool takeProfit = false;
            
            if (avgBuyPrice > 0)
            {
                decimal currentProfitRatio = (data.Close - avgBuyPrice) / avgBuyPrice;
                stopLoss = currentProfitRatio < -0.10m; // 10%止损，比短期策略更宽松
                takeProfit = currentProfitRatio > 0.20m; // 20%止盈，比短期策略更高
            }

            // 趋势反转信号
            bool trendReversal = ma.Signal == MovingAverageSignalType.Bearish || 
                               (ma.ShortMa < ma.LongMa && data.Close < ma.ShortMa * 0.98m); // 价格跌破均线2%

            bool macdTrendDown = macd.Signal == MacdSignalType.DeathCross || 
                               (macd.Dif < macd.Dea && macd.Dif < 0);

            // RSI超卖或严重超买
            bool rsiExtreme = rsi < 25 || rsi > 80;

            return (stopLoss || takeProfit || (trendReversal && macdTrendDown) || rsiExtreme) && 
                   positionManager.HasPosition(stockCode);
        }

        public override string AnalyzeBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            var conditions = new List<string>();

            if (ma.ShortMa > ma.LongMa)
                conditions.Add("短期均线上穿长期均线");
            if (macd.Signal == MacdSignalType.GoldenCross || macd.Dif > macd.Dea)
                conditions.Add("MACD趋势向上");
            if (rsi > 35 && rsi < 60)
                conditions.Add("RSI适中");
            if (kdj.K > kdj.D || kdj.Signal == KdjSignalType.GoldenCross)
                conditions.Add("KDJ趋势向上");

            return string.Join(", ", conditions);
        }

        public override string AnalyzeSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            var conditions = new List<string>();

            if (ma.Signal == MovingAverageSignalType.Bearish || (ma.ShortMa < ma.LongMa && data.Close < ma.ShortMa))
                conditions.Add("均线趋势向下");
            if (macd.Signal == MacdSignalType.DeathCross || macd.Dif < macd.Dea)
                conditions.Add("MACD趋势向下");
            if (rsi < 25 || rsi > 80)
                conditions.Add("RSI极端值");

            return string.Join(", ", conditions);
        }

        #region 辅助方法
        private decimal GetTotalPositionValue(PositionManager positionManager, List<StockData> dataList)
        {
            decimal totalValue = 0;
            var allPositions = positionManager.GetAllPositions();
            
            foreach (var position in allPositions.Values)
            {
                if (dataList != null && dataList.Count > 0)
                {
                    var currentPrice = dataList[dataList.Count - 1].Close;
                    totalValue += position.Shares * currentPrice;
                }
                else
                {
                    totalValue += position.Value;
                }
            }
            
            return totalValue;
        }

        private decimal GetAvgBuyPrice(PositionManager positionManager, string stockCode)
        {
            var position = positionManager.GetPosition(stockCode);
            if (position != null)
            {
                return position.AvgBuyPrice;
            }
            return 0;
        }
        #endregion
    }

    /// <summary>
    /// 波段交易策略
    /// </summary>
    public class SwingTradingStrategy : BaseTradingStrategy
    {
        public override string Name => "波段交易策略";

        public override bool IsBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, PositionManager positionManager, decimal currentBalance, List<StockData> dataList)
        {
            // 检查总仓位
            decimal totalValue = currentBalance + GetTotalPositionValue(positionManager, dataList);
            decimal currentStockValue = GetTotalPositionValue(positionManager, dataList);
            decimal currentPositionRatio = totalValue > 0 ? currentStockValue / totalValue : 0;
            bool canBuyMore = currentPositionRatio < 0.8m;

            // 波段操作：寻找相对低点买入，相对高点卖出
            // RSI在30-45之间，表示调整到位
            bool rsiOversold = rsi > 30 && rsi < 45;

            // KDJ在20-50之间，且有金叉迹象
            bool kdjOversold = kdj.K > kdj.D && kdj.K > 20 && kdj.K < 50;

            // MACD在0轴附近或略上方，准备金叉
            bool macdReady = macd.Dif > macd.Dea && Math.Abs(macd.Dif) < 0.1m;

            // 价格在均线上方或接近均线，趋势未坏
            bool priceTrendOK = data.Close > ma.LongMa * 0.98m; // 价格不低于长期均线2%

            bool buyCondition = rsiOversold && kdjOversold && macdReady && priceTrendOK;

            return buyCondition && canBuyMore;
        }

        public override bool IsSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, PositionManager positionManager, string stockCode = "UNKNOWN")
        {
            decimal avgBuyPrice = GetAvgBuyPrice(positionManager, stockCode);
            bool stopLoss = false;
            bool takeProfit = false;
            
            if (avgBuyPrice > 0)
            {
                decimal currentProfitRatio = (data.Close - avgBuyPrice) / avgBuyPrice;
                stopLoss = currentProfitRatio < -0.07m; // 7%止损
                takeProfit = currentProfitRatio > 0.15m; // 15%止盈
            }

            // RSI超买 > 75
            bool rsiOverbought = rsi > 75;

            // KDJ超买，K > 80 且 K < D (开始掉头)
            bool kdjOverbought = kdj.K > 80 && kdj.K < kdj.D;

            // MACD死叉或DIF开始下行
            bool macdTrendDown = macd.Signal == MacdSignalType.DeathCross || 
                               (macd.Dif < macd.Dea && macd.Dif > 0);

            // 价格跌破重要支撑位
            bool priceBreakdown = data.Close < ma.ShortMa * 0.98m;

            return (stopLoss || takeProfit || rsiOverbought || kdjOverbought || macdTrendDown || priceBreakdown) && 
                   positionManager.HasPosition(stockCode);
        }

        public override string AnalyzeBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            var conditions = new List<string>();

            if (rsi > 30 && rsi < 45)
                conditions.Add("RSI调整到位");
            if (kdj.K > kdj.D && kdj.K > 20 && kdj.K < 50)
                conditions.Add("KDJ底部金叉");
            if (macd.Dif > macd.Dea && Math.Abs(macd.Dif) < 0.1m)
                conditions.Add("MACD准备金叉");
            if (data.Close > ma.LongMa * 0.98m)
                conditions.Add("价格趋势健康");

            return string.Join(", ", conditions);
        }

        public override string AnalyzeSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            var conditions = new List<string>();

            if (rsi > 75)
                conditions.Add("RSI超买");
            if (kdj.K > 80 && kdj.K < kdj.D)
                conditions.Add("KDJ高位死叉");
            if (macd.Signal == MacdSignalType.DeathCross || (macd.Dif < macd.Dea && macd.Dif > 0))
                conditions.Add("MACD趋势转空");
            if (data.Close < ma.ShortMa * 0.98m)
                conditions.Add("价格破位");

            return string.Join(", ", conditions);
        }

        #region 辅助方法
        private decimal GetTotalPositionValue(PositionManager positionManager, List<StockData> dataList)
        {
            decimal totalValue = 0;
            var allPositions = positionManager.GetAllPositions();
            
            foreach (var position in allPositions.Values)
            {
                if (dataList != null && dataList.Count > 0)
                {
                    var currentPrice = dataList[dataList.Count - 1].Close;
                    totalValue += position.Shares * currentPrice;
                }
                else
                {
                    totalValue += position.Value;
                }
            }
            
            return totalValue;
        }

        private decimal GetAvgBuyPrice(PositionManager positionManager, string stockCode)
        {
            var position = positionManager.GetPosition(stockCode);
            if (position != null)
            {
                return position.AvgBuyPrice;
            }
            return 0;
        }
        #endregion
    }
}