using System.Collections.Generic;
using MahjongGame.Model;
using MahjongGame.GameLogic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 首关新手引导小关卡目录加载器。
/// </summary>
public static class TutorialStageCatalogLoader
{
    private const string ResourcePath = "Mahjong/TutorialStages";

    private static MahjongLevelCatalog catalog;
    private static Dictionary<int, MahjongLevelDefinition> stagesById;

    /// <summary>
    /// 获取当前引导小关卡总数。
    /// </summary>
    public static int StageCount
    {
        get
        {
            EnsureLoaded();
            return stagesById.Count;
        }
    }

    /// <summary>
    /// 按顺序编号获取引导小关卡配置。
    /// </summary>
    public static MahjongLevelDefinition GetStage(int stageIndex)
    {
        EnsureLoaded();
        return stagesById.TryGetValue(stageIndex, out MahjongLevelDefinition stage) ? stage : null;
    }

    /// <summary>
    /// 为指定引导小关卡生成随机配对牌面。第 3 小关随机后重排为两边同种、中间同种。
    /// </summary>
    public static MahjongLevelDefinition CreateRuntimeStage(int stageIndex)
    {
        MahjongLevelDefinition source = GetStage(stageIndex);
        if (source == null)
        {
            return null;
        }

        System.Random random = new System.Random(unchecked(System.Environment.TickCount * 31 + stageIndex));
        MahjongLevelDefinition randomized = MahjongLevelRandomizer.CreateRandomizedLevel(source, catalog.cardTypeIds, random);
        MahjongLevelDefinition result = randomized ?? source;

        if (stageIndex == 3)
        {
            result = RearrangeStage3(result);
        }

        return result;
    }

    private static MahjongLevelDefinition RearrangeStage3(MahjongLevelDefinition source)
    {
        if (source == null || source.cards == null || source.cards.Count < 4)
        {
            return source;
        }

        int typeA = source.cards[0].typeId;
        int typeB = 0;
        int firstB = -1;
        for (int i = 1; i < source.cards.Count; i++)
        {
            if (source.cards[i].typeId != typeA)
            {
                typeB = source.cards[i].typeId;
                firstB = i;
                break;
            }
        }

        if (firstB < 0)
        {
            return source;
        }

        int otherA = -1;
        int otherB = -1;
        for (int i = 0; i < source.cards.Count; i++)
        {
            if (i == 0)
            {
                continue;
            }

            if (source.cards[i].typeId == typeA && otherA < 0)
            {
                otherA = i;
            }
            else if (source.cards[i].typeId == typeB && i != firstB && otherB < 0)
            {
                otherB = i;
            }
        }

        if (otherA < 0 || otherB < 0)
        {
            return source;
        }

        var cards = new List<MahjongLevelCardDefinition>(source.cards.Count);
        for (int i = 0; i < source.cards.Count; i++)
        {
            int pickIndex;
            if (i == 0 || i == source.cards.Count - 1)
            {
                pickIndex = (i == 0) ? 0 : otherA;
            }
            else
            {
                pickIndex = (i == 1) ? firstB : otherB;
            }

            MahjongLevelCardDefinition src = source.cards[pickIndex];
            cards.Add(new MahjongLevelCardDefinition
            {
                typeId = src.typeId,
                layer = source.cards[i].layer,
                coordY = source.cards[i].coordY,
                coordX = source.cards[i].coordX
            });
        }

        return new MahjongLevelDefinition
        {
            level = source.level,
            gridColumnCount = source.gridColumnCount,
            gridRowCount = source.gridRowCount,
            randomizeTypeIds = source.randomizeTypeIds,
            cards = cards
        };
    }

    private static void EnsureLoaded()
    {
        if (catalog != null)
        {
            return;
        }

        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            stagesById = new Dictionary<int, MahjongLevelDefinition>();
            Debug.LogError("TutorialStages resource not found: " + ResourcePath);
            return;
        }

        catalog = JsonConvert.DeserializeObject<MahjongLevelCatalog>(asset.text);
        stagesById = new Dictionary<int, MahjongLevelDefinition>(catalog.levels.Count);
        for (int i = 0; i < catalog.levels.Count; i++)
        {
            MahjongLevelDefinition stage = catalog.levels[i];
            stagesById[stage.level] = stage;
        }
    }
}
