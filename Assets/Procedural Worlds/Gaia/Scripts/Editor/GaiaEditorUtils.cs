using System;
using System.Collections;
using System.Collections.Generic;
using PWCommon2;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace Gaia
{
    public static class GaiaEditorUtils
    {
        /// <summary>
        /// Marks the active scene dirty.
        /// </summary>
        public static void MarkSceneDirty()
        {
            if (!EditorSceneManager.GetActiveScene().isDirty)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        /// <summary>
        /// Helper function to create a GUIContent for button icons - decided whether to use Unity Standard or Pro Icons and adds the tooltip text
        /// </summary>
        /// <param name="key">Localization key to get the tooltip from</param>
        /// <param name="standardIcon">Icon to use for Unity Standard Skin</param>
        /// <param name="proIcon">Icon to use for Unity Pro Skin</param>
        /// <param name="editorUtils">Current Editor Utils class to pass in to look up the localized tooltip</param>
        /// <returns>GUIContent with correct texture and tooltip.</returns>
        public static GUIContent GetIconGUIContent(string key, Texture2D standardIcon, Texture2D proIcon, EditorUtils editorUtils)
        {
            Texture icon = null;
            if (EditorGUIUtility.isProSkin)
            {
                icon = proIcon;
            }
            else
            {
                icon = standardIcon;
            }

            return new GUIContent(icon, editorUtils.GetTooltip(key));

        }

        /// <summary>
        /// Handy layer mask interface
        /// </summary>
        /// <param name="label"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public static LayerMask LayerMaskField(GUIContent label, LayerMask layerMask)
        {
            List<string> layers = new List<string>();
            List<int> layerNumbers = new List<int>();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName != "")
                {
                    layers.Add(layerName);
                    layerNumbers.Add(i);
                }
            }
            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                    maskWithoutEmpty |= (1 << i);
            }
            maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers.ToArray());
            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                    mask |= (1 << layerNumbers[i]);
            }
            layerMask.value = mask;
            return layerMask;
        }
    }
}
