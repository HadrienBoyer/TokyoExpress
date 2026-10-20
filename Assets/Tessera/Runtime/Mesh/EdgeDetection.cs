using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sylves;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// This utility analyses a mesh, finding a hash that indicates what it can connect two.
    /// There's two steps to the analysis.
    /// * For each corner of the cell, determine if it is inside or outside the mesh. This handles non watertight meshes.
    /// * For each face of the cell, find the mesh edges coincident to that face.
    /// 
    /// The hashes are unique to a specific face and specific rotation of the tile, and are structured in a way that
    /// you can get the same has for a left pointing and right pointing face if they share the same mesh edges and corners.
    /// </summary>
    // TODO: Interior detection is kinda sucky, but we need it as we erase information about mesh edge direction (which 
    // also encodes interior information. Perhaps we could design a hash that doesn't erase that, and we could skip interior detection?
    // But we probably want to know interiors anyway
    public static class EdgeDetection
    {
        public const float DefaultTolerance = 1e-6f;

        private const bool ExtendedDebugInfo = false;

        public class Face
        {
            public TesseraTileBase tile;
            public int hash1;
            public int hash2;
            public Vector3Int offset;
            public Sylves.CellRotation rotation;
            public Sylves.CellDir dir;
        }

        /// <summary>
        /// null mesh => all exterior
        /// empty mesh => all interior
        /// </summary>
        public static HashSet<(Vector3Int, CellCorner)> GetInteriorCorners(TesseraTileBase tile, MeshData meshData, bool debug=false)
        {
            if(meshData == null)
            {
                return new HashSet<(Vector3Int, CellCorner)>();
            }

            var cellType = tile.SylvesCellType;
            var cellGrid = tile.SylvesCellGrid;

            // TODO: Use dual to reduce redundancy
            var allCorners = tile.sylvesOffsets.SelectMany(offset => cellType.GetCellCorners()
                .Select(corner => (offset, corner, pos: cellGrid.GetCellCorner(offset, corner, tile.center, tile.cellSize)))
                ).ToList();
            var interiors = IsInterior(meshData, allCorners.Select(x => x.pos).ToArray(), debug);
            return new HashSet<(Vector3Int, CellCorner)>(allCorners.Zip(interiors, (a, b) => (a, b)).Where(t => t.b).Select(t => (t.a.offset, t.a.corner)));

        }

        /// <summary>
        /// Extracts a summary of all the tile faces.
        /// </summary>
        public static IList<Face> GetFaces(TesseraTileBase tile, IEnumerable<Sylves.CellRotation> rotations, Mesh mesh, float tolerance = DefaultTolerance, bool debug = false)
        {
            var meshData = mesh == null ? null : new MeshData(mesh);
            return GetFaces(tile, rotations, meshData, GetInteriorCorners(tile, meshData), tolerance, debug);
        }

        public static IList<Face> GetFaces(TesseraTileBase tile, IEnumerable<Sylves.CellRotation> rotations, MeshData meshData, HashSet<(Vector3Int, CellCorner)> interiorCorners, float tolerance = DefaultTolerance, bool debug = false)
        {
            var cellType = tile.SylvesCellType;
            var cellGrid = tile.SylvesCellGrid;
            var subFaceInfo = tile.SubFaceInfo;
            var results = new List<Face>();

            if (meshData == null)
            {
                // Return an empty cube.
                // Note this is a different result to a mesh with no faces!
                foreach (var rot in rotations)
                {
                    foreach (var (offset, dir, faceDetails) in tile.sylvesFaceDetails)
                    {
                        results.Add(new Face
                        {
                            tile = tile,
                            hash1 = 0,
                            hash2 = 0,
                            offset = offset,
                            rotation = rot,
                            dir = cellType.Rotate(dir, rot),
                        });
                    }
                }
                return results;
            }

            foreach (var rot in rotations)
            {
                // Check each face
                foreach (var (offset, dir, faceDetails) in tile.sylvesFaceDetails)
                {
                    var (hash1, hash2) = GetHash(tile, offset, dir, rot, meshData, cellType, cellGrid, subFaceInfo, interiorCorners, tolerance, debug);
                    results.Add(new Face
                    {
                        tile = tile,
                        hash1 = hash1,
                        hash2 = hash2,
                        offset = offset,
                        rotation = rot,
                        dir = cellType.Rotate(dir, rot),
                    });
                }
            }

            return results;
        }

        private static (int, int) GetHash(
            TesseraTileBase tile,
            Vector3Int offset,
            CellDir dir,
            Sylves.CellRotation rot,
            MeshData meshData,
            ICellType cellType,
            IGrid cellGrid,
            ISubFaceInfo subFaceInfo,
            HashSet<(Vector3Int, CellCorner)> interiorCorners,
            float tolerance,
            bool debug)
        {
            var (invFaceTrs, clipPlanes) = GetFaceTrs(tile, offset, dir, cellGrid, subFaceInfo);

            cellType.Rotate(dir, rot, out var dir2, out var connection);

            var debugText = "";

            if (debug)
            {
                debugText += $"Computing hash of {tile.name} {offset} {cellType.Format(dir)} at rotation {cellType.Format(rot)}\n";
            }

            //var mat = connection.ToTesseraMatrix();
            var mat = connection.ToTesseraMatrix();

            // Compute hash from edges
            // TODO: the hash currently uses unordered edges.
            // Ordered ones would work, as long as we swap the order if mat
            // is a reflection.
            int hash1 = 0;
            int hash2 = 0;
            foreach (var (u1, u2, v1, v2) in GetEdges(invFaceTrs, clipPlanes, meshData, tolerance))
            {
                var mv1 = mat.MultiplyVector(v1);
                var mv2 = mat.MultiplyVector(v2);
                var v1i = Vector3Int.RoundToInt(mv1 / tolerance);
                var v2i = Vector3Int.RoundToInt(mv2 / tolerance);
                var h1 = HashUtils.Hash(v1i.x, v1i.y, v1i.z);
                var h2 = HashUtils.Hash(v2i.x, v2i.y, v2i.z);
                var h = HashUtils.Hash(h1, h2) + HashUtils.Hash(h2, h1);
                hash1 = h ^ hash1;
                // hash2 is mirrored, and with edge directions inverted.
                // The idea 
                h1 = HashUtils.Hash(-v1i.x, v1i.y, v1i.z);
                h2 = HashUtils.Hash(-v2i.x, v2i.y, v2i.z);
                var hx = HashUtils.Hash(h1, h2) + HashUtils.Hash(h2, h1);
                hash2 = hx ^ hash2;
                if (debug)
                {
                    debugText += $"    Adding edge {u1} {u2}.";
                    if (ExtendedDebugInfo) debugText += $"Locally {mv1} {mv2}, ints {v1i} {v2i} hash {h}";
                    debugText += "\n";
                }
            }
            // Compute hash from interior points
            foreach (var corner in cellType.GetCellCorners())
            {
                if (interiorCorners.Contains((offset, corner)))
                {
                    var pos = cellGrid.GetCellCorner(offset, corner, tile.center, tile.cellSize);
                    var p = invFaceTrs.MultiplyPoint(pos);
                    if (Mathf.Abs(p.z) < tolerance)
                    {
                        var mp = mat.MultiplyVector(p);
                        var pi = Vector3Int.RoundToInt(mp / tolerance);
                        //Debug.Log($"{tile.name} {rot} {(CubeDir)dir} {(CubeDir)cellType.Rotate(dir, rot)}  adding corner {pos} hash code {pi.GetHashCode()}");
                        // Unity's built in hash is too simple, and reacts badly to doing stuff like xor/add
                        // So we mix in a different hash.
                        var h = HashUtils.Hash(pi.x, pi.y, pi.z, 1337); ;
                        hash1 += h;
                        var hx = HashUtils.Hash(-pi.x, pi.y, pi.z, 1337);
                        hash2 += hx;

                        if (debug)
                        {
                            debugText += $"    Adding corner {cellType.Format(corner)}.";
                            if (ExtendedDebugInfo) debugText += $"Locally {mp} ints {pi} hash {h}";
                            debugText += $"\n";
                        }
                    }
                }
            }

            if (debug)
            {
                debugText += $"Finished. hash1 = {hash1} hash2 = {hash2}\n";
                Debug.Log(debugText);
            }

            return (hash1, hash2);
        }

        /// <summary>
        /// Finds a transform from face local space (XY plane) to tile space
        /// Also finds a bunch of planes to filter things by in local space
        /// </summary>
        private static (Matrix4x4, List<(Vector3 p, Vector3 n)>)  GetFaceTrs(TesseraTileBase tile,
            Vector3Int offset,
            CellDir dir,
            IGrid cellGrid,
            ISubFaceInfo subFaceInfo)
        {
            // Find faceCenter and normal
            var center1 = cellGrid.GetCellCenter(offset, tile.center, tile.cellSize);
            var otherOffset = (Vector3Int)cellGrid.Move((Cell)offset, dir).Value;
            var center2 = cellGrid.GetCellCenter(otherOffset, tile.center, tile.cellSize);
            var faceNormal = (center2 - center1).normalized;
            var faceCenter = (center1 + center2) / 2;

            var asdf = subFaceInfo.GetSubFaces(offset, dir)
                .Where(x => x.type == SubFaceType.Corner)
                .Select(x => subFaceInfo.GetCorner(offset, dir, x))
                .ToList();

            var corners = asdf
                .Select(x=> cellGrid.GetCellCorner(offset, x, tile.center, tile.cellSize))
                .ToList();

            var faceRight = ((corners[0] + corners[1]) / 2 - faceCenter).normalized;
            var faceUp = Vector3.Cross(faceRight, faceNormal).normalized;

            var transform = new Matrix4x4(
                faceRight,
                faceUp,
                faceNormal,
                new Vector4(faceCenter.x, faceCenter.y, faceCenter.z, 1)
                );

            var iTransform = transform.inverse;

            //if (tile.sylvesOffsets.Count > 1)
            if(true)
            {

                corners = corners.Select(x => iTransform.MultiplyPoint(x)).ToList();

                var clipPlanes = new List<(Vector3 p, Vector3 n)>();
                for (var i = 0; i < corners.Count; i++)
                {
                    var c0 = corners[i];
                    var c1 = corners[(i + 1) % corners.Count];
                    clipPlanes.Add((c0, Vector3.Cross(c0 - c1, Vector3.forward)));
                }

                return (iTransform, clipPlanes);
            }
            else
            {
                return (iTransform, null);
            }
        }

        /// <summary>
        /// Extracts edges from the mesh that are co-incident with a plane
        /// </summary>
        private static IEnumerable<(Vector3 v1, Vector3 v2, Vector3 localV1, Vector3 localV2)> GetEdges(Matrix4x4 invFaceTrs, List<(Vector3 p, Vector3 n)> clipPlanes, MeshData meshData, float tolerance)
        {
            foreach (var face in Sylves.MeshUtils.GetFaces(meshData))
            {
                for (var i = 0; i < face.Count; i++)
                {
                    var i1 = face[i];
                    var i2 = face[(i + 1) % face.Count];
                    var u1 = meshData.vertices[i1];
                    var u2 = meshData.vertices[i2];
                    var v1 = invFaceTrs.MultiplyPoint(u1);
                    var v2 = invFaceTrs.MultiplyPoint(u2);
                    if (Mathf.Abs(v1.z) < tolerance &&
                        Mathf.Abs(v2.z) < tolerance)
                    {
                        if(clipPlanes != null)
                        {
                            var ok = true;
                            foreach(var (p, n) in clipPlanes)
                            {
                                if(Vector3.Dot(v1 - p, n) < -tolerance || Vector3.Dot(v2 - p, n) < -tolerance )
                                {
                                    ok = false;
                                    break;
                                }
                            }
                            if (!ok)
                                continue;
                        }

                        yield return (u1, u2, v1, v2);
                    }
                }
            }
        }


        // TODO: I think this procedure can fail for big tiles?
        /// <summary>
        /// Determines if a set of verts are inside or outside of the mesh.
        /// </summary>
        private static bool[] IsInterior(MeshData meshData, Vector3[] verts, bool debug)
        {
            var meshGrid = new MeshGrid(meshData);

            // Find a good cell
            foreach (var c in meshGrid.GetCells())
            {
                var p = meshGrid.GetCellCenter(c);
                if (float.IsNaN(p.x))
                    continue;
                var n = meshGrid.GetTRS(c).ToMatrix().MultiplyVector(Vector3.forward);
                var results = new bool[verts.Length];
                for (var i = 0; i < verts.Length; i++)
                {
                    var origin = p;
                    var dir = verts[i] - p;
                    var count = meshGrid.Raycast(origin, dir, 1)
                        .Where(x => x.cell != c)
                        .Count();
                    var facesAway = Vector3.Dot(n, dir) > 0;
                    results[i] = facesAway ^ (count % 2 == 0);

                    if(debug)
                    {
                        Debug.Log($"Checking interiors: Traced from {verts[i]} to {origin}, found {count} crossings and faces {(facesAway? "away" : "towards")}");
                        Debug.DrawLine(verts[i], origin, Color.red);
                    }
                }
                return results;
            }

            // Totally empty or bad mesh. Assume this means all interior
            // (no mesh == all exterior)
            return verts.Select(x => true).ToArray();
        }
    }
}
