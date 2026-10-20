#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TokyoExpress.Audio;
using TokyoExpress.Core;
using TokyoExpress.Data;
using TokyoExpress.Delivery;
using TokyoExpress.Grid;
using TokyoExpress.Monetization;
using TokyoExpress.Player;
using TokyoExpress.UI;

namespace TokyoExpress.GameEditor
{
    public static class TokyoDemoSceneBuilder
    {
        public const string DEMO_SCENE_PATH = "Assets/_TokyoExpress/Scenes/TokyoExpress_DemoScene.unity";

        [MenuItem("Tokyo Express/🚀 Créer Scène Démo (Plug & Play)", false, 1)]
        public static void GenerateCompleteDemoScene()
        {
            // 0. Ensure Tags and Layers are configured
            TokyoTagAndLayerManager.SetupTagsAndLayers();

            // 1. Ensure folders & generate all SOs and URP materials
            TokyoAssetFactory.EnsureDirectories();
            TokyoAssetFactory.GenerateDemoScriptableObjects();

            // 2. Create Pop Colors Materials (matching Rail Express hyper-casual palette)
            Material roadMat = TokyoAssetFactory.GetOrCreateMaterial("Road_PopIndigo", new Color(0.24f, 0.22f, 0.42f), 0.35f);
            Material markMat = TokyoAssetFactory.GetOrCreateMaterial("Road_NeonCyan", new Color(0.1f, 0.95f, 0.95f), 0.8f);
            Material groundMat = TokyoAssetFactory.GetOrCreateMaterial("Ground_LimePop", new Color(0.85f, 0.95f, 0.35f), 0.2f);
            Material buildingMat = TokyoAssetFactory.GetOrCreateMaterial("Building_SoftLilac", new Color(0.88f, 0.9f, 0.98f), 0.25f);
            Material bikeMat = TokyoAssetFactory.GetOrCreateMaterial("Moto_PopYellow", new Color(1f, 0.82f, 0.1f), 0.7f);
            Material riderMat = TokyoAssetFactory.GetOrCreateMaterial("Rider_BrightBlue", new Color(0.15f, 0.55f, 0.98f), 0.5f);
            Material trafficMat = TokyoAssetFactory.GetOrCreateMaterial("Traffic_VibrantPurple", new Color(0.65f, 0.18f, 0.88f), 0.6f);
            Material destMat = TokyoAssetFactory.GetOrCreateMaterial("Destination_NeonGreen", new Color(0.2f, 0.95f, 0.45f), 0.9f);

            // Station Materials
            Material stationPlatformMat = TokyoAssetFactory.GetOrCreateMaterial("Station_Base", new Color(0.95f, 0.96f, 1f), 0.3f);
            Material stationCanopyMat = TokyoAssetFactory.GetOrCreateMaterial("Station_RoofCyan", new Color(0.12f, 0.78f, 0.92f), 0.6f);
            Material stationSignMat = TokyoAssetFactory.GetOrCreateMaterial("Station_SignBlue", new Color(0.15f, 0.45f, 0.95f), 0.8f);

            // 3. Create Prefabs (Straight, Rounded Turn Arc, Intersection, Station, Player)
            GameObject roadStraightPrefab = TokyoPrefabFactory.CreateRoadTilePrefab("Road_Straight", CellTileType.StraightVertical, roadMat, markMat);
            GameObject roadTurnPrefab = TokyoPrefabFactory.CreateRoundedTurnRoadPrefab("Road_TurnRounded", CellTileType.TurnNE, roadMat, markMat, groundMat);
            GameObject intersectionPrefab = TokyoPrefabFactory.CreateRoadTilePrefab("Road_Intersection", CellTileType.Crossroad, roadMat, markMat);
            GameObject buildingPrefab = TokyoPrefabFactory.CreateBuildingPrefab("Skyscraper_01", buildingMat);
            GameObject playerPrefab = TokyoPrefabFactory.CreatePlayerMotoPrefab(bikeMat, riderMat);
            GameObject trafficPrefab = TokyoPrefabFactory.CreateTrafficMotoPrefab(trafficMat);
            GameObject destMarkerPrefab = TokyoPrefabFactory.CreateDestinationFlagPrefab(destMat);
            GameObject startStationPrefab = TokyoPrefabFactory.CreateStartStationPrefab(stationPlatformMat, stationCanopyMat, stationSignMat);

            // 4. Create New Scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 5. Environment & Lights (Pop Sky / Ambient)
            GameObject dirLightGO = new GameObject("Directional Light");
            var dirLight = dirLightGO.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1f, 0.98f, 0.92f);
            dirLight.intensity = 1.35f;
            dirLightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Pop Ocean / Background floor
            GameObject seaPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            seaPlane.name = "Sea_Floor";
            seaPlane.transform.position = new Vector3(8f, -0.2f, 14f);
            seaPlane.transform.localScale = new Vector3(6f, 1f, 6f);
            Material seaMat = TokyoAssetFactory.GetOrCreateMaterial("TokyoBay_PopCyan", new Color(0.35f, 0.82f, 0.92f), 0.85f);
            seaPlane.GetComponent<MeshRenderer>().sharedMaterial = seaMat;

            // 6. Managers (Created as Root GameObjects so DontDestroyOnLoad works properly)
            // Audio Manager
            GameObject audioGO = new GameObject("TokyoAudioManager");
            audioGO.AddComponent<TokyoAudioManager>();

            // Monetization Manager
            GameObject monGO = new GameObject("MonetizationManager");
            monGO.AddComponent<MonetizationManager>();

            // One Touch Input Manager
            GameObject inputGO = new GameObject("OneTouchInputManager");
            inputGO.AddComponent<OneTouchInputManager>();

            // 7. Grid & Runtime Builder
            GameObject levelSystemGO = new GameObject("--- LEVEL SYSTEM ---");
            var builder = levelSystemGO.AddComponent<LevelRuntimeBuilder>();
            var bridge = levelSystemGO.AddComponent<TesseraCityGeneratorBridge>();

            // Assign prefabs to builder via reflection / serialized object
            SerializedObject builderSO = new SerializedObject(builder);
            builderSO.FindProperty("defaultStraightRoadPrefab").objectReferenceValue = roadStraightPrefab;
            builderSO.FindProperty("defaultTurnRoadPrefab").objectReferenceValue = roadTurnPrefab;
            builderSO.FindProperty("defaultIntersectionPrefab").objectReferenceValue = intersectionPrefab;
            builderSO.FindProperty("defaultBuildingPrefab").objectReferenceValue = buildingPrefab;
            builderSO.FindProperty("destinationTargetPrefab").objectReferenceValue = destMarkerPrefab;
            builderSO.FindProperty("startStationPrefab").objectReferenceValue = startStationPrefab;
            builderSO.ApplyModifiedProperties();

            // Load Level 1 SO
            LevelData level01 = AssetDatabase.LoadAssetAtPath<LevelData>($"{TokyoAssetFactory.SO_FOLDER}/Levels/Level_01.asset");
            builder.BuildLevel(level01);

            // 8. Player Moto Instantiation
            Vector3 startPos = builder.GridToWorldPosition(level01.startCoord, level01.cellSize) + Vector3.up * 0.1f;
            GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.name = "Player_TokyoMoto";
            playerInstance.transform.position = startPos;
            
            // Check start tile type to orient player in the valid initial track direction
            Vector3 initialHeading = Vector3.forward;
            var startEntry = level01.GetEntryAt(level01.startCoord);
            if (startEntry != null)
            {
                if (startEntry.tileType == CellTileType.TurnNE) initialHeading = Vector3.right;
                else if (startEntry.tileType == CellTileType.StraightHorizontal) initialHeading = Vector3.right;
                else if (startEntry.tileType == CellTileType.StraightVertical) initialHeading = Vector3.forward;
            }
            playerInstance.transform.rotation = Quaternion.LookRotation(initialHeading, Vector3.up);
            var initialMotoController = playerInstance.GetComponent<TokyoMotoController>();
            if (initialMotoController != null)
            {
                initialMotoController.ShiftDirectionOrLane(initialHeading);
            }

            // 9. Top-Down Camera setup (matching hyper-casual screenshot perspective)
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.68f, 0.88f, 0.88f); // Soft pastel teal sky matching screenshots
            cam.fieldOfView = 45f;
            camGO.AddComponent<AudioListener>();

            // Enable URP Post-Processing & Anti-Aliasing
            var urpCamData = camGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            urpCamData.renderPostProcessing = true;
            urpCamData.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing; // SMAA
            urpCamData.antialiasingQuality = UnityEngine.Rendering.Universal.AntialiasingQuality.High;

            var followCam = camGO.AddComponent<TokyoFollowCamera>();
            followCam.SetTarget(playerInstance.transform);
            followCam.PositionCameraForLevelFrame();

            // 10. Traffic Obstacles Spawn
            GameObject trafficContainer = new GameObject("--- TRAFFIC ---");
            for (int i = 0; i < 3; i++)
            {
                Vector2Int trafficCell = new Vector2Int(1, 4 + i * 4);
                Vector3 trafficPos = builder.GridToWorldPosition(trafficCell, level01.cellSize) + Vector3.up * 0.1f;
                GameObject trafficObj = (GameObject)PrefabUtility.InstantiatePrefab(trafficPrefab, trafficContainer.transform);
                trafficObj.transform.position = trafficPos;
                var comp = trafficObj.GetComponent<TrafficMotoObstacle>();
                if (comp != null) comp.SetDirection(Vector3.forward);
            }

            // 11. Delivery Mission Manager
            GameObject missionGO = new GameObject("DeliveryMissionManager");
            var missionMgr = missionGO.AddComponent<DeliveryMissionManager>();
            SerializedObject missionSO = new SerializedObject(missionMgr);
            missionSO.FindProperty("playerMoto").objectReferenceValue = playerInstance.GetComponent<TokyoMotoController>();
            missionSO.ApplyModifiedProperties();

            // 12. Complete UI Canvas (HUD + Crash Modal + Win Modal)
            CreateDemoUI(missionMgr);

            // 13. Auto Start Mission Runner
            GameObject runnerGO = new GameObject("MissionAutoStarter");
            var runner = runnerGO.AddComponent<TokyoMissionRunner>();
            runner.levelToRun = level01;

            // Save Scene
            if (!Directory.Exists("Assets/_TokyoExpress/Scenes"))
            {
                Directory.CreateDirectory("Assets/_TokyoExpress/Scenes");
            }
            EditorSceneManager.SaveScene(scene, DEMO_SCENE_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Ensure the demo scene is properly opened in the editor
            EditorSceneManager.OpenScene(DEMO_SCENE_PATH, OpenSceneMode.Single);

            Debug.Log("[TokyoExpress] 🚀 Scène de Démo 'TokyoExpress_DemoScene.unity' créée et ouverte avec succès ! Cliquez sur PLAY pour jouer.");
        }

        private static void CreateDemoUI(DeliveryMissionManager missionMgr)
        {
            // Event System
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemGO.AddComponent<StandaloneInputModule>();
#endif

            // Canvas
            GameObject canvasGO = new GameObject("UI_Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // HUD Root
            GameObject hudRoot = new GameObject("InGameHUD");
            hudRoot.transform.SetParent(canvasGO.transform, false);
            var rect = hudRoot.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var hudComp = hudRoot.AddComponent<InGameDeliveryHUD>();

            // Top Header Banner
            GameObject bannerGO = new GameObject("HeaderBanner");
            bannerGO.transform.SetParent(hudRoot.transform, false);
            var bannerRect = bannerGO.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 1f);
            bannerRect.anchorMax = new Vector2(0.5f, 1f);
            bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.sizeDelta = new Vector2(340, 90);
            bannerRect.anchoredPosition = new Vector2(0, -20);
            var bannerImg = bannerGO.AddComponent<Image>();
            bannerImg.color = new Color(0.12f, 0.12f, 0.18f, 0.85f);

            // Timer Text
            GameObject timerGO = new GameObject("TimerText");
            timerGO.transform.SetParent(bannerGO.transform, false);
            var timerTMP = timerGO.AddComponent<TextMeshProUGUI>();
            timerTMP.text = "40s";
            timerTMP.fontSize = 34;
            timerTMP.alignment = TextAlignmentOptions.Center;
            timerTMP.color = new Color(1f, 0.85f, 0.2f);
            timerGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 12);

            // Package Info
            GameObject pkgGO = new GameObject("PackageInfoText");
            pkgGO.transform.SetParent(bannerGO.transform, false);
            var pkgTMP = pkgGO.AddComponent<TextMeshProUGUI>();
            pkgTMP.text = "[LIVRAISON] Ramen Chaud";
            pkgTMP.fontSize = 18;
            pkgTMP.alignment = TextAlignmentOptions.Center;
            pkgTMP.color = Color.white;
            pkgGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -22);

            // Controls Hint (Bottom)
            GameObject hintGO = new GameObject("ControlsHintText");
            hintGO.transform.SetParent(hudRoot.transform, false);
            var hintRect = hintGO.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0, 25);
            var hintTMP = hintGO.AddComponent<TextMeshProUGUI>();
            hintTMP.text = "[TAP] Sauter  |  [SWIPE] Tourner & Esquiver";
            hintTMP.fontSize = 20;
            hintTMP.alignment = TextAlignmentOptions.Center;
            hintTMP.color = new Color(1f, 1f, 1f, 0.8f);

            // Crash Modal
            GameObject crashModal = CreateModal(hudRoot.transform, "CrashModal", "CRASH !", new Color(0.85f, 0.2f, 0.2f));
            crashModal.SetActive(false);

            // Victory Modal
            GameObject victoryModal = CreateModal(hudRoot.transform, "VictoryModal", "LIVRAISON RÉUSSIE !", new Color(0.15f, 0.8f, 0.35f));
            victoryModal.SetActive(false);

            // Wire HUD
            SerializedObject hudSO = new SerializedObject(hudComp);
            hudSO.FindProperty("timerText").objectReferenceValue = timerTMP;
            hudSO.FindProperty("packageInfoText").objectReferenceValue = pkgTMP;
            hudSO.FindProperty("crashModal").objectReferenceValue = crashModal;
            hudSO.FindProperty("victoryModal").objectReferenceValue = victoryModal;
            hudSO.ApplyModifiedProperties();
        }

        private static GameObject CreateModal(Transform parent, string name, string title, Color bannerColor)
        {
            GameObject modal = new GameObject(name);
            modal.transform.SetParent(parent, false);
            var rect = modal.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var bgImg = modal.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.75f);

            GameObject box = new GameObject("Box");
            box.transform.SetParent(modal.transform, false);
            var boxRect = box.AddComponent<RectTransform>();
            boxRect.sizeDelta = new Vector2(320, 240);
            var boxImg = box.AddComponent<Image>();
            boxImg.color = new Color(0.15f, 0.16f, 0.22f);

            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(box.transform, false);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = title;
            titleTMP.fontSize = 26;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = bannerColor;
            titleGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 70);

            return modal;
        }
    }
}
#endif
