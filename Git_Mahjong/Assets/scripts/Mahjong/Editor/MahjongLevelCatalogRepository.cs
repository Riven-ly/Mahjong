using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MahjongGame.Model;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace MahjongGame.EditorTools
{
    /// <summary>
    /// 麻将关卡编辑器的 JSON 目录读取、克隆和保存仓库。
    /// </summary>
    public static class MahjongLevelCatalogRepository
    {
        public const string CatalogAssetPath = "Assets/Resources/Mahjong/Levels.json"; // 关卡目录资产路径

        /// <summary>
        /// 从磁盘读取关卡目录并返回可独立修改的副本。
        /// </summary>
        public static MahjongLevelCatalog LoadCatalog()
        {
            TextAsset levelAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogAssetPath);
            return JsonConvert.DeserializeObject<MahjongLevelCatalog>(levelAsset.text);
        }

        /// <summary>
        /// 严格校验并保存当前编辑关卡；相同编号关卡会被直接更新。
        /// </summary>
        public static void SaveLevel(MahjongLevelEditorState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            MahjongLevelCatalog catalog = LoadCatalog();
            catalog.cardTypeIds = new List<int>(state.cardTypeIds);
            MahjongLevelDefinition editedLevel = state.CreateLevelDefinition();
            editedLevel.cards.Sort(CompareCards);

            int originalIndex = FindLevelIndex(catalog, state.originalLevel);
            int targetIndex = FindLevelIndex(catalog, editedLevel.level);
            if (targetIndex >= 0)
            {
                catalog.levels[targetIndex] = editedLevel;
                if (originalIndex >= 0 && originalIndex != targetIndex)
                {
                    catalog.levels.RemoveAt(originalIndex);
                }
            }
            else if (originalIndex >= 0)
            {
                catalog.levels[originalIndex] = editedLevel;
            }
            else
            {
                catalog.levels.Add(editedLevel);
            }

            catalog.levels.Sort((left, right) => left.level.CompareTo(right.level));
            MahjongLevelCatalogEditorValidator.ValidateCatalog(catalog);
            WriteCatalog(catalog);
            state.originalLevel = editedLevel.level;
            EditorUtility.SetDirty(state);
        }

        /// <summary>
        /// 删除指定编号关卡并保存目录。目录中必须保留至少一个关卡。
        /// </summary>
        public static void DeleteLevel(int level)
        {
            MahjongLevelCatalog catalog = LoadCatalog();
            int levelIndex = FindLevelIndex(catalog, level);
            if (levelIndex < 0)
            {
                return;
            }

            catalog.levels.RemoveAt(levelIndex);
            MahjongLevelCatalogEditorValidator.ValidateCatalog(catalog);
            WriteCatalog(catalog);
        }

        /// <summary>
        /// 查找指定编号关卡在目录中的索引；不存在时返回负一。
        /// </summary>
        private static int FindLevelIndex(MahjongLevelCatalog catalog, int level)
        {
            for (int i = 0; i < catalog.levels.Count; i++)
            {
                if (catalog.levels[i].level == level)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 按层级、行和列稳定排序卡牌布局。
        /// </summary>
        private static int CompareCards(MahjongLevelCardDefinition left, MahjongLevelCardDefinition right)
        {
            int layerComparison = left.layer.CompareTo(right.layer);
            if (layerComparison != 0)
            {
                return layerComparison;
            }

            int rowComparison = left.row.CompareTo(right.row);
            return rowComparison != 0 ? rowComparison : left.column.CompareTo(right.column);
        }

        /// <summary>
        /// 将目录格式化写入 JSON 并触发 Unity 资源导入。
        /// </summary>
        private static void WriteCatalog(MahjongLevelCatalog catalog)
        {
            string json = JsonConvert.SerializeObject(catalog, Formatting.Indented);
            File.WriteAllText(CatalogAssetPath, json, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(CatalogAssetPath, ImportAssetOptions.ForceUpdate);
        }
    }
}
