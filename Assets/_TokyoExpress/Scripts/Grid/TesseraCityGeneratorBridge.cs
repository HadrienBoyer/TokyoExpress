using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tessera;

namespace TokyoExpress.Grid
{
    /// <summary>
    /// Bridges Tessera's Wave Function Collapse generator with Tokyo Express grid & road mechanics.
    /// Can generate full procedural road networks, loops, intersections and buildings, then export to LevelData.
    /// </summary>
    public class TesseraCityGeneratorBridge : MonoBehaviour
    {
        [Header("Tessera Reference")]
        [SerializeField] private TesseraGenerator tesseraGenerator;

        [Header("Level Generation Dimensions")]
        public Vector3Int cityGridDimensions = new Vector3Int(10, 1, 16);
        public float gridCellUnit = 2.0f;

        [Header("Procedural Rules")]
        [Range(0.2f, 0.8f)] public float roadDensity = 0.45f;
        public bool ensureConnectedLoop = true;
        public int randomSeed = 0;

        public event Action<LevelData> OnCityGenerated;

        private void Reset()
        {
            if (tesseraGenerator == null)
            {
                tesseraGenerator = GetComponent<TesseraGenerator>();
            }
        }

        /// <summary>
        /// Generates a procedural city layout via Tessera WFC engine
        /// </summary>
        public void GenerateProceduralCity(LevelData targetLevel = null, Action<LevelData> callback = null)
        {
            if (tesseraGenerator == null)
            {
                tesseraGenerator = GetComponent<TesseraGenerator>();
            }

            if (tesseraGenerator != null && tesseraGenerator.tiles != null && tesseraGenerator.tiles.Count > 0)
            {
                StartCoroutine(RunTesseraCoroutine(targetLevel, callback));
            }
            else
            {
                // Fallback algorithmic loop generator inspired by Rail Express loop generation
                LevelData level = GenerateAlgorithmicTokyoRoads(targetLevel);
                callback?.Invoke(level);
                OnCityGenerated?.Invoke(level);
            }
        }

        private IEnumerator RunTesseraCoroutine(LevelData targetLevel, Action<LevelData> callback)
        {
            tesseraGenerator.size = cityGridDimensions;
            tesseraGenerator.cellSize = new Vector3(gridCellUnit, gridCellUnit, gridCellUnit);

            var options = new TesseraGenerateOptions
            {
                cancellationToken = default,
                multithreaded = true
            };

            var generatorRoutine = tesseraGenerator.StartGenerate(options);
            yield return generatorRoutine;

            var completion = generatorRoutine.Result;
            if (completion != null && completion.success)
            {
                LevelData exported = ExportCompletionToLevelData(completion, targetLevel);
                callback?.Invoke(exported);
                OnCityGenerated?.Invoke(exported);
            }
            else
            {
                Debug.LogWarning("[TesseraCityGeneratorBridge] Tessera failed or had contradiction. Generating algorithmic fallback.");
                LevelData fallback = GenerateAlgorithmicTokyoRoads(targetLevel);
                callback?.Invoke(fallback);
                OnCityGenerated?.Invoke(fallback);
            }
        }

        private LevelData ExportCompletionToLevelData(TesseraCompletion completion, LevelData targetLevel)
        {
            if (targetLevel == null)
            {
                targetLevel = ScriptableObject.CreateInstance<LevelData>();
            }

            targetLevel.gridSize = new Vector2Int(cityGridDimensions.x, cityGridDimensions.z);
            targetLevel.cellSize = gridCellUnit;
            targetLevel.tileEntries.Clear();

            // Parse tile data dictionary from Tessera
            if (completion.tileData != null)
            {
                foreach (var kvp in completion.tileData)
                {
                    Vector3Int cellPos = new Vector3Int(kvp.Key.x, kvp.Key.y, kvp.Key.z);
                    Vector2Int coord = new Vector2Int(cellPos.x, cellPos.z);
                    
                    var tileModel = kvp.Value.Tile;
                    CellTileType determinedType = DeduceTileTypeFromTesseraTile(tileModel);
                    
                    targetLevel.SetTile(coord, determinedType, 0);
                }
            }

            targetLevel.startCoord = new Vector2Int(1, 1);
            targetLevel.destinationCoord = new Vector2Int(cityGridDimensions.x - 2, cityGridDimensions.z - 2);

            return targetLevel;
        }

        private CellTileType DeduceTileTypeFromTesseraTile(object tileObj)
        {
            if (tileObj == null) return CellTileType.Empty;
            string tileName = tileObj.ToString().ToLower();

            if (tileName.Contains("straight") || tileName.Contains("road")) return CellTileType.StraightVertical;
            if (tileName.Contains("turn") || tileName.Contains("corner")) return CellTileType.TurnNE;
            if (tileName.Contains("cross") || tileName.Contains("junction")) return CellTileType.Crossroad;
            if (tileName.Contains("t-int") || tileName.Contains("tjunction")) return CellTileType.TIntersection;
            if (tileName.Contains("building") || tileName.Contains("house")) return CellTileType.Building;

            return CellTileType.StraightVertical;
        }

        /// <summary>
        /// Pure algorithmic procedural loop generator matching the Rail Express style shown in screenshots
        /// </summary>
        public LevelData GenerateAlgorithmicTokyoRoads(LevelData targetLevel = null)
        {
            if (targetLevel == null)
            {
                targetLevel = ScriptableObject.CreateInstance<LevelData>();
            }

            int width = cityGridDimensions.x;
            int height = cityGridDimensions.z;
            targetLevel.gridSize = new Vector2Int(width, height);
            targetLevel.cellSize = gridCellUnit;
            targetLevel.tileEntries.Clear();

            // 1. Create Outer boundary road loop
            for (int x = 1; x < width - 1; x++)
            {
                targetLevel.SetTile(new Vector2Int(x, 1), CellTileType.StraightHorizontal, 0);
                targetLevel.SetTile(new Vector2Int(x, height - 2), CellTileType.StraightHorizontal, 0);
            }

            for (int y = 1; y < height - 1; y++)
            {
                targetLevel.SetTile(new Vector2Int(1, y), CellTileType.StraightVertical, 0);
                targetLevel.SetTile(new Vector2Int(width - 2, y), CellTileType.StraightVertical, 0);
            }

            // Corners
            targetLevel.SetTile(new Vector2Int(1, 1), CellTileType.TurnNE, 0);
            targetLevel.SetTile(new Vector2Int(width - 2, 1), CellTileType.TurnNW, 0);
            targetLevel.SetTile(new Vector2Int(1, height - 2), CellTileType.TurnSE, 0);
            targetLevel.SetTile(new Vector2Int(width - 2, height - 2), CellTileType.TurnSW, 0);

            // 2. Interior avenues and cross-connections (Rail Express pattern)
            int midY1 = height / 3;
            int midY2 = (height * 2) / 3;
            int midX = width / 2;

            for (int x = 2; x < width - 2; x++)
            {
                targetLevel.SetTile(new Vector2Int(x, midY1), CellTileType.StraightHorizontal, 0);
                targetLevel.SetTile(new Vector2Int(x, midY2), CellTileType.StraightHorizontal, 0);
            }

            for (int y = 2; y < height - 2; y++)
            {
                targetLevel.SetTile(new Vector2Int(midX, y), CellTileType.StraightVertical, 0);
            }

            // Intersections
            targetLevel.SetTile(new Vector2Int(midX, midY1), CellTileType.Crossroad, 0);
            targetLevel.SetTile(new Vector2Int(midX, midY2), CellTileType.Crossroad, 0);
            targetLevel.SetTile(new Vector2Int(1, midY1), CellTileType.TIntersection, 0);
            targetLevel.SetTile(new Vector2Int(width - 2, midY1), CellTileType.TIntersection, 0);
            targetLevel.SetTile(new Vector2Int(1, midY2), CellTileType.TIntersection, 0);
            targetLevel.SetTile(new Vector2Int(width - 2, midY2), CellTileType.TIntersection, 0);

            // 3. Fill city blocks with Tokyo skyscrapers / buildings
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (targetLevel.GetEntryAt(pos) == null)
                    {
                        // Outer rim or inner blocks
                        if (x == 0 || x == width - 1 || y == 0 || y == height - 1 || UnityEngine.Random.value < 0.85f)
                        {
                            targetLevel.SetTile(pos, CellTileType.Building, 0);
                        }
                    }
                }
            }

            targetLevel.startCoord = new Vector2Int(1, 1);
            targetLevel.destinationCoord = new Vector2Int(width - 2, height - 2);

            return targetLevel;
        }
    }
}
