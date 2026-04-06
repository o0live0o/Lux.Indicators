using System.Collections.Generic;
using Lux.Indicators.Models;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 仓位管理器接口
    /// </summary>
    public interface IPositionManager
    {
        /// <summary>
        /// 检查是否持有某股票
        /// </summary>
        bool HasPosition(string stockCode);

        /// <summary>
        /// 获取指定股票的持仓信息
        /// </summary>
        PositionInfo GetPosition(string stockCode);

        /// <summary>
        /// 获取总持仓价值
        /// </summary>
        decimal GetTotalPositionValue(List<StockData> dataList);

        /// <summary>
        /// 更新持仓
        /// </summary>
        void UpdatePosition(string stockCode, decimal shares, decimal price);

        /// <summary>
        /// 清空持仓
        /// </summary>
        void ClearPosition(string stockCode);

        /// <summary>
        /// 移除持仓
        /// </summary>
        void RemovePosition(string stockCode);

        /// <summary>
        /// 设置持仓
        /// </summary>
        void SetPosition(string stockCode, decimal shares, decimal avgBuyPrice);

        /// <summary>
        /// 保存到文件
        /// </summary>
        void SaveToFile(string filePath);

        /// <summary>
        /// 从文件加载
        /// </summary>
        void LoadFromFile(string filePath);
    }
}