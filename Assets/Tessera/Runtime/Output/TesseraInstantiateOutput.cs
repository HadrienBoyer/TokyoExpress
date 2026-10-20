using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Tessera
{
    public enum TesseraInstantiateBehaviour
    {
        Default,
        Nothing,
        InstantiateAs,
        Constraint,
        Function,
        InstantiateAddressable
    }

    [Serializable]
    public class TesseraInstantiateEvent : UnityEvent<TesseraCompletion, TesseraTileInstance, Action<GameObject>> { }

    [Serializable]
    public class TesseraInstantiateOutputTileMapping
    {
        public TesseraTileBase from;

        public TesseraInstantiateBehaviour behaviour;

        public GameObject to;

        public bool instantiateChildrenOnly;

        public TesseraBaseSceneConstraintComponent constraint;

        public TesseraInstantiateEvent instantiateEvent;
    }

    /// <summary>
    /// Attach this to a TesseraGenerator to control how tiles are instantiated.
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    [RequireComponent(typeof(TesseraGenerator))]
    [AddComponentMenu("Tessera/Tessera Instantiate Output", 40)]
    public class TesseraInstantiateOutput : MonoBehaviour, ITesseraTileOutput
    {
        public Transform parent;

        public TesseraInstantiateOutputTileMapping[] tileMappings = new TesseraInstantiateOutputTileMapping[0];

        private Dictionary<Vector3Int, GameObject[]> instantiated = new Dictionary<Vector3Int, GameObject[]>();

        public TesseraInstantiateOutput()
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
                switch(mapping.behaviour)
                {
                    case TesseraInstantiateBehaviour.Default:
                        instantiated[i.Cells.First()] = DoDefault(completion, i, mapping, engine);
                        break;
                    case TesseraInstantiateBehaviour.Nothing:
                        break;
                    case TesseraInstantiateBehaviour.InstantiateAs:
                        instantiated[i.Cells.First()] = DoInstantiateAs(completion, i, mapping, engine);
                        break;
                    case TesseraInstantiateBehaviour.Constraint:
                        instantiated[i.Cells.First()] = DoConstraint(completion, i, mapping, engine);
                        break;
                    case TesseraInstantiateBehaviour.Function:
                        instantiated[i.Cells.First()] = DoFunction(completion, i, mapping, engine);
                        break;
                    default:
                        throw new Exception($"Unexpected behaviour {mapping.behaviour}");
                }
            }
        }


        private GameObject[] DoDefault(TesseraCompletion completion, TesseraTileInstance i, TesseraInstantiateOutputTileMapping mapping, IEngineInterface engine)
        {
            return TesseraGeneratorUtils.Instantiate(i, GetParent(), engine);
        }

        private GameObject[] DoInstantiateAs(TesseraCompletion completion, TesseraTileInstance i, TesseraInstantiateOutputTileMapping mapping, IEngineInterface engine)
        {
            return TesseraGeneratorUtils.Instantiate(i, GetParent(), mapping.to, mapping.instantiateChildrenOnly, engine);
        }

        private GameObject[] DoConstraint(TesseraCompletion completion, TesseraTileInstance i, TesseraInstantiateOutputTileMapping mapping, IEngineInterface engine)
        {
            if (mapping.constraint == null)
                return Array.Empty<GameObject>();
            var constraint = mapping.constraint;
            if(constraint is TesseraVolume volume)
            {
                if (volume.invertArea)
                    throw new Exception($"Volume {volume} has invertArea=true, which doesn't make sense when used with {nameof(TesseraInstantiateOutput)}");
                var go = new GameObject($"{constraint} {i.Cell}");
                go.transform.position = i.Position;
                go.transform.rotation = i.Rotation;
                go.transform.localScale = i.LocalScale;
                go.transform.parent = GetParent();

                var newVolume = go.AddComponent<TesseraVolume>();
                newVolume.volumeType = volume.volumeType;
                newVolume.tiles = volume.tiles;

                var tileBase = (TesseraTileBase)go.AddComponent(i.Tile.GetType());
                foreach (var offset in i.Tile.sylvesOffsets)
                    tileBase.AddOffset(offset);
                tileBase.center = i.Tile.center;
                tileBase.cellSize = i.Tile.cellSize;

                return new[] { go };
            }
            if(constraint is TesseraPinned pin)
            {
                var go = new GameObject($"{constraint} {i.Cell}");
                go.transform.position = i.Position;
                go.transform.rotation = i.Rotation;
                go.transform.localScale = i.LocalScale;

                go.transform.parent = GetParent();

                var newPin = go.AddComponent<TesseraPinned>();
                newPin.pinType = pin.pinType;
                newPin.tile = pin.tile;

                return new[] { go };
            }
            throw new Exception($"Constraint {constraint} is not a recognized type");
        }


        Vector3 GetPosition(TesseraCompletion completion, Vector3Int cell)
        {
            return completion.gridTransform.ToMatrix().MultiplyPoint3x4(completion.grid.GetCellCenter((Sylves.Cell)cell));
        }

        private GameObject[] DoFunction(TesseraCompletion completion, TesseraTileInstance i, TesseraInstantiateOutputTileMapping mapping, IEngineInterface engine)
        {
            var gos = new List<GameObject>();
            mapping.instantiateEvent?.Invoke(completion, i, gos.Add);
            return gos.ToArray();
        }

    }
    }
