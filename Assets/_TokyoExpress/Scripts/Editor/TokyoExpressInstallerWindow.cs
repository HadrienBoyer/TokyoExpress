#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using TokyoExpress.Grid;
using TokyoExpress.Core;

namespace TokyoExpress.GameEditor
{
    public class TokyoExpressInstallerWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private bool tagsLayersConfigured = false;
        private bool demoAssetsPresent = false;
        private bool demoScenePresent = false;
        private bool isUrpConfigured = false;

        [MenuItem("Tokyo Express/🛠️ Installation Wizard & Setup", false, 0)]
        public static void OpenWindow()
        {
            var window = GetWindow<TokyoExpressInstallerWindow>("Tokyo Express Installer");
            window.minSize = new Vector2(520, 620);
            window.Show();
        }

        private void OnEnable()
        {
            CheckStatus();
        }

        private void CheckStatus()
        {
            isUrpConfigured = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            tagsLayersConfigured = TokyoTagAndLayerManager.CheckIfTagsAndLayersInstalled();
            demoAssetsPresent = File.Exists($"{TokyoAssetFactory.SO_FOLDER}/Bikes/bike_scooter_classic.asset") &&
                                File.Exists($"{TokyoAssetFactory.PREFAB_FOLDER}/Prefab_PlayerTokyoMoto.prefab");
            demoScenePresent = File.Exists(TokyoDemoSceneBuilder.DEMO_SCENE_PATH);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawHeader();

            EditorGUILayout.Space(12);

            DrawUrpCheckStep();

            EditorGUILayout.Space(12);

            DrawStep1TagsAndLayers();

            EditorGUILayout.Space(12);

            DrawStep2DemoPack();

            EditorGUILayout.Space(12);

            DrawStep3DemoScene();

            EditorGUILayout.Space(15);

            DrawOneClickFullInstall();

            EditorGUILayout.Space(15);

            DrawQuickLinks();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("TOKYO EXPRESS", titleStyle);
            
            GUIStyle subStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic
            };
            EditorGUILayout.LabelField("Hyper-Casual Delivery Template & Procedural City Framework", subStyle);
            EditorGUILayout.EndVertical();
        }

        private void DrawUrpCheckStep()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            string statusIcon = isUrpConfigured ? "✓" : "!";
            Color statusColor = isUrpConfigured ? Color.green : new Color(1f, 0.4f, 0.3f);

            GUI.color = statusColor;
            EditorGUILayout.LabelField($"[{statusIcon}] Pipeline Graphique : URP", EditorStyles.boldLabel, GUILayout.Width(220));
            GUI.color = Color.white;

            EditorGUILayout.LabelField(isUrpConfigured ? "URP Actif" : "Non configuré (Built-in)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (!isUrpConfigured)
            {
                EditorGUILayout.HelpBox("Aucun Render Pipeline Asset n'est assigné dans 'Project Settings > Graphics'. Pour profiter des shaders URP, assignez Mobile_RPAsset ou PC_RPAsset.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Universal Render Pipeline (URP) est bien actif sur ce projet.", MessageType.None);
            }

            if (GUILayout.Button("Ouvrir 'Project Settings > Graphics'", GUILayout.Height(26)))
            {
                SettingsService.OpenProjectSettings("Project/Graphics");
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawStep1TagsAndLayers()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            string statusIcon = tagsLayersConfigured ? "✓" : "!";
            Color statusColor = tagsLayersConfigured ? Color.green : new Color(1f, 0.6f, 0.1f);
            
            GUI.color = statusColor;
            EditorGUILayout.LabelField($"[{statusIcon}] Étape 1 : Tags & Layers", EditorStyles.boldLabel, GUILayout.Width(220));
            GUI.color = Color.white;

            EditorGUILayout.LabelField(tagsLayersConfigured ? "Configuré" : "Non configuré", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Crée automatiquement les Tags (Obstacle, TrafficMoto, Road, DeliveryTarget) et Layers (Road, PlayerMoto, TrafficObstacles, Buildings) dans le TagManager.", MessageType.None);

            if (GUILayout.Button("Installer Tags & Layers", GUILayout.Height(28)))
            {
                TokyoTagAndLayerManager.SetupTagsAndLayers();
                CheckStatus();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawStep2DemoPack()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            string statusIcon = demoAssetsPresent ? "✓" : "!";
            Color statusColor = demoAssetsPresent ? Color.green : new Color(1f, 0.6f, 0.1f);

            GUI.color = statusColor;
            EditorGUILayout.LabelField($"[{statusIcon}] Étape 2 : Pack Prefabs + SO Data", EditorStyles.boldLabel, GUILayout.Width(240));
            GUI.color = Color.white;

            EditorGUILayout.LabelField(demoAssetsPresent ? "Prêt" : "À générer", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Génère l'ensemble des ScriptableObjects (Motos avec abilities, Skins, 3 Niveaux démo) ainsi que les Prefabs URP (Routes, Carrefours, Immeubles, Moto du Joueur, Trafic IA, Arrivée).", MessageType.None);

            if (GUILayout.Button("Générer Pack Prefabs + ScriptableObjects", GUILayout.Height(28)))
            {
                GenerateAssetsPackOnly();
                CheckStatus();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawStep3DemoScene()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            string statusIcon = demoScenePresent ? "✓" : "!";
            Color statusColor = demoScenePresent ? Color.green : new Color(1f, 0.6f, 0.1f);

            GUI.color = statusColor;
            EditorGUILayout.LabelField($"[{statusIcon}] Étape 3 : Scène de Démo Plug & Play", EditorStyles.boldLabel, GUILayout.Width(260));
            GUI.color = Color.white;

            EditorGUILayout.LabelField(demoScenePresent ? "Créée" : "Non créée", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Construit la scène complète (Managers Audio & Monétisation, Caméra Isométrique, Joueur, Trafic IA, UI HUD, Décompte et Missions) prête à jouer.", MessageType.None);

            if (GUILayout.Button("Créer & Ouvrir la Scène de Démo", GUILayout.Height(28)))
            {
                EditorApplication.delayCall += () =>
                {
                    TokyoDemoSceneBuilder.GenerateCompleteDemoScene();
                    CheckStatus();
                    Repaint();
                };
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawOneClickFullInstall()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.45f);
            if (GUILayout.Button("⚡ INSTALLATION COMPLÈTE EN 1 CLIC ⚡", GUILayout.Height(42)))
            {
                EditorApplication.delayCall += () =>
                {
                    TokyoTagAndLayerManager.SetupTagsAndLayers();
                    TokyoDemoSceneBuilder.GenerateCompleteDemoScene();
                    CheckStatus();
                    Repaint();
                };
                GUIUtility.ExitGUI();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }

        private void DrawQuickLinks()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ouvrir le Level Editor", GUILayout.Height(26)))
            {
                TokyoLevelEditorWindow.OpenWindow();
            }
            if (GUILayout.Button("Réinitialiser Sauvegarde Joueur", GUILayout.Height(26)))
            {
                TokyoExpress.Data.SaveSystem.ResetProfile();
                Debug.Log("[TokyoExpress] Profil joueur réinitialisé.");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void GenerateAssetsPackOnly()
        {
            TokyoAssetFactory.EnsureDirectories();
            TokyoAssetFactory.GenerateDemoScriptableObjects();

            Material roadMat = TokyoAssetFactory.GetOrCreateMaterial("Road_PopIndigo", new Color(0.24f, 0.22f, 0.42f), 0.35f);
            Material markMat = TokyoAssetFactory.GetOrCreateMaterial("Road_NeonCyan", new Color(0.1f, 0.95f, 0.95f), 0.8f);
            Material groundMat = TokyoAssetFactory.GetOrCreateMaterial("Ground_LimePop", new Color(0.85f, 0.95f, 0.35f), 0.2f);
            Material buildingMat = TokyoAssetFactory.GetOrCreateMaterial("Building_SoftLilac", new Color(0.88f, 0.9f, 0.98f), 0.25f);
            Material bikeMat = TokyoAssetFactory.GetOrCreateMaterial("Moto_PopYellow", new Color(1f, 0.82f, 0.1f), 0.7f);
            Material riderMat = TokyoAssetFactory.GetOrCreateMaterial("Rider_BrightBlue", new Color(0.15f, 0.55f, 0.98f), 0.5f);
            Material trafficMat = TokyoAssetFactory.GetOrCreateMaterial("Traffic_VibrantPurple", new Color(0.65f, 0.18f, 0.88f), 0.6f);
            Material destMat = TokyoAssetFactory.GetOrCreateMaterial("Destination_NeonGreen", new Color(0.2f, 0.95f, 0.45f), 0.9f);

            Material stationPlatformMat = TokyoAssetFactory.GetOrCreateMaterial("Station_Base", new Color(0.95f, 0.96f, 1f), 0.3f);
            Material stationCanopyMat = TokyoAssetFactory.GetOrCreateMaterial("Station_RoofCyan", new Color(0.12f, 0.78f, 0.92f), 0.6f);
            Material stationSignMat = TokyoAssetFactory.GetOrCreateMaterial("Station_SignBlue", new Color(0.15f, 0.45f, 0.95f), 0.8f);

            TokyoPrefabFactory.CreateRoadTilePrefab("Road_Straight", CellTileType.StraightVertical, roadMat, markMat);
            TokyoPrefabFactory.CreateRoundedTurnRoadPrefab("Road_TurnRounded", CellTileType.TurnNE, roadMat, markMat, groundMat);
            TokyoPrefabFactory.CreateRoadTilePrefab("Road_Intersection", CellTileType.Crossroad, roadMat, markMat);
            TokyoPrefabFactory.CreateBuildingPrefab("Skyscraper_01", buildingMat);
            TokyoPrefabFactory.CreatePlayerMotoPrefab(bikeMat, riderMat);
            TokyoPrefabFactory.CreateTrafficMotoPrefab(trafficMat);
            TokyoPrefabFactory.CreateDestinationFlagPrefab(destMat);
            TokyoPrefabFactory.CreateStartStationPrefab(stationPlatformMat, stationCanopyMat, stationSignMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Tokyo Express", "Pack Pop Prefabs (avec virages arrondis & Start Station) et ScriptableObjects généré avec succès !", "OK");
        }
    }
}
#endif
