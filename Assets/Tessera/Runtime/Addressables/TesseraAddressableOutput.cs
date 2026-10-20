
#if HAS_ADDRESSABLES
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

namespace Tessera
{
    [Serializable]
    public class TesseraAddressableOutputTileMapping
    {
        public TesseraTileBase from;

        public AssetReferenceGameObject to;
    }

    /// <summary>
    /// Attach this to a TesseraGenerator to instantiate addressible assets for each tile.
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    [RequireComponent(typeof(TesseraGenerator))]
    [AddComponentMenu("Tessera/Tessera Addressable Output", 40)]
    public class TesseraAddressableOutput : MonoBehaviour, ITesseraTileOutput
    {
        public Transform parent;

        public TesseraAddressableOutputTileMapping[] tileMappings = new TesseraAddressableOutputTileMapping[0];

        private Dictionary<Vector3Int, GameObject[]> instantiated = new Dictionary<Vector3Int, GameObject[]>();
        private Dictionary<Vector3Int, Action> unsubscribes = new Dictionary<Vector3Int, Action>();

        public Dictionary<AssetReferenceGameObject, AsyncOperationHandle<GameObject>> Cache = new Dictionary<AssetReferenceGameObject, AsyncOperationHandle<GameObject>>();

        public TesseraAddressableOutput()
        {
        }

        private Transform GetParent() => parent == null ? transform : parent;

        public bool IsEmpty => GetParent().childCount == 0;

        public bool SupportsIncremental => true;

        private void Clear(Vector3Int p, IEngineInterface engine)
        {
            if (instantiated.TryGetValue(p, out var gos) && gos != null)
            {
                foreach (var go in gos)
                {
                    engine.Destroy(go);
                }
            }
            if(unsubscribes.TryGetValue(p, out var u) && u != null)
            {
                u();
            }

            instantiated[p] = null;
        }

        public void ClearTiles(IEngineInterface engine)
        {
            foreach (var k in instantiated.Keys.ToList())
            {
                Clear(k, engine);
            }
            var children = GetParent().Cast<Transform>().ToList();
            foreach (var child in children)
            {
                engine.Destroy(child.gameObject);
            }
        }

        public void UpdateTiles(TesseraCompletion completion, IEngineInterface engine)
        {
            var parent = GetParent();
            var tileMappingsDict = tileMappings.ToDictionary(x => x.from);
            foreach (var i in completion.tileInstances)
            {
                foreach (var p in i.Cells)
                {
                    Clear(p, engine);
                }
                if (i.Tile == null)
                    continue;
                if (!tileMappingsDict.TryGetValue(i.Tile, out var mapping))
                {
                    // Default behaviour
                    if (i.Tile != null)
                    {
                        var prefab = i.Tile.gameObject;
                        var instantiateChildrenOnly = i.Tile.instantiateChildrenOnly;
                        instantiated[i.Cells.First()] = TesseraGeneratorUtils.Instantiate(i, parent, prefab, instantiateChildrenOnly, engine);
                    }
                    continue;
                }

                if (!Cache.TryGetValue(mapping.to, out var aoh))
                {
                    aoh = Cache[mapping.to] = mapping.to.LoadAssetAsync();
                }

                Action<AsyncOperationHandle<GameObject>> onLoaded = null;
                unsubscribes[i.Cells.First()] = () =>
                {
                    aoh.Completed -= onLoaded;
                };
                onLoaded = _ => {
                        Debug.Log($"Creating {i.Tile.name}");
                    instantiated[i.Cells.First()] = TesseraGeneratorUtils.Instantiate(i, GetParent(), aoh.Result, false, engine);
                    aoh.Completed -= onLoaded;
                };
                aoh.Completed += onLoaded;
            }
        }
    }
}
#endif
