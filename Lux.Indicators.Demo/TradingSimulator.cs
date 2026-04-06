using System;
using System.Collections.Generic;
using System.Linq;
using Lux.Indicators;
using Lux.Indicators.DivergenceDetectors;
using Lux.Indicators.MomentumIndicators;
using Lux.Indicators.Models;
using Lux.Indicators.TrendIndicators;
using Lux.Indicators.Demo.Interfaces;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 重构后的交易模拟器类，用于模拟实盘交易
    /// 遵循单一职责原则，将功能分解为不同的组件
    /// </summary>
    public class TradingSimulator
    {
        private readonly decimal _initialBalance;
        private decimal _currentBalance;
        private readonly PositionManager _positionManager; // 持仓管理器
        private readonly List<TradeRecord> _trades;
        private readonly IDataProcessor _dataProcessor;
        private readonly ITradingSignalProcessor _signalProcessor;
        private readonly ITradeExecutor _tradeExecutor;

        public TradingSimulator(decimal initialBalance = 100000m, 
            ITradingStrategy strategy = null, 
            IPositionManagement positionManagement = null,
            IDataProcessor dataProcessor = null,
            ITradingSignalProcessor signalProcessor = null,
            ITradeExecutor tradeExecutor = null)
        {
            _initialBalance = initialBalance;
            _currentBalance = initialBalance;
            _positionManager = new PositionManager();
            _trades = new List<TradeRecord>();
            
            // 初始化各组件，实现职责分离并支持依赖注入
            _dataProcessor = dataProcessor ?? new DataProcessor();
            _signalProcessor = signalProcessor ?? new TradingSignalProcessor(
                strategy ?? new ShortTermTradingStrategy());
            _tradeExecutor = tradeExecutor ?? new TradeExecutor(_positionManager, _trades, 
                strategy ?? new ShortTermTradingStrategy(), 
                positionManagement ?? new BalancedPositionManagement());
        }

        /// <summary>
        /// 添加数据点并进行分析
        /// </summary>
        public void AddDataPoint(StockData stockData, string stockCode = "UNKNOWN")
        {
            // 处理单个数据点，通过组合模式委托给专门的处理器
            ProcessSingleDataPoint(stockData, stockCode);
        }

        /// <summary>
        /// 处理单个数据点
        /// </summary>
        private void ProcessSingleDataPoint(StockData data, string stockCode = "UNKNOWN")
        {
            // 使用数据处理器计算技术指标
            var indicators = _dataProcessor.ProcessData(data);
            
            // 使用信号处理器判断交易信号
            var signal = _signalProcessor.GetTradingSignal(data, indicators, _positionManager, _currentBalance);
            
            // 使用交易执行器执行交易
            var result = _tradeExecutor.ExecuteTrade(data, indicators, signal, stockCode, ref _currentBalance);
            
            // 如果交易成功，更新交易记录
            if (result.Success && result.TradeRecord != null)
            {
                _trades.Add(result.TradeRecord);
            }
        }

        /// <summary>
        /// 运行模拟交易
        /// </summary>
        public void RunSimulation(List<StockData> dataList, string stockCode = "UNKNOWN")
        {
            if (dataList == null || dataList.Count == 0) return;

            // 逐个处理数据
            foreach (var data in dataList)
            {
                ProcessSingleDataPoint(data, stockCode);
            }

            // 模拟结束时清空指定股票的持仓
            if (_positionManager.HasPosition(stockCode) && dataList.Any())
            {
                var lastData = dataList.Last();
                var indicators = _dataProcessor.ProcessData(lastData);
                var sellSignal = TradingSignal.Sell; // 强制平仓
                var result = _tradeExecutor.ExecuteTrade(lastData, indicators, sellSignal, stockCode, ref _currentBalance);
                
                if (result.Success && result.TradeRecord != null)
                {
                    _trades.Add(result.TradeRecord);
                }
            }
        }

        /// <summary>
        /// 获取最终结果
        /// </summary>
        public SimulationResult GetResult()
        {
            // 计算最终持仓价值（使用最新的股票价格）
            decimal positionValue = GetTotalPositionValue(null); // 使用最新的数据计算持仓价值
            decimal finalBalance = _currentBalance + positionValue; // 包括现金和持仓价值
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

        /// <summary>
        /// 获取总持仓价值
        /// </summary>
        private decimal GetTotalPositionValue(List<StockData> dataList)
        {
            decimal totalValue = 0;
            var allPositions = _positionManager.GetAllPositions();
            
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

        /// <summary>
        /// 初始化持仓（用于继续交易时设置现有持仓）
        /// </summary>
        public void InitializePosition(string stockCode, decimal shares, decimal avgBuyPrice)
        {
            _positionManager.SetPosition(stockCode, shares, avgBuyPrice);
        }

        /// <summary>
        /// 保存当前状态到文件
        /// </summary>
        public void SaveState(string filePath)
        {
            // 保存功能暂不实现
        }

        /// <summary>
        /// 从文件加载状态
        /// </summary>
        public void LoadState(string filePath)
        {
            // 加载功能暂不实现
        }
    }
}