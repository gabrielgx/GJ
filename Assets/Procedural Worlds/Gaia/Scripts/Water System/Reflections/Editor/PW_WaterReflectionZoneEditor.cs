using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PWCommon2;
using Gaia.Internal;
using UnityEditor;
using Gaia;

namespace ProcedualWorlds.WaterSystem
{
    [CustomEditor(typeof(PW_WaterReflectionZone))]
    public class PW_WaterReflectionZoneEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private PW_WaterReflectionZone WaterReflections;
        private PWS_WaterSystem PWS_WaterSystem;

        private void OnEnable()
        {
            WaterReflections = (PW_WaterReflectionZone)target;

            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
        }

        #region Inspector Region

        /// <summary>
        /// Custom editor for PWS_WaterReflections
        /// </summary
        public override void OnInspectorGUI()
        {
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            if (WaterReflections == null)
            {
                WaterReflections = (PW_WaterReflectionZone)target;
            }

            if (PWS_WaterSystem == null)
            {
                PWS_WaterSystem = FindObjectOfType<PWS_WaterSystem>();
            }

            m_editorUtils.Panel("GlobalSettings", GlobalSettings, true);
        }

        #endregion

        #region Panel

        /// <summary>
        /// Global Main Panel
        /// </summary>
        /// <param name="helpEnabled"></param>
        private void GlobalSettings(bool helpEnabled)
        {
            m_editorUtils.Heading("ReflectionSetup");
            EditorGUILayout.BeginHorizontal();
            WaterReflections.m_reflectionLayers = GaiaEditorUtils.LayerMaskField(new GUIContent(m_editorUtils.GetTextValue("ReflectionLayers"), m_editorUtils.GetTooltip("ReflectionLayers")), WaterReflections.m_reflectionLayers);
            EditorGUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("ReflectionLayers", helpEnabled);
            EditorGUILayout.Space();

            m_editorUtils.Heading("ReflectionRendering");
            WaterReflections.m_reflectionQuality = (GaiaConstants.GaiaProWaterReflectionsQuality)m_editorUtils.EnumPopup("ReflectionQuality", WaterReflections.m_reflectionQuality, helpEnabled);
            if (PWS_WaterSystem != null)
            {
                m_editorUtils.LabelField("ReflectionDistances");
                if (PWS_WaterSystem.m_customReflectionDistances)
                {
                    List<string> layers = new List<string>();
                    layers.Clear();
                    int layerCount = 0;
                    for (int i = 0; i < 32; i++)
                    {
                        string layerName = LayerMask.LayerToName(i);
                        if (layerName.Length > 1)
                        {
                            layers.Add(layerName);
                            layerCount++;
                        }
                    }

                    for (int i = 0; i < layerCount; i++)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(layers[i]);
                        WaterReflections.m_distances[i] = EditorGUILayout.FloatField(WaterReflections.m_distances[i]);
                        EditorGUILayout.EndHorizontal();
                        EditorGUI.indentLevel--;
                    }
                }
                else if (PWS_WaterSystem.m_customReflectionDistance)
                {
                    EditorGUI.indentLevel++;
                    WaterReflections.m_renderDistance = m_editorUtils.FloatField("CustomRenderDistance", WaterReflections.m_renderDistance);
                    EditorGUI.indentLevel--;
                }
            }
        }

        #endregion
    }
}