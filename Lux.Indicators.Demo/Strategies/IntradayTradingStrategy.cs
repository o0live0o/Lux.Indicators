using System;
using System.Collections.Generic;
using Lux.Indicators;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.Models;
using Lux.Indicators.TrendIndicators;

namespace Lux.Indicators.Demo.Strategies
{
    /// <summary>
    /// 日内交易策略 - 模拟最常见的日内交易策略
    /// 基于技术指标的短期买卖决策，通常在一天内完成买卖
    /// </summary>
    public class IntradayTradingStrategy : BaseTradingStrategy
    {
        public override string Name => "日内交易策略";

        public override bool IsBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, PositionManager positionManager, decimal currentBalance, List<StockData> dataList)
        {
            // 检查当前总仓位是否已满（不超过80%）
            decimal totalValue = currentBalance + GetTotalPositionValue(positionManager, dataList);
            decimal currentStockValue = GetTotalPositionValue(positionManager, dataList);
            decimal currentPositionRatio = totalValue > 0 ? currentStockValue / totalValue : 0;

            // 允许在不超过80%总仓位的情况下继续买入
            bool canBuyMore = currentPositionRatio < 0.8m;

            // 检查当前是否已经持有该股票
            bool alreadyHolding = positionManager.HasPosition("600001"); // 检查是否已经持有这只股票

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

            return qualitySignal && canBuyMore && !alreadyHolding; // 避免重复买入同一股票
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

        public override bool IsSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, PositionManager positionManager, string stockCode = "UNKNOWN")
        {
            // 检查是否持有该股票
            bool hasPosition = positionManager.HasPosition(stockCode);
            if (!hasPosition) return false; // 没有持仓不能卖出

            // 获取持仓信息
            var position = positionManager.GetPosition(stockCode);
            if (position == null || position.AvgBuyPrice <= 0) return false;

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
            else if (kdj.K > kdj.D && kdj.K < 80 && kdj.D < 80)
                conditions.Add("K>D且未超买");

            // RSI条件
            if (rsi > 30 && rsi < 65)
                conditions.Add("RSI适中");
            else if (rsi < 30)
                conditions.Add("RSI超卖反弹");

            // 均线条件
            if (ma.Signal == MovingAverageSignalType.Bullish)
                conditions.Add("均线多头排列");
            else if (ma.ShortMa > ma.LongMa)
                conditions.Add("短期均线上穿长期均线");

            return conditions.Count > 0 ? string.Join(" + ", conditions) : "无明显买入信号";
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
            else if (kdj.K < kdj.D && kdj.K > 20 && kdj.D > 20)
                conditions.Add("K<D且未超卖");

            // RSI条件
            if (rsi > 70)
                conditions.Add("RSI超买");
            else if (rsi < 30)
                conditions.Add("RSI超卖");

            // 均线条件
            if (ma.Signal == MovingAverageSignalType.Bearish)
                conditions.Add("均线空头排列");
            else if (ma.ShortMa < ma.LongMa)
                conditions.Add("短期均线下穿长期均线");

            return conditions.Count > 0 ? string.Join(" + ", conditions) : "无明显卖出信号";
        }
    }
}