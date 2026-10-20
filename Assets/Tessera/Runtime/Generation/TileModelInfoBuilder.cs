using DeBroglie;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Tessera.TileModelInfo;

namespace Tessera
{
    internal class TileModelInfoBuilder
    {
        TileModelInfo tileModelInfo;
        private readonly List<TileEntry> tiles;
        private readonly Sylves.IGrid cellGrid;
        private readonly Sylves.ICellType cellType;

        private TileModelInfoBuilder(List<TileEntry> tiles, Sylves.IGrid cellGrid, bool isMeshEdges, float meshEdgesTolerance)
        {
            if (tiles == null || tiles.Count == 0)
            {
                throw new Exception("Cannot run generator with zero tiles configured.");
            }

            this.cellGrid = cellGrid;
            this.cellType = cellGrid.GetCellType();

            // Filter out bad tiles
            this.tiles = tiles.Where(x =>
            {
                if (x.tile == null)
                {
                    return false;
                }
                if (!IsContiguous(x.tile))
                {
                    Debug.LogWarning($"Cannot use {x.tile} as it is not contiguous");
                    return false;
                }
                return true;
            }).ToList();


            tileModelInfo = new TileModelInfo();
            tileModelInfo.DirectionMapping = SylvesDeBroglieUtils.GetDirectionMapping(cellGrid);
            SetupCanonicalization();
            SetupAllTilesAndInternalAdjacencies();
            if (isMeshEdges)
            {
                SetupTilesByDirectionFromMeshes(meshEdgesTolerance);
            }
            else
            {
                SetupTilesByDirection();
            }
        }

        public TileModelInfo Build() => tileModelInfo;

        private void SetupCanonicalization()
        {
            // Canonicalize
            var canonical = new Dictionary<Tile, Tile>();
            foreach (var tile in tiles.Select(x => x.tile))
            {
                var rots = cellType.GetRotations(tile.rotatable, tile.reflectable, tile.rotationGroupType).ToList();
                var done = new HashSet<Sylves.CellRotation>();
                foreach (var rot1 in rots)
                {
                    if (tile.symmetric)
                    {
                        foreach (var rot2 in rots)
                        {
                            if (done.Contains(rot2))
                                continue;

                            if (IsPaintEquivalent(tile, rot1, rot2, out var _, out var realign))
                            {
                                foreach (var kv in realign)
                                {
                                    var modelTile1 = new ModelTile
                                    {
                                        Tile = tile,
                                        Rotation = rot1,
                                        Offset = kv.Key,
                                    };
                                    var modelTile2 = new ModelTile
                                    {
                                        Tile = tile,
                                        Rotation = rot2,
                                        Offset = kv.Value,
                                    };
                                    canonical[new Tile(modelTile2)] = new Tile(modelTile1);
                                }
                                //Debug.Log($"Canonicalize {tile} {rot2} -> {rot1}");
                                done.Add(rot2);
                            }
                        }
                    }
                    else
                    {
                        foreach (var offset in tile.sylvesOffsets)
                        {
                            var modelTile = new Tile(new ModelTile
                            {
                                Tile = tile,
                                Rotation = rot1,
                                Offset = offset,
                            });
                            canonical[modelTile] = modelTile;
                        }
                    }
                }
            }
            var uncanonical = canonical.ToLookup(x => x.Value, x => x.Key);

            tileModelInfo.Canonicalization = canonical;
            tileModelInfo.Uncanonicalization = uncanonical;
        }

        private void SetupAllTilesAndInternalAdjacencies()
        {
            var uncanonicalization = tileModelInfo.Uncanonicalization;


            var allTiles = new List<(Tile, float)>();
            var internalAdjacencies = new List<InternalAdjacency>();

            // Build all tiles
            foreach (var tileEntry in tiles)
            {
                var tile = tileEntry.tile;

                foreach (var rot in cellType.GetRotations(tile.rotatable, tile.reflectable, tile.rotationGroupType))
                {
                    var sylvesOffsets = tile.sylvesOffsets;
                    // Set up internal connections
                    foreach (var offset in sylvesOffsets)
                    {
                        var modelTile = new Tile(new ModelTile(tile, rot, offset));

                        if (!uncanonicalization.Contains(modelTile))
                            continue;

                        var frequency = tileEntry.weight * uncanonicalization[modelTile].Count() / sylvesOffsets.Count;
                        allTiles.Add((modelTile, frequency));

                        // Skip tiles without any internal connections
                        if (sylvesOffsets.Count <= 1)
                            continue;

                        foreach (var dir in cellType.GetCellDirs())
                        {
                            if (!cellGrid.TryMove((Sylves.Cell)offset, dir, out var offset2, out var inverseDir, out var connection))
                                continue;
                            if (connection != new Sylves.Connection())
                            {
                                throw new NotImplementedException();
                            }
                            if (!sylvesOffsets.Contains((Vector3Int)offset2))
                                continue;
                            var modelTile2 = new Tile(new ModelTile(tile, rot, (Vector3Int)offset2));

                            if (!uncanonicalization.Contains(modelTile2))
                                continue;

                            internalAdjacencies.Add(new InternalAdjacency
                            {
                                Src = modelTile,
                                Dest = modelTile2,
                                GridDir = cellType.Rotate(dir, rot),
                                InverseGridDir = cellType.Rotate(inverseDir, rot),
                            });
                        }
                    }
                }
            }

            tileModelInfo.AllTiles = allTiles;
            tileModelInfo.InternalAdjacencies = internalAdjacencies;
        }

        private void SetupTilesByDirection()
        {
            var uncanonicalization = tileModelInfo.Uncanonicalization;

            var tilesByDirection = cellType.GetCellDirs().ToDictionary(d => d, _ => new List<(FaceDetails, Tile)>());

            // Generate all tiles, and extract their face details
            foreach (var tileEntry in tiles)
            {
                var tile = tileEntry.tile;
                foreach (var rot in cellType.GetRotations(tile.rotatable, tile.reflectable, tile.rotationGroupType))
                {
                    // Set up external connections
                    foreach (var (offset, dir, faceDetails) in tile.sylvesFaceDetails)
                    {
                        var modelTile = new Tile(new ModelTile(tile, rot, offset));

                        if (!uncanonicalization.Contains(modelTile))
                            continue;

                        var (rFaceDir, rFaceDetails) = cellType.RotateBy(dir, faceDetails, rot);
                        tilesByDirection[rFaceDir].Add((rFaceDetails, modelTile));
                    }
                }
            }

            tileModelInfo.TilesByDirection = tilesByDirection;
        }

        private void SetupTilesByDirectionFromMeshes(float tolerance)
        {
            var tileRotations = tileModelInfo.Canonicalization.Values
                .Select(x => (ModelTile)x.Value)
                .Select(x => (x.Tile, x.Rotation))
                .Distinct()
                .ToLookup(x=>x.Tile, x=>x.Rotation);

            var faces = new List<EdgeDetection.Face>();
            foreach (var tileEntry in tiles)
            {
                var tile = tileEntry.tile;
                var mesh = tile.GetSharedMesh();
                faces.AddRange(EdgeDetection.GetFaces(tile, tileRotations[tile], mesh, tolerance));
            }

            var sylvesTilesByDirection = faces
                .GroupBy(x => x.dir)
                .ToDictionary(g => g.Key, g => g.Select(x => (
                    new FaceDetails { faceType = FaceType.Point, meshEdgeHash1 = x.hash1, meshEdgeHash2 = x.hash2 },
                    new Tile(new ModelTile(x.tile, x.rotation, x.offset))
                )).ToList());

            tileModelInfo.TilesByDirection = sylvesTilesByDirection;
        }

        /// <summary>
        /// Summarizes the tiles, in preparation for building a model.
        /// </summary>
        internal static TileModelInfo Create(List<TileEntry> tiles, Sylves.IGrid cellGrid, bool isEdgeMeshes, float meshEdgesTolerance)
        {
            return new TileModelInfoBuilder(tiles, cellGrid, isEdgeMeshes, meshEdgesTolerance).Build();
        }

        /// <summary>
        /// Returns true if the tile has the same paint in these two different orientations, and if so
        /// the mapping from rot1 to rot2.
        /// </summary>
        private static bool IsPaintEquivalent(TesseraTileBase tile, Sylves.CellRotation rot1, Sylves.CellRotation rot2, out Sylves.CellRotation rotation, out IDictionary<Vector3Int, Vector3Int> realign)
        {
            var cellGrid = tile.SylvesCellGrid;
            var cellType = cellGrid.GetCellType();
            rotation = cellType.Multiply(cellType.Invert(rot1), rot2);
            var offsets = new HashSet<Sylves.Cell>(tile.sylvesOffsets.Select(x => (Sylves.Cell)(x)));
            var symmetry = cellGrid.FindGridSymmetry(offsets, offsets, (Sylves.Cell)tile.sylvesOffsets.First(), rotation);

            if (symmetry == null)
            {
                realign = null;
                return false;
            }
            // Check paint
            realign = new Dictionary<Vector3Int, Vector3Int>();
            foreach (var (offset, faceDir, faceDetails) in tile.sylvesFaceDetails)
            {
                if (!cellGrid.TryApplySymmetry(symmetry, (Sylves.Cell)offset, out var dest, out var r2))
                {
                    return false;
                }
                var offset2 = (Vector3Int)dest;
                realign[offset] = offset2;
                if (r2 != rotation)
                {
                    throw new NotSupportedException();
                }
                var (faceDir2, faceDetails2) = cellType.RotateBy(faceDir, faceDetails, rotation);
                var otherFaceDetails = tile.Get(offset2, faceDir2);
                if (!faceDetails2.IsEquivalent(otherFaceDetails))
                    return false;
            }
            // Loop over offsets too to cover offsets with no paint
            foreach (var offset in tile.sylvesOffsets)
            {
                if (!cellGrid.TryApplySymmetry(symmetry, (Sylves.Cell)offset, out var dest, out var r2))
                {
                    return false;
                }
                var offset2 = (Vector3Int)dest;
                realign[offset] = offset2;
            }
            return true;
        }

        private static bool IsContiguous(TesseraTileBase tile)
        {
            if (!(tile is TesseraTile))
            {
                // TODO
                return true;
            }

            if (tile.sylvesOffsets.Count == 1)
                return true;

            // Floodfill offset
            var offsets = new HashSet<Vector3Int>(tile.sylvesOffsets);
            var toRemove = new Stack<Vector3Int>();
            toRemove.Push(offsets.First());
            while (toRemove.Count > 0)
            {
                var o = toRemove.Pop();
                offsets.Remove(o);

                foreach (CubeFaceDir faceDir in Enum.GetValues(typeof(CubeFaceDir)))
                {
                    var o2 = o + faceDir.Forward();
                    if (offsets.Contains(o2))
                    {
                        toRemove.Push(o2);
                    }
                }
            }

            return offsets.Count == 0;
        }
    }
}
