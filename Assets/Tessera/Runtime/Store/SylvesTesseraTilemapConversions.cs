using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    internal static class SylvesTesseraTilemapConversions
    {
        internal static IEnumerable<TesseraTileInstance> ToTileInstances(TesseraTilemap tilemap, TRS align = null)
        {
            return ToTileInstances(tilemap.Data, tilemap.Grid);
        }

        internal static IEnumerable<TesseraTileInstance> ToTileInstances(IDictionary<Vector3Int, ModelTile> data, Sylves.IGrid grid, TRS align = null)
        {
            var filled = new HashSet<Vector3Int>();
            foreach (var kv in data)
            {
                var cell = kv.Key;
                var modelTile = kv.Value;
                // Skip if already filled
                if (filled.Contains(cell))
                    continue;
                var tile = modelTile.Tile;
                if (tile == null)
                    continue;

                var ti = ToTileInstance(cell, modelTile, grid);

                // Fill locations
                foreach (var p2 in ti.Cells)
                {
                    filled.Add(p2);
                }

                if(align != null && ti != null)
                {
                    ti.Align(align);
                }

                if (ti != null)
                {
                    yield return ti;
                }
            }
        }

        internal static IEnumerable<KeyValuePair<Vector3Int, ModelTile>> GrowModelTile(Vector3Int p, ModelTile modelTile, Sylves.IGrid grid)
        {
            var rot = modelTile.Rotation;
            var tile = modelTile.Tile;
            for (var i = 0; i < tile.sylvesOffsets.Count; i++)
            {
                var offset = tile.sylvesOffsets[i];
                //if (!grid.TryMoveByOffset((Sylves.Cell)p, modelTile.Offset, offset, rot, out var cell, out var rotation))
                if (!grid.ParallelTransport(grid.GetCellType().GetCellGrid(), (Sylves.Cell)modelTile.Offset, (Sylves.Cell)offset, (Sylves.Cell)p, rot, out var cell, out var rotation))
                {
                    throw new Exception($"BigTile {modelTile.Tile} is not fully contained in topology. This indicates an internal error.");
                }
                var mt2 = new ModelTile { Tile = tile, Offset = offset, Rotation = rotation };
                yield return new KeyValuePair<Vector3Int, ModelTile>((Vector3Int)cell, mt2);
            }
        }

        // TODO: Optimize this method or allow users to spread the cost over multiple frames.
        private static TesseraTileInstance ToTileInstance(Vector3Int cell, ModelTile modelTile, Sylves.IGrid grid)
        {
            var rot = modelTile.Rotation;
            var tile = modelTile.Tile;
            var cellGrid = tile.SylvesCellGrid;
            var cellType = cellGrid.GetCellType();

            var cellToGridTrs = grid.GetTRS((Sylves.Cell)cell);
            var cellRotTrs = new Sylves.TRS(cellType.GetMatrix(rot));
            var cellToTileTrs = cellGrid.GetCellSizeTransform(tile.center, tile.cellSize) * Matrix4x4.Translate(cellGrid.GetCellCenter((Sylves.Cell)modelTile.Offset));
            var tileToCellTrs = new Sylves.TRS(cellToTileTrs.inverse);

            var localTrs = cellToGridTrs * cellRotTrs * tileToCellTrs;

            var cells = new Vector3Int[tile.sylvesOffsets.Count];
            var rotations = new Sylves.CellRotation[tile.sylvesOffsets.Count];
            for (var i = 0; i < tile.sylvesOffsets.Count; i++)
            {
                var offset = tile.sylvesOffsets[i];
                //if (!grid.TryMoveByOffset((Sylves.Cell)cell, modelTile.Offset, offset, rot, out var endCell, out var endRotation))
                if (!grid.ParallelTransport(cellGrid, (Sylves.Cell)modelTile.Offset, (Sylves.Cell)offset, (Sylves.Cell)cell, rot, out var endCell, out var endRotation))
                {
                    throw new Exception($"BigTile {modelTile.Tile} is not fully contained in topology. This indicates an internal error.");
                }
                cells[i] = (Vector3Int)endCell;
                rotations[i] = endRotation;
            }
            var instance = new TesseraTileInstance
            {
                Tile = tile,
                LocalPosition = localTrs.Position,
                LocalRotation = localTrs.Rotation,
                LocalScale = localTrs.Scale,
                Cell = cells[0],
                Cells = cells,
                CellRotation = rot,
                CellRotations = rotations,
            };
            if (grid.Unwrapped is Sylves.MeshPrismGrid)
            {
                instance.MeshDeformation = GetDeformation(grid, instance);
            }
            return instance;
        }

        /// <summary>
        /// Get tile to grid deformation for a single cell.
        /// </summary>
        private static Sylves.Deformation GetDeformation(Sylves.IGrid grid, TesseraTileInstance i, Vector3Int cell, Vector3Int offset, Sylves.CellRotation rotation)
        {
            // NB: This has a lot of duplication from ToTileInstance? Share more?
            var tile = i.Tile;
            var cellGrid = tile.SylvesCellGrid;
            var cellType = cellGrid.GetCellType();

            var cellToGridDeformation = grid.GetDeformation((Sylves.Cell)cell);
            var cellRotMatrix = cellType.GetMatrix(rotation);
            var cellToTileMatrix = cellGrid.GetCellSizeTransform(tile.center, tile.cellSize) * Matrix4x4.Translate(cellGrid.GetCellCenter((Sylves.Cell)offset));
            var tileToCellMatrix = cellToTileMatrix.inverse;

            return cellToGridDeformation * cellRotMatrix * tileToCellMatrix;
        }


        /// <summary>
        /// Get tile to grid deformation that covers an entire tile instance.
        /// </summary>
        private static Sylves.Deformation GetDeformation(Sylves.IGrid grid, TesseraTileInstance i)
        {
            if (i.Cells.Length == 1)
            {
                var cell = i.Cells[0];
                var rotation = i.CellRotations[0];
                var offset = i.Tile.sylvesOffsets[0];

                return GetDeformation(grid, i, cell, offset, rotation);
            }
            else
            {
                // For big tiles, we need to load the transform for every cell the tile covers
                // and apply the correct one

                var tile = i.Tile;
                var cellGrid = tile.SylvesCellGrid;
                var cellType = cellGrid.GetCellType();
                var cellToTileMatrix = cellGrid.GetCellSizeTransform(tile.center, tile.cellSize);
                var tileToCellMatrix = cellToTileMatrix.inverse;

                var initialTransformsByOffset = new Dictionary<Vector3Int, Sylves.Deformation>();
                var offsetsPositions = new List<(Vector3Int, Vector3)>();
                var deformationsByOffset = new Dictionary<Vector3Int, Sylves.Deformation>();
                for (var x = 0; x < tile.sylvesOffsets.Count; x++)
                {
                    var cell = i.Cells[x];
                    var rotation = i.CellRotations[x];
                    var offset = tile.sylvesOffsets[x];
                    deformationsByOffset[offset] = initialTransformsByOffset[offset] = GetDeformation(grid, i, cell, offset, rotation);
                    offsetsPositions.Add((offset, cellGrid.GetCellCenter(offset, tile.center, tile.cellSize)));
                }
                Sylves.Deformation GetNearest(Vector3 v)
                {
                    var cellV = tileToCellMatrix.MultiplyPoint3x4(v);

                    cellGrid.FindCell(cellV, out var cell);
                    var offset = (Vector3Int)cell;

                    // Check cache, do we already have a deformation for this cell?
                    if (deformationsByOffset.TryGetValue(offset, out var nearest))
                    {
                        return nearest;
                    }
                    // Otherwise find cell closest by distance, and use that deformation
                    // TODO: Is there some way to cache this?
                    var nearestOffset = Vector3Int.zero;
                    var nearestDist = float.PositiveInfinity;
                    foreach (var (o, op) in offsetsPositions)
                    {
                        var dist = (op - v).sqrMagnitude;
                        if (dist < nearestDist)
                        {
                            nearestOffset = o;
                            nearestDist = dist;
                        }
                    }
                    return initialTransformsByOffset[nearestOffset];
                }
                // Functions that forward to the nearest deformation
                Vector3 DeformPoint(Vector3 p)
                {
                    return GetNearest(p).DeformPoint(p);
                }
                Vector3 DeformNormal(Vector3 p, Vector3 v)
                {
                    return GetNearest(p).DeformNormal(p, v);
                }
                Vector4 DeformTangent(Vector3 p, Vector4 t)
                {
                    return GetNearest(p).InnerDeformTangent(p, t);
                }
                void GetJacobi(Vector3 p, out Matrix4x4 jacobi)
                {
                    GetNearest(p).GetJacobi(p, out jacobi);
                }
                return new Sylves.Deformation(DeformPoint, DeformNormal, DeformTangent, GetJacobi, deformationsByOffset.First().Value.InvertWinding);
            }
        }

        internal static IDictionary<Vector3Int, ModelTile> ToModelTiles(IList<TesseraTileInstance> instances)
        {
            var results = new Dictionary<Vector3Int, ModelTile>();
            foreach (var i in instances)
            {
                for (var j = 0; j < i.Cells.Length; j++)
                {
                    results[i.Cells[j]] = new ModelTile(i.Tile, i.CellRotations[j], i.Tile.sylvesOffsets[j]);
                }
            }
            return results;
        }
    }
}
