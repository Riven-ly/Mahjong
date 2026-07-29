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
        private static MahjongLevelCatalog catalog; // 已加载的关卡目录缓存
        private static Dictionary<int, MahjongLevelDefinition> levelsById; // 关卡编号到配置的查询缓存

        /// <summary>
        /// 按玩家等级获取关卡；缺失对应等级时使用随机种子随机选择已配置关卡。
        /// </summary>
        public static MahjongLevelDefinition GetLevel(int playerLevel, int randomSeed)
        {
            EnsureLoaded();
            if (levelsById.TryGetValue(playerLevel, out MahjongLevelDefinition levelDefinition))
            {
                return levelDefinition;
            }

            int randomIndex = new System.Random(randomSeed).Next(catalog.levels.Count);
            MahjongLevelDefinition fallbackLevel = catalog.levels[randomIndex];
            Debug.LogWarning($"未找到玩家等级{playerLevel}对应关卡，随机使用关卡{fallbackLevel.level}。");
            return fallbackLevel;
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
            for (int i = 0; i < catalog.levels.Count; i++)
            {
                MahjongLevelDefinition levelDefinition = catalog.levels[i];
                levelsById[levelDefinition.level] = levelDefinition;
            }
        }
    }
}
