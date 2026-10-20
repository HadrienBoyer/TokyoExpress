using System.Collections.Generic;
using UnityEngine;
using Sylves;
using System.Linq;

namespace Tessera
{
    public enum SubFaceType
    {
        Center,
        Dir,
        Corner,
    }

    /// <summary>
    /// Each face of a tessera tile is split up into several sub faces that are independently paintable.
    /// There's 9 for a square face - one center, 4 corners and 4 "dirs".
    /// The corner / dir values come from Sylves.SquareCellType, and similarly for faces with another number of sides.
    /// SubFaceTuple is used for address a specific subface.
    /// 
    /// It's an improvement on the earlier enum SubFace, which covers the same thing and has yet to be deprecated.
    /// </summary>
    public struct SubFaceTuple
    {
        public SubFaceType type;

        public int value;

        public SubFaceTuple(SubFaceType type, int value)
        {
            this.type = type;
            this.value = value;
        }

        public override int GetHashCode()
        {
            return (type, value).GetHashCode();
        }

        public bool Equals(SubFaceTuple other)
        {
            return type == other.type && value == other.value;
        }

        public override bool Equals(object obj)
        {
            if (obj is SubFaceTuple other)
                return Equals(other);
            return base.Equals(obj);
        }

        public override string ToString()
        {
            return $"{type} {value}";
        }

        public static bool operator ==(SubFaceTuple a, SubFaceTuple b) => a.Equals(b);
        public static bool operator !=(SubFaceTuple a, SubFaceTuple b) => !(a == b);

        public SubFaceTuple Rotate(Sylves.Connection connection)
        {
            // These formulas match NGonCellType's, but backwards.
            // As noted in ToTesseraMatrix,
            // Sylves and Tessera have opposite conventions
            var rotation = connection.Sides - connection.Rotation;
            var n = connection.Sides;
            switch (type)
            {
                case SubFaceType.Center:
                    return this;
                case SubFaceType.Dir:
                    if (!connection.Mirror)
                    {
                        return new SubFaceTuple { type = type, value = (value + rotation) % n };
                    }
                    else
                    {
                        return new SubFaceTuple { type = type, value = (n - value + rotation) % n };
                    }
                case SubFaceType.Corner:
                    if (!connection.Mirror)
                    {
                        return new SubFaceTuple { type = type, value = (value + rotation) % n };
                    }
                    else
                    {
                        return new SubFaceTuple { type = type, value = (1 + n - value + rotation) % n };
                    }
                default:
                    throw new System.Exception();
            }
        }
    }

    // Converts from a SubFaceTuple to a CellDir and CellCorner.
    // The former is relative to a specific face, the latter is relative to the whole cell.
    public interface ISubFaceInfo
    {
        IEnumerable<SubFaceTuple> GetSubFaces(Vector3Int offset, Sylves.CellDir dir);

        //SubFaceTuple GetSubFace(Vector3Int offset, Sylves.CellDir dir, Sylves.CellDir dir2);
        //SubFaceTuple GetSubFace(Vector3Int offset, Sylves.CellDir dir, Sylves.CellCorner corner);

        //CellDir GetDir(Vector3Int offset, Sylves.CellDir dir, SubFaceTuple subFace);
        CellCorner GetCorner(Vector3Int offset, Sylves.CellDir dir, SubFaceTuple subFace);
    }

    internal class BaseSubFaceInfo : ISubFaceInfo
    {
        //private Dictionary<(CellDir, CellDir), SubFaceTuple> dirMappings;

        //private Dictionary<(CellDir, SubFaceTuple), CellDir> iDirMappings;

        private Dictionary<(CellDir, CellCorner), SubFaceTuple> cornerMappings;
        private Dictionary<CellDir, SubFaceTuple[]> subFaces;
        private Dictionary<(CellDir, SubFaceTuple), CellCorner> iCornerMappings;

        public BaseSubFaceInfo(ICellType cellType, Dictionary<CellDir, CellCorner[]> baseDirCorners)
        {
            // Expand baseDirCorners via rotations

            cornerMappings = new Dictionary<(CellDir, CellCorner), SubFaceTuple>();
            iCornerMappings = new Dictionary<(CellDir, SubFaceTuple), CellCorner>();
            subFaces = new Dictionary<CellDir, SubFaceTuple[]>();

            foreach (var rotation in cellType.GetRotations())
            {
                foreach(var kv in baseDirCorners)
                {
                    var dir = kv.Key;
                    cellType.Rotate(dir, rotation, out var rotatedDir, out var connection);
                    for (var i = 0; i < kv.Value.Length; i++)
                    {
                        var corner = kv.Value[i];
                        var subFace = new SubFaceTuple(SubFaceType.Corner, i);
                        var rotatedCorner = cellType.Rotate(corner, rotation);
                        var rotatedSubFace = subFace.Rotate(connection);
                        // Sanity check everything is consistent
                        if(cornerMappings.ContainsKey((rotatedDir, rotatedCorner)))
                        {
                            var expected = cornerMappings[(rotatedDir, rotatedCorner)];
                            if (expected != rotatedSubFace)
                            {
                                throw new System.Exception($"baseDir={cellType.Format(dir)} baseCorner={cellType.Format(corner)} baseSubFace={subFace} rotation={cellType.Format(rotation)} rotatedDir={cellType.Format(rotatedDir)} rotatedCorner={cellType.Format(rotatedCorner)} expected={expected} new={rotatedSubFace} connection={connection}");
                            }
                        }

                        //Debug.Log($"adding baseDir={cellType.Format(dir)} baseCorner={cellType.Format(corner)} baseSubFace={subFace} rotation={cellType.Format(rotation)} rotatedDir={cellType.Format(rotatedDir)} rotatedCorner={cellType.Format(rotatedCorner)} new={rotatedSubFace} connection={connection}");
                        cornerMappings[(rotatedDir, rotatedCorner)] = rotatedSubFace;
                        iCornerMappings[(rotatedDir, rotatedSubFace)] = rotatedCorner;
                    }
                    subFaces[rotatedDir] = NGonSubFaces(kv.Value.Length).ToArray();
                }
            }
        }


        public CellCorner GetCorner(Vector3Int offset, CellDir dir, SubFaceTuple subFace)
        {
            if (subFace.type != SubFaceType.Corner)
                throw new System.Exception();
            return iCornerMappings[(dir, subFace)];
        }

        /*
        public CellDir GetDir(Vector3Int offset, CellDir dir, SubFaceTuple subFace)
        {
            if (subFace.type != SubFaceType.Dir)
                throw new System.Exception();
            return iDirMappings[(dir, subFace)];
        }

        public SubFaceTuple GetSubFace(Vector3Int offset, CellDir dir, CellDir dir2)
        {
            return dirMappings[(dir, dir2)];
        }

        public SubFaceTuple GetSubFace(Vector3Int offset, CellDir dir, CellCorner corner)
        {
            return cornerMappings[(dir, corner)];
        }
        */

        public IEnumerable<SubFaceTuple> GetSubFaces(Vector3Int offset, CellDir dir) => subFaces[dir];

        private static IEnumerable<SubFaceTuple> NGonSubFaces(int n)
        {
            yield return new SubFaceTuple(SubFaceType.Center, 0);
            for (var i = 0; i < n; i++)
            {
                yield return new SubFaceTuple(SubFaceType.Corner, i);
                yield return new SubFaceTuple(SubFaceType.Dir, i);
            }
        }
    }

    internal class CubeSubFaceInfo : BaseSubFaceInfo
    {
        public static CubeSubFaceInfo Instance = new CubeSubFaceInfo();

        private CubeSubFaceInfo() : base(CubeCellType.Instance, new Dictionary<CellDir, CellCorner[]>
        {
            { (CellDir)CubeDir.Back, new[]{
                (CellCorner)CubeCorner.BackDownRight,
                (CellCorner)CubeCorner.BackUpRight,
                (CellCorner)CubeCorner.BackUpLeft,
                (CellCorner)CubeCorner.BackDownLeft,
            } },
        })
        { }
    }


    internal class HexPrismSubFaceInfo : BaseSubFaceInfo
    {
        public static HexPrismSubFaceInfo Instance = new HexPrismSubFaceInfo();

        private HexPrismSubFaceInfo() : base(SylvesExtensions.HexPrismCellType, new Dictionary<CellDir, CellCorner[]>
        {
            { (CellDir)SylvesHexPrismDir.Right, new[]{
                (CellCorner)SylvesHexPrismCorner.DownForwardRight,
                (CellCorner)SylvesHexPrismCorner.UpForwardRight,
                (CellCorner)SylvesHexPrismCorner.UpBackRight,
                (CellCorner)SylvesHexPrismCorner.DownBackRight,
            } },
            { (CellDir)SylvesHexPrismDir.Up, new[]{
                (CellCorner)SylvesHexPrismCorner.UpBackRight,
                (CellCorner)SylvesHexPrismCorner.UpForwardRight,
                (CellCorner)SylvesHexPrismCorner.UpForward,
                (CellCorner)SylvesHexPrismCorner.UpForwardLeft,
                (CellCorner)SylvesHexPrismCorner.UpBackLeft,
                (CellCorner)SylvesHexPrismCorner.UpBack,
            } },
            { (CellDir)SylvesHexPrismDir.Down, new[]{
                (CellCorner)SylvesHexPrismCorner.DownBackLeft,
                (CellCorner)SylvesHexPrismCorner.DownForwardLeft,
                (CellCorner)SylvesHexPrismCorner.DownForward,
                (CellCorner)SylvesHexPrismCorner.DownForwardRight,
                (CellCorner)SylvesHexPrismCorner.DownBackRight,
                (CellCorner)SylvesHexPrismCorner.DownBack,
            } },
        })
        { }
    }


    internal class UpTrianglePrismSubFaceInfo : BaseSubFaceInfo
    {

        internal UpTrianglePrismSubFaceInfo() : base(SylvesExtensions.TrianglePrismCellType, new Dictionary<CellDir, CellCorner[]>
        {
            { (CellDir)SylvesTrianglePrismDir.Back, new[]{
                (CellCorner)SylvesHexPrismCorner.DownBackRight,
                (CellCorner)SylvesTrianglePrismCorner.UpBackRight,
                (CellCorner)SylvesTrianglePrismCorner.UpBackLeft,
                (CellCorner)SylvesTrianglePrismCorner.DownBackLeft,
            } },
            { (CellDir)SylvesTrianglePrismDir.Up, new[]{
                (CellCorner)SylvesTrianglePrismCorner.UpBackRight,
                (CellCorner)SylvesTrianglePrismCorner.UpForward,
                (CellCorner)SylvesTrianglePrismCorner.UpBackLeft,
            } },
            { (CellDir)SylvesTrianglePrismDir.Down, new[]{
                (CellCorner)SylvesTrianglePrismCorner.DownBackLeft,
                (CellCorner)SylvesTrianglePrismCorner.DownForward,
                (CellCorner)SylvesTrianglePrismCorner.DownBackRight,
            } },
        })
        { }
    }

    internal class DownTrianglePrismSubFaceInfo : BaseSubFaceInfo
    {

        internal DownTrianglePrismSubFaceInfo() : base(SylvesExtensions.TrianglePrismCellType, new Dictionary<CellDir, CellCorner[]>
        {
            { (CellDir)SylvesTrianglePrismDir.Forward, new[]{
                (CellCorner)SylvesHexPrismCorner.DownForwardLeft,
                (CellCorner)SylvesTrianglePrismCorner.UpForwardLeft,
                (CellCorner)SylvesTrianglePrismCorner.UpForwardRight,
                (CellCorner)SylvesTrianglePrismCorner.DownForwardRight,
            } },
            { (CellDir)SylvesTrianglePrismDir.Up, new[]{
                (CellCorner)SylvesTrianglePrismCorner.UpForwardLeft,
                (CellCorner)SylvesTrianglePrismCorner.UpBack,
                (CellCorner)SylvesTrianglePrismCorner.UpForwardRight,
            } },
            { (CellDir)SylvesTrianglePrismDir.Down, new[]{
                (CellCorner)SylvesTrianglePrismCorner.DownForwardRight,
                (CellCorner)SylvesTrianglePrismCorner.DownBack,
                (CellCorner)SylvesTrianglePrismCorner.DownForwardLeft,
            } },
        })
        { }
    }

    internal class TrianglePrismSubFaceInfo : ISubFaceInfo
    {
        private DownTrianglePrismSubFaceInfo down = new DownTrianglePrismSubFaceInfo();
        private UpTrianglePrismSubFaceInfo up = new UpTrianglePrismSubFaceInfo();

        bool IsUp(Vector3Int offset) => (offset.x % 2) == 0;

        ISubFaceInfo Get(Vector3Int offset) => IsUp(offset) ? (ISubFaceInfo)up: (ISubFaceInfo)down;

        public static TrianglePrismSubFaceInfo Instance = new TrianglePrismSubFaceInfo();

        public CellCorner GetCorner(Vector3Int offset, CellDir dir, SubFaceTuple subFace)
        {
            return Get(offset).GetCorner(offset, dir, subFace);
        }

        public IEnumerable<SubFaceTuple> GetSubFaces(Vector3Int offset, CellDir dir)
        {
            return Get(offset).GetSubFaces(offset, dir);
        }
    }



}
