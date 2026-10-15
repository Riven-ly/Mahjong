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
    /// 为指定引导小关卡生成运行时牌面。牌型固定的小关卡直接使用配置中的固定牌型。
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
        return randomized ?? source;
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
