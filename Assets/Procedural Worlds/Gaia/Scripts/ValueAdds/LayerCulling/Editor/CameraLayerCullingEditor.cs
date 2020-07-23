using UnityEditor;
using UnityEngine;

namespace Gaia
{
    [CustomEditor(typeof(CameraLayerCulling))]
    public class CameraLayerCullingEditor : Editor
    {
        private GUIStyle m_boxStyle;

        public override void OnInspectorGUI()
        {
            //Initialization

            //Set up the box style
            if (m_boxStyle == null)
            {
                m_boxStyle = new GUIStyle(GUI.skin.box);
                m_boxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                m_boxStyle.fontStyle = FontStyle.Bold;
                m_boxStyle.alignment = TextAnchor.UpperLeft;
            }

            //Get culling object
            CameraLayerCulling culling = (CameraLayerCulling) target;

            //Monitor for changes
            EditorGUI.BeginChangeCheck();

            GUILayout.BeginVertical("Scene Setup", m_boxStyle);
            GUILayout.Space(20);
            culling.m_mainCamera = (Camera)EditorGUILayout.ObjectField("Main Camera", culling.m_mainCamera, typeof(Camera), true);
            culling.SunLight = (Light)EditorGUILayout.ObjectField("Sun Light", culling.SunLight, typeof(Light), true);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("General Settings", m_boxStyle);
            GUILayout.Space(20);
            culling.m_applyToEditorCamera = EditorGUILayout.Toggle("Apply In Editor", culling.m_applyToEditorCamera);
            culling.m_realtimeUpdate = EditorGUILayout.Toggle("Realtime Update", culling.m_realtimeUpdate);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("Object Culling Settings", m_boxStyle);
            GUILayout.Space(20);
            for (int i = 0; i < culling.m_layerDistances.Length; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    culling.m_layerDistances[i] = EditorGUILayout.FloatField(string.Format("[{0}] {1}", i, layerName), culling.m_layerDistances[i]);
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("Shadow Culling Settings", m_boxStyle);
            GUILayout.Space(20);
            for (int i = 0; i < culling.m_shadowLayerDistances.Length; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    culling.m_shadowLayerDistances[i] = EditorGUILayout.FloatField(string.Format("[{0}] {1}", i, layerName), culling.m_shadowLayerDistances[i]);
                }
            }
            GUILayout.EndVertical();

            if (GUILayout.Button("Revert To Defaults"))
            {
                culling.UpdateDefaults();
            }

            //Check for changes, make undo record, make changes and let editor know we are dirty
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(culling, "Made camera culling changes");
                EditorUtility.SetDirty(culling);
                culling.ApplySceneSetup(culling.m_applyToEditorCamera);
            }
        }
    }
}