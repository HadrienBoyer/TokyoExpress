using System;
using System.Collections.Generic;
using UnityEngine;

namespace TokyoExpress.Grid
{
    [Serializable]
    public class RoadSegmentPrefabMapping
    {
        public CellTileType tileType;
        public GameObject prefab;
    }

    public class LevelRuntimeBuilder : MonoBehaviour
    {
        public static LevelRuntimeBuilder Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private List<RoadSegmentPrefabMapping> tilePrefabs = new List<RoadSegmentPrefabMapping>();
        [SerializeField] private GameObject defaultStraightRoadPrefab;
        [SerializeField] private GameObject defaultTurnRoadPrefab;
        [SerializeField] private GameObject defaultIntersectionPrefab;
        [SerializeField] private GameObject defaultBuildingPrefab;
        [SerializeField] private GameObject destinationTargetPrefab;
        [SerializeField] private GameObject startStationPrefab;

        [Header("Runtime State")]
        [SerializeField] private LevelData currentActiveLevel;
        private Dictionary<Vector2Int, GameObject> spawnedGridObjects = new Dictionary<Vector2Int, GameObject>();

        public LevelData ActiveLevel => currentActiveLevel;
        public IReadOnlyDictionary<Vector2Int, GameObject> SpawnedGridObjects => spawnedGridObjects;

        public GameObject GetTileObjectAt(Vector2Int coord)
        {
            if (spawnedGridObjects.TryGetValue(coord, out GameObject obj) && obj != null)
            {
                return obj;
            }

            // Fallback: search among child tiles if scene was saved with static children
            if (currentActiveLevel != null)
            {
                Vector3 targetPos = GridToWorldPosition(coord, currentActiveLevel.cellSize);
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (Vector2.Distance(new Vector2(child.position.x, child.position.z), new Vector2(targetPos.x, targetPos.z)) < currentActiveLevel.cellSize * 0.45f)
                    {
                        spawnedGridObjects[coord] = child.gameObject;
                        return child.gameObject;
                    }
                }
            }

            return null;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                IndexExistingChildTiles();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void IndexExistingChildTiles()
        {
            if (currentActiveLevel == null) return;
            float cellSize = currentActiveLevel.cellSize;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Vector2Int coord = WorldToGridPosition(child.position, cellSize);
                if (!spawnedGridObjects.ContainsKey(coord))
                {
                    spawnedGridObjects[coord] = child.gameObject;
                }
            }
        }

        public void ClearSpawnedLevel()
        {
            foreach (var kvp in spawnedGridObjects)
            {
                if (kvp.Value != null)
                {
                    if (Application.isPlaying) Destroy(kvp.Value);
                    else DestroyImmediate(kvp.Value);
                }
            }
            spawnedGridObjects.Clear();
        }

        public void BuildLevel(LevelData level)
        {
            ClearSpawnedLevel();
            if (level == null) return;
            currentActiveLevel = level;

            foreach (var tile in level.tileEntries)
            {
                if (tile.tileType == CellTileType.Empty) continue;

                Vector3 worldPos = GridToWorldPosition(tile.coordinates, level.cellSize);
                float baseRotationY = GetBaseRotationForTileType(tile.tileType);
                Quaternion rotation = Quaternion.Euler(0f, baseRotationY + tile.rotationSteps * 90f, 0f);

                GameObject prefab = GetPrefabForType(tile.tileType);
                if (prefab != null)
                {
                    GameObject obj = Instantiate(prefab, worldPos, rotation, transform);
                    obj.name = $"Tile_{tile.tileType}_{tile.coordinates.x}_{tile.coordinates.y}";
                    spawnedGridObjects[tile.coordinates] = obj;
                }
                else
                {
                    // Fallback primitive visualization if prefab not yet assigned
                    GameObject fallback = CreateProceduralTileFallback(tile.tileType, worldPos, rotation, level.cellSize);
                    fallback.transform.SetParent(transform);
                    spawnedGridObjects[tile.coordinates] = fallback;
                }
            }

            // Spawn Start & Destination markers
            if (level.startCoord != Vector2Int.zero)
            {
                Vector3 startPos = GridToWorldPosition(level.startCoord, level.cellSize);
                if (startStationPrefab != null)
                {
                    Instantiate(startStationPrefab, startPos, Quaternion.identity, transform);
                }
            }

            if (level.destinationCoord != Vector2Int.zero)
            {
                Vector3 destPos = GridToWorldPosition(level.destinationCoord, level.cellSize);
                if (destinationTargetPrefab != null)
                {
                    Instantiate(destinationTargetPrefab, destPos, Quaternion.identity, transform);
                }
            }
        }

        public Vector3 GridToWorldPosition(Vector2Int coord, float cellSize)
        {
            return new Vector3(coord.x * cellSize, 0f, coord.y * cellSize);
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPos, float cellSize)
        {
            int x = Mathf.RoundToInt(worldPos.x / cellSize);
            int y = Mathf.RoundToInt(worldPos.z / cellSize);
            return new Vector2Int(x, y);
        }

        private float GetBaseRotationForTileType(CellTileType type)
        {
            switch (type)
            {
                case CellTileType.StraightHorizontal: return 90f;
                case CellTileType.StraightVertical: return 0f;
                case CellTileType.TurnNE: return 0f;    // Corner bottom-left (curves South to East)
                case CellTileType.TurnNW: return 90f;   // Corner bottom-right (curves South to West)
                case CellTileType.TurnSW: return 180f;  // Corner top-right (curves North to West)
                case CellTileType.TurnSE: return 270f;  // Corner top-left (curves North to East)
                default: return 0f;
            }
        }

        private GameObject GetPrefabForType(CellTileType type)
        {
            var mapping = tilePrefabs.Find(p => p.tileType == type);
            if (mapping != null && mapping.prefab != null) return mapping.prefab;

            switch (type)
            {
                case CellTileType.StraightHorizontal:
                case CellTileType.StraightVertical:
                    return defaultStraightRoadPrefab;
                case CellTileType.TurnNE:
                case CellTileType.TurnNW:
                case CellTileType.TurnSE:
                case CellTileType.TurnSW:
                    return defaultTurnRoadPrefab;
                case CellTileType.TIntersection:
                case CellTileType.Crossroad:
                    return defaultIntersectionPrefab;
                case CellTileType.Building:
                    return defaultBuildingPrefab;
                default:
                    return null;
            }
        }

        private GameObject CreateProceduralTileFallback(CellTileType type, Vector3 pos, Quaternion rot, float size)
        {
            GameObject container = new GameObject($"Fallback_{type}");
            container.transform.position = pos;
            container.transform.rotation = rot;

            if (type == CellTileType.Building)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(container.transform, false);
                cube.transform.localScale = new Vector3(size * 0.9f, UnityEngine.Random.Range(2f, 6f), size * 0.9f);
                cube.transform.localPosition = new Vector3(0f, cube.transform.localScale.y * 0.5f, 0f);
                var renderer = cube.GetComponent<MeshRenderer>();
                renderer.material.color = new Color(0.2f, 0.25f, 0.4f);
            }
            else
            {
                // Road slab
                GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.transform.SetParent(container.transform, false);
                slab.transform.localScale = new Vector3(size, 0.15f, size);
                slab.transform.localPosition = new Vector3(0f, -0.075f, 0f);
                var renderer = slab.GetComponent<MeshRenderer>();
                renderer.material.color = new Color(0.18f, 0.18f, 0.22f);

                // Add road mark line
                GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mark.transform.SetParent(container.transform, false);
                mark.transform.localScale = new Vector3(0.15f, 0.18f, size * 0.6f);
                mark.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                mark.GetComponent<MeshRenderer>().material.color = new Color(1f, 0.85f, 0.1f);
            }

            return container;
        }
    }
}
