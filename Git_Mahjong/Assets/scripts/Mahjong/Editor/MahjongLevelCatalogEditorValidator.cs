using System;
using System.Collections.Generic;
using MahjongGame.GameLogic;
using MahjongGame.Model;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace MahjongGame.EditorTools
{
    /// <summary>
    /// 在编辑器生成或修改关卡目录时执行结构与可解性校验。
    /// </summary>
    public sealed class MahjongLevelCatalogEditorValidator : AssetPostprocessor
    {
        private const string CatalogAssetPath = "Assets/Resources/Mahjong/Levels.json"; // 关卡目录资产路径
        private static bool validationScheduled; // 是否已安排延迟校验

        /// <summary>
        /// 通过编辑器菜单手动校验全部关卡，并输出每关通关路径长度。
        /// </summary>
        [MenuItem("Tools/麻将/校验全部关卡")]
        public static void ValidateCatalogFromMenu()
        {
            ValidateCatalog();
        }

        /// <summary>
        /// 读取关卡目录并执行结构、边界与可解性校验。任一关卡失败时抛出异常。
        /// </summary>
        public static void ValidateCatalog()
        {
            TextAsset levelAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogAssetPath);
            if (levelAsset == null)
            {
                throw new InvalidOperationException($"关卡配置不存在：{CatalogAssetPath}");
            }

            MahjongLevelCatalog catalog = JsonConvert.DeserializeObject<MahjongLevelCatalog>(levelAsset.text);
            ValidateCatalog(catalog);
        }

        /// <summary>
        /// 校验内存中的完整关卡目录和每关可解性，供编辑器保存前调用。
        /// </summary>
        public static void ValidateCatalog(MahjongLevelCatalog catalog)
        {
            if (catalog == null || catalog.version != MahjongConfig.LevelCatalogVersion || catalog.levels == null || catalog.levels.Count == 0)
            {
                throw new InvalidOperationException("麻将关卡目录为空或版本不受支持。");
            }

            if (catalog.cardTypeIds == null || catalog.cardTypeIds.Count == 0)
            {
                throw new InvalidOperationException("麻将关卡目录未配置可用卡牌类型。");
            }

            var availableTypeIds = new HashSet<int>();
            for (int i = 0; i < catalog.cardTypeIds.Count; i++)
            {
                int typeId = catalog.cardTypeIds[i];
                if (typeId <= 0 || !availableTypeIds.Add(typeId))
                {
                    throw new InvalidOperationException($"卡牌类型面板存在无效或重复ID：{typeId}。");
                }
            }

            var levelIds = new HashSet<int>();
            for (int i = 0; i < catalog.levels.Count; i++)
            {
                MahjongLevelDefinition levelDefinition = catalog.levels[i];
                MahjongLevelValidator.Validate(levelDefinition);
                if (!levelIds.Add(levelDefinition.level))
                {
                    throw new InvalidOperationException($"麻将关卡目录重复配置关卡{levelDefinition.level}。");
                }

                for (int cardIndex = 0; cardIndex < levelDefinition.cards.Count; cardIndex++)
                {
                    if (!availableTypeIds.Contains(levelDefinition.cards[cardIndex].typeId))
                    {
                        throw new InvalidOperationException($"关卡{levelDefinition.level}使用了牌型面板中不存在的类型{levelDefinition.cards[cardIndex].typeId}。");
                    }
                }

                if (!MahjongLevelSolver.TrySolve(levelDefinition, out List<int> solutionCardIndexes))
                {
                    throw new InvalidOperationException($"关卡{levelDefinition.level}无法按当前玩法规则通关。");
                }

                Debug.Log($"关卡{levelDefinition.level}校验通过，卡牌数{levelDefinition.cards.Count}，求解步骤{solutionCardIndexes.Count}。");
            }
        }

        /// <summary>
        /// 关卡目录导入时安排一次延迟校验，避免在资源导入回调中重复执行搜索。
        /// </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool catalogImported = false;
            for (int i = 0; i < importedAssets.Length; i++)
            {
                if (importedAssets[i] == CatalogAssetPath)
                {
                    catalogImported = true;
                    break;
                }
            }

            if (!catalogImported || validationScheduled)
            {
                return;
            }

            validationScheduled = true;
            EditorApplication.delayCall += ValidateImportedCatalog;
        }

        /// <summary>
        /// 执行导入后的关卡校验，并将错误输出到 Console。
        /// </summary>
        private static void ValidateImportedCatalog()
        {
            validationScheduled = false;
            try
            {
                ValidateCatalog();
            }
            catch (Exception exception)
            {
                Debug.LogError($"麻将关卡配置校验失败：{exception.Message}");
            }
        }
    }
}
