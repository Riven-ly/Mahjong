using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame.View
{
    /// <summary>
    /// 单个麻将卡牌类型的图片映射配置。
    /// </summary>
    [Serializable]
    public sealed class MahjongCardVisualEntry
    {
        public int typeId = 1; // 卡牌类型ID
        public Sprite sprite; // 卡牌对应图片
    }

    /// <summary>
    /// 为运行时与关卡编辑器提供共享的卡牌类型图片映射。
    /// </summary>
    [CreateAssetMenu(fileName = "MahjongCardVisualCatalog", menuName = "Mahjong/Card Visual Catalog")]
    public sealed class MahjongCardVisualCatalog : ScriptableObject
    {
        [SerializeField] private List<MahjongCardVisualEntry> entries = new List<MahjongCardVisualEntry>(); // 卡牌类型图片映射列表

        private Dictionary<int, Sprite> spriteByTypeId; // 运行时卡牌类型图片查询缓存

        /// <summary>
        /// 根据卡牌类型ID查找已配置的图片；未配置时返回空。
        /// </summary>
        public Sprite GetSprite(int typeId)
        {
            EnsureCache();
            return spriteByTypeId.TryGetValue(typeId, out Sprite sprite) ? sprite : null;
        }

        /// <summary>
        /// 在配置修改后清除查询缓存。
        /// </summary>
        private void OnValidate()
        {
            spriteByTypeId = null;
        }

        /// <summary>
        /// 按当前映射列表构建卡牌类型图片查询缓存。
        /// </summary>
        private void EnsureCache()
        {
            if (spriteByTypeId != null)
            {
                return;
            }

            spriteByTypeId = new Dictionary<int, Sprite>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                MahjongCardVisualEntry entry = entries[i];
                if (entry != null && entry.typeId > 0 && entry.sprite != null)
                {
                    spriteByTypeId[entry.typeId] = entry.sprite;
                }
            }
        }
    }

    /// <summary>
    /// 加载并缓存共享的麻将卡牌图片目录。
    /// </summary>
    public static class MahjongCardVisualCatalogLoader
    {
        private const string ResourcePath = "Mahjong/MahjongCardVisualCatalog"; // Resources卡牌图片目录加载路径
        private static MahjongCardVisualCatalog cachedCatalog; // 已加载的卡牌图片目录

        /// <summary>
        /// 获取共享卡牌图片目录；资源不存在时返回空。
        /// </summary>
        public static MahjongCardVisualCatalog Load()
        {
            if (cachedCatalog == null)
            {
                cachedCatalog = Resources.Load<MahjongCardVisualCatalog>(ResourcePath);
            }

            return cachedCatalog;
        }

        /// <summary>
        /// 根据卡牌类型ID查找图片；目录或映射缺失时返回空。
        /// </summary>
        public static Sprite GetSprite(int typeId)
        {
            MahjongCardVisualCatalog catalog = Load();
            return catalog != null ? catalog.GetSprite(typeId) : null;
        }
    }
}
