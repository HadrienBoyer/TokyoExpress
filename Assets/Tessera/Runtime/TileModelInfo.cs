using DeBroglie;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Contains an analysis of a set of model tiles. 
    /// In particular, it has adjacency information which lets
    /// you look up which tiles connect to each other, and "canonicalization"
    /// which describes tile symmetry.
    /// </summary>
    internal class TileModelInfo
    {

        // All tiles, and the weights
        public List<(Tile, float)> AllTiles { get; set; }

        // Adjacencies necessary to make big tiles work
        public List<InternalAdjacency> InternalAdjacencies { get; set; }

        // Tiles sorted by their outside face
        public Dictionary<Sylves.CellDir, List<(FaceDetails, Tile)>> TilesByDirection { get; set; }

        // Relationship between Sylves dirs and DeBroglie dirs.
        public BiMap<Sylves.CellDir, Direction> DirectionMapping { get; set; }

        // For Symmetric tiles, picks a particular rotation to be the canonical one.
        // All the other equivalent rotations are not used in DeBroglie, but are listed here
        // so we can rotate tiles then find the canonical one.
        public Dictionary<Tile, Tile> Canonicalization { get; set; }

        // Inverse of Canonicalization
        public ILookup<Tile, Tile> Uncanonicalization { get; set; }

        internal class InternalAdjacency
        {
            public Tile Src { get; set; }
            public Tile Dest { get; set; }
            // Dir from Src to Dest in grid space (i.e. post rotation)
            public Sylves.CellDir GridDir { get; set; }
            public Sylves.CellDir InverseGridDir { get; set; }
        }

        public static bool CanPlaceAdjacent(TesseraTileBase tile1, TesseraTileBase tile2, Sylves.CellDir dir, TesseraPalette palette = null)
        {
            return CanPlaceAdjacent(tile1, tile2, dir, tile1.SylvesCellType.GetIdentity(), tile2.SylvesCellType.GetIdentity(), palette);
        }

        public static bool CanPlaceAdjacent(TesseraTileBase tile1, TesseraTileBase tile2, Sylves.CellDir dir, Sylves.CellRotation rotation1, Sylves.CellRotation rotation2, TesseraPalette palette = null)
        {
            palette = palette ?? TesseraPalette.defaultPalette;
            var cellType = tile1.SylvesCellType;
            if (tile1.sylvesOffsets.Count > 1 || tile2.sylvesOffsets.Count > 1)
                throw new NotSupportedException("This method doesn't work on big tiles");
            var dir1 = dir;
            var dir2 = cellType.Invert(dir1);

            foreach (var (offset1, dir1_, faceDetails1) in tile1.sylvesFaceDetails)
            {
                var (rFaceDir1, rFaceDetails1) = cellType.RotateBy(dir1_, faceDetails1, rotation1);
                if (rFaceDir1 != dir1)
                    continue;

                foreach (var (offset2, dir2_, faceDetails2) in tile2.sylvesFaceDetails)
                {
                    var (rFaceDir2, rFaceDetails2) = cellType.RotateBy(dir2_, faceDetails2, rotation2);
                    if (rFaceDir2 != dir2)
                        continue;

                    if (!palette.Match(rFaceDetails1, rFaceDetails2))
                        return false;
                }
            }

            return true;
        }
    }
}
