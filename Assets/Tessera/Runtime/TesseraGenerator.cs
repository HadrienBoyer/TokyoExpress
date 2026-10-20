using DeBroglie;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

namespace Tessera
{
    public enum FailureMode
    {
        /// <summary>
        /// If a failure occurs, don't output anything
        /// </summary>
        Cancel,
        /// <summary>
        /// If a failure occurs, output the progress so far
        /// </summary>
        Last,
        /// <summary>
        /// If a failure occurs, backtrack to the last safe point.
        /// </summary>
        LastGood,
        /// <summary>
        /// Examines the progress so far for the minimal set of tiles that cause an issue
        /// </summary>
        Minimal,
    }

    /// <summary>
    /// Different models Tessera supports.
    /// The model dictates how nearby tiles relate to each other.
    /// </summary>
    public enum ModelType
    {
        /// <summary>
        /// The default model using the painting system to determine tile adjacencies.
        /// </summary>
        AdjacentPaint,
        /// <summary>
        /// See [overlapping](../articles/models/overlapping.md).
        /// </summary>
        Overlapping,
        /// <summary>
        /// See [overlapping](../articles/models/overlapping.md).
        /// </summary>
        Adjacent,
        /// <summary>
        /// Detects where mesh edges line up.
        /// </summary>
        MeshEdges,
    }

    public enum TileSelectionType
    {
        Random,
        Ordered,
    }


    [Serializable]
    public class TileList
    {
        public List<TesseraTileBase> tiles;
    }


    /// <summary>
    /// GameObjects with this behaviour contain utilities to generate tile based levels using Wave Function Collapse (WFC).
    /// Call <see cref="Generate"/> or <see cref="StartGenerate"/> to run.
    /// The generation takes the following steps:
    /// * Inspect the tiles in <see cref="tiles"/> and work out how they rotate and connect to each other.
    /// * Setup any initial constraints that fix parts of the generation (<see cref="TesseraGenerateOptions.initialConstraints"/>).
    /// * Fix the boundary of the generation if <see cref="skyBox"/> is set.
    /// * Generate a set of tile instances that fits the above tiles and constraints.
    /// * Optionally <see cref="retries"/> or <see cref="backtrack"/>.
    /// * Instantiates the tile instances.
    /// </summary>
    [AddComponentMenu("Tessera/Tessera Generator")]
    public class TesseraGenerator : MonoBehaviour
    {


        #region Grid Shape

        [SerializeField]
        [Tooltip("The size of the generator area, counting in cells.")]
        private Vector3Int m_size = new Vector3Int(10, 1, 10);


        /// <summary>
        /// The size of the generator area, counting in cells each of size <see cref="cellSize"/>.
        /// </summary>
        public Vector3Int size
        {
            get { return m_size; }
            set
            {
                m_size = value;
            }
        }

        [SerializeField]
        [Tooltip("The local position of the center of the area to generate.")]
        private Vector3 m_center = Vector3.zero;

        /// <summary>
        /// The local position of the center of the area to generate.
        /// </summary>
        public Vector3 center
        {
            get
            {
                return m_center;
            }
            set
            {
                m_center = value;
            }
        }

        public Vector3 origin
        {
            get
            {
                var cellType = CellType;
                if (cellType == SylvesExtensions.HexPrismCellType)
                {
                    // Some convention differences between cell grids and grids?
                    var maxOffset = new Vector3Int(size.x - 1, size.z - 1, size.y - 1);
                    var c = SylvesExtensions.HexPrismGridInstance.GetCellCenter(maxOffset, Vector3.zero, cellSize);
                    return center - c / 2;
                }
                else if (cellType == SylvesExtensions.TrianglePrismCellType)
                {
                    // Some convention differences between cell grids and grids?
                    var triangleGrid = new Sylves.XZTrianglePrismGrid(1, 1);// TODO: to static?
                    var maxOffset = new Vector3Int(2 * size.x - 1, size.z - 1, size.y - 1);
                    var c1 = triangleGrid.GetCellCenter(maxOffset, Vector3.zero, cellSize);
                    var c2 = triangleGrid.GetCellCenter(new Vector3Int(0, 0, 0), Vector3.zero, cellSize);
                    return center - (c1 + c2) / 2;
                }
                else
                {
                    return center - CubeGeometryUtils.GetCellCenter(size - Vector3Int.one, Vector3.zero, cellSize) / 2;
                }
            }
            set
            {
                var cellType = CellType;
                if (cellType == SylvesExtensions.HexPrismCellType)
                {
                    center = value + HexGeometryUtils.GetCellCenter(size - Vector3Int.one, Vector3.zero, cellSize) / 2;
                }
                else if (cellType == SylvesExtensions.TrianglePrismCellType)
                {
                    center = value + TrianglePrismGeometryUtils.GetCellCenter(size - Vector3Int.one, Vector3.zero, cellSize) / 2;
                    if ((size.x & 1) == 1)
                    {
                        m_center.z += TrianglePrismGeometryUtils.GetCellCenter(new Vector3Int(1, 0, 0), Vector3.zero, cellSize).z / 2;
                    }
                }
                else
                {
                    center = value + CubeGeometryUtils.GetCellCenter(size - Vector3Int.one, Vector3.zero, cellSize) / 2;
                }
            }
        }

        /// <summary>
        /// The area of generation.
        /// Setting this will cause the size to be rounded to a multiple of <see cref="cellSize"/>
        /// </summary>
        public Bounds bounds
        {
            get
            {
                return new Bounds(m_center, Vector3.Scale(cellSize, m_size));
            }
            set
            {
                m_center = value.center;
                m_size = new Vector3Int(
                    Math.Max(1, (int)Math.Round(value.size.x / cellSize.x)),
                    Math.Max(1, (int)Math.Round(value.size.y / cellSize.y)),
                    Math.Max(1, (int)Math.Round(value.size.z / cellSize.z))
                    );
            }
        }

        /// <summary>
        /// The stride between each cell in the generation.
        /// "big" tiles may occupy a multiple of this cell size.
        /// </summary>
        [Tooltip("The stride between each cell in the generation.")]
        [SerializeField]
        [FormerlySerializedAs("tileSize")]
        private Vector3 m_cellSize = Vector3.one;

        /// <summary>
        /// The stride between each cell in the generation.
        /// "big" tiles may occupy a multiple of this cell size.
        /// </summary>
        public Vector3 cellSize
        {
            get
            {
                return m_cellSize;
            }
            set
            {
                m_cellSize = value;
            }
        }

        /// <summary>
        /// If set, this tile is used to define extra initial constraints for the boundary.
        /// </summary>
        [Tooltip("If set, this tile is used to define extra initial constraints for the boundary.")]
        public TesseraTileBase skyBox = null;

        /// <summary>
        /// If set, then tiles are generated on the surface of this mesh instead of a regular grid.
        /// </summary>
        [Tooltip("If set, then tiles are generated on the surface of this mesh instead of a regular grid.")]
        public Mesh surfaceMesh;

        /// <summary>
        /// Height above the surface mesh that the bottom layer of tiles is generated at.
        /// </summary>
        [Tooltip("Height above the surface mesh that the bottom layer of tiles is generated at.")]
        public float surfaceOffset;

        /// <summary>
        /// Controls how normals are treated for meshes deformed to fit the surfaceMesh.
        /// </summary>
        [Tooltip("Controls how normals are treated for meshes deformed to fit the surfaceMesh.")]
        public bool surfaceSmoothNormals;

        /// <summary>
        /// If true, and a <see cref="surfaceMesh"/> is set with multiple submeshes (materials),
        /// then use <see cref="surfaceSubmeshTiles"/>.
        /// </summary>
        [Tooltip("If true, filters which tiles appear on each material (submesh) of the surface mesh.")]
        public bool filterSurfaceSubmeshTiles;

        /// <summary>
        /// A list of tiles to filter each submesh of <see cref="surfaceMesh"/> to.
        /// Ignored unless <see cref="filterSurfaceSubmeshTiles"/> is true.
        /// </summary>
        public List<TileList> surfaceSubmeshTiles = new List<TileList>();

        #endregion

        #region Generation
        /// <summary>
        /// If set, backtracking will be used during generation.
        /// Backtracking can find solutions that would otherwise be failures,
        /// but can take a long time.
        /// </summary>
        [Tooltip("If set, backtracking will be used during generation.\nBacktracking can find solutions that would otherwise be failures, but can take a long time.")]
        public bool backtrack = false;

        /// <summary>
        /// Fixes the seed for random number generator.
        /// If the value is zero, the seed is taken from Unity.Random 
        /// </summary>
        [Tooltip("Fixes the seed for random number generator.\n If the value is zero, the seed is taken from Unity.Random.")]
        public int seed = 0;

        /// <summary>
        /// If backtracking is off, how many times to retry generation if a solution
        /// cannot be found.
        /// </summary>
        [Tooltip("How many times to retry generation if a solution cannot be found.")]
        public int retries = 5;

        /// <summary>
        /// How many steps to take before retrying from the start.
        /// </summary>
        [Tooltip("How many steps to take before retrying from the start.")]
        public int stepLimit = 0;

        /// <summary>
        /// Controls the algorithm used internally for Wave Function Collapse.
        /// </summary>
        [Tooltip("Controls the algorithm used internally for Wave Function Collapse.")]
        public TesseraWfcAlgorithm algorithm;

        /// <summary>
        /// Records undo/redo when run by pressing the Generate button in the Inspector.
        /// </summary>
        [Tooltip("Records undo/redo when run in the editor.")]
        public bool recordUndo = true;

        /// <summary>
        /// Controls what is output when the generation fails.
        /// </summary>
        public FailureMode failureMode = FailureMode.Cancel;

        /// <summary>
        /// Game object to show in cells that have yet to be fully solved.
        /// </summary>
        public GameObject uncertaintyTile;

        /// <summary>
        /// Game object to show in cells that cannot be solved.
        /// </summary>
        public GameObject contradictionTile;

        /// <summary>
        /// If true, the uncertainty tiles shrink as the solver gets more certain.
        /// </summary>
        public bool scaleUncertainyTile = true;

        /// <summary>
        /// If true, then active tiles in the scene will be taken as initial constraints.
        /// If false, then no initial constraints are used.
        /// Using <see cref="TesseraGenerateOptions.initialConstraints"/> overrides either outcome.
        /// </summary>
        public bool searchInitialConstraints = true;
        #endregion

        #region Model

        /// <summary>
        /// Sets which sort of model the generator uses.
        /// The model dictates how nearby tiles relate to each other.
        /// </summary>
        public ModelType modelType;

        /// <summary>
        /// For overlapping models, a list of objects to use as input samples.
        /// Each one will have its children inspected and read out.
        /// <see cref="ModelType.Overlapping"/>
        /// </summary>
        public List<GameObject> samples = new List<GameObject>();

        /// <summary>
        /// The size of the overlap parameter for the overlapping model.
        /// <see cref="ModelType.Overlapping"/>
        /// </summary>
        public Vector3Int overlapSize = new Vector3Int(3, 3, 3);

        /// <summary>
        /// Controls the tolerance (snap size) of comparing vertices for
        /// <see cref="ModelType.MeshEdges"/>
        /// </summary>
        public float meshEdgesTolerance = EdgeDetection.DefaultTolerance;


        #endregion

        #region Tiles

        /// <summary>
        /// The list of tiles eligible for generation.
        /// </summary>
        [Tooltip("The list of tiles eligible for generation.")]
        public List<TileEntry> tiles = new List<TileEntry>();

        public TileSelectionType tileSelectionType;

        /// <summary>
        /// Inherited from the first tile in <see cref="tiles"/>.
        /// </summary>
        public TesseraPalette palette => tiles.Select(x => x.tile?.palette).FirstOrDefault() ?? TesseraPalette.defaultPalette;

        #endregion


        /// <summary>
        /// Clears previously generated content.
        /// </summary>
        public void Clear()
        {
            var output = GetComponent<ITesseraTileOutput>() ?? new InstantiateOutput(transform);
            output.ClearTiles(UnityEngineInterface.Instance);
        }

        /// <summary>
        /// Synchronously runs the generation process described in the class docs.
        /// </summary>
        /// <param name="onCreate">Called for each newly generated tile. By default, they are Instantiated in the scene.</param>
        public TesseraCompletion Generate(TesseraGenerateOptions options = null)
        {
            var e = StartGenerate(options);
            while (e.MoveNext()) { }
            return e.Result;
        }

        /// <summary>
        /// Runs Clear, then Generate
        /// </summary>
        public TesseraCompletion Regenerate(TesseraGenerateOptions options = null)
        {
            Clear();
            return Generate(options);
        }

        /// <summary>
        /// Dumps out the internal view of the sample.
        /// Can be used for diagnosing issues with reading samples.
        /// </summary>
        public void CloneSample(int index = 0, IEngineInterface engineInterface = null)
        {
            Clear();
            var tilemap = TesseraGeneratorUtils.ToTesseraTilemap(this, samples[index]);
            var completion = new TesseraCompletion
            {
                grid = tilemap.Grid,
                gridTransform = new TRS(Vector3.zero),
                tileData = tilemap.Data,
            };
            var output = GetTileOutput();
            output.UpdateTiles(completion, engineInterface);
        }

        /// <summary>
        /// Describes the geometry and layout of the cells.
        /// See separate Sylves documentations for more details.
        /// https://www.boristhebrave.com/docs/sylves/1
        /// Note: If you are using a Mesh Surface based grid, then GetGrid() can be quite slow, and you are recommended to cache the result of it.
        /// </summary>
        public Sylves.IGrid GetGrid()
        {
            var cellType = CellType;

            if (surfaceMesh != null)
            {
                var surfaceMeshData = new Sylves.MeshData(surfaceMesh);
                // The grid has an extra layer on the top/bottom for use with pins.
                var meshGrid = new Sylves.MeshPrismGrid(surfaceMeshData, new Sylves.MeshPrismGridOptions
                {
                    MinLayer = -1,
                    MaxLayer = size.y + 1,
                    LayerHeight = cellSize.y,
                    LayerOffset = surfaceOffset,
                    SmoothNormals = surfaceSmoothNormals,
                    UseXZPlane = true,
                    DoubleOddFaces = true,
                });
                var sizey = size.y;
                Func<Sylves.Cell, bool> mask = c => 0 <= c.z && c.z < sizey;
                var maskedMeshGrid = new Sylves.MaskModifier(meshGrid, mask, meshGrid.GetCells().Where(mask).ToList());
                return maskedMeshGrid;
            }
            else if (cellType == SylvesExtensions.HexPrismCellType)
            {
                var hexBound = new Sylves.HexBound(new Vector3Int(0, 0, -size.x - size.z + 2), new Vector3Int(size.x, size.z, 1));
                var bound = new Sylves.HexPrismBound(hexBound, 0, size.y);
                return new Sylves.TransformModifier(
                    new Sylves.XZHexPrismGrid(cellSize.x * Mathf.Sqrt(3) * 2 / 3, cellSize.y, bound: bound),
                    Matrix4x4.Translate(origin));

            }
            else if (cellType == SylvesExtensions.TrianglePrismCellType)
            {
                var triangleBound = new Sylves.TriangleBound(new Vector3Int(0, 0, -size.x - size.z + 2), new Vector3Int(size.x, size.z, 3));
                var bound = new Sylves.TrianglePrismBound(triangleBound, 0, size.y);
                return
                    new Sylves.TransformModifier(
                        new Sylves.XZTrianglePrismGrid(cellSize.x * Mathf.Sqrt(3), cellSize.y, Sylves.TriangleOrientation.FlatTopped, bound: bound),
                        Matrix4x4.Translate(origin));
            }
            else
            if (cellType == SylvesExtensions.SquareCellType)
            {
                return
                    new Sylves.TransformModifier(
                        new Sylves.SquareGrid(new Vector2(cellSize.x, cellSize.y), new Sylves.SquareBound(Vector2Int.zero, new Vector2Int(size.x, size.y))),
                        Matrix4x4.Translate(origin - new Vector3(cellSize.x, cellSize.y) / 2));
            }
            else if (cellType == SylvesExtensions.CubeCellType)
            {
                return
                    new Sylves.TransformModifier(
                        new Sylves.CubeGrid(cellSize, new Sylves.CubeBound(Vector3Int.zero, size)),
                        Matrix4x4.Translate(origin - cellSize / 2));
            }
            else
            {
                throw new Exception($"Unknown cell type ${cellType}");
            }
        }

        public ITesseraTileOutput GetTileOutput(bool forceIncremental = false)
        {
            var component = GetComponent<ITesseraTileOutput>();
            if (component != null)
            {
                return component;
            }
            if (forceIncremental)
            {
                return new UpdatableInstantiateOutput(transform);
            }
            return new InstantiateOutput(transform);
        }
        /// <summary>
        /// Asynchronously runs the generation process described in the class docs, for use with StartCoroutine.
        /// </summary>
        /// <remarks>The default instantiation is still synchronous, so this can still cause frame glitches unless you override onCreate.</remarks>
        public EnumeratorWithResult<TesseraCompletion> StartGenerate(TesseraGenerateOptions options = null)
        {
            return new EnumeratorWithResult<TesseraCompletion>(StartGenerateInner(options));
        }

        private IEnumerator StartGenerateInner(TesseraGenerateOptions options = null)
        {
            options = options ?? new TesseraGenerateOptions();

            var generatorHelper = TesseraGeneratorUtils.CreateTesseraGeneratorHelper(this, options);


            for (var r = 0; r < retries; r++)
            {
                var name = gameObject.name;
                TilePropagator propagator;
                TilePropagator Run()
                {
                    try
                    {
                        Profiler.BeginThreadProfiling("Tessera Generation", name);
                        generatorHelper.FullRun(r >= retries - 1);
                        return generatorHelper.Propagator;
                    }
                    finally
                    {
                        Profiler.EndThreadProfiling();
                    }
                }

                if (options.multithreaded && Application.platform != RuntimePlatform.WebGLPlayer)
                {
                    var runTask = Task.Run(Run, options.cancellationToken);

                    while (!runTask.IsCompleted)
                        yield return null;

                    options.cancellationToken.ThrowIfCancellationRequested();

                    propagator = runTask.Result;
                }
                else
                {
                    propagator = Run();
                }

                var status = propagator.Status;

                var contradictionTile = new ModelTile { };

                var result = propagator.ToValueArray<ModelTile?>(contradiction: contradictionTile);


                if (status == DeBroglie.Resolution.Contradiction)
                {
                    if (r < retries - 1)
                    {
                        continue;
                    }
                }


                // Log out stats about the run
                if (TesseraStats.DebugMode)
                {
                    var stats = generatorHelper.Stats;
                    Debug.Log($@"Run Details:
    Model Tile Count = {stats.modelTileCount}
    Cell Count = {stats.cellCount}
");

                    Debug.Log($@"Tessera timings (ms):
    Total Time = {stats.totalTime * 1000:n1}
    Create Helper Time = {stats.createHelperTime * 1000:n1}
    Initialize Time = {stats.initializeTime * 1000:n1}
    Create Propagator Time = {stats.createPropagatorTime * 1000:n1}
    Initial Constraints Time = {stats.initialConstraintsTime * 1000:n1}
    Skybox Time = {stats.skyboxTime * 1000:n1}
    Ban Big Tiles Time = {stats.banBigTilesTime * 1000:n1}
    Run Time = {stats.runTime * 1000:n1}
    Post-Process Time = {stats.postProcessTime * 1000:n1}
");
                }

                var startCompleteTime = DateTime.Now;
                var completion = new TesseraCompletion();
                completion.retries = r;
                completion.backtrackCount = propagator.BacktrackCount;
                completion.success = status == DeBroglie.Resolution.Decided;
                completion.tileData = SylvesDeBroglieUtils.ToTileDictionary(result, generatorHelper.Grid);
                completion.contradictionLocation = completion.success ? null : SylvesDeBroglieUtils.GetContradictionLocation(result, generatorHelper.Grid);
                completion.contradictionReason = completion.success ? null : propagator.ContradictionReason;
                completion.isIncremental = false;
                completion.grid = generatorHelper.Grid;
                completion.gridTransform = TRS.World(transform);


                if (options.onComplete != null)
                {
                    options.onComplete(completion);
                }
                else
                {
                    HandleComplete(options, completion);
                }

                if (completion.success == false && failureMode != FailureMode.Cancel && (uncertaintyTile != null || this.contradictionTile != null))
                {
                    TesseraGeneratorUtils.InstantiateUncertaintyObjects(this, generatorHelper.Grid, propagator);
                }

                if (TesseraStats.DebugMode)
                {
                    Debug.Log($"Completion Time = {(DateTime.Now - startCompleteTime).TotalSeconds * 1000:n1}");
                }

                yield return completion;

                // Exit retries
                break;
            }
        }

        /// <summary>
        /// For validation purposes
        /// </summary>
        public IList<Sylves.ICellType> GetCellTypes()
        {
            return tiles.Select(x => x.tile)
                .Where(x => x != null)
                .Select(x => x.SylvesCellType)
                .Distinct()
                .ToList();
        }


        /// <summary>
        /// Checks tiles are consistently setup
        /// </summary>
        internal void Validate()
        {
            var allTiles = tiles.Select(x => x.tile).Where(x => x != null);
            if (surfaceMesh != null)
            {
                if (surfaceMesh.GetTopology(0) != MeshTopology.Quads && CellType == SylvesExtensions.CubeCellType)
                {
                    Debug.LogWarning($"Mesh topology {surfaceMesh.GetTopology(0)} not supported with cubes. You need to select \"Keep Quads\" in the import options.");
                }
                if (surfaceMesh.GetTopology(0) != MeshTopology.Triangles && CellType == SylvesExtensions.TrianglePrismCellType)
                {
                    Debug.LogWarning($"Mesh topology {surfaceMesh.GetTopology(0)} not supported with triangles. You need to deselect \"Keep Quads\" in the import options.");
                }
                if (!surfaceMesh.isReadable)
                {
                    Debug.LogWarning($"Surface mesh needs to be readable.");
                }
                //if (surfaceSmoothNormals && !surfaceMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent))
                if (surfaceSmoothNormals && surfaceMesh.tangents.Length == 0)
                {
                    Debug.LogWarning($"Surface mesh needs tangents to calculate smoothed normals. You need to select \"Calculate\" the tangent field of the import options.");
                }
                var unreadable = allTiles.Where(tile => tile.GetComponentsInChildren<MeshFilter>().Any(mf => !mf.sharedMesh.isReadable)).ToList();
                if (unreadable.Count > 0)
                {
                    Debug.LogWarning($"Some tiles have meshes that are not readable. They will not be transformed to fit the mesh. E.g {unreadable.First().name}");
                }
                if (filterSurfaceSubmeshTiles)
                {
                    for (var i = 0; i < surfaceSubmeshTiles.Count; i++)
                    {
                        if (surfaceSubmeshTiles[i].tiles.Count == 0)
                        {
                            Debug.LogWarning($"Submesh {i} is filtered to zero tiles. Generation is impossible");
                        }
                    }
                }

                return;
            }

            if (Application.isPlaying && GetComponent<ITesseraTileOutput>() is TesseraMeshOutput)
            {
                var unreadable = allTiles.Where(tile => tile.GetComponentsInChildren<MeshFilter>().Any(mf => !mf.sharedMesh.isReadable)).ToList();
                if (unreadable.Count > 0)
                {
                    Debug.LogWarning($"Some tiles have meshes that are not readable. They will not be added to the mesh output. E.g {unreadable.First().name}");
                }
            }
            var cellTypes = GetCellTypes();
            if (cellTypes.Count > 1)
            {
                Debug.LogWarning($"You cannot mix tiles of multiple cell types, such as {string.Join(", ", cellTypes.Select(x => x.GetType().Name))} .\n");
            }

            var palette = tiles.Select(x => x == null ? null : x.tile.palette).FirstOrDefault();
            var wrongPaletteTiles = allTiles.Where(x => x.palette != palette).ToList();
            if (wrongPaletteTiles.Count > 0)
            {
                Debug.LogWarning($"Some tiles do not all have the same palette.\b" +
                    "Affected tiles:\n" +
                    string.Join("\n", wrongPaletteTiles)
                    );
            }

            var isStatic = tiles.Where(x => x.tile != null && x.tile.gameObject.isStatic).Select(x => x.tile).ToList();
            if (Application.isPlaying && isStatic.Count > 0)
            {
                Debug.LogWarning($"Some tiles are marked as static and cannot be generated at runtime.\b" +
                    "Affected tiles:\n" +
                    string.Join("\n", isStatic)
                    );
            }
        }

        private void HandleComplete(TesseraGenerateOptions options, TesseraCompletion completion)
        {
            completion.LogErrror();

            if (!completion.success && failureMode == FailureMode.Cancel)
            {
                return;
            }

            ITesseraTileOutput to = null;
            if (options.onCreate != null)
            {
                to = new ForEachOutput(options.onCreate);
            }
            else
            {
                to = GetTileOutput();
            }

            to.UpdateTiles(completion, UnityEngineInterface.Instance);
        }
        /// <summary>
        /// Indicates the cell type of the tiles set up.
        /// </summary>
        public Sylves.IGrid CellGrid => tiles.Select(x => x.tile).Where(x => x != null).FirstOrDefault()?.SylvesCellGrid ?? SylvesExtensions.CubeGridInstance;
        /// <summary>
        /// Indicates the cell type of the tiles set up.
        /// </summary>
        public Sylves.ICellType CellType => tiles.Select(x => x.tile).Where(x => x != null).FirstOrDefault()?.SylvesCellType ?? SylvesExtensions.CubeCellType;

        public TesseraInitialConstraintBuilder GetInitialConstraintBuilder()
        {
            return new TesseraInitialConstraintBuilder(transform, GetGrid());
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
            return TesseraGeneratorUtils.Instantiate(instance, parent, gameObject, instantiateChildrenOnly, engine);
        }
    }
}