using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Tessera
{
    [CustomEditor(typeof(TesseraInstantiateOutput))]
    class TesseraInstantiateOutputEditor : Editor
    {
        private SerializedProperty tileMappingsProperty;

        private void OnEnable()
        {
            tileMappingsProperty = serializedObject.FindProperty("tileMappings");


        }

        private void DoMappings()
        {
            var tesseraInstantiateOutput = (target as TesseraInstantiateOutput);
            var generator = tesseraInstantiateOutput.GetComponent<TesseraGenerator>();

            // Build dict
            var dict = new Dictionary<TesseraTileBase, (int, TesseraInstantiateOutputTileMapping)>();
            int i = 0;
            foreach(var mapping in tesseraInstantiateOutput.tileMappings)
            {
                dict[mapping.from] = (i++, mapping);
            }

            EditorGUI.indentLevel++;
            foreach (var tileEntry in generator.tiles)
            {
                var tile = tileEntry.tile;

                if (tile == null)
                    continue;

                if (!dict.TryGetValue(tile, out var t))
                {
                    var newBehaviour = (TesseraInstantiateBehaviour)EditorGUILayout.EnumPopup(new GUIContent(tile.name),  TesseraInstantiateBehaviour.Default);
                    if(newBehaviour != TesseraInstantiateBehaviour.Default)
                    {
                        tileMappingsProperty.arraySize = tileMappingsProperty.arraySize + 1;
                        var newElement = tileMappingsProperty.GetArrayElementAtIndex(tileMappingsProperty.arraySize - 1);
                        newElement.FindPropertyRelative("from").objectReferenceValue = tile;
                        newElement.FindPropertyRelative("behaviour").enumValueIndex = (int)newBehaviour;
                    }
                }
                else
                {
                    var (index, mapping) = t;

                    SerializedProperty targetElement = tileMappingsProperty.GetArrayElementAtIndex(index);

                    var behaviourProperty = targetElement.FindPropertyRelative("behaviour");
                    EditorGUILayout.PropertyField(behaviourProperty, new GUIContent(tile.name));
                    EditorGUI.indentLevel++;
                    if (behaviourProperty.enumValueIndex == (int)TesseraInstantiateBehaviour.InstantiateAs)
                    {
                        // Draw to property
                        var toProperty = targetElement.FindPropertyRelative("to");
                        EditorGUILayout.PropertyField(toProperty);

                        // Draw instantiateChildrenOnly property
                        var instantiateChildrenOnlyProperty = targetElement.FindPropertyRelative("instantiateChildrenOnly");
                        EditorGUILayout.PropertyField(instantiateChildrenOnlyProperty);
                    }
                    if (behaviourProperty.enumValueIndex == (int)TesseraInstantiateBehaviour.Constraint)
                    {
                        var constraintProperty = targetElement.FindPropertyRelative(nameof(TesseraInstantiateOutputTileMapping.constraint));
                        EditorGUILayout.PropertyField(constraintProperty);
                    }
                    if (behaviourProperty.enumValueIndex == (int)TesseraInstantiateBehaviour.Function)
                    {
                        var instantiateEventProperty = targetElement.FindPropertyRelative(nameof(TesseraInstantiateOutputTileMapping.instantiateEvent));
                        EditorGUILayout.PropertyField(instantiateEventProperty);
                    }

                    EditorGUI.indentLevel--;
                }
                GUILayout.Space(5);
            }
            EditorGUI.indentLevel--;
        }


        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("parent"));

            tileMappingsProperty.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(tileMappingsProperty.isExpanded, new GUIContent("Tile Mappings"));

            if (tileMappingsProperty.isExpanded)
            {
                DoMappings();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
