using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.Models;
using Lux.Indicators.TrendIndicators;
using Lux.Indicators.Demo.Interfaces;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 数据处理器 - 负责技术指标计算
    /// </summary>
    public class DataProcessor : IDataProcessor
    {
        private readonly Queue<StockData> _recentData;
        private readonly int _maxDataPoints;
        private readonly object _lock = new object();

        public DataProcessor(int maxDataPoints = 50)
        {
            _recentData = new Queue<StockData>();
            _maxDataPoints = maxDataPoints;
        }

        public IndicatorResult ProcessData(StockData data)
        {
            // 调用带股票代码的重载方法，使用默认股票代码
            return ProcessData(data, "UNKNOWN");
        }
        
        public IndicatorResult ProcessData(StockData data, string stockCode)
        {
            lock(_lock)
            {
                // 将新数据加入队列
                _recentData.Enqueue(data);
                if (_recentData.Count > _maxDataPoints)
                {
                    _recentData.Dequeue(); // 移除最旧的数据
                }

                var dataList = _recentData.ToList();
                var closePrices = new decimal[dataList.Count];
                var highPrices = new decimal[dataList.Count];
                var lowPrices = new decimal[dataList.Count];

                // 高效地提取价格数组，避免多次LINQ查询
                for (int i = 0; i < dataList.Count; i++)
                {
                    closePrices[i] = dataList[i].Close;
                    highPrices[i] = dataList[i].High;
                    lowPrices[i] = dataList[i].Low;
                }

                // 计算技术指标
                var macd = CalculateMacd(closePrices);
                var kdj = CalculateKdj(highPrices, lowPrices, closePrices);
                var ma = CalculateMovingAverage(closePrices);
                var rsi = CalculateRsi(closePrices);

                return new IndicatorResult
                {
                    Macd = macd,
                    Kdj = kdj,
                    Ma = ma,
                    Rsi = rsi
                };
            }
        }

        private MacdOutput CalculateMacd(decimal[] closePrices)
        {
            if (closePrices.Length < 26)
            {
                return new MacdOutput { Dif = 0, Dea = 0, Histogram = 0, Signal = MacdSignalType.None };
            }
            
            var macdResult = MacdAnalyzer.Analyze(closePrices.ToList(), 12, 26, 9);
            return macdResult.Last();
        }

        private KdjOutput CalculateKdj(decimal[] highPrices, decimal[] lowPrices, decimal[] closePrices)
        {
            bool hasValidHighLow = Array.TrueForAll(highPrices, h => h > 0) && 
                                 Array.TrueForAll(lowPrices, l => l > 0);
            if (!hasValidHighLow || highPrices.Length < 9 || lowPrices.Length < 9 || closePrices.Length < 9)
            {
                return new KdjOutput { K = 50, D = 50, J = 50, Signal = KdjSignalType.None };
            }
            
            var kdjResult = KdjAnalyzer.Analyze(highPrices.ToList(), lowPrices.ToList(), closePrices.ToList(), 9, 3, 3);
            return kdjResult.Last();
        }

        private MovingAverageOutput CalculateMovingAverage(decimal[] closePrices)
        {
            if (closePrices.Length < 10)
            {
                return new MovingAverageOutput { ShortMa = 0, LongMa = 0, Signal = MovingAverageSignalType.None };
            }
            
            var maResult = MovingAverageAnalyzer.Analyze(closePrices.ToList(), 5, 10);
            return maResult.Last();
        }

        private decimal CalculateRsi(decimal[] closePrices)
        {
            if (closePrices.Length < 15)
            {
                return 50;
            }
            
            var rsiResult = CalculateRsiInternal(closePrices, 14);
            return rsiResult[rsiResult.Length - 1];
        }

        private decimal[] CalculateRsiInternal(decimal[] closePrices, int period)
        {
            var rsiValues = new decimal[closePrices.Length];
            if (closePrices.Length < period + 1)
            {
                for (int i = 0; i < closePrices.Length; i++)
                {
                    rsiValues[i] = 50;
                }
                return rsiValues;
            }

            for (int i = 0; i < closePrices.Length; i++)
            {
                if (i < period)
                {
                    rsiValues[i] = 50;
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
                    rsiValues[i] = 100;
                }
                else
                {
                    decimal rs = avgGain / avgLoss;
                    decimal rsi = 100 - (100 / (1 + rs));
                    rsiValues[i] = rsi;
                }
            }
            return rsiValues;
        }
    }
}