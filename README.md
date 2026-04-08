# Lux.Indicators

Lux.Indicators 是一个全面的股票技术指标计算库，提供了多种常用技术指标的计算功能，包括MACD、KDJ、布林带(BOLL)和移动平均线等。

## 功能特性

- **MACD指标**: 计算DIF、DEA和柱状图(Histogram)，并提供金叉/死叉信号
- **KDJ指标**: 计算K、D、J值，并提供超买超卖及金叉死叉信号
- **布林带指标**: 计算布林带的上轨、中轨、下轨及带宽
- **移动平均线**: 计算短期和长期移动平均线及交叉信号
- **背离检测**: 检测价格与技术指标之间的顶背离和底背离
- **最优设计模式**: 使用静态分析器类避免不必要的对象创建
- **高效算法**: 时间复杂度为O(n)，适合大量数据处理
- **类型安全**: 强类型的输出类设计，防止数据混淆
- **优雅命名**: 使用专业的分析器(Analyzer)和输出(Output)命名模式
- **可读性强**: 使用清晰的变量和属性名称，提高代码可维护性

## 安装

```bash
# 从NuGet包安装
Install-Package Lux.Indicators
```

或者直接引用生成的nupkg文件：

```bash
# 直接安装本地包
dotnet add package Lux.Indicators -s path/to/package/directory
```

## 使用示例

```csharp
using System;
using System.Collections.Generic;
using Lux.Indicators;
using Lux.Indicators.Models;

class Program
{
    static void Main()
    {
        // 准备股票数据
        List<double> closePrices = new List<double> { /* 收盘价序列 */ };
        List<double> highPrices = new List<double> { /* 最高价序列 */ };
        List<double> lowPrices = new List<double> { /* 最低价序列 */ };

        // 分析MACD (使用默认参数)
        var macdResults = MacdAnalyzer.Analyze(closePrices);

        // 分析MACD (使用纯参数模式)
        var macdResultsPure = MacdAnalyzer.Analyze(closePrices, 10, 24, 7);

        // 分析MACD (使用自定义参数)
        var macdResultsWithOptions = MacdAnalyzer.Analyze(closePrices, new MacdOptions 
        { 
            FastPeriod = 10, 
            SlowPeriod = 24, 
            SignalPeriod = 7 
        });

        // 分析KDJ (使用默认参数)
        var kdjResults = KdjAnalyzer.Analyze(highPrices, lowPrices, closePrices);

        // 分析KDJ (使用纯参数模式)
        var kdjResultsPure = KdjAnalyzer.Analyze(highPrices, lowPrices, closePrices, 12, 5, 5);

        // 分析KDJ (使用自定义参数)
        var kdjResultsWithOptions = KdjAnalyzer.Analyze(highPrices, lowPrices, closePrices, new KdjOptions 
        { 
            RsvPeriod = 12, 
            KPeriod = 5, 
            DPeriod = 5 
        });

        // 分析布林带 (使用默认参数)
        var bollResults = BollingerBandsAnalyzer.Analyze(closePrices);

        // 分析布林带 (使用纯参数模式)
        var bollResultsPure = BollingerBandsAnalyzer.Analyze(closePrices, 25, 2.5m);

        // 分析布林带 (使用自定义参数)
        var bollResultsWithOptions = BollingerBandsAnalyzer.Analyze(closePrices, new BollingerBandsOptions 
        { 
            Period = 25, 
            StdDevMultiplier = 2.5m 
        });

        // 分析移动平均线 (使用默认参数)
        var maResults = MovingAverageAnalyzer.Analyze(closePrices);

        // 分析移动平均线 (使用纯参数模式)
        var maResultsPure = MovingAverageAnalyzer.Analyze(closePrices, 10, 30);

        // 分析移动平均线 (使用自定义参数)
        var maResultsWithOptions = MovingAverageAnalyzer.Analyze(closePrices, new MovingAverageOptions 
        { 
            ShortPeriod = 10, 
            LongPeriod = 30 
        });
        
        // 检测MACD背离
        var macdDivergences = MacdDivergenceAnalyzer.FindDivergences(closePrices, macdResults);
        foreach (var divergence in macdDivergences)
        {
            Console.WriteLine($"背离类型: {divergence.Type}, 描述: {divergence.Description}");
        }
        
        // 检测KDJ背离
        var kdjResults = KdjAnalyzer.Analyze(highPrices, lowPrices, closePrices);
        var kdjDivergences = KdjDivergenceAnalyzer.FindDivergences(closePrices, kdjResults);
        foreach (var divergence in kdjDivergences)
        {
            Console.WriteLine($"背离类型: {divergence.Type}, 描述: {divergence.Description}");
        }
        
        // 检测布林带背离
        var bollResults = BollingerBandsAnalyzer.Analyze(closePrices);
        var bollDivergences = BollingerBandsDivergenceAnalyzer.FindDivergences(closePrices, bollResults);
        foreach (var divergence in bollDivergences)
        {
            Console.WriteLine($"背离类型: {divergence.Type}, 描述: {divergence.Description}");
        }
        
        // 检测移动平均线背离
        var maResults = MovingAverageAnalyzer.Analyze(closePrices);
        var maDivergences = MovingAverageDivergenceAnalyzer.FindDivergences(closePrices, maResults);
        foreach (var divergence in maDivergences)
        {
            Console.WriteLine($"背离类型: {divergence.Type}, 描述: {divergence.Description}");
        }
    }
}
```

## 需要的基础数据

不同指标需要不同的基础数据：

- **MACD**: 需要收盘价序列
- **KDJ**: 需要最高价、最低价和收盘价序列
- **布林带**: 需要收盘价序列
- **移动平均线**: 需要收盘价序列

## 指标结果说明

每个指标计算都会返回对应的结果对象，包含：

- 数值：主要计算结果
- 信号：买卖信号或状态指示
- 其他相关数据

## 项目结构

```
Lux.Indicators/              # 主库项目
├── Models/                  # 实体模型
│   ├── StockData.cs         # 股票基础数据
│   ├── MacdOutput.cs        # MACD分析输出
│   ├── KdjOutput.cs         # KDJ分析输出
│   ├── BollingerBandsOutput.cs # 布林带分析输出
│   └── MovingAverageOutput.cs # 移动平均分析输出
├── Indicators/              # 指标分析器
│   ├── IndicatorCalculator.cs # 通用计算工具
│   ├── DivergenceCommon.cs  # 背离检测公共类
│   ├── TrendIndicators/     # 趋势指标
│   │   └── MovingAverageAnalyzer.cs # 移动平均分析器
│   ├── MomentumIndicators/  # 动量指标
│   │   ├── MacdAnalyzer.cs  # MACD分析器
│   │   └── KdjAnalyzer.cs   # KDJ分析器
│   ├── VolatileIndicators/  # 波动率指标
│   │   └── BollingerBandsAnalyzer.cs # 布林带分析器
│   └── DivergenceDetectors/ # 背离检测器
│       ├── MacdDivergenceAnalyzer.cs # MACD背离分析器
│       ├── KdjDivergenceAnalyzer.cs # KDJ背离分析器
│       ├── BollingerBandsDivergenceAnalyzer.cs # 布林带背离分析器
│       └── MovingAverageDivergenceAnalyzer.cs # 移动平均线背离分析器
└── Lux.Indicators.csproj    # 项目文件

Lux.Indicators.Demo/         # 演示项目
├── Program.cs               # 示例代码
└── Lux.Indicators.Demo.csproj # 项目文件
```

## 构建NuGet包

```bash
# 构建并生成NuGet包
dotnet pack Lux.Indicators --configuration Release
```

生成的包位于 `bin/Release/` 目录下。

## 许可证

MIT License

## 解决方案文件

本项目使用 `.slnx` 解决方案文件格式。