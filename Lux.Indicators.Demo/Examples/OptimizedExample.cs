using Lux.Indicators.Demo.Factories;
using Lux.Indicators.Demo;

// 示例：使用优化后的交易模拟器
Console.WriteLine("=== 优化后的交易模拟器示例 ===");

// 方式1：使用默认配置
var simulator1 = TradingSimulatorFactory.CreateDefault(100000m);

// 方式2：使用特定策略
var simulator2 = TradingSimulatorFactory.CreateWithStrategy(
    new LongTermInvestmentStrategy(),
    new ConservativePositionManagement(),
    50000m);

// 方式3：完全自定义配置
var simulator3 = TradingSimulatorFactory.CreateCustom(
    initialBalance: 200000m,
    strategy: new SwingTradingStrategy(),
    positionManagement: new AggressivePositionManagement());

Console.WriteLine("交易模拟器创建成功！");
Console.WriteLine($"模拟器1初始资金: {simulator1.GetType().Name}");
Console.WriteLine($"模拟器2初始资金: {simulator2.GetResult().InitialBalance:C}");
Console.WriteLine($"模拟器3初始资金: {simulator3.GetResult().InitialBalance:C}");

// 注意：这里只是演示创建，实际运行需要真实数据
Console.WriteLine("\n系统已准备好进行交易模拟！");