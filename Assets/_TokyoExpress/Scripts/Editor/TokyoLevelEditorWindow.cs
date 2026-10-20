#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TokyoExpress.Core;
using TokyoExpress.Grid;
using UnityEditor;
using UnityEngine;

namespace TokyoExpress.GameEditor
{
    public class TokyoLevelEditorWindow : EditorWindow
    {
        private enum EditorTab
        {
            Levels,
            FieldElements,
            EditorSettings
        }

        private EditorTab currentTab = EditorTab.Levels;
        private List<LevelData> levelAssetList = new List<LevelData>();
        private int selectedLevelIndex = 0;
        private LevelData activeLevel;

        // Grid Interaction
        private Vector2 levelListScroll;
        private Vector2 gridScrollPos;
        private CellTileType selectedToolTile = CellTileType.StraightVertical;
        private int selectedToolRotation = 0;
        private const float CELL_GUI_SIZE = 24f;

        [MenuItem("Tokyo Express/Level Editor & Generator", false, 10)]
        public static void OpenWindow()
        {
            var window = GetWindow<TokyoLevelEditorWindow>("Tokyo Level Editor");
            window.minSize = new Vector2(780, 520);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshLevelsList();
        }

        private void RefreshLevelsList()
        {
            levelAssetList.Clear();
            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (data != null) levelAssetList.Add(data);
            }

            if (levelAssetList.Count > 0 && selectedLevelIndex < levelAssetList.Count)
            {
                activeLevel = levelAssetList[selectedLevelIndex];
            }
        }

        private void OnGUI()
        {
            DrawHeaderTabs();

            EditorGUILayout.Space(5);

            switch (currentTab)
            {
                case EditorTab.Levels:
                    DrawLevelsTab();
                    break;
                case EditorTab.FieldElements:
                    DrawFieldElementsTab();
                    break;
                case EditorTab.EditorSettings:
                    DrawSettingsTab();
                    break;
            }
        }

        private void DrawHeaderTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Toggle(currentTab == EditorTab.Levels, "Levels", EditorStyles.toolbarButton))
                currentTab = EditorTab.Levels;
            if (GUILayout.Toggle(currentTab == EditorTab.FieldElements, "Field elements", EditorStyles.toolbarButton))
                currentTab = EditorTab.FieldElements;
            if (GUILayout.Toggle(currentTab == EditorTab.EditorSettings, "Editor Settings", EditorStyles.toolbarButton))
                currentTab = EditorTab.EditorSettings;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLevelsTab()
        {
            EditorGUILayout.BeginHorizontal();

            // Left Side: Levels List
            DrawLevelListColumn(180);

            // Right Side: Grid Inspector & Procedural Generator
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (activeLevel != null)
            {
                DrawLevelProperties(activeLevel);
                DrawInteractiveGrid(activeLevel);
                DrawBottomActionToolbar(activeLevel);
            }
            else
            {
                EditorGUILayout.HelpBox("Aucun niveau sélectionné. Cliquez sur '+' pour créer un niveau.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLevelListColumn(float width)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(width));
            EditorGUILayout.LabelField("Levels Database", EditorStyles.boldLabel);

            levelListScroll = EditorGUILayout.BeginScrollView(levelListScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < levelAssetList.Count; i++)
            {
                var lvl = levelAssetList[i];
                string label = $"#{lvl.levelId:00} - {lvl.levelName}";
                bool isSelected = (i == selectedLevelIndex);

                GUI.backgroundColor = isSelected ? new Color(0.25f, 0.5f, 0.9f) : Color.white;
                if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Height(22)))
                {
                    selectedLevelIndex = i;
                    activeLevel = lvl;
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ New", EditorStyles.miniButtonLeft))
            {
                CreateNewLevel();
            }
            if (GUILayout.Button("- Delete", EditorStyles.miniButtonRight))
            {
                DeleteActiveLevel();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawLevelProperties(LevelData level)
        {
            EditorGUILayout.BeginHorizontal();
            level.levelName = EditorGUILayout.TextField("Nom du Niveau", level.levelName);
            level.levelId = EditorGUILayout.IntField("ID", level.levelId, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            level.packageType = (PackageType)EditorGUILayout.EnumPopup("Type de Colis", level.packageType);
            level.timeLimit = EditorGUILayout.FloatField("Timer (s)", level.timeLimit);
            level.rewardCoins = EditorGUILayout.IntField("Coins", level.rewardCoins);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            level.gridSize = EditorGUILayout.Vector2IntField("Grid Size", level.gridSize);
            level.cellSize = EditorGUILayout.FloatField("Cell Size (m)", level.cellSize);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            level.startCoord = EditorGUILayout.Vector2IntField("Start Station", level.startCoord);
            level.destinationCoord = EditorGUILayout.Vector2IntField("Destination", level.destinationCoord);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
        }

        private void DrawInteractiveGrid(LevelData level)
        {
            EditorGUILayout.LabelField("Level Grid Editor (Click to paint / edit roads)", EditorStyles.boldLabel);

            gridScrollPos = EditorGUILayout.BeginScrollView(gridScrollPos, GUILayout.Height(260));

            int cols = level.gridSize.x;
            int rows = level.gridSize.y;

            for (int y = rows - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < cols; x++)
                {
                    Vector2Int coord = new Vector2Int(x, y);
                    var entry = level.GetEntryAt(coord);
                    CellTileType type = entry != null ? entry.tileType : CellTileType.Empty;

                    Color cellColor = GetColorForTileType(type, coord == level.startCoord, coord == level.destinationCoord);
                    GUI.backgroundColor = cellColor;

                    string cellLabel = GetIconForTileType(type, coord == level.startCoord, coord == level.destinationCoord);
                    if (GUILayout.Button(cellLabel, GUILayout.Width(CELL_GUI_SIZE), GUILayout.Height(CELL_GUI_SIZE)))
                    {
                        // Paint on click
                        level.SetTile(coord, selectedToolTile, selectedToolRotation);
                        EditorUtility.SetDirty(level);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();
        }

        private void DrawBottomActionToolbar(LevelData level)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Palette d'Outils:", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            selectedToolTile = (CellTileType)EditorGUILayout.EnumPopup(selectedToolTile, GUILayout.Width(160));
            if (GUILayout.Button("Pinceau: Route", EditorStyles.miniButton)) selectedToolTile = CellTileType.StraightVertical;
            if (GUILayout.Button("Virage", EditorStyles.miniButton)) selectedToolTile = CellTileType.TurnNE;
            if (GUILayout.Button("Carrefour", EditorStyles.miniButton)) selectedToolTile = CellTileType.Crossroad;
            if (GUILayout.Button("Immeuble", EditorStyles.miniButton)) selectedToolTile = CellTileType.Building;
            if (GUILayout.Button("Gomme (Vide)", EditorStyles.miniButton)) selectedToolTile = CellTileType.Empty;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
            if (GUILayout.Button("Remove All Fillers", GUILayout.Height(26)))
            {
                level.tileEntries.RemoveAll(t => t.tileType == CellTileType.Building || t.tileType == CellTileType.Empty);
                EditorUtility.SetDirty(level);
            }

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("Auto-Fill Loop (Tessera / Procedural)", GUILayout.Height(26)))
            {
                TesseraCityGeneratorBridge bridge = FindAnyObjectByType<TesseraCityGeneratorBridge>();
                if (bridge == null)
                {
                    GameObject bridgeGO = new GameObject("TesseraCityGeneratorBridge");
                    bridge = bridgeGO.AddComponent<TesseraCityGeneratorBridge>();
                }
                bridge.cityGridDimensions = new Vector3Int(level.gridSize.x, 1, level.gridSize.y);
                bridge.GenerateProceduralCity(level, (res) =>
                {
                    EditorUtility.SetDirty(level);
                    AssetDatabase.SaveAssets();
                });
            }

            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.6f);
            if (GUILayout.Button("🚀 Build Demo Scene (Plug & Play)", GUILayout.Height(26)))
            {
                TokyoDemoSceneBuilder.GenerateCompleteDemoScene();
            }

            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.95f);
            if (GUILayout.Button("Test Level in Scene", GUILayout.Height(26)))
            {
                LevelRuntimeBuilder builder = FindAnyObjectByType<LevelRuntimeBuilder>();
                if (builder != null)
                {
                    builder.BuildLevel(level);
                }
                else
                {
                    Debug.LogWarning("LevelRuntimeBuilder non trouvé dans la scène ouverte.");
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private Color GetColorForTileType(CellTileType type, bool isStart, bool isDest)
        {
            if (isStart) return new Color(0.2f, 0.9f, 0.2f);
            if (isDest) return new Color(1f, 0.85f, 0.1f);

            switch (type)
            {
                case CellTileType.StraightHorizontal:
                case CellTileType.StraightVertical:
                    return new Color(0.35f, 0.5f, 0.9f);
                case CellTileType.TurnNE:
                case CellTileType.TurnNW:
                case CellTileType.TurnSE:
                case CellTileType.TurnSW:
                    return new Color(0.55f, 0.4f, 0.9f);
                case CellTileType.Crossroad:
                case CellTileType.TIntersection:
                    return new Color(0.2f, 0.7f, 0.8f);
                case CellTileType.Building:
                    return new Color(0.55f, 0.55f, 0.65f);
                default:
                    return new Color(0.18f, 0.18f, 0.2f);
            }
        }

        private string GetIconForTileType(CellTileType type, bool isStart, bool isDest)
        {
            if (isStart) return "S";
            if (isDest) return "D";
            switch (type)
            {
                case CellTileType.StraightVertical: return "║";
                case CellTileType.StraightHorizontal: return "═";
                case CellTileType.Crossroad: return "╬";
                case CellTileType.TIntersection: return "╦";
                case CellTileType.TurnNE: return "╚";
                case CellTileType.Building: return "■";
                default: return "";
            }
        }

        private void DrawFieldElementsTab()
        {
            EditorGUILayout.LabelField("Field Elements & Palette (Rails, Buildings, Props)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configurez ici les prefabs de rails, embranchements, arrêts de tram/moto et bâtiments inspirés du template Rail Express.", MessageType.Info);
        }

        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("Tokyo Express - Global Editor Settings", EditorStyles.boldLabel);
            if (GUILayout.Button("Reset PlayerPrefs / Save Data"))
            {
                TokyoExpress.Data.SaveSystem.ResetProfile();
                Debug.Log("[TokyoLevelEditor] Profil réinitialisé !");
            }
        }

        private void CreateNewLevel()
        {
            string folder = "Assets/_TokyoExpress/ScriptableObjects/Levels";
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            int nextId = levelAssetList.Count + 1;
            LevelData newLevel = ScriptableObject.CreateInstance<LevelData>();
            newLevel.levelId = nextId;
            newLevel.levelName = $"Level {nextId:00}";

            string assetPath = $"{folder}/Level_{nextId:00}.asset";
            AssetDatabase.CreateAsset(newLevel, assetPath);
            AssetDatabase.SaveAssets();

            RefreshLevelsList();
            selectedLevelIndex = levelAssetList.IndexOf(newLevel);
            activeLevel = newLevel;
        }

        private void DeleteActiveLevel()
        {
            if (activeLevel == null) return;
            string path = AssetDatabase.GetAssetPath(activeLevel);
            AssetDatabase.DeleteAsset(path);
            RefreshLevelsList();
        }
    }
}
#endif
