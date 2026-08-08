using System;
using System.Collections.Generic;
using MahjongGame.Model;
using Newtonsoft.Json;
using UnityEngine;

namespace MahjongGame.View
{
    /// <summary>
    /// 从 Resources 加载并查询麻将关卡目录。
    /// </summary>
    public static class MahjongLevelCatalogLoader
    {
        private const int ExcludedFallbackLevelCount = 5; // 兜底选关时排除的最小关卡编号数量
        private static MahjongLevelCatalog catalog; // 已加载的关卡目录缓存
        private static Dictionary<int, MahjongLevelDefinition> levelsById; // 关卡编号到配置的查询缓存
        private static List<MahjongLevelDefinition> fallbackLevels; // 排除简单关后的随机兜底候选关卡
        private static Dictionary<int, MahjongLevelDefinition> sessionFallbackLevels = new Dictionary<int, MahjongLevelDefinition>(); // 当前应用会话内按玩家等级缓存的随机兜底关卡

        /// <summary>
        /// 按玩家等级获取关卡；缺失对应等级时在当前应用会话内随机并固定一关。
        /// </summary>
        public static MahjongLevelDefinition GetLevel(int playerLevel)
        {
            EnsureLoaded();
            if (levelsById.TryGetValue(playerLevel, out MahjongLevelDefinition levelDefinition))
            {
                return CreateRuntimeLevel(levelDefinition);
            }

            if (sessionFallbackLevels.TryGetValue(playerLevel, out MahjongLevelDefinition fallbackLevel))
            {
                return CreateRuntimeLevel(fallbackLevel);
            }

            fallbackLevel = fallbackLevels[UnityEngine.Random.Range(0, fallbackLevels.Count)];
            sessionFallbackLevels.Add(playerLevel, fallbackLevel);
            Debug.LogWarning($"未找到玩家等级{playerLevel}对应关卡，当前会话随机使用关卡{fallbackLevel.level}。");
            return CreateRuntimeLevel(fallbackLevel);
        }

        /// <summary>
        /// 根据关卡配置决定直接使用固定牌面或生成本局随机牌面。
        /// </summary>
        private static MahjongLevelDefinition CreateRuntimeLevel(MahjongLevelDefinition levelDefinition)
        {
            if (!levelDefinition.randomizeTypeIds)
            {
                return levelDefinition;
            }

            return MahjongGame.GameLogic.MahjongLevelRandomizer.CreateRandomizedLevel(
                levelDefinition,
                catalog.cardTypeIds,
                new System.Random(unchecked(Environment.TickCount * 31 + levelDefinition.level)));
        }

        /// <summary>
        /// 加载并缓存关卡目录。运行时直接信任编辑器已生成和校验的配置。
        /// </summary>
        private static void EnsureLoaded()
        {
            if (catalog != null)
            {
                return;
            }

            TextAsset levelAsset = Resources.Load<TextAsset>(MahjongConfig.LevelCatalogResourcePath);
            catalog = JsonConvert.DeserializeObject<MahjongLevelCatalog>(levelAsset.text);
            levelsById = new Dictionary<int, MahjongLevelDefinition>(catalog.levels.Count);
            List<MahjongLevelDefinition> sortedLevels = new List<MahjongLevelDefinition>(catalog.levels);
            sortedLevels.Sort((left, right) => left.level.CompareTo(right.level));
            for (int i = 0; i < sortedLevels.Count; i++)
            {
                MahjongLevelDefinition levelDefinition = sortedLevels[i];
                levelsById[levelDefinition.level] = levelDefinition;
            }

            int fallbackStartIndex = Mathf.Min(ExcludedFallbackLevelCount, sortedLevels.Count - 1);
            fallbackLevels = sortedLevels.GetRange(fallbackStartIndex, sortedLevels.Count - fallbackStartIndex);
        }
    }
}
