using System;
using System.Collections.Generic;
using MahjongGame.Model;

namespace MahjongGame.GameLogic
{
    /// <summary>
    /// 麻将固定关卡布局生成器。
    /// </summary>
    public sealed class MahjongLayoutGenerator
    {
        /// <summary>
        /// 根据编辑器已生成的关卡配置创建完整固定卡牌布局。
        /// </summary>
        public List<MahjongCardModel> Generate(MahjongLevelDefinition levelDefinition)
        {
            var cards = new List<MahjongCardModel>(levelDefinition.cards.Count);
            for (int i = 0; i < levelDefinition.cards.Count; i++)
            {
                MahjongLevelCardDefinition cardDefinition = levelDefinition.cards[i];
                cards.Add(new MahjongCardModel(
                    i + 1,
                    cardDefinition.typeId,
                    cardDefinition.layer,
                    new MahjongGridPosition(
                        cardDefinition.coordY / MahjongLayoutGeometry.HalfGridUnitsPerCell,
                        cardDefinition.coordX / MahjongLayoutGeometry.HalfGridUnitsPerCell),
                    cardDefinition.coordY,
                    cardDefinition.coordX));
            }

            return cards;
        }

    }
}
