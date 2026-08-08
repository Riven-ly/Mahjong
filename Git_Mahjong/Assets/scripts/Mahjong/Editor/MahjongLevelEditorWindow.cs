using System;
using System.Collections.Generic;
using MahjongGame.GameLogic;
using MahjongGame.Model;
using MahjongGame.View;
using UnityEditor;
using UnityEngine;

namespace MahjongGame.EditorTools
{
    /// <summary>
    /// 支持牌型面板、分层网格编辑、撤销与严格保存的麻将关卡编辑窗口。
    /// </summary>
    public sealed class MahjongLevelEditorWindow : EditorWindow
    {
        private MahjongLevelCatalog catalog; // 当前从 JSON 读取的关卡目录
        private MahjongLevelEditorState state; // 支持撤销的当前关卡编辑状态
        private Vector2 windowScroll; // 窗口主滚动位置
        private Vector2 gridScroll; // 网格区域滚动位置
        private int selectedLevelIndex; // 当前目录关卡索引
        private int newTypeId = 9; // 待新增卡牌类型ID
        private float cellSize = CellCardWidth * 0.5f; // 编辑器网格单元格显示宽度
        private float iconNativeSizeScale = 0.2f; // 图标原生尺寸缩放比例
        private bool showLowerLayers = true; // 是否半透明显示低层卡牌
        private bool editHalfGrid; // 是否以当前层半格网格显示和编辑
        private string statusMessage = "尚未校验"; // 当前校验或保存状态提示
        private MessageType statusType = MessageType.Info; // 当前状态提示类型
        private bool painting; // 是否正在拖动绘制或擦除
        private bool erasing; // 当前拖动操作是否为擦除
        private readonly HashSet<int> paintedCells = new HashSet<int>(); // 本次拖动已处理的网格单元格
        private static Sprite defaultCardBackgroundSprite; // 关卡编辑器使用的默认卡牌背景图片
        private const float CellCardWidth = 171f; // Cell卡牌原始宽度
        private const float CellCardHeight = 199f; // Cell卡牌原始高度

        /// <summary>
        /// 打开麻将关卡编辑器窗口。
        /// </summary>
        [MenuItem("Tools/麻将/关卡编辑器")]
        public static void OpenWindow()
        {
            MahjongLevelEditorWindow window = GetWindow<MahjongLevelEditorWindow>("Mahjong Level Editor");
            window.minSize = new Vector2(760f, 620f);
            window.Show();
        }

        /// <summary>
        /// 创建临时编辑状态、加载目录并注册撤销回调。
        /// </summary>
        private void OnEnable()
        {
            if (state == null)
            {
                state = CreateInstance<MahjongLevelEditorState>();
                state.hideFlags = HideFlags.HideAndDontSave;
            }

            Undo.undoRedoPerformed += HandleUndoRedo;
            ReloadCatalog(false);
        }

        /// <summary>
        /// 注销撤销回调并销毁临时编辑状态。
        /// </summary>
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
            if (state != null)
            {
                DestroyImmediate(state);
            }
        }

        /// <summary>
        /// 绘制关卡选择、牌型面板、分层工具栏、网格和保存操作。
        /// </summary>
        private void OnGUI()
        {
            if (catalog == null || state == null)
            {
                EditorGUILayout.HelpBox("关卡目录加载失败。", MessageType.Error);
                return;
            }

            DrawHeaderToolbar();
            windowScroll = EditorGUILayout.BeginScrollView(windowScroll);
            DrawLevelSettings();
            DrawPalette();
            DrawLayerToolbar();
            DrawGrid();
            DrawSummary();
            EditorGUILayout.EndScrollView();
            DrawFooter();
        }

        /// <summary>
        /// 绘制目录刷新、关卡选择、新建和删除工具栏。
        /// </summary>
        private void DrawHeaderToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    ReloadCatalog(true);
                }

                string[] levelNames = new string[catalog.levels.Count];
                for (int i = 0; i < catalog.levels.Count; i++)
                {
                    levelNames[i] = $"Level {catalog.levels[i].level}";
                }

                int newIndex = EditorGUILayout.Popup(selectedLevelIndex, levelNames, EditorStyles.toolbarPopup, GUILayout.Width(130f));
                if (newIndex != selectedLevelIndex && ConfirmLeaveDirtyState())
                {
                    LoadLevel(newIndex);
                }

                if (GUILayout.Button("New Level", EditorStyles.toolbarButton, GUILayout.Width(80f)) && ConfirmLeaveDirtyState())
                {
                    CreateNewLevel();
                }

                using (new EditorGUI.DisabledScope(state.originalLevel == 0 || catalog.levels.Count <= 1))
                {
                    if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(60f)) &&
                        EditorUtility.DisplayDialog("Delete Level", $"确认删除 Level {state.originalLevel}？", "Delete", "Cancel"))
                    {
                        DeleteCurrentLevel();
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label(EditorUtility.IsDirty(state) ? "● Unsaved" : "Saved", EditorStyles.miniLabel);
            }
        }

        /// <summary>
        /// 绘制关卡编号、网格尺寸、卡牌数量和缩放选项。
        /// </summary>
        private void DrawLevelSettings()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);
            int level = EditorGUILayout.IntField("Level", state.level, GUILayout.Width(180f));
            if (level != state.level)
            {
                RecordState("Change Level ID");
                state.level = level;
            }

            int columns = EditorGUILayout.IntField("Columns", state.gridColumnCount, GUILayout.Width(180f));
            int rows = EditorGUILayout.IntField("Rows", state.gridRowCount, GUILayout.Width(180f));
            if (columns != state.gridColumnCount || rows != state.gridRowCount)
            {
                ResizeGrid(Mathf.Max(1, columns), Mathf.Max(1, rows));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Cards: {state.cards.Count}", GUILayout.Width(100f));
                EditorGUILayout.LabelField($"Layers: {state.GetLayerCount()}", GUILayout.Width(100f));
                showLowerLayers = EditorGUILayout.ToggleLeft("Show Lower Layers", showLowerLayers, GUILayout.Width(140f));
                editHalfGrid = GUILayout.Toggle(editHalfGrid, "Half Grid", EditorStyles.miniButton, GUILayout.Width(80f));
                GUILayout.Label("Zoom", GUILayout.Width(36f));
                float gridZoom = cellSize / CellCardWidth;
                gridZoom = GUILayout.HorizontalSlider(gridZoom, 0.25f, 1.5f, GUILayout.Width(160f));
                cellSize = CellCardWidth * gridZoom;
                //GUILayout.Label("Icon Scale", GUILayout.Width(65f));
                //iconNativeSizeScale = GUILayout.HorizontalSlider(iconNativeSizeScale, 0.01f, 1f, GUILayout.Width(120f));
                //GUILayout.Label(iconNativeSizeScale.ToString("0.00"), GUILayout.Width(30f));
            }
        }

        /// <summary>
        /// 绘制可增删的全部卡牌类型面板。
        /// </summary>
        private void DrawPalette()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Card Palette", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                newTypeId = EditorGUILayout.IntField(newTypeId, GUILayout.Width(48f));
                if (GUILayout.Button("Add", GUILayout.Width(44f)))
                {
                    AddCardType();
                }

                using (new EditorGUI.DisabledScope(state.selectedTypeId <= 0))
                {
                    if (GUILayout.Button("Remove Selected", GUILayout.Width(112f)))
                    {
                        RemoveSelectedCardType();
                    }
                }
            }

            const float paletteCellWidth = CellCardWidth * 0.5f;
            const float paletteCellHeight = CellCardHeight * 0.5f;
            int cellsPerRow = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 32f) / (paletteCellWidth + 4f)));
            for (int startIndex = 0; startIndex < state.cardTypeIds.Count; startIndex += cellsPerRow)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    int endIndex = Mathf.Min(startIndex + cellsPerRow, state.cardTypeIds.Count);
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        int typeId = state.cardTypeIds[i];
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(paletteCellWidth)))
                        {
                            bool selected = state.selectedTypeId == typeId;
                            Rect cardRect = GUILayoutUtility.GetRect(paletteCellWidth, paletteCellHeight, GUILayout.Width(paletteCellWidth), GUILayout.Height(paletteCellHeight));
                            if (GUI.Button(cardRect, GUIContent.none, GUIStyle.none))
                            {
                                RecordState("Select Card Type");
                                state.selectedTypeId = typeId;
                            }

                            DrawCard(cardRect, typeId, 1f);

                            if (selected)
                            {
                                DrawBorder(cardRect, new Color(0.2f, 0.65f, 1f), 2f);
                            }
                            GUILayout.Label(typeId.ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(paletteCellWidth));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 绘制层级切换、新增、清空和删除最高层操作。
        /// </summary>
        private void DrawLayerToolbar()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                int layerCount = state.GetLayerCount();
                for (int layer = 0; layer < layerCount; layer++)
                {
                    GUIStyle style = state.currentLayer == layer ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                    if (GUILayout.Button($"Layer {layer}", style, GUILayout.Width(70f)))
                    {
                        RecordState("Change Layer");
                        state.currentLayer = layer;
                    }
                }

                using (new EditorGUI.DisabledScope(layerCount >= MahjongConfig.MaxLayerCount))
                {
                    if (GUILayout.Button("+ Layer", GUILayout.Width(64f)))
                    {
                        RecordState("Add Layer");
                        state.currentLayer = layerCount;
                    }
                }

                if (GUILayout.Button("Clear", GUILayout.Width(52f)) &&
                    EditorUtility.DisplayDialog("Clear Layer", $"清空 Layer {state.currentLayer}？", "Clear", "Cancel"))
                {
                    RecordState("Clear Layer");
                    state.ClearLayer(state.currentLayer);
                }

                if (GUILayout.Button("Delete Highest", GUILayout.Width(100f)) &&
                    EditorUtility.DisplayDialog("Delete Layer", "删除最高层及其中全部卡牌？", "Delete", "Cancel"))
                {
                    RecordState("Delete Highest Layer");
                    state.DeleteHighestLayer();
                }
            }
        }

        /// <summary>
        /// 绘制当前层的完整格或半格中心坐标网格。
        /// </summary>
        private void DrawGrid()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"{(editHalfGrid ? "Half Grid" : "Grid")} — Editing Layer {state.currentLayer}", EditorStyles.boldLabel);
            const float headerSize = 32f;
            float gridCellHeight = cellSize * CellCardHeight / CellCardWidth;
            float halfCellWidth = editHalfGrid ? cellSize * 0.5f : cellSize;
            float halfCellHeight = editHalfGrid ? gridCellHeight * 0.5f : gridCellHeight;
            int halfColumnCount = editHalfGrid ? (state.gridColumnCount - 1) * MahjongLayoutGeometry.HalfGridUnitsPerCell + 1 : state.gridColumnCount;
            int halfRowCount = editHalfGrid ? (state.gridRowCount - 1) * MahjongLayoutGeometry.HalfGridUnitsPerCell + 1 : state.gridRowCount;
            float width = headerSize + (halfColumnCount - 1) * halfCellWidth + cellSize;
            float height = headerSize + (halfRowCount - 1) * halfCellHeight + gridCellHeight;
            gridScroll = EditorGUILayout.BeginScrollView(gridScroll, GUILayout.Height(Mathf.Min(height + 20f, 900f)));
            Rect gridRect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(gridRect, new Color(0.13f, 0.13f, 0.13f));
            Vector2 baseOrigin = new Vector2(gridRect.x + headerSize, gridRect.y + headerSize);

            for (int columnIndex = 0; columnIndex < halfColumnCount; columnIndex++)
            {
                int coordY = editHalfGrid ? columnIndex : columnIndex * MahjongLayoutGeometry.HalfGridUnitsPerCell;
                Rect headerRect = new Rect(baseOrigin.x + columnIndex * halfCellWidth, gridRect.y, cellSize, headerSize);
                GUI.Label(headerRect, coordY.ToString(), EditorStyles.centeredGreyMiniLabel);
            }

            for (int displayRow = 0; displayRow < halfRowCount; displayRow++)
            {
                int rowIndex = halfRowCount - 1 - displayRow;
                int coordX = editHalfGrid ? rowIndex : rowIndex * MahjongLayoutGeometry.HalfGridUnitsPerCell;
                float rowY = baseOrigin.y + displayRow * halfCellHeight;
                Rect headerRect = new Rect(gridRect.x, rowY, headerSize, gridCellHeight);
                GUI.Label(headerRect, coordX.ToString(), EditorStyles.centeredGreyMiniLabel);
                for (int columnIndex = 0; columnIndex < halfColumnCount; columnIndex++)
                {
                    int coordY = editHalfGrid ? columnIndex : columnIndex * MahjongLayoutGeometry.HalfGridUnitsPerCell;
                    Rect cellRect = new Rect(baseOrigin.x + columnIndex * halfCellWidth, rowY, cellSize, gridCellHeight);
                    EditorGUI.DrawRect(cellRect, editHalfGrid && (coordY % MahjongLayoutGeometry.HalfGridUnitsPerCell != 0 || coordX % MahjongLayoutGeometry.HalfGridUnitsPerCell != 0) ? new Color(0.17f, 0.22f, 0.29f) : new Color(0.2f, 0.2f, 0.2f));
                    DrawBorder(cellRect, new Color(0.32f, 0.32f, 0.32f), 1f);
                }
            }

            if (showLowerLayers)
            {
                for (int i = 0; i < state.cards.Count; i++)
                {
                    MahjongLevelCardDefinition card = state.cards[i];
                    if (card.layer < state.currentLayer)
                    {
                        DrawCard(GetCardRect(baseOrigin, gridCellHeight * 0.5f, card), card.typeId, 0.28f);
                    }
                }
            }

            for (int i = 0; i < state.cards.Count; i++)
            {
                MahjongLevelCardDefinition card = state.cards[i];
                if (card.layer != state.currentLayer)
                {
                    continue;
                }

                Rect cardRect = GetCardRect(baseOrigin, gridCellHeight * 0.5f, card);
                DrawCard(cardRect, card.typeId, 1f);
                if (IsBlocked(card))
                {
                    DrawBorder(cardRect, new Color(1f, 0.35f, 0.2f), 3f);
                }
            }

            Rect contentRect = new Rect(baseOrigin.x, baseOrigin.y, (halfColumnCount - 1) * halfCellWidth + cellSize, (halfRowCount - 1) * halfCellHeight + gridCellHeight);
            HandleGridInput(contentRect, halfColumnCount, halfRowCount);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 获取指定卡牌按精确半格中心坐标绘制的编辑器区域。
        /// </summary>
        private Rect GetCardRect(Vector2 baseOrigin, float halfCellHeight, MahjongLevelCardDefinition card)
        {
            int coordY = MahjongLayoutGeometry.GetCenterColumnInHalfGridUnits(card);
            int coordX = MahjongLayoutGeometry.GetCenterRowInHalfGridUnits(card);
            return new Rect(
                baseOrigin.x + coordY * cellSize * 0.5f,
                baseOrigin.y + ((state.gridRowCount - 1) * MahjongLayoutGeometry.HalfGridUnitsPerCell - coordX) * halfCellHeight,
                cellSize,
                cellSize * CellCardHeight / CellCardWidth);
        }

        /// <summary>
        /// 使用默认背景和配置图片绘制卡牌；图片缺失时显示类型颜色和数字占位。
        /// </summary>
        private void DrawCard(Rect cellRect, int typeId, float alpha)
        {
            Rect cardRect = new Rect(cellRect.x + 4f, cellRect.y + 4f, cellRect.width - 8f, cellRect.height - 8f);
            Sprite cardSprite = MahjongCardVisualCatalogLoader.GetSprite(typeId);
            Color backgroundColor = cardSprite == null ? MahjongCardColorUtility.GetColor(typeId) : Color.white;
            backgroundColor.a = alpha;
            DrawSprite(cardRect, GetDefaultCardBackgroundSprite(), backgroundColor);

            if (cardSprite != null)
            {
                DrawSpriteNativeCentered(cardRect, cardSprite, new Color(1f, 1f, 1f, alpha));
                return;
            }

            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(cellRect.width * 0.32f, 12f, 30f)),
                normal = { textColor = new Color(0.1f, 0.1f, 0.1f, alpha) }
            };
            GUI.Label(cardRect, typeId.ToString(), labelStyle);
        }

        /// <summary>
        /// 获取关卡编辑器绘制卡牌时使用的默认背景图片。
        /// </summary>
        private static Sprite GetDefaultCardBackgroundSprite()
        {
            if (defaultCardBackgroundSprite == null)
            {
                defaultCardBackgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Texture/Mahjong/bg/di.png");
            }

            return defaultCardBackgroundSprite;
        }

        /// <summary>
        /// 使用原生尺寸并随网格缩放在指定区域中心绘制卡牌图标，并应用颜色。
        /// </summary>
        private void DrawSpriteNativeCentered(Rect targetRect, Sprite sprite, Color color)
        {
            Rect textureRect = sprite.textureRect;
            float iconScale = targetRect.width / CellCardWidth * iconNativeSizeScale;
            float iconWidth = textureRect.width * iconScale;
            float iconHeight = textureRect.height * iconScale;
            Rect nativeRect = new Rect(
                targetRect.center.x - iconWidth * 0.5f,
                targetRect.center.y - iconHeight * 0.5f,
                iconWidth,
                iconHeight);
            DrawSprite(nativeRect, sprite, color);
        }

        /// <summary>
        /// 在指定区域内按原始比例居中绘制 Sprite，并应用颜色。
        /// </summary>
        private static void DrawSprite(Rect targetRect, Sprite sprite, Color color)
        {
            Rect textureRect = sprite.textureRect;
            float scale = Mathf.Min(targetRect.width / textureRect.width, targetRect.height / textureRect.height);
            Rect drawRect = new Rect(
                targetRect.x + (targetRect.width - textureRect.width * scale) * 0.5f,
                targetRect.y + (targetRect.height - textureRect.height * scale) * 0.5f,
                textureRect.width * scale,
                textureRect.height * scale);
            Rect textureCoordinates = new Rect(
                textureRect.x / sprite.texture.width,
                textureRect.y / sprite.texture.height,
                textureRect.width / sprite.texture.width,
                textureRect.height / sprite.texture.height);
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, textureCoordinates, true);
            GUI.color = previousColor;
        }

        /// <summary>
        /// 处理网格左键绘制、右键擦除、Shift吸取和拖动连续操作。
        /// </summary>
        private void HandleGridInput(Rect contentRect, int halfColumnCount, int halfRowCount)
        {
            Event currentEvent = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            if (currentEvent.type == EventType.MouseDown && contentRect.Contains(currentEvent.mousePosition) && (currentEvent.button == 0 || currentEvent.button == 1))
            {
                painting = true;
                erasing = currentEvent.button == 1;
                paintedCells.Clear();
                GUIUtility.hotControl = controlId;
                Undo.RegisterCompleteObjectUndo(state, erasing ? "Erase Mahjong Cards" : "Paint Mahjong Cards");
                PaintGridCell(contentRect, currentEvent.mousePosition, currentEvent.shift, halfColumnCount, halfRowCount);
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && painting && GUIUtility.hotControl == controlId)
            {
                PaintGridCell(contentRect, currentEvent.mousePosition, false, halfColumnCount, halfRowCount);
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp && painting && GUIUtility.hotControl == controlId)
            {
                painting = false;
                paintedCells.Clear();
                GUIUtility.hotControl = 0;
                EditorUtility.SetDirty(state);
                currentEvent.Use();
            }
        }

        /// <summary>
        /// 将鼠标位置转换为当前网格模式对应的中心坐标，并执行本次绘制、擦除或吸取。
        /// </summary>
        private void PaintGridCell(Rect contentRect, Vector2 mousePosition, bool pickType, int halfColumnCount, int halfRowCount)
        {
            if (!contentRect.Contains(mousePosition))
            {
                return;
            }

            float halfCellWidth = editHalfGrid ? cellSize * 0.5f : cellSize;
            float halfCellHeight = editHalfGrid ? cellSize * CellCardHeight / CellCardWidth * 0.5f : cellSize * CellCardHeight / CellCardWidth;
            int columnIndex = Mathf.Clamp(Mathf.RoundToInt((mousePosition.x - contentRect.x) / halfCellWidth), 0, halfColumnCount - 1);
            int displayRow = Mathf.Clamp(Mathf.RoundToInt((mousePosition.y - contentRect.y) / halfCellHeight), 0, halfRowCount - 1);
            int rowIndex = halfRowCount - 1 - displayRow;
            int coordY = editHalfGrid ? columnIndex : columnIndex * MahjongLayoutGeometry.HalfGridUnitsPerCell;
            int coordX = editHalfGrid ? rowIndex : rowIndex * MahjongLayoutGeometry.HalfGridUnitsPerCell;
            int cellKey = coordX * halfColumnCount + coordY;
            if (!paintedCells.Add(cellKey))
            {
                return;
            }

            MahjongLevelCardDefinition card = state.GetCard(state.currentLayer, coordY, coordX);
            if (pickType)
            {
                if (card != null)
                {
                    state.selectedTypeId = card.typeId;
                }
                return;
            }

            if (erasing)
            {
                state.RemoveCard(state.currentLayer, coordY, coordX);
            }
            else if (state.selectedTypeId > 0)
            {
                state.SetCard(state.currentLayer, coordY, coordX, state.selectedTypeId);
            }

            EditorUtility.SetDirty(state);
            Repaint();
        }

        /// <summary>
        /// 绘制当前关卡每种牌数量及校验提示。
        /// </summary>
        private void DrawSummary()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Type Counts", EditorStyles.boldLabel);
            var typeCounts = new Dictionary<int, int>();
            for (int i = 0; i < state.cards.Count; i++)
            {
                typeCounts.TryGetValue(state.cards[i].typeId, out int count);
                typeCounts[state.cards[i].typeId] = count + 1;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (KeyValuePair<int, int> pair in typeCounts)
                {
                    Color previousColor = GUI.color;
                    GUI.color = pair.Value % MahjongConfig.MatchCount == 0 ? Color.white : new Color(1f, 0.45f, 0.35f);
                    GUILayout.Label($"Type {pair.Key}: {pair.Value}", EditorStyles.helpBox, GUILayout.Width(88f));
                    GUI.color = previousColor;
                }
            }
        }

        /// <summary>
        /// 绘制校验、求解、保存按钮和状态提示。
        /// </summary>
        private void DrawFooter()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(statusMessage, statusType);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate & Solve", GUILayout.Height(30f)))
                {
                    ValidateCurrentLevel();
                }

                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                {
                    if (GUILayout.Button("Save / Update JSON", GUILayout.Height(30f)))
                    {
                        SaveCurrentLevel();
                    }
                }
            }

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode 中禁止保存关卡。", MessageType.Warning);
            }
        }

        /// <summary>
        /// 修改网格尺寸；缩小时要求用户确认裁剪越界卡牌。
        /// </summary>
        private void ResizeGrid(int columns, int rows)
        {
            bool hasOutsideCards = false;
            int maximumColumnHalf = (columns - 1) * MahjongLayoutGeometry.HalfGridUnitsPerCell;
            int maximumRowHalf = (rows - 1) * MahjongLayoutGeometry.HalfGridUnitsPerCell;
            for (int i = 0; i < state.cards.Count; i++)
            {
                MahjongLevelCardDefinition card = state.cards[i];
                int coordY = MahjongLayoutGeometry.GetCenterColumnInHalfGridUnits(card);
                int coordX = MahjongLayoutGeometry.GetCenterRowInHalfGridUnits(card);
                if (coordY < 0 || coordY > maximumColumnHalf ||
                    coordX < 0 || coordX > maximumRowHalf)
                {
                    hasOutsideCards = true;
                    break;
                }
            }

            if (hasOutsideCards && !EditorUtility.DisplayDialog("Resize Grid", "缩小网格会删除越界卡牌，是否继续？", "Crop", "Cancel"))
            {
                return;
            }

            RecordState("Resize Mahjong Grid");
            state.gridColumnCount = columns;
            state.gridRowCount = rows;
            if (hasOutsideCards)
            {
                state.CropOutsideGrid();
            }
        }

        /// <summary>
        /// 向牌型面板添加新的正整数类型ID。
        /// </summary>
        private void AddCardType()
        {
            if (newTypeId <= 0 || state.cardTypeIds.Contains(newTypeId))
            {
                SetStatus("TypeId 必须为未使用的正整数。", MessageType.Error);
                return;
            }

            RecordState("Add Mahjong Card Type");
            state.cardTypeIds.Add(newTypeId);
            state.cardTypeIds.Sort();
            state.selectedTypeId = newTypeId;
            newTypeId++;
        }

        /// <summary>
        /// 从牌型面板删除当前牌型；任一关卡仍使用时拒绝删除。
        /// </summary>
        private void RemoveSelectedCardType()
        {
            int typeId = state.selectedTypeId;
            if (IsTypeUsed(typeId))
            {
                SetStatus($"Type {typeId} 仍被关卡使用，无法删除。", MessageType.Error);
                return;
            }

            RecordState("Remove Mahjong Card Type");
            state.cardTypeIds.Remove(typeId);
            state.selectedTypeId = state.cardTypeIds.Count > 0 ? state.cardTypeIds[0] : 0;
        }

        /// <summary>
        /// 判断指定牌型是否被当前编辑关卡或目录中其他关卡使用。
        /// </summary>
        private bool IsTypeUsed(int typeId)
        {
            for (int i = 0; i < state.cards.Count; i++)
            {
                if (state.cards[i].typeId == typeId)
                {
                    return true;
                }
            }

            for (int levelIndex = 0; levelIndex < catalog.levels.Count; levelIndex++)
            {
                MahjongLevelDefinition level = catalog.levels[levelIndex];
                if (level.level == state.originalLevel)
                {
                    continue;
                }

                for (int cardIndex = 0; cardIndex < level.cards.Count; cardIndex++)
                {
                    if (level.cards[cardIndex].typeId == typeId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 严格校验当前编辑关卡并执行可解性搜索。
        /// </summary>
        private bool ValidateCurrentLevel()
        {
            try
            {
                MahjongLevelDefinition levelDefinition = state.CreateLevelDefinition();
                MahjongLevelValidator.Validate(levelDefinition);
                if (!MahjongLevelSolver.TrySolve(levelDefinition, out List<int> solution))
                {
                    throw new InvalidOperationException("当前关卡无法按玩法规则通关。");
                }

                SetStatus($"校验通过：{levelDefinition.cards.Count} 张牌，{solution.Count} 步可通关。", MessageType.Info);
                return true;
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
                return false;
            }
        }

        /// <summary>
        /// 严格校验并将当前关卡更新或追加到 JSON。
        /// </summary>
        private void SaveCurrentLevel()
        {
            try
            {
                int savedLevel = state.level;
                MahjongLevelCatalogRepository.SaveLevel(state);
                catalog = MahjongLevelCatalogRepository.LoadCatalog();
                selectedLevelIndex = FindLevelIndex(savedLevel);
                LoadLevel(selectedLevelIndex);
                SetStatus("关卡已校验并写入 Levels.json。", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
            }
        }

        /// <summary>
        /// 重新读取 JSON 目录，并根据需要确认未保存修改。
        /// </summary>
        private void ReloadCatalog(bool confirmDirty)
        {
            if (confirmDirty && !ConfirmLeaveDirtyState())
            {
                return;
            }

            catalog = MahjongLevelCatalogRepository.LoadCatalog();
            if (catalog.cardTypeIds == null)
            {
                catalog.cardTypeIds = new List<int>();
            }

            if (catalog.levels.Count > 0)
            {
                selectedLevelIndex = Mathf.Clamp(selectedLevelIndex, 0, catalog.levels.Count - 1);
                LoadLevel(selectedLevelIndex);
            }
        }

        /// <summary>
        /// 加载指定目录索引关卡到可撤销编辑状态。
        /// </summary>
        private void LoadLevel(int levelIndex)
        {
            selectedLevelIndex = Mathf.Clamp(levelIndex, 0, catalog.levels.Count - 1);
            state.Load(catalog, catalog.levels[selectedLevelIndex]);
            EditorUtility.ClearDirty(state);
            SetStatus($"已加载 Level {state.level}。", MessageType.Info);
            Repaint();
        }

        /// <summary>
        /// 创建使用下一个关卡编号的空白关卡。
        /// </summary>
        private void CreateNewLevel()
        {
            int maxLevel = 0;
            for (int i = 0; i < catalog.levels.Count; i++)
            {
                maxLevel = Mathf.Max(maxLevel, catalog.levels[i].level);
            }

            state.CreateNew(catalog, maxLevel + 1);
            EditorUtility.SetDirty(state);
            SetStatus($"已创建未保存的 Level {state.level}。", MessageType.Info);
        }

        /// <summary>
        /// 删除当前已保存关卡并重新加载目录。
        /// </summary>
        private void DeleteCurrentLevel()
        {
            try
            {
                MahjongLevelCatalogRepository.DeleteLevel(state.originalLevel);
                selectedLevelIndex = 0;
                ReloadCatalog(false);
                SetStatus("关卡已删除。", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
            }
        }

        /// <summary>
        /// 离开脏状态前提供保存、丢弃和取消选项。
        /// </summary>
        private bool ConfirmLeaveDirtyState()
        {
            if (!EditorUtility.IsDirty(state))
            {
                return true;
            }

            int option = EditorUtility.DisplayDialogComplex("Unsaved Level", "当前关卡有未保存修改。", "Save", "Cancel", "Discard");
            if (option == 0)
            {
                SaveCurrentLevel();
                return !EditorUtility.IsDirty(state);
            }

            return option == 2;
        }

        /// <summary>
        /// 判断指定卡牌是否被更高层覆盖。
        /// </summary>
        private bool IsBlocked(MahjongLevelCardDefinition card)
        {
            for (int i = 0; i < state.cards.Count; i++)
            {
                MahjongLevelCardDefinition other = state.cards[i];
                if (other != card && other.layer > card.layer && MahjongLayoutGeometry.HasAreaOverlap(card, other))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 绘制指定颜色和宽度的矩形边框。
        /// </summary>
        private static void DrawBorder(Rect rect, Color color, float width)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, width, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
        }

        /// <summary>
        /// 记录一次可撤销状态修改并标记状态为脏。
        /// </summary>
        private void RecordState(string operationName)
        {
            Undo.RegisterCompleteObjectUndo(state, operationName);
            EditorUtility.SetDirty(state);
        }

        /// <summary>
        /// 处理撤销重做后重新绘制窗口。
        /// </summary>
        private void HandleUndoRedo()
        {
            Repaint();
        }

        /// <summary>
        /// 查找指定关卡编号在当前目录中的索引。
        /// </summary>
        private int FindLevelIndex(int level)
        {
            for (int i = 0; i < catalog.levels.Count; i++)
            {
                if (catalog.levels[i].level == level)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// 更新窗口底部状态提示。
        /// </summary>
        private void SetStatus(string message, MessageType messageType)
        {
            statusMessage = message;
            statusType = messageType;
            Repaint();
        }
    }
}
