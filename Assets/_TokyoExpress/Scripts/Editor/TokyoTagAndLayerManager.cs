#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TokyoExpress.GameEditor
{
    public static class TokyoTagAndLayerManager
    {
        public static readonly string[] RequiredTags = new string[]
        {
            "Player",
            "Obstacle",
            "TrafficMoto",
            "Road",
            "DeliveryTarget",
            "StartStation"
        };

        public static readonly (int index, string name)[] RequiredLayers = new (int, string)[]
        {
            (10, "Road"),
            (11, "PlayerMoto"),
            (12, "TrafficObstacles"),
            (13, "Buildings")
        };

        public static bool CheckIfTagsAndLayersInstalled()
        {
            SerializedObject tagManager = GetTagManagerObject();
            if (tagManager == null) return false;

            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            HashSet<string> existingTags = new HashSet<string>();
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                existingTags.Add(tagsProp.GetArrayElementAtIndex(i).stringValue);
            }

            foreach (string t in RequiredTags)
            {
                if (t != "Player" && !existingTags.Contains(t)) return false;
            }

            SerializedProperty layersProp = tagManager.FindProperty("layers");
            foreach (var reqLayer in RequiredLayers)
            {
                if (reqLayer.index < layersProp.arraySize)
                {
                    string cur = layersProp.GetArrayElementAtIndex(reqLayer.index).stringValue;
                    if (cur != reqLayer.name) return false;
                }
            }

            return true;
        }

        public static void SetupTagsAndLayers()
        {
            SerializedObject tagManager = GetTagManagerObject();
            if (tagManager == null)
            {
                Debug.LogError("[TokyoExpress] Impossible de charger TagManager.asset.");
                return;
            }

            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            HashSet<string> existingTags = new HashSet<string>();
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                existingTags.Add(tagsProp.GetArrayElementAtIndex(i).stringValue);
            }

            int tagsAdded = 0;
            foreach (string tag in RequiredTags)
            {
                if (tag == "Player") continue; // Built-in tag

                if (!existingTags.Contains(tag))
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                    existingTags.Add(tag);
                    tagsAdded++;
                }
            }

            SerializedProperty layersProp = tagManager.FindProperty("layers");
            int layersAdded = 0;
            foreach (var reqLayer in RequiredLayers)
            {
                if (reqLayer.index < layersProp.arraySize)
                {
                    SerializedProperty layerElem = layersProp.GetArrayElementAtIndex(reqLayer.index);
                    if (string.IsNullOrEmpty(layerElem.stringValue))
                    {
                        layerElem.stringValue = reqLayer.name;
                        layersAdded++;
                    }
                    else if (layerElem.stringValue != reqLayer.name)
                    {
                        Debug.LogWarning($"[TokyoExpress] Layer {reqLayer.index} est déjà occupé par '{layerElem.stringValue}'. Écrasé par '{reqLayer.name}'.");
                        layerElem.stringValue = reqLayer.name;
                        layersAdded++;
                    }
                }
            }

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log($"[TokyoExpress] Tags et Layers configurés avec succès ! (+{tagsAdded} Tags, +{layersAdded} Layers)");
        }

        private static SerializedObject GetTagManagerObject()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets != null && assets.Length > 0)
            {
                return new SerializedObject(assets[0]);
            }
            return null;
        }
    }
}
#endif
