using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators;
using Lux.Indicators.DivergenceDetectors;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.Models;
using Lux.Indicators.TrendIndicators;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 交易模拟器类，用于模拟实盘交易
    /// </summary>
    public class TradingSimulator
    {
        private readonly decimal _initialBalance;
        private decimal _currentBalance;
        private decimal _currentPosition;
        private decimal _positionValue;
        private readonly List<TradeRecord> _trades;
        private readonly List<StockData> _dataList;
        private readonly List<MacdOutput> _macdResults;
        private readonly List<KdjOutput> _kdjResults;
        private readonly List<MovingAverageOutput> _maResults;
        private readonly List<decimal> _rsiResults;

        public TradingSimulator(decimal initialBalance = 100000m)
        {
            _initialBalance = initialBalance;
            _currentBalance = initialBalance;
            _currentPosition = 0;
            _positionValue = 0;
            _trades = new List<TradeRecord>();
            _dataList = new List<StockData>();
            _macdResults = new List<MacdOutput>();
            _kdjResults = new List<KdjOutput>();
            _maResults = new List<MovingAverageOutput>();
            _rsiResults = new List<decimal>();
        }

        /// <summary>
        /// 添加数据点并进行分析
        /// </summary>
        public void AddDataPoint(StockData stockData)
        {
            _dataList.Add(stockData);

            // 计算技术指标
            UpdateIndicators();

            // 检查交易信号
            CheckTradingSignals();
        }

        /// <summary>
        /// 更新技术指标
        /// </summary>
        private void UpdateIndicators()
        {
            // 提取最近的数据用于计算
            var recentCount = Math.Min(_dataList.Count, 50);
            var recentData = _dataList.TakeLast(recentCount).ToList();
            var closePrices = recentData.Select(s => s.Close).ToList();
            var highPrices = recentData.Select(s => s.High).ToList();
            var lowPrices = recentData.Select(s => s.Low).ToList();

            // 检查是否有有效的高低价格数据，如果没有则使用收盘价代替
            bool hasValidHighLow = highPrices.All(h => h > 0) && lowPrices.All(l => l > 0);
            
            // 计算MACD
            MacdOutput macdOutput;
            if (closePrices.Count < 26) 
            {
                // 如果数据不足，使用默认值
                macdOutput = new MacdOutput { Dif = 0, Dea = 0, Histogram = 0, Signal = MacdSignalType.None };
            }
            else 
            {
                var macdResult = MacdAnalyzer.Analyze(closePrices, 12, 26, 9);
                macdOutput = macdResult.Last();
            }
            _macdResults.Add(macdOutput);

            // 计算KDJ - 如果没有有效的高低价格数据，则跳过或使用默认值
            KdjOutput kdjOutput;
            if (!hasValidHighLow || highPrices.Count < 9 || lowPrices.Count < 9 || closePrices.Count < 9) 
            {
                // 如果数据不足或无效，使用默认值
                kdjOutput = new KdjOutput { K = 50, D = 50, J = 50, Signal = KdjSignalType.None };
            }
            else 
            {
                var kdjResult = KdjAnalyzer.Analyze(highPrices, lowPrices, closePrices, 9, 3, 3);
                kdjOutput = kdjResult.Last();
            }
            _kdjResults.Add(kdjOutput);

            // 计算移动平均线
            MovingAverageOutput maOutput;
            if (closePrices.Count < 10) 
            {
                // 如果数据不足，使用默认值
                maOutput = new MovingAverageOutput { ShortMa = 0, LongMa = 0, Signal = MovingAverageSignalType.None };
            }
            else 
            {
                var maResult = MovingAverageAnalyzer.Analyze(closePrices, 5, 10);
                maOutput = maResult.Last();
            }
            _maResults.Add(maOutput);

            // 计算RSI
            decimal rsiValue;
            if (closePrices.Count < 15) // 需要14+1个数据点来计算RSI
            {
                // 如果数据不足，使用默认值
                rsiValue = 50;
            }
            else 
            {
                var rsiResult = CalculateRsi(closePrices, 14);
                rsiValue = rsiResult.Last();
            }
            _rsiResults.Add(rsiValue);
        }

        /// <summary>
        /// 计算RSI
        /// </summary>
        private List<decimal> CalculateRsi(List<decimal> closePrices, int period)
        {
            var rsiValues = new List<decimal>();
            if (closePrices.Count < period + 1)
            {
                // 初始化默认值
                for (int i = 0; i < closePrices.Count; i++)
                {
                    rsiValues.Add(50);
                }
                return rsiValues;
            }

            for (int i = 0; i < closePrices.Count; i++)
            {
                if (i < period)
                {
                    rsiValues.Add(50); // 初始值
                    continue;
                }

                decimal gainSum = 0;
                decimal lossSum = 0;

                for (int j = i - period + 1; j <= i; j++)
                {
                    decimal change = closePrices[j] - closePrices[j - 1];
                    if (change > 0)
                    {
                        gainSum += change;
                    }
                    else
                    {
                        lossSum += Math.Abs(change);
                    }
                }

                decimal avgGain = gainSum / period;
                decimal avgLoss = lossSum / period;

                if (avgLoss == 0)
                {
                    rsiValues.Add(100);
                }
                else
                {
                    decimal rs = avgGain / avgLoss;
                    decimal rsi = 100 - (100 / (1 + rs));
                    rsiValues.Add(rsi);
                }
            }

            return rsiValues;
        }

        /// <summary>
        /// 检查交易信号
        /// </summary>
        private void CheckTradingSignals()
        {
            int currentIndex = _dataList.Count - 1;

            // 需要足够的数据点才能进行分析
            if (currentIndex < 26) return;

            var currentData = _dataList[currentIndex];
            var currentMacd = _macdResults[currentIndex];
            var currentKdj = _kdjResults[currentIndex];
            var currentMa = _maResults[currentIndex];
            var currentRsi = _rsiResults[currentIndex];

            // 买入信号判断
            if (IsBuySignal(currentIndex, currentData, currentMacd, currentKdj, currentMa, currentRsi))
            {
                ExecuteBuy(currentData);
            }
            // 卖出信号判断
            else if (IsSellSignal(currentIndex, currentData, currentMacd, currentKdj, currentMa, currentRsi))
            {
                ExecuteSell(currentData);
            }
        }

        /// <summary>
        /// 判断买入信号
        /// </summary>
        private bool IsBuySignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            // 条件1: MACD金叉 或 DIF > DEA 且 DIF > 0
            bool macdCondition = macd.Signal == MacdSignalType.GoldenCross || 
                                (macd.Dif > macd.Dea && macd.Dif > 0);

            // 条件2: KDJ金叉 或 K > D 且 K < 80 (未超买)
            bool kdjCondition = kdj.Signal == KdjSignalType.GoldenCross || 
                               (kdj.K > kdj.D && kdj.K < 80);

            // 条件3: 短期均线上穿长期均线 或 价格在均线上方
            bool maCondition = ma.Signal == MovingAverageSignalType.Bullish || 
                              (ma.ShortMa > ma.LongMa && data.Close > ma.ShortMa);

            // 条件4: RSI > 30 且 RSI < 70 (非极端区域)
            bool rsiCondition = rsi > 30 && rsi < 70;

            // 综合判断：至少满足3个条件
            int satisfiedConditions = (macdCondition ? 1 : 0) +
                                    (kdjCondition ? 1 : 0) +
                                    (maCondition ? 1 : 0) +
                                    (rsiCondition ? 1 : 0);

            return satisfiedConditions >= 3 && _currentPosition == 0; // 且当前无持仓
        }

        /// <summary>
        /// 判断卖出信号
        /// </summary>
        private bool IsSellSignal(int index, StockData data, MacdOutput macd, KdjOutput kdj, MovingAverageOutput ma, decimal rsi)
        {
            // 条件1: MACD死叉 或 DIF < DEA 且 DIF < 0
            bool macdCondition = macd.Signal == MacdSignalType.DeathCross || 
                                (macd.Dif < macd.Dea && macd.Dif < 0);

            // 条件2: KDJ死叉 或 K < D 且 K > 20 (未超卖)
            bool kdjCondition = kdj.Signal == KdjSignalType.DeathCross || 
                               (kdj.K < kdj.D && kdj.K > 20);

            // 条件3: 短期均线下穿长期均线 或 价格在均线下方
            bool maCondition = ma.Signal == MovingAverageSignalType.Bearish || 
                              (ma.ShortMa < ma.LongMa && data.Close < ma.ShortMa);

            // 条件4: RSI < 30 或 RSI > 70 (极端区域)
            bool rsiCondition = rsi < 30 || rsi > 70;

            // 综合判断：至少满足3个条件
            int satisfiedConditions = (macdCondition ? 1 : 0) +
                                    (kdjCondition ? 1 : 0) +
                                    (maCondition ? 1 : 0) +
                                    (rsiCondition ? 1 : 0);

            return satisfiedConditions >= 3 && _currentPosition > 0; // 且当前持有仓位
        }

        /// <summary>
        /// 执行买入操作
        /// </summary>
        private void ExecuteBuy(StockData data)
        {
            // 使用一半的资金买入
            decimal amountToSpend = _currentBalance * 0.5m;
            decimal sharesToBuy = Math.Floor(amountToSpend / data.Close);
            
            if (sharesToBuy > 0)
            {
                decimal cost = sharesToBuy * data.Close;
                _currentBalance -= cost;
                _currentPosition += sharesToBuy;
                _positionValue = sharesToBuy * data.Close;

                _trades.Add(new TradeRecord
                {
                    Action = TradeAction.Buy,
                    DateTime = data.Date,
                    Price = data.Close,
                    Shares = sharesToBuy,
                    Amount = cost,
                    BalanceAfter = _currentBalance
                });
            }
        }

        /// <summary>
        /// 执行卖出操作
        /// </summary>
        private void ExecuteSell(StockData data)
        {
            if (_currentPosition > 0)
            {
                decimal revenue = _currentPosition * data.Close;
                _currentBalance += revenue;
                decimal sharesSold = _currentPosition;
                _currentPosition = 0;
                _positionValue = 0;

                _trades.Add(new TradeRecord
                {
                    Action = TradeAction.Sell,
                    DateTime = data.Date,
                    Price = data.Close,
                    Shares = sharesSold,
                    Amount = revenue,
                    BalanceAfter = _currentBalance
                });
            }
        }

        /// <summary>
        /// 运行模拟交易
        /// </summary>
        public void RunSimulation(List<StockData> dataList)
        {
            foreach (var data in dataList)
            {
                AddDataPoint(data);
            }

            // 模拟结束时清空所有持仓
            if (_currentPosition > 0)
            {
                var lastData = dataList.Last();
                ExecuteSell(lastData);
            }
        }

        /// <summary>
        /// 获取最终结果
        /// </summary>
        public SimulationResult GetResult()
        {
            decimal finalBalance = _currentBalance + _positionValue; // 包括现金和持仓价值
            decimal profit = finalBalance - _initialBalance;
            decimal profitPercentage = (_initialBalance != 0) ? (profit / _initialBalance) * 100 : 0;

            return new SimulationResult
            {
                InitialBalance = _initialBalance,
                FinalBalance = finalBalance,
                Profit = profit,
                ProfitPercentage = profitPercentage,
                TotalTrades = _trades.Count,
                Trades = _trades
            };
        }
    }

    /// <summary>
    /// 交易记录
    /// </summary>
    public class TradeRecord
    {
        public TradeAction Action { get; set; }
        public DateTime DateTime { get; set; }
        public decimal Price { get; set; }
        public decimal Shares { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
    }

    /// <summary>
    /// 交易动作枚举
    /// </summary>
    public enum TradeAction
    {
        Buy,
        Sell
    }

    /// <summary>
    /// 模拟结果
    /// </summary>
    public class SimulationResult
    {
        public decimal InitialBalance { get; set; }
        public decimal FinalBalance { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitPercentage { get; set; }
        public int TotalTrades { get; set; }
        public List<TradeRecord> Trades { get; set; } = new List<TradeRecord>();
    }
}