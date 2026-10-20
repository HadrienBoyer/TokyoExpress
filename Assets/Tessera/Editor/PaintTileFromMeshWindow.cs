using Sylves;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    public class PaintTileFromMeshWindow : EditorWindow
    {
        public enum SubFaceStyle
        {
            Clear,
            [InspectorName("Mesh Hash")]
            MeshHash,
            [InspectorName("Interior vs exterior")]
            Interior,
            [InspectorName("Don't Change")]
            DontChange,
        }

        public SubFaceStyle Face = SubFaceStyle.MeshHash;
        public SubFaceStyle Corners = SubFaceStyle.Interior;

        public float tolerance = EdgeDetection.DefaultTolerance;
        public bool debug = false;

        private void OnGUI()
        {
            titleContent = new GUIContent("Paint Tile from Mesh");
            var selectedCount = Selection.transforms.Length;
            GUILayout.Label($"{selectedCount} items selected in scene.");
            Corners = (SubFaceStyle)EditorGUILayout.EnumPopup(new GUIContent("Corner Paint"), Corners);
            Face = (SubFaceStyle)EditorGUILayout.EnumPopup(new GUIContent("Other Paint"), Face);
            tolerance = EditorGUILayout.FloatField(new GUIContent("Tolerance"), tolerance);
            debug = EditorGUILayout.Toggle(new GUIContent("Debug"), debug);
            GUI.enabled = selectedCount > 0;
            if (GUILayout.Button("Go"))
            {
                Run();
            }
            GUI.enabled = true;
        }

        // We need a high quality hash here for reasons i don't totally undersant.
        // Because we're adding various hashes together, low quality hashes can
        // lead to collisiions.
        // There's got to be a better choice than this though.
        private static int Hash(int v1, int v2, int v3)
        {
            var md5 = MD5.Create();
            return BitConverter.ToInt32(md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes($"{v1}{v2}{v3}")), 0);
        }

        public IEnumerable<SubFaceTuple> GetSubFaces(ISubFaceInfo subFaceInfo, Vector3Int offset, Sylves.CellDir dir, SubFaceStyle style)
        {
            return subFaceInfo.GetSubFaces(offset, dir)
                .Where(sf => sf.type == SubFaceType.Corner ? Corners == style : Face == style);
        }

        private void Clear(TesseraTileBase tile, ISubFaceInfo subFaceInfo)
        {
            foreach (var f in tile.sylvesFaceDetails)
            {
                foreach(var subFace in GetSubFaces(subFaceInfo, f.offset, f.dir, SubFaceStyle.Clear))
                {
                    f.faceDetails[subFace] = 0;
                }
            }
        }

        private void MeshEdgeToPaint(
            TesseraTileBase tile,
            ICellType cellType,
            ISubFaceInfo subFaceInfo,
            MeshData meshData,
            HashSet<(Vector3Int, CellCorner)> interiorCorners
            )
        {

            // We always use all rotations, regardless of what the tile supports.
            // Otherwise hashes wouldn't agree correctly.
            var allRotations = cellType.GetRotations(true);
            var faces = EdgeDetection.GetFaces(tile, allRotations, meshData, interiorCorners, tolerance, debug)
                .ToDictionary(x => (x.offset, x.rotation, x.dir));


            // Copy face hashes into face details
            foreach (var f in tile.sylvesFaceDetails)
            {
                var logging = true;
                //logging = f.dir == (CellDir)SylvesHexPrismDir.Right;
                if (logging)
                {
                    Debug.Log($"Setting face hashes of {tile.name} {f.offset} {cellType.Format(f.dir)}:");
                }

                var identityFace = faces[(f.offset, cellType.GetIdentity(), f.dir)];


                List<SubFaceTuple> subfaces = GetSubFaces(subFaceInfo, f.offset, f.dir, SubFaceStyle.MeshHash).ToList();

                var hashes = subfaces.ToDictionary(x => x, x => 0);

                // Find the subgroup of rotations that fix this face
                var group = new List<(Sylves.CellRotation rotation, Sylves.Connection connection)>();
                foreach (var rotation in allRotations)
                {
                    cellType.Rotate(f.dir, rotation, out var dir2, out var connection);
                    // Skip if this is rotation doesn't fix this face
                    if (dir2 != f.dir)
                        continue;
                    group.Add((rotation, connection));
                }

                // Find the subgroup of group
                var symmetryGroup = group.Where(x => faces[(f.offset, x.rotation, f.dir)].hash1 == identityFace.hash1).ToList();

                if (logging)
                {
                    Debug.Log($"symmetry group: {string.Join(",", symmetryGroup.Select(x => x.connection))}");
                }

                if(symmetryGroup.Count == group.Count)
                {
                    // Fully symmetric.
                    // Handle this case as a solid face, as it looks more aesthetic
                    foreach (var subface in subfaces)
                    {
                        f.faceDetails[subface] = Math.Abs(identityFace.hash1) + 2;
                    }
                    continue;
                }

                // Loop over group/symmetryGroup (the quotient group)
                var quotient = group.ToList();
                while (quotient.Count > 0)
                {
                    var (r, c) = quotient[0];
                    // TODO: I don't understand why this is necessary?
                    var m = new Connection { Mirror = true, Rotation = 0, Sides = c.Sides };
                    c = m * c * m;
                    foreach (var r2 in symmetryGroup.Select(x => cellType.Multiply(r, x.rotation)))
                    {
                        quotient = quotient.Where(x => x.rotation != r2).ToList();
                    }

                    var face = faces[(f.offset, r, f.dir)];
                    foreach (var subface in subfaces)
                    {
                        var h = Hash(face.hash1, (int)subface.type, subface.value); ;
                        if (logging)
                        {
                            Debug.Log($"{subface}: Adding {(face.hash1, subface)} ({h}) to {subface.Rotate(c)}");
                        }
                        hashes[subface.Rotate(c)] += h;
                    }
                }

                if (logging)
                {
                    Debug.Log("half hash");
                    foreach (var kv in hashes)
                    {
                        Debug.Log($"  {kv.Key} {kv.Value}");
                    }
                }

                var hashes2 = subfaces.ToDictionary(x => x, x => 0);
                foreach (var (r, c) in symmetryGroup)
                {
                    foreach (var subface in subfaces)
                    {
                        hashes2[subface.Rotate(c)] += hashes[subface];
                    }
                }
                hashes = hashes2;

                if (logging)
                {
                    Debug.Log("full hash");
                    foreach (var kv in hashes)
                    {
                        //Debug.Log($"  {kv.Key} {kv.Value}");
                    }
                }

                foreach (var kv in hashes)
                {
                    // Default palette behaves weirdly for <=0
                    f.faceDetails[kv.Key] = Math.Abs(kv.Value) + 1;
                }

                // Default palette behaves weirdly for <=0
                //var hash = Math.Abs(face.hash) + 1;
                //f.faceDetails.center = hash;
                //Debug.Log($"Face {f.offset} {cellType.Format(f.dir)} has hash {face.hash}");
            }
        }

        private void InteriorToPaint(TesseraTileBase tile, ISubFaceInfo subFaceInfo, HashSet<(Vector3Int, CellCorner)> interiorCorners)
        {
            foreach (var f in tile.sylvesFaceDetails)
            {
                foreach (var subface in GetSubFaces(subFaceInfo, f.offset, f.dir, SubFaceStyle.Interior))
                {
                    var corner = subFaceInfo.GetCorner(f.offset, f.dir, subface);
                    var isInterior = interiorCorners.Contains((f.offset, corner));
                    f.faceDetails[subface] = isInterior ? 1 : 2;
                }
            }
        }

        private void Run()
        {
            foreach (var t in Selection.transforms)
            {
                var tile = t.GetComponent<TesseraTileBase>();
                var mesh = tile.GetSharedMesh();

                if (tile == null)
                {
                    tile = t.gameObject.AddComponent<TesseraTile>();
                    var b = mesh.bounds;
                    tile.center = b.center;
                    tile.cellSize = b.size;
                }

                var cellType = tile.SylvesCellType;
                var subFaceInfo = tile.SubFaceInfo;
                var meshData = mesh == null ? null : new Sylves.MeshData(mesh);
                var interiorCorners = EdgeDetection.GetInteriorCorners(tile, meshData, debug);

                InteriorToPaint(tile, subFaceInfo, interiorCorners);
                MeshEdgeToPaint(tile, cellType, subFaceInfo, meshData, interiorCorners);
                Clear(tile, subFaceInfo);
                SceneView.RepaintAll();
            }
        }
    }
}
