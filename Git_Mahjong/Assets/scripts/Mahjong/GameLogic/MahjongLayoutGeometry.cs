using System;

namespace MahjongGame.GameLogic
{
    /// <summary>
    /// 麻将分层布局的离散半格坐标与矩形重叠规则。
    /// </summary>
    public static class MahjongLayoutGeometry
    {
        public const int HalfGridUnitsPerCell = 2; // 一个完整逻辑网格占用的半格单位数
        public const int OffsetUnitsOnOddLayer = 1; // 奇数层在每个轴上的半格偏移量

        /// <summary>
        /// 判断指定层是否需要应用横纵各半格的视觉与逻辑偏移。
        /// </summary>
        public static bool IsOffsetLayer(int layer)
        {
            return layer % 2 != 0;
        }

        /// <summary>
        /// 获取卡牌中心的半格列坐标。
        /// </summary>
        public static int GetCenterColumnInHalfGridUnits(int column, int layer)
        {
            return column * HalfGridUnitsPerCell + (IsOffsetLayer(layer) ? OffsetUnitsOnOddLayer : 0);
        }

        /// <summary>
        /// 获取卡牌中心的半格行坐标。
        /// </summary>
        public static int GetCenterRowInHalfGridUnits(int row, int layer)
        {
            return row * HalfGridUnitsPerCell + (IsOffsetLayer(layer) ? OffsetUnitsOnOddLayer : 0);
        }

        /// <summary>
        /// 判断两张完整网格尺寸的卡牌是否存在面积重叠。
        /// </summary>
        public static bool HasAreaOverlap(int firstColumn, int firstRow, int firstLayer, int secondColumn, int secondRow, int secondLayer)
        {
            int columnDistance = Math.Abs(
                GetCenterColumnInHalfGridUnits(firstColumn, firstLayer) -
                GetCenterColumnInHalfGridUnits(secondColumn, secondLayer));
            int rowDistance = Math.Abs(
                GetCenterRowInHalfGridUnits(firstRow, firstLayer) -
                GetCenterRowInHalfGridUnits(secondRow, secondLayer));
            return columnDistance < HalfGridUnitsPerCell && rowDistance < HalfGridUnitsPerCell;
        }
    }
}
