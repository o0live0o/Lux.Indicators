using Lux.Indicators.Demo.Interfaces;

namespace Lux.Indicators.Demo.Factories
{
    /// <summary>
    /// 交易模拟器工厂类
    /// </summary>
    public static class TradingSimulatorFactory
    {
        /// <summary>
        /// 创建具有默认配置的交易模拟器
        /// </summary>
        public static TradingSimulator CreateDefault(decimal initialBalance = 100000m)
        {
            return new TradingSimulator(initialBalance);
        }

        /// <summary>
        /// 创建自定义配置的交易模拟器
        /// </summary>
        public static TradingSimulator CreateCustom(
            decimal initialBalance = 100000m,
            ITradingStrategy strategy = null,
            IPositionManagement positionManagement = null,
            IDataProcessor dataProcessor = null,
            ITradingSignalProcessor signalProcessor = null,
            ITradeExecutor tradeExecutor = null)
        {
            return new TradingSimulator(
                initialBalance,
                strategy,
                positionManagement,
                dataProcessor,
                signalProcessor,
                tradeExecutor);
        }

        /// <summary>
        /// 创建具有特定策略的交易模拟器
        /// </summary>
        public static TradingSimulator CreateWithStrategy(
            ITradingStrategy strategy,
            IPositionManagement positionManagement = null,
            decimal initialBalance = 100000m)
        {
            return new TradingSimulator(
                initialBalance,
                strategy,
                positionManagement);
        }
    }
}