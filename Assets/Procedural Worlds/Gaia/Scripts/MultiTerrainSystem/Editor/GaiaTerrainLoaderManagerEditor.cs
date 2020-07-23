using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PWCommon2;
using Gaia.Internal;
using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Gaia
{
    [CustomEditor(typeof(GaiaTerrainLoaderManager))]
    public class GaiaTerrainLoaderManagerEditor : PWEditor, IPWEditor
    {
        private GaiaTerrainLoaderManager m_terrainLoaderManager;
        #if GAIA_PRO_PRESENT
        private GaiaTerrainLoader[] m_terrainLoaders;
        #endif
        private EditorUtils m_editorUtils;

        public void OnEnable()
        {
            m_terrainLoaderManager = (GaiaTerrainLoaderManager)target;
            #if GAIA_PRO_PRESENT
            m_terrainLoaders = Resources.FindObjectsOfTypeAll<GaiaTerrainLoader>();
            #endif
            //m_placeHolders = Resources.FindObjectsOfTypeAll<GaiaTerrainPlaceHolder>();
            //m_placeHolders = m_placeHolders.OrderBy(x => x.name).ToArray();
            //foreach (GaiaTerrainPlaceHolder placeHolder in m_placeHolders)
            //{
            //    placeHolder.UpdateLoadState();
            //}

            //Init editor utils
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
        }


        public override void OnInspectorGUI()
        {
            //Init editor utils
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
            m_editorUtils.Initialize(); // Do not remove this!
            m_editorUtils.Panel("LoaderPanel", DrawLoaders, true);
            m_editorUtils.Panel("PlaceholderPanel", DrawTerrains, true);
        }

        private void DrawTerrains(bool helpEnabled)
        {
            EditorGUILayout.BeginHorizontal();
            if (m_editorUtils.Button("AddToBuildSettings"))
            {
                if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("AddToBuildSettingsPopupTitle"), m_editorUtils.GetTextValue("AddToBuildSettingsPopupText"), m_editorUtils.GetTextValue("Continue"), m_editorUtils.GetTextValue("Cancel")))
                {
                    #if GAIA_PRO_PRESENT
                    GaiaSessionManager.AddTerrainScenesToBuildSettings(m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes);
                    #endif
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (m_editorUtils.Button("UnloadAll"))
            {
                m_terrainLoaderManager.UnloadAll();
            }
            if (m_editorUtils.Button("LoadAll"))
            {
                if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("LoadAllPopupTitle"), m_editorUtils.GetTextValue("LoadAllPopupText"), m_editorUtils.GetTextValue("Continue"), m_editorUtils.GetTextValue("Cancel")))
                {
                    foreach (TerrainScene terrainScene in m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes)
                    {
                        terrainScene.AddReference(m_terrainLoaderManager.gameObject);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            float buttonWidth = 60;
            foreach (TerrainScene terrainScene in m_terrainLoaderManager.TerrainSceneStorage.m_terrainScenes)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(terrainScene.GetTerrainName());

                bool isLoaded = terrainScene.m_loadState == LoadState.Loaded && terrainScene.TerrainObj != null && terrainScene.TerrainObj.activeInHierarchy;

                bool currentGUIState = GUI.enabled;
                GUI.enabled = isLoaded;
                if (m_editorUtils.Button("SelectPlaceholder", GUILayout.Width(buttonWidth)))
                {
                    Selection.activeGameObject = GameObject.Find(terrainScene.GetTerrainName());
                    SceneView.lastActiveSceneView.FrameSelected();
                }
                GUI.enabled = currentGUIState;
                if (isLoaded)
                {
                    if (m_editorUtils.Button("UnloadPlaceholder", GUILayout.Width(buttonWidth)))
                    {
                        terrainScene.RemoveAllReferences(true);
                    }
                }
                else
                {
                    if (m_editorUtils.Button("LoadPlaceholder", GUILayout.Width(buttonWidth)))
                    {
                        terrainScene.AddReference(m_terrainLoaderManager.gameObject);
                    }
                }
                EditorGUILayout.EndHorizontal();
                if (terrainScene.References.Count > 0)
                {
                    EditorGUI.indentLevel++;
                    terrainScene.m_isFoldedOut = m_editorUtils.Foldout(terrainScene.m_isFoldedOut, "ShowTerrainReferences");
                    if (terrainScene.m_isFoldedOut)
                    {
                        
                        foreach (GameObject go in terrainScene.References)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            m_editorUtils.Label(new GUIContent(go.name, m_editorUtils.GetTextValue("TerrainReferenceToolTip")));
                            if (m_editorUtils.Button("TerrainReferenceSelect", GUILayout.Width(buttonWidth)))
                            {
                                Selection.activeObject = go;
                                SceneView.lastActiveSceneView.FrameSelected();
                            }
                            if (m_editorUtils.Button("TerrainReferenceRemove", GUILayout.Width(buttonWidth)))
                            {
                                terrainScene.RemoveReference(go);
                            }
                            GUILayout.Space(100);
                            EditorGUILayout.EndHorizontal();
                        }
                        
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawLoaders(bool helpEnabled)
        {
            #if GAIA_PRO_PRESENT
            foreach (GaiaTerrainLoader terrainLoader in m_terrainLoaders)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(terrainLoader.name);
                terrainLoader.LoadMode = (LoadMode)EditorGUILayout.EnumPopup(terrainLoader.LoadMode);
                if (m_editorUtils.Button("SelectLoader"))
                {
                    Selection.activeGameObject = terrainLoader.gameObject;
                    SceneView.lastActiveSceneView.FrameSelected();
                }
                EditorGUILayout.EndHorizontal();
            }
            #endif
        }
    }
}
