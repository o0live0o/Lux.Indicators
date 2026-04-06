using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators.Demo.Interfaces;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.TrendIndicators;
using Lux.Indicators.Models;

namespace Lux.Indicators.Demo.Traders
{
    /// <summary>
    /// 日内交易员 - 专门执行日内交易策略的交易员
    /// </summary>
    public class IntradayTrader : BaseTrader
    {
        public IntradayTrader(string name, decimal initialBalance, ITradingStrategy strategy, IPositionManagement positionManagement)
            : base(name, initialBalance, strategy, positionManagement)
        {
        }

        public override void ProcessDataPoint(StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, string stockCode)
        {
            // 只处理订阅的股票
            if (!IsSubscribedToSymbol(stockCode))
            {
                return;
            }

            _historicalData.Add(data);

            // 使用策略判断是否买入
            var mockPositionManager = new MockPositionManager(_positions);
            if (_strategy.IsBuySignal(_historicalData.Count - 1, data, macd, kdj, ma, rsi, mockPositionManager, _balance, _historicalData))
            {
                // 检查是否允许新开仓
                if (_positionManagement.AllowNewPosition(TotalValue, GetCurrentPositionValue(_historicalData)))
                {
                    // 获取策略分析的买入信号详情
                    string buyReason = _strategy.AnalyzeBuySignal(_historicalData.Count - 1, data, macd, kdj, ma, rsi);
                    ExecuteBuy(data, macd, kdj, ma, rsi, stockCode);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {Name} 买入 {stockCode}，理由：{buyReason}");
                }
            }
            
            // 即使买入信号触发，也要检查是否卖出（使用独立的if而不是else if）
            // 这样可以在同一条数据上先买入后卖出，或根据持仓情况进行卖出
            var updatedMockPositionManager = new MockPositionManager(_positions); // 更新持仓状态后再检查卖出
            if (_strategy.IsSellSignal(_historicalData.Count - 1, data, macd, kdj, ma, rsi, updatedMockPositionManager, stockCode))
            {
                // 获取策略分析的卖出信号详情
                string sellReason = _strategy.AnalyzeSellSignal(_historicalData.Count - 1, data, macd, kdj, ma, rsi);
                ExecuteSell(data, macd, kdj, ma, rsi, stockCode);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {Name} 卖出 {stockCode}，理由：{sellReason}");
            }
        }

        public decimal GetCurrentPositionValue(List<string[]> stockDataList)
        {
            decimal totalValue = 0;

            foreach (var kvp in _positions)
            {
                if (kvp.Value.Shares > 0)
                {
                    // 使用最新的价格计算持仓价值
                    // 这里简化处理，实际应使用最新市场价
                    totalValue += kvp.Value.Shares * kvp.Value.AvgBuyPrice;
                }
            }

            return totalValue;
        }
        
        /// <summary>
        /// 强制卖出指定股票的所有持仓
        /// </summary>
        public void ForceSell(StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi, string stockCode)
        {
            var position = _positions.TryGetValue(stockCode, out var pos) ? pos : null;
            if (position != null && position.Shares > 0)
            {
                // 卖出全部持仓
                ExecuteSell(data, macd, kdj, ma, rsi, stockCode);
            }
        }
    }
}