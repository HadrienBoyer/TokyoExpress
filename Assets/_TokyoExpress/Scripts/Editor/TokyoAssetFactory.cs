using System.IO;
using UnityEditor;
using UnityEngine;
using TokyoExpress.Data;
using TokyoExpress.Grid;
using TokyoExpress.Player;
using TokyoExpress.Core;

namespace TokyoExpress.GameEditor
{
    public static class TokyoAssetFactory
    {
        public const string ROOT_FOLDER = "Assets/_TokyoExpress";
        public const string MAT_FOLDER = "Assets/_TokyoExpress/Materials";
        public const string PREFAB_FOLDER = "Assets/_TokyoExpress/Prefabs";
        public const string SO_FOLDER = "Assets/_TokyoExpress/ScriptableObjects";

        public static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder(MAT_FOLDER))
                AssetDatabase.CreateFolder(ROOT_FOLDER, "Materials");
            if (!AssetDatabase.IsValidFolder(PREFAB_FOLDER))
                AssetDatabase.CreateFolder(ROOT_FOLDER, "Prefabs");
            if (!AssetDatabase.IsValidFolder(SO_FOLDER))
                AssetDatabase.CreateFolder(ROOT_FOLDER, "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder($"{SO_FOLDER}/Bikes"))
                AssetDatabase.CreateFolder(SO_FOLDER, "Bikes");
            if (!AssetDatabase.IsValidFolder($"{SO_FOLDER}/Skins"))
                AssetDatabase.CreateFolder(SO_FOLDER, "Skins");
            if (!AssetDatabase.IsValidFolder($"{SO_FOLDER}/Levels"))
                AssetDatabase.CreateFolder(SO_FOLDER, "Levels");
        }

        public static Shader GetURPShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return shader;
        }

        public static Material GetOrCreateMaterial(string name, Color color, float smoothness = 0.5f)
        {
            EnsureDirectories();
            string path = $"{MAT_FOLDER}/M_{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(GetURPShader());
                mat.name = $"M_{name}";
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                // Update color in case pop colors are updated
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        public static void GenerateDemoScriptableObjects()
        {
            EnsureDirectories();

            // Pop Colors for skins & characters
            Material riderYellow = GetOrCreateMaterial("Rider_Yellow", new Color(1f, 0.88f, 0.1f), 0.7f);
            Material riderCyan = GetOrCreateMaterial("Rider_Cyan", new Color(0.0f, 0.95f, 0.95f), 0.7f);
            Material riderPink = GetOrCreateMaterial("Rider_Pink", new Color(1f, 0.25f, 0.65f), 0.7f);

            // 1. Bikes (Speed balanced for hyper-casual readability)
            CreateBikeSO("bike_scooter_classic", "Tokyo Scooter 50cc", "Scooter agile pour livraisons de ramens rapides.", 0, 5.5f, 2.5f, SpecialAbilityType.None, 0f);
            CreateBikeSO("bike_cyber_ninja", "Cyber Ninja 600", "Moto de sport survoltée équipée de Nitro.", 450, 7.5f, 3.0f, SpecialAbilityType.NitroBoost, 3.5f);
            CreateBikeSO("bike_gravity_hopper", "Shibuya Jumper", "Amortisseurs hydrauliques haute suspension pour sauter les obstacles.", 600, 6.0f, 4.5f, SpecialAbilityType.HighJump, 0f);
            CreateBikeSO("bike_time_shifter", "Neo Akiba Chrono", "Capacité Bullet Time pour ralentir le temps aux carrefours.", 900, 6.5f, 2.8f, SpecialAbilityType.BulletTime, 3.0f);

            // 2. Skins
            CreateSkinSO("skin_tokyo_rider", "Livreur Tokyo Express", riderYellow, 0);
            CreateSkinSO("skin_cyber_neon", "Cyber Rider Neon", riderCyan, 200);
            CreateSkinSO("skin_sakura_pink", "Sakura Delivery", riderPink, 250);

            // 3. Demo Levels
            CreateDemoLevel(1, "Niveau 01 - Ramen Express", PackageType.Food, 40f, 150, new Vector2Int(8, 14));
            CreateDemoLevel(2, "Niveau 02 - Vase Ming Fragile", PackageType.Fragile, 50f, 300, new Vector2Int(10, 16));
            CreateDemoLevel(3, "Niveau 03 - Shinjuku Rush Hour", PackageType.VIPRush, 30f, 500, new Vector2Int(10, 18));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateBikeSO(string id, string name, string desc, int price, float speed, float jump, SpecialAbilityType ability, float abilityDur)
        {
            string path = $"{SO_FOLDER}/Bikes/{id}.asset";
            BikeItemData bike = AssetDatabase.LoadAssetAtPath<BikeItemData>(path);
            if (bike == null)
            {
                bike = ScriptableObject.CreateInstance<BikeItemData>();
                bike.id = id;
                bike.bikeName = name;
                bike.description = desc;
                bike.priceCoins = price;
                bike.baseSpeed = speed;
                bike.jumpForce = jump;
                bike.ability = ability;
                bike.abilityDuration = abilityDur;
                AssetDatabase.CreateAsset(bike, path);
            }
        }

        private static void CreateSkinSO(string id, string name, Material mat, int price)
        {
            string path = $"{SO_FOLDER}/Skins/{id}.asset";
            SkinItemData skin = AssetDatabase.LoadAssetAtPath<SkinItemData>(path);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<SkinItemData>();
                skin.id = id;
                skin.skinName = name;
                skin.characterMaterial = mat;
                skin.priceCoins = price;
                AssetDatabase.CreateAsset(skin, path);
            }
        }

        private static void CreateDemoLevel(int id, string name, PackageType pkgType, float time, int coins, Vector2Int size)
        {
            string path = $"{SO_FOLDER}/Levels/Level_{id:00}.asset";
            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelData>();
                level.levelId = id;
                level.levelName = name;
                level.packageType = pkgType;
                level.timeLimit = time;
                level.rewardCoins = coins;
                level.gridSize = size;
                level.cellSize = 2.0f;

                // Algorithmic layout
                var bridge = new GameObject("TempBridge").AddComponent<TesseraCityGeneratorBridge>();
                bridge.cityGridDimensions = new Vector3Int(size.x, 1, size.y);
                bridge.gridCellUnit = 2.0f;
                bridge.GenerateAlgorithmicTokyoRoads(level);
                Object.DestroyImmediate(bridge.gameObject);

                AssetDatabase.CreateAsset(level, path);
            }
        }
    }
}
