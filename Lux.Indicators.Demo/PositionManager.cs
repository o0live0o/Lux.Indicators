using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Lux.Indicators.Demo
{
    /// <summary>
    /// 持仓信息
    /// </summary>
    public class PositionInfo
    {
        public string StockCode { get; set; }
        public decimal Shares { get; set; }
        public decimal AvgBuyPrice { get; set; }
        public decimal Value => Shares * AvgBuyPrice;
    }

    /// <summary>
    /// 持仓管理器
    /// </summary>
    public class PositionManager
    {
        private Dictionary<string, PositionInfo> _positions = new Dictionary<string, PositionInfo>();

        /// <summary>
        /// 获取指定股票的持仓信息
        /// </summary>
        public PositionInfo GetPosition(string stockCode)
        {
            if (_positions.ContainsKey(stockCode))
                return _positions[stockCode];
            return null;
        }

        /// <summary>
        /// 添加或更新持仓
        /// </summary>
        public void UpdatePosition(string stockCode, decimal shares, decimal price)
        {
            if (_positions.ContainsKey(stockCode))
            {
                // 如果已有该股票持仓，更新平均买入价格
                var existingPosition = _positions[stockCode];
                decimal totalShares = existingPosition.Shares + shares;
                decimal totalCost = (existingPosition.AvgBuyPrice * existingPosition.Shares) + (shares * price);
                _positions[stockCode] = new PositionInfo
                {
                    StockCode = stockCode,
                    Shares = totalShares,
                    AvgBuyPrice = totalCost / totalShares
                };
            }
            else
            {
                // 新增该股票持仓
                _positions[stockCode] = new PositionInfo
                {
                    StockCode = stockCode,
                    Shares = shares,
                    AvgBuyPrice = price
                };
            }
        }

        /// <summary>
        /// 减少持仓
        /// </summary>
        public void ReducePosition(string stockCode, decimal sharesToReduce)
        {
            if (_positions.ContainsKey(stockCode))
            {
                var existingPosition = _positions[stockCode];
                decimal newShares = existingPosition.Shares - sharesToReduce;

                if (newShares <= 0)
                {
                    // 如果减少的份额大于等于现有份额，则清除持仓
                    _positions.Remove(stockCode);
                }
                else
                {
                    // 否则更新剩余份额，平均买入价保持不变
                    _positions[stockCode] = new PositionInfo
                    {
                        StockCode = stockCode,
                        Shares = newShares,
                        AvgBuyPrice = existingPosition.AvgBuyPrice
                    };
                }
            }
        }

        /// <summary>
        /// 设置持仓（用于从外部初始化）
        /// </summary>
        public void SetPosition(string stockCode, decimal shares, decimal avgBuyPrice)
        {
            _positions[stockCode] = new PositionInfo
            {
                StockCode = stockCode,
                Shares = shares,
                AvgBuyPrice = avgBuyPrice
            };
        }

        /// <summary>
        /// 移除持仓
        /// </summary>
        public void RemovePosition(string stockCode)
        {
            if (_positions.ContainsKey(stockCode))
            {
                _positions.Remove(stockCode);
            }
        }

        /// <summary>
        /// 检查是否持有某股票
        /// </summary>
        public bool HasPosition(string stockCode)
        {
            return _positions.ContainsKey(stockCode) && _positions[stockCode].Shares > 0;
        }

        /// <summary>
        /// 获取所有持仓
        /// </summary>
        public Dictionary<string, PositionInfo> GetAllPositions()
        {
            return _positions;
        }

        /// <summary>
        /// 获取持仓数量
        /// </summary>
        public decimal GetPositionShares(string stockCode)
        {
            if (_positions.ContainsKey(stockCode))
                return _positions[stockCode].Shares;
            return 0;
        }

        /// <summary>
        /// 保存持仓到文件
        /// </summary>
        public void SaveToFile(string filePath)
        {
            // 保存功能暂不实现
        }

        /// <summary>
        /// 从文件加载持仓
        /// </summary>
        public void LoadFromFile(string filePath)
        {
            // 加载功能暂不实现
        }
    }
}