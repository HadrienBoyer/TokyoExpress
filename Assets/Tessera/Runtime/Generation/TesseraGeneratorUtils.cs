using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Topo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

namespace Tessera
{
    /// <summary>
    /// Contains much of the details of TesseraGenerator.
    /// Most of these methods need to be run in the main Unity thread.
    /// Anything safe to run elsewhere is inside TesseraGeneratorHelper instead.
    /// </summary>
    public static class TesseraGeneratorUtils
    {
        #region Create TesseraGeneratorHelper
        internal static TesseraGeneratorHelper CreateTesseraGeneratorHelper(TesseraGenerator generator, TesseraGenerateOptions options = null)
        {
            var t1 = DateTime.Now;
            options = options ?? new TesseraGenerateOptions();
            var progress = options.progress;

            var seed = options.seed ?? generator.seed;
            if (seed == 0) seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

            var xororng = new XoRoRNG(seed);

            generator.Validate();

            var sylvesCellGrid = generator.CellGrid;

            var (actualTiles, samples) = GetTilesAndSamples(generator);

            var tileModelInfo = TileModelInfoBuilder.Create(actualTiles, sylvesCellGrid, generator.modelType == ModelType.MeshEdges, generator.meshEdgesTolerance);

            var grid = generator.GetGrid();

            var actualInitialConstraints = GetAllInitialConstraints(generator, options, grid);

            var tileConstraints = GetTileConstraints(generator, tileModelInfo, grid);

            var actualSkyBox = generator.skyBox == null ? null : new TesseraInitialConstraint
            {
                faceDetails = generator.skyBox.sylvesFaceDetails,
                offsets = generator.skyBox.sylvesOffsets,
            };
            
            var stats = new TesseraStats {
                createHelperTime = (DateTime.Now - t1).TotalSeconds,
                modelTileCount = tileModelInfo.AllTiles.Count,
                cellCount = grid.IndexCount,
            };

            var helperOptions = new TesseraGeneratorHelperOptions
            {
                grid = grid,
                palette = generator.palette,
                modelType = generator.modelType,
                tileSelectionType = generator.tileSelectionType,
                tileModelInfo = tileModelInfo,
                samples = samples,
                overlapSize = generator.overlapSize,
                initialConstraints = actualInitialConstraints,
                constraints = tileConstraints,
                skyBox = actualSkyBox,
                backtrack = generator.backtrack,
                stepLimit = generator.stepLimit,
                algorithm = generator.algorithm,
                progress = progress,
                progressTiles = null,
                xororng = xororng,
                cancellationToken = options.cancellationToken,
                failureMode = generator.failureMode,
                stats = stats,
            };

            return new TesseraGeneratorHelper(helperOptions);
        }

        private static (List<TileEntry>, List<TesseraTilemap>) GetTilesAndSamples(TesseraGenerator generator)
        {
            List<TesseraTilemap> samples = null;

            List<TileEntry> actualTiles;
            if (generator.modelType == ModelType.Overlapping || generator.modelType == ModelType.Adjacent)
            {
                samples = generator.samples
                    .Select(x => ToTesseraTilemap(generator, x))
                    .Where(x => x != null)
                    .ToList();

                actualTiles = samples
                    .SelectMany(x => x.Data.Values)
                    .Select(x => x.Tile)
                    .Distinct()
                    // TODO: Does the weight actually matter?
                    .Select(x => new TileEntry { tile = x, weight = 1 })
                    .ToList();

                // TODO: To validation
                if (samples.Count == 0)
                {
                    throw new Exception($"No samples specified.");
                }
                if (actualTiles.Count == 0)
                {
                    throw new Exception($"No tiles found in the samples");
                }
            }
            else if (generator.modelType == ModelType.AdjacentPaint || generator.modelType == ModelType.MeshEdges)
            {
                actualTiles = generator.tiles;
            }
            else
            {
                throw new Exception($"Unknown model type {generator.modelType}");
            }

            return (actualTiles, samples);
        }

        private static List<ITesseraInitialConstraint> GetAllInitialConstraints(TesseraGenerator generator, TesseraGenerateOptions options, Sylves.IGrid grid)
        {
            var actualInitialConstraints = new List<ITesseraInitialConstraint>();

            actualInitialConstraints.AddRange(GetSubmeshTilesInitialConstraints(generator, grid));

            actualInitialConstraints.AddRange(options.initialConstraints ?? SearchInitialConstraints(generator, grid));

            actualInitialConstraints.AddRange(GetGeneratorInitialConstraints(generator, grid));

            return actualInitialConstraints;
        }


        private static IEnumerable<ITesseraInitialConstraint> GetSubmeshTilesInitialConstraints(TesseraGenerator generator, Sylves.IGrid grid)
        {
            if (generator.surfaceMesh != null && generator.filterSurfaceSubmeshTiles)
            {
                foreach (var (subMesh, tileList) in generator.surfaceSubmeshTiles.Select((x, i) => (i, x)))
                {
                    var cells = grid.GetCells().Where(x => x.y == subMesh).ToList();
                    var volumeFilter = new TesseraVolumeFilter
                    {
                        name = "Submesh " + subMesh.ToString(),
                        tiles = tileList.tiles,
                        cells = cells,
                    };

                    yield return volumeFilter;
                }
            }
            yield break;
        }

        private static IEnumerable<ITesseraInitialConstraint> SearchInitialConstraints(TesseraGenerator generator, Sylves.IGrid grid)
        {
            var initialConstraintBuilder = new TesseraInitialConstraintBuilder(generator.transform, grid);
            if (generator.searchInitialConstraints)
            {
                return initialConstraintBuilder.SearchInitialConstraints() ?? Enumerable.Empty<ITesseraInitialConstraint>();
            }
            else
            {
                return Enumerable.Empty<ITesseraInitialConstraint>();
            }
        }

        private static IEnumerable<ITesseraInitialConstraint> GetGeneratorInitialConstraints(TesseraGenerator generator, Sylves.IGrid grid)
        {
            foreach (var constraintComponent in generator.GetComponents<TesseraConstraint>())
            {
                if (constraintComponent.enabled)
                {
                    var constraints = constraintComponent.GetInitialConstraints(grid);
                    foreach (var c in constraints)
                        yield return c;
                }
            }
            yield break;
        }

        /// <summary>
        /// Converts generator constraints into a format suitable for DeBroglie.
        /// </summary>
        private static List<ITileConstraint> GetTileConstraints(TesseraGenerator generator, TileModelInfo tileModelInfo, Sylves.IGrid grid)
        {
            var l = new List<ITileConstraint>();
            foreach (var constraintComponent in generator.GetComponents<TesseraConstraint>())
            {
                if (constraintComponent.enabled)
                {
                    var constraints = constraintComponent.GetTileConstraint(tileModelInfo, grid);
                    l.AddRange(constraints);
                }
            }
            return l;
        }

        #endregion

        /// <summary>
        /// Reads the tiles in a given game object into a list of TesseraTileInstance
        /// </summary>
        internal static TesseraTilemap ToTesseraTilemap(TesseraGenerator generator, GameObject parent)
        {
            if (parent == null)
                return null;

            var transform = parent.transform;
            var cellSize = Vector3.zero;
            Sylves.IGrid tempGrid = null;

            var data = new Dictionary<Vector3Int, ModelTile>();

            var tilemap = transform.GetComponent<Tilemap>();

            var tilesByPrefix = new PrefixLookup<TesseraTileBase>();
            foreach (var tile in generator.tiles)
            {
                tilesByPrefix.Add(tile.tile.name, tile.tile);
            }
            TesseraTileBase FindTile(GameObject go)
            {
                // Use pin if available
                var pinned = go.GetComponent<TesseraPinned>();
                if (pinned != null)
                {
                    return pinned.tile;
                }
                return FindTileByName(go.name);
            }


            TesseraTileBase FindTileByName(string name)
            {
                if (tilesByPrefix.TryFindLongestPrefix(name, out var tile))
                {
                    return tile;
                }
                throw new Exception($"Couldn't find tile for {name}");
            }




            if (tilemap != null)
            {
                // Create grid
                var layoutGrid = tilemap.layoutGrid;
                if (tilemap.cellLayout == GridLayout.CellLayout.Rectangle)
                {
                    tempGrid = new Sylves.SquareGrid(tilemap.cellSize);
                }
                else
                {
                    throw new Exception($"Unsupported tilemap cellLayout {tilemap.cellLayout}");
                }

                foreach (var cell in tilemap.cellBounds.allPositionsWithin)
                {
                    var unityTile = tilemap.GetTile(cell);
                    if (unityTile == null)
                        continue;
                    var tile = FindTileByName(unityTile.name);
                    // TODO: Rotation, big tiles
                    if (tile.sylvesOffsets.Count > 1)
                        throw new Exception("Big tiles not supported when reading samples from tilemaps");
                    if (tilemap.GetTransformMatrix(cell).rotation != Quaternion.identity)
                        throw new NotImplementedException("Rotations not supported when reading samples from tilemaps");
                    var modelTile = new ModelTile { Tile = tile, Offset = tile.sylvesOffsets[0], Rotation = tempGrid.GetCellType().GetIdentity() };
                    data[cell] = modelTile;
                }
            }
            else
            {
                bool foundFirst = false;
                foreach (Transform childTransform in transform)
                {

                    // Figure out the tile in question
                    var tile = FindTile(childTransform.gameObject);
                    if (tile == null)
                        continue;

                    if (tile is TesseraTile cubeTile)
                    {
                        // Work out position of this cell in the temp grid
                        var baseOffset = tile.sylvesOffsets.First();
                        var baseOffsetCenter = tile.SylvesCellGrid.GetCellCenter(baseOffset, tile.center, cellSize);

                        // Infer the grid to use
                        if (!foundFirst)
                        {
                            foundFirst = true;
                            cellSize = cubeTile.cellSize;
                            var origin = (transform.worldToLocalMatrix * childTransform.transform.localToWorldMatrix).MultiplyPoint(baseOffsetCenter);
                            tempGrid = new Sylves.TransformModifier(
                                new Sylves.CubeGrid(cellSize),
                                Matrix4x4.Translate(origin)
                                );
                        }

                        // Work out cell from position
                        if (!tempGrid.FindCell(baseOffsetCenter, transform.worldToLocalMatrix * childTransform.transform.localToWorldMatrix, out var cell, out var cellRotation))
                        {
                            Debug.LogWarning($"Couldn't find cell position for cell {tile.name}");
                            continue;
                        }

                        var modelTile = new ModelTile { Tile = tile, Offset = baseOffset, Rotation = cellRotation };
                        foreach (var kv in SylvesTesseraTilemapConversions.GrowModelTile((Vector3Int)cell, modelTile, tempGrid))
                        {
                            data[kv.Key] = kv.Value;
                        }
                    }
                    else
                    {
                        throw new NotImplementedException("Non-cube tiles not supported");
                    }
                }

                if (!foundFirst)
                {
                    return null;
                }
            }

            // Translate tempGrid to the actual grid.
            // This is necessary as grid bounds implicitly start from zero
            var minCell = data.Keys.Aggregate(Vector3Int.Min);
            var maxCell = data.Keys.Aggregate(Vector3Int.Max);
            var size = maxCell - minCell + Vector3Int.one;
            var unwrapped = tempGrid.Unwrapped;
            var grid = unwrapped is Sylves.CubeGrid ? (Sylves.IGrid)tempGrid.BoundBy(new Sylves.CubeBound(Vector3Int.zero, size)) :
                unwrapped is Sylves.SquareGrid ? tempGrid.BoundBy(new Sylves.SquareBound(Vector2Int.zero, new Vector2Int(size.x, size.y))) :
                throw new Exception();
            data = data.ToDictionary(kv => kv.Key - minCell, kv => kv.Value);

            // Experimental: rather than translating, we use bounds that are non zero based
            //var grid = tempGrid.BoundBy(tempGrid.GetBound(data.Keys.Select(x=>(Sylves.Cell)x)));

            return new TesseraTilemap
            {
                Grid = grid,
                Data = data,
            };
        }
        #region Instantiation


        // See showUncertainty
        public static void InstantiateUncertaintyObjects(TesseraGenerator generator, Sylves.IGrid grid, TilePropagator propagator)
        {
            var tileCount = propagator.TileModel.Tiles.Count();
            const float MinScale = 0.0f;
            const float MaxScale = 1.0f;

            var modelTileSets = propagator.ToValueSets<ModelTile>();

            foreach (var cell in grid.GetCells())
            {
                var i = grid.GetIndex(cell);
                var modelTiles = modelTileSets.Get(i);
                if (modelTiles == null || modelTiles.Count == 1)
                {
                    continue;
                }
                var isContradiction = modelTiles.Count == 0;
                // TODO: A lot of this seems shared with GetTesseraTileInstance. Refactor?
                var name = (isContradiction ? "Contradiction" : "Uncertain") + $" ({ cell.x }, { cell.y}, { cell.z})";
                //var go = new GameObject(name);
                var tile = isContradiction && generator.contradictionTile != null ? generator.contradictionTile : generator.uncertaintyTile;
                if (tile == null)
                {
                    continue;
                }
                var go = GameObject.Instantiate(tile);
                go.name = name;
                go.transform.parent = generator.transform;
                go.transform.localPosition = grid.GetCellCenter(cell);
                if (generator.scaleUncertainyTile && !isContradiction)
                {
                    var scale = (MaxScale - MinScale) * modelTiles.Count / tileCount + MinScale;
                    go.transform.localScale = go.transform.localScale * scale;
                }
                var uncertaintyInspector = go.GetComponent<TesseraUncertainty>();
                // Always add
                if (uncertaintyInspector == null)
                {
                    uncertaintyInspector = go.AddComponent<TesseraUncertainty>();
                }
                uncertaintyInspector.modelTiles = modelTiles;
            }
        }

        /// <summary>
        /// Utility function that instantiates a tile instance in the scene.
        /// This is the default function used when you do not pass <c>onCreate</c> to the Generate method.
        /// It is essentially the same as Unity's normal Instantiate method with extra features:
        /// * respects <see cref="TesseraTileBase.instantiateChildrenOnly"/>
        /// * applies mesh transformations (Pro only)
        /// </summary>
        /// <param name="instance">The instance being created.</param>
        /// <param name="parent">The game object to parent the new game object to. This does not affect the world position of the instance</param>
        /// <returns>The game objects created.</returns>
        public static GameObject[] Instantiate(TesseraTileInstance instance, Transform parent, IEngineInterface engine = null)
        {
            return Instantiate(instance, parent, instance.Tile.gameObject, instance.Tile.instantiateChildrenOnly, engine);
        }

        /// <summary>
        /// Utility function that instantiates a tile instance in the scene.
        /// This is the default function used when you do not pass <c>onCreate</c> to the Generate method.
        /// It is essentially the same as Unity's normal Instantiate method with extra features:
        /// * respects <see cref="TesseraTileBase.instantiateChildrenOnly"/>
        /// * applies mesh transformations (Pro only)
        /// </summary>
        /// <param name="instance">The instance being created.</param>
        /// <param name="parent">The game object to parent the new game object to. This does not affect the world position of the instance</param>
        /// <param name="gameObject">The game object to actually instantiate. Usually this is instance.Tile.gameObject</param>
        /// <param name="instantiateChildrenOnly">Should gameObject be created, or just it's children.</param>
        /// <returns>The game objects created.</returns>
        public static GameObject[] Instantiate(TesseraTileInstance instance, Transform parent, GameObject gameObject, bool instantiateChildrenOnly, IEngineInterface engine = null)
        {
            var transformsAndGameObjects = InstantiateUndeformed(instance, parent, gameObject, instantiateChildrenOnly, engine);
            var gameObjects = transformsAndGameObjects.Select(x => x.Item2).ToArray();
            if (instance.MeshDeformation != null)
            {
                var cell = instance.Cells.First();
                foreach (var (localTransform, go) in transformsAndGameObjects)
                {
                    // MeshDeformation maps vertices from tile space to generator space.
                    // We include some matrices so we can go from child space -> tile space -[deform]-> generator space -> go space
                    var childToTile = localTransform;
                    var tileToGenerator = instance.MeshDeformation;
                    var generatorToGo = TRS.Local(go.transform).ToMatrix().inverse;
                    var meshDeformation = generatorToGo * tileToGenerator * childToTile;
                    MeshUtils.TransformRecursively(go, meshDeformation);
                }
            }
            else
            {
                // Flip box transformations to stop Unity whining: "BoxColliders does not support negative scale or size."
                foreach (var go in gameObjects)
                {
                    var localScale = go.transform.localScale;
                    var flip = new Vector3(Math.Sign(localScale.x), Math.Sign(localScale.y), Math.Sign(localScale.z));
                    if (flip == Vector3.one)
                        continue;
                    foreach (var bc in go.GetComponentsInChildren<BoxCollider>())
                    {
                        bc.size = Vector3.Scale(flip, bc.size);
                    }
                }
            }
            return gameObjects;
        }


        private static (Matrix4x4, GameObject)[] InstantiateUndeformed(TesseraTileInstance instance, Transform parent, GameObject gameObject, bool instantiateChildrenOnly, IEngineInterface engine = null)
        {
            engine = engine ?? UnityEngineInterface.Instance;
            var cell = instance.Cell;
            if (instantiateChildrenOnly)
            {
                // These two methods are mostly equivalent, but we need to investigate which is actually faster in Unity
                if (true)
                {
                    var worldTransform = Matrix4x4.TRS(instance.Position, instance.Rotation, instance.LossyScale);
                    var localTransform = Matrix4x4.TRS(instance.LocalPosition, instance.LocalRotation, instance.LossyScale);
                    return gameObject.transform.Cast<Transform>().Select(child =>
                    {
                        var childToInstance = gameObject.transform.worldToLocalMatrix * child.transform.localToWorldMatrix;
                        var local = new TRS(localTransform * childToInstance);
                        var world = new TRS(worldTransform * childToInstance);
                        var go = engine.Instantiate(child.gameObject, world.Position, world.Rotation, parent);
                        go.transform.localScale = local.Scale;
                        go.name = child.gameObject.name + $" ({cell.x}, {cell.y}, {cell.z}) {instance.CellRotation}";
                        return (childToInstance, go);
                    }).ToArray();
                }
                /*
                else
                {
                    var go = GameObject.Instantiate(instance.Tile.gameObject, instance.Position, instance.Rotation, parent);
                    go.transform.localScale = instance.LocalScale;
                    var children = new List<GameObject>();
                    foreach (Transform child in go.transform)
                    {
                        children.Add(child.gameObject);
                        child.SetParent(parent);
                        child.name = child.name + $" ({cell.x}, {cell.y}, {cell.z})";
                    }
                    Destroy(go);
                    return children.Select(x => (Matrix4x4.identity, x)).ToArray();
                }
                */
            }
            else
            {
                var go = engine.Instantiate(gameObject, instance.Position, instance.Rotation, parent);
                go.transform.localScale = instance.LocalScale;
                go.name = gameObject.name + $" ({cell.x}, {cell.y}, {cell.z}) {instance.CellRotation}";
                return new[] { (Matrix4x4.identity, go) };
            }
        }
#endregion
    }



}
