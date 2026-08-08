using System;
using MahjongGame.Model;

namespace MahjongGame.GameLogic
{
    /// <summary>
    /// 麻将分层布局的离散半格坐标与矩形重叠规则。
    /// </summary>
    public static class MahjongLayoutGeometry
    {
        public const int HalfGridUnitsPerCell = 2; // 一个完整逻辑网格占用的半格单位数
        /// <summary>
        /// 获取关卡卡牌中心的半格列坐标。
        /// </summary>
        public static int GetCenterColumnInHalfGridUnits(MahjongLevelCardDefinition card)
        {
            return card.coordY;
        }

        /// <summary>
        /// 获取关卡卡牌中心的半格行坐标。
        /// </summary>
        public static int GetCenterRowInHalfGridUnits(MahjongLevelCardDefinition card)
        {
            return card.coordX;
        }

        /// <summary>
        /// 判断两张关卡卡牌是否存在面积重叠。
        /// </summary>
        public static bool HasAreaOverlap(MahjongLevelCardDefinition first, MahjongLevelCardDefinition second)
        {
            int columnDistance = Math.Abs(GetCenterColumnInHalfGridUnits(first) - GetCenterColumnInHalfGridUnits(second));
            int rowDistance = Math.Abs(GetCenterRowInHalfGridUnits(first) - GetCenterRowInHalfGridUnits(second));
            return columnDistance < HalfGridUnitsPerCell && rowDistance < HalfGridUnitsPerCell;
        }

    }
}
