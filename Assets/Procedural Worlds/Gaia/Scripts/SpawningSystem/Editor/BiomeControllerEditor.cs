using Gaia.Internal;
using PWCommon2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace Gaia
{

    /// <summary>
    /// Editor for the Biome Controller, allows you to control one set of spawners as a Biome with common position, masking & spawning
    /// </summary>
    [CustomEditor(typeof(BiomeController))]
    public class BiomeControllerEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private BiomeController m_biomeController;
        private Color m_normalBGColor;
        private Color m_dirtyColor;
        private UnityEditorInternal.ReorderableList m_masksReorderable;
        private UnityEditorInternal.ReorderableList m_autoSpawnerReorderable;
        private bool m_masksExpanded = true;
        private CollisionMask[] m_collisionMaskListBeingDrawn;
        private GaiaSettings m_gaiaSettings;
        private float m_lastXPos;
        private float m_lastZPos;
        private bool m_activatePreviewRequested;
        private long m_activatePreviewTimeStamp;
        private GUIStyle m_imageMaskHeader;
        private GUIStyle m_noSpawnersLabelStyle;
        private List<Texture2D> m_tempTextureList = new List<Texture2D>();

        private GaiaSessionManager m_sessionManager;
        private bool m_autoSpawnStarted;
        private GUIStyle m_smallButtonStyle;
        private string m_SaveAndLoadMessage;
        private MessageType m_SaveAndLoadMessageType;

        private GaiaSessionManager SessionManager
        {
            get
            {
                if (m_sessionManager == null)
                {
                    m_sessionManager = GaiaSessionManager.GetSessionManager(false);
                }
                return m_sessionManager;
            }
        }

        private void OnEnable()
        {
            m_biomeController = (BiomeController)target;
            m_biomeController.m_oldName = m_biomeController.transform.name;
            m_normalBGColor = GUI.backgroundColor;
            m_dirtyColor = GaiaUtils.GetColorFromHTML("#FF666666");
            //Init editor utils
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = GaiaUtils.GetGaiaSettings();
            }

           


            CreateBiomeMaskList();
            CreateAutoSpawnerList();

            ImageMask.RefreshSpawnRuleGUIDs();

            GaiaLighting.SetPostProcessingStatus(false);
#if GAIA_PRO_PRESENT
            m_biomeController.TerrainLoader.m_isSelected = true;
#endif

        }

        private void OnDestroy()
        {
            for (int i = 0; i < m_tempTextureList.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(m_tempTextureList[i]);
            }
        }

        private void OnDisable()
        {
            GaiaLighting.SetPostProcessingStatus(true);
#if GAIA_PRO_PRESENT
            m_biomeController.TerrainLoader.m_isSelected = false;
            m_biomeController.TerrainLoader.UpdateTerrains();
#endif
        }

        private void CreateAutoSpawnerList()
        {
            m_autoSpawnerReorderable = new UnityEditorInternal.ReorderableList(m_biomeController.m_autoSpawners, typeof(BiomeSpawnerListEntry), true, true, true, true);
            m_autoSpawnerReorderable.elementHeightCallback = OnElementHeightAutoSpawnerListEntry;
            m_autoSpawnerReorderable.drawElementCallback = DrawAutoSpawnerListElement; ;
            m_autoSpawnerReorderable.drawHeaderCallback = DrawAutoSpawnerListHeader;
            m_autoSpawnerReorderable.onAddCallback = OnAddAutoSpawnerListEntry;
            m_autoSpawnerReorderable.onRemoveCallback = OnRemoveAutosSpawnerListEntry;
            m_autoSpawnerReorderable.onReorderCallback = OnReorderAutoSpawnerList;
        }

        private void OnReorderAutoSpawnerList(ReorderableList list)
        {
            //Do nothing, changing the order does not immediately affect anything in the stamper
        }

        private void OnRemoveAutosSpawnerListEntry(ReorderableList list)
        {
            m_biomeController.m_autoSpawners = StamperAutoSpawnerListEditor.OnRemoveListEntry(m_biomeController.m_autoSpawners, m_autoSpawnerReorderable.index);
            list.list = m_biomeController.m_autoSpawners;
        }

        private void OnAddAutoSpawnerListEntry(ReorderableList list)
        {
            m_biomeController.m_autoSpawners = StamperAutoSpawnerListEditor.OnAddListEntry(m_biomeController.m_autoSpawners);
            list.list = m_biomeController.m_autoSpawners;
            m_biomeController.m_changesMadeSinceLastSave = true;
        }

        private void DrawAutoSpawnerListHeader(Rect rect)
        {
            StamperAutoSpawnerListEditor.DrawListHeader(rect, true, m_biomeController.m_autoSpawners, m_editorUtils, ref m_biomeController.m_autoSpawnerArea);
        }

        private void DrawAutoSpawnerListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            StamperAutoSpawnerListEditor.DrawListElement(rect, m_biomeController.m_autoSpawners[index], ref m_biomeController.m_changesMadeSinceLastSave);
        }

        private void DrawClearControls(bool helpEnabled)
        {
            GUILayout.BeginVertical();
            m_editorUtils.Label("ClearSpawnsLabel");
            m_biomeController.m_settings.m_clearSpawnsToggleTrees = m_editorUtils.Toggle("ClearSpawnsToggleTrees", m_biomeController.m_settings.m_clearSpawnsToggleTrees, helpEnabled);
            m_biomeController.m_settings.m_clearSpawnsToggleDetails = m_editorUtils.Toggle("ClearSpawnsToggleDetails", m_biomeController.m_settings.m_clearSpawnsToggleDetails, helpEnabled);
            m_biomeController.m_settings.m_clearSpawnsToggleGOs = m_editorUtils.Toggle("ClearSpawnsToggleGOs", m_biomeController.m_settings.m_clearSpawnsToggleGOs, helpEnabled);
            m_biomeController.m_settings.m_clearSpawnsToggleSpawnExtensions = m_editorUtils.Toggle("ClearSpawnsToggleSpawnExtensions", m_biomeController.m_settings.m_clearSpawnsToggleSpawnExtensions, helpEnabled);
            m_biomeController.m_settings.m_clearSpawnsFrom = (ClearSpawnFromBiomes)m_editorUtils.EnumPopup("ClearSpawnsFrom", m_biomeController.m_settings.m_clearSpawnsFrom, helpEnabled);
            m_biomeController.m_settings.m_clearSpawnsFor = (ClearSpawnFor)m_editorUtils.EnumPopup("ClearSpawnsFor", m_biomeController.m_settings.m_clearSpawnsFor, helpEnabled);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(m_editorUtils.GetContent("ClearSpawnsButton")))
            {
                ClearOperationSettings clearOperationSettings = ScriptableObject.CreateInstance<ClearOperationSettings>();
                clearOperationSettings.m_clearTrees = m_biomeController.m_settings.m_clearSpawnsToggleTrees;
                clearOperationSettings.m_clearTerrainDetails = m_biomeController.m_settings.m_clearSpawnsToggleDetails;
                clearOperationSettings.m_clearGameObjects = m_biomeController.m_settings.m_clearSpawnsToggleGOs;
                clearOperationSettings.m_clearSpawnExtensions = m_biomeController.m_settings.m_clearSpawnsToggleSpawnExtensions;
                //Translate the biome specific enum to the spawner specific one
                clearOperationSettings.m_clearSpawnFrom = m_biomeController.m_settings.m_clearSpawnsFrom == ClearSpawnFromBiomes.OnlyThisBiome ? ClearSpawnFrom.OnlyThisSpawner: ClearSpawnFrom.AnySource;
                clearOperationSettings.m_clearSpawnFor = m_biomeController.m_settings.m_clearSpawnsFor;

                //Generate a "fake" combined spawner settings objects that hold all the spawn rules & resources of all included spawners
                //=> this will then be used to delete only the spawns made by this biome, treating it as one single spawner
                SpawnerSettings combinedSpawnerSettings = ScriptableObject.CreateInstance<SpawnerSettings>();
                combinedSpawnerSettings.name = m_biomeController.name + " Combined";
                int treeResourceIDOffset = 0;
                int textureResourceIDOffset = 0;
                int detailResourceIDOffset = 0;
                int gameObjectResourceIDOffset = 0;
                int spawnExtensionResourceIDOffset = 0;

                foreach (AutoSpawner autoSpawner in m_biomeController.m_autoSpawners)
                {
                    foreach (SpawnRule sr in autoSpawner.spawner.m_settings.m_spawnerRules)
                    {
                        int resourceIDPlusOffset = 0;
                        switch (sr.m_resourceType)
                        {
                            case GaiaConstants.SpawnerResourceType.TerrainTexture:
                                resourceIDPlusOffset = sr.m_resourceIdx + textureResourceIDOffset;
                                break;
                            case GaiaConstants.SpawnerResourceType.TerrainDetail:
                                resourceIDPlusOffset = sr.m_resourceIdx + detailResourceIDOffset;
                                break;
                            case GaiaConstants.SpawnerResourceType.TerrainTree:
                                resourceIDPlusOffset = sr.m_resourceIdx + treeResourceIDOffset;
                                break;
                            case GaiaConstants.SpawnerResourceType.GameObject:
                                resourceIDPlusOffset = sr.m_resourceIdx + gameObjectResourceIDOffset;
                                break;
                            case GaiaConstants.SpawnerResourceType.SpawnExtension:
                                resourceIDPlusOffset = sr.m_resourceIdx + spawnExtensionResourceIDOffset;
                                break;
                        }
                        combinedSpawnerSettings.m_spawnerRules.Add(new SpawnRule { m_resourceType = sr.m_resourceType, m_name = sr.m_name, m_resourceIdx = resourceIDPlusOffset });
                    }

                    //Copy the resources over, and increase the offset
                    foreach (ResourceProtoTexture protoTexture in autoSpawner.spawner.m_settings.m_resources.m_texturePrototypes)
                    {
                        combinedSpawnerSettings.m_resources.m_texturePrototypes = GaiaUtils.AddElementToArray(combinedSpawnerSettings.m_resources.m_texturePrototypes, protoTexture);
                        textureResourceIDOffset++;
                    }
                    foreach (ResourceProtoDetail protoDetail in autoSpawner.spawner.m_settings.m_resources.m_detailPrototypes)
                    {
                        combinedSpawnerSettings.m_resources.m_detailPrototypes = GaiaUtils.AddElementToArray(combinedSpawnerSettings.m_resources.m_detailPrototypes, protoDetail);
                        detailResourceIDOffset++;
                    }
                    foreach (ResourceProtoTree protoTree in autoSpawner.spawner.m_settings.m_resources.m_treePrototypes)
                    {
                        combinedSpawnerSettings.m_resources.m_treePrototypes = GaiaUtils.AddElementToArray(combinedSpawnerSettings.m_resources.m_treePrototypes, protoTree);
                        treeResourceIDOffset++;
                    }
                    foreach (ResourceProtoGameObject protoGO in autoSpawner.spawner.m_settings.m_resources.m_gameObjectPrototypes)
                    {
                        combinedSpawnerSettings.m_resources.m_gameObjectPrototypes = GaiaUtils.AddElementToArray(combinedSpawnerSettings.m_resources.m_gameObjectPrototypes, protoGO);
                        gameObjectResourceIDOffset++;
                    }
                    foreach (ResourceProtoSpawnExtension protoSpawnExtension in autoSpawner.spawner.m_settings.m_resources.m_spawnExtensionPrototypes)
                    {
                        combinedSpawnerSettings.m_resources.m_spawnExtensionPrototypes = GaiaUtils.AddElementToArray(combinedSpawnerSettings.m_resources.m_spawnExtensionPrototypes, protoSpawnExtension);
                        spawnExtensionResourceIDOffset++;
                    }
                }


                GaiaSessionManager.ClearSpawns(clearOperationSettings, combinedSpawnerSettings, true);


            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private float OnElementHeightAutoSpawnerListEntry(int index)
        {
            return StamperAutoSpawnerListEditor.OnElementHeight();
        }

        private void CreateBiomeMaskList()
        {
            m_masksReorderable = new UnityEditorInternal.ReorderableList(m_biomeController.m_settings.m_imageMasks, typeof(ImageMask), true, true, true, true);
            m_masksReorderable.elementHeightCallback = OnElementHeightStamperMaskListEntry;
            m_masksReorderable.drawElementCallback = DrawStamperMaskListElement; ;
            m_masksReorderable.drawHeaderCallback = DrawStamperMaskListHeader;
            m_masksReorderable.onAddCallback = OnAddStamperMaskListEntry;
            m_masksReorderable.onRemoveCallback = OnRemoveStamperMaskListEntry;
            m_masksReorderable.onReorderCallback = OnReorderStamperMaskList;

            foreach (ImageMask mask in m_biomeController.m_settings.m_imageMasks)
            {
                mask.m_reorderableCollisionMaskList = CreateStamperCollisionMaskList(mask.m_reorderableCollisionMaskList, mask.m_collisionMasks);
            }
        }

        private float OnElementHeightStamperMaskListEntry(int index)
        {
            return ImageMaskListEditor.OnElementHeight(index, m_biomeController.m_settings.m_imageMasks[index]);
        }

        private void DrawStamperMaskListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            //bool isCopiedMask = m_biomeController.m_biomeMasks[index] != null && m_biomeController.m_biomeMasks[index] == m_copiedImageMask;
            ImageMask copiedImageMask = SessionManager.m_copiedImageMask;

            MaskListButtonCommand mlbc = ImageMaskListEditor.DrawMaskListElement(rect, index, m_biomeController.m_settings.m_imageMasks, ref m_collisionMaskListBeingDrawn, m_editorUtils, Terrain.activeTerrain, false, copiedImageMask, m_imageMaskHeader.normal.background, m_gaiaSettings, SessionManager);

            switch (mlbc)
            {
                case MaskListButtonCommand.Delete:
                    m_masksReorderable.index = index;
                    OnRemoveStamperMaskListEntry(m_masksReorderable);
                    break;
                case MaskListButtonCommand.Duplicate:
                    ImageMask newImageMask = ImageMask.Clone(m_biomeController.m_settings.m_imageMasks[index]);
                    m_biomeController.m_settings.m_imageMasks = GaiaUtils.InsertElementInArray(m_biomeController.m_settings.m_imageMasks, newImageMask, index + 1);
                    m_masksReorderable.list = m_biomeController.m_settings.m_imageMasks;
                    m_biomeController.m_settings.m_imageMasks[index + 1].m_reorderableCollisionMaskList = CreateStamperCollisionMaskList(m_biomeController.m_settings.m_imageMasks[index + 1].m_reorderableCollisionMaskList, m_biomeController.m_settings.m_imageMasks[index + 1].m_collisionMasks);
                    serializedObject.ApplyModifiedProperties();

                    break;
                case MaskListButtonCommand.Copy:
                    SessionManager.m_copiedImageMask = m_biomeController.m_settings.m_imageMasks[index];
                    break;
                case MaskListButtonCommand.Paste:
                    m_biomeController.m_settings.m_imageMasks[index] = ImageMask.Clone(copiedImageMask);
                    //Rebuild collsion mask list with new content from the cloning
                    m_biomeController.m_settings.m_imageMasks[index].m_reorderableCollisionMaskList = CreateStamperCollisionMaskList(m_biomeController.m_settings.m_imageMasks[index].m_reorderableCollisionMaskList, m_biomeController.m_settings.m_imageMasks[index].m_collisionMasks);
                    SessionManager.m_copiedImageMask = null;
                    break;

            }
        }

        private void DrawStamperMaskListHeader(Rect rect)
        {
            m_masksExpanded = ImageMaskListEditor.DrawFilterListHeader(rect, m_masksExpanded, m_biomeController.m_settings.m_imageMasks, m_editorUtils);
        }

        private void OnAddStamperMaskListEntry(ReorderableList list)
        {
            float maxWorldHeight = 0f;
            float minWorldHeight = 0f;
            float seaLevel;

            SessionManager.GetWorldMinMax(ref minWorldHeight, ref maxWorldHeight);
            seaLevel = SessionManager.GetSeaLevel();

            m_biomeController.m_settings.m_imageMasks = ImageMaskListEditor.OnAddMaskListEntry(m_biomeController.m_settings.m_imageMasks, maxWorldHeight, minWorldHeight, seaLevel);
            ImageMask lastElement = m_biomeController.m_settings.m_imageMasks[m_biomeController.m_settings.m_imageMasks.Length - 1];
            lastElement.m_reorderableCollisionMaskList = CreateStamperCollisionMaskList(lastElement.m_reorderableCollisionMaskList, lastElement.m_collisionMasks);
            list.list = m_biomeController.m_settings.m_imageMasks;
            m_biomeController.m_changesMadeSinceLastSave = true;
        }

        private void OnRemoveStamperMaskListEntry(ReorderableList list)
        {
            m_biomeController.m_settings.m_imageMasks = ImageMaskListEditor.OnRemoveMaskListEntry(m_biomeController.m_settings.m_imageMasks, list.index);
            list.list = m_biomeController.m_settings.m_imageMasks;
            m_biomeController.m_changesMadeSinceLastSave = true;
        }

        private void OnReorderStamperMaskList(ReorderableList list)
        {
            m_biomeController.m_changesMadeSinceLastSave = true;
        }


        /// <summary>
        /// Creates the reorderable collision mask list for collision masks in the spawner itself.
        /// </summary>
        public ReorderableList CreateStamperCollisionMaskList(ReorderableList list, CollisionMask[] collisionMasks)
        {
            list = new ReorderableList(collisionMasks, typeof(CollisionMask), true, true, true, true);
            list.elementHeightCallback = OnElementHeightCollisionMaskList;
            list.drawElementCallback = DrawStamperCollisionMaskElement;
            list.drawHeaderCallback = DrawStamperCollisionMaskListHeader;
            list.onAddCallback = OnAddStamperCollisionMaskListEntry;
            list.onRemoveCallback = OnRemoveStamperCollisionMaskMaskListEntry;
            return list;
        }

        private void OnRemoveStamperCollisionMaskMaskListEntry(ReorderableList list)
        {
            //look up the collision mask in the spawner's mask list
            foreach (ImageMask imagemask in m_biomeController.m_settings.m_imageMasks)
            {
                if (imagemask.m_reorderableCollisionMaskList == list)
                {
                    imagemask.m_collisionMasks = CollisionMaskListEditor.OnRemoveMaskListEntry(imagemask.m_collisionMasks, list.index);
                    list.list = imagemask.m_collisionMasks;
                    return;
                }
            }
        }

        private void OnAddStamperCollisionMaskListEntry(ReorderableList list)
        {
            //look up the collision mask in the spawner's mask list
            foreach (ImageMask imagemask in m_biomeController.m_settings.m_imageMasks)
            {
                if (imagemask.m_reorderableCollisionMaskList == list)
                {
                    imagemask.m_collisionMasks = CollisionMaskListEditor.OnAddMaskListEntry(imagemask.m_collisionMasks);
                    list.list = imagemask.m_collisionMasks;
                    return;
                }
            }
        }

        private void DrawStamperCollisionMaskListHeader(Rect rect)
        {
            foreach (ImageMask imagemask in m_biomeController.m_settings.m_imageMasks)
            {
                if (imagemask.m_collisionMasks == m_collisionMaskListBeingDrawn)
                {
                    imagemask.m_collisionMaskExpanded = CollisionMaskListEditor.DrawFilterListHeader(rect, imagemask.m_collisionMaskExpanded, imagemask.m_collisionMasks, m_editorUtils);
                }
            }
        }

        private void DrawStamperCollisionMaskElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (m_collisionMaskListBeingDrawn != null && m_collisionMaskListBeingDrawn.Length > index && m_collisionMaskListBeingDrawn[index] != null)
            {
                CollisionMaskListEditor.DrawMaskListElement(rect, index, m_collisionMaskListBeingDrawn[index], m_editorUtils, Terrain.activeTerrain, GaiaConstants.FeatureOperation.Contrast);
            }
        }

        private float OnElementHeightCollisionMaskList(int index)
        {
            return CollisionMaskListEditor.OnElementHeight(index, m_collisionMaskListBeingDrawn);
        }

        public override void OnInspectorGUI()
        {

            m_editorUtils.Initialize(); // Do not remove this!
            m_biomeController = (BiomeController)target;

            //Update location in settings (used when loading back from the settings, e.g. for the session playback)
            m_biomeController.m_settings.m_x = m_biomeController.transform.position.x;
            m_biomeController.m_settings.m_y = m_biomeController.transform.position.y;
            m_biomeController.m_settings.m_z = m_biomeController.transform.position.z;

            //Handle terrain auto-loading
            if (!m_biomeController.m_autoSpawnRequested)
            {
                m_biomeController.UpdateAutoLoadRange();
            }



            if (m_imageMaskHeader == null || m_imageMaskHeader.normal.background == null)
            {
                m_imageMaskHeader = new GUIStyle();
                // Setup colors for Unity Pro
                if (EditorGUIUtility.isProSkin)
                {
                    m_imageMaskHeader.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML("2d2d2dff"), m_tempTextureList);
                }
                else
                {
                    m_imageMaskHeader.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML("a2a2a2ff"), m_tempTextureList);
                }
            }

            //Do we still have outstanding spawn requests?
            if (m_biomeController.m_autoSpawnRequested)
            {
                //we do have, lock GUI
                GUI.enabled = false;

                if (!m_autoSpawnStarted)
                {
                    bool worldSpawn = m_biomeController.m_autoSpawnerArea == GaiaConstants.AutoSpawnerArea.World;
                    float range = m_biomeController.m_settings.m_range;
#if GAIA_PRO_PRESENT
                    m_biomeController.TerrainLoader.LoadMode = LoadMode.Disabled;
                    m_biomeController.TerrainLoader.UnloadTerrains();
#endif
                    Spawner.HandleAutoSpawnerStack(m_biomeController.m_autoSpawners.FindAll(x => x.isActive == true), m_biomeController.transform, range, worldSpawn, m_biomeController.m_settings);
                    m_autoSpawnStarted = true;


                }
                else
                {
                    if (m_biomeController.m_autoSpawners.Count <= 0 || m_biomeController.m_autoSpawners[0].spawner.m_spawnComplete)
                    {
                        m_autoSpawnStarted = false;
                        m_biomeController.m_autoSpawnRequested = false;
                        //unlock GUI
                        GUI.enabled = true;
#if GAIA_PRO_PRESENT
                        m_biomeController.TerrainLoader.LoadMode = m_biomeController.m_loadTerrainMode;
#endif
                    }
                }
            }
            else
            {
                if (m_activatePreviewRequested)
                {
                    m_activatePreviewTimeStamp = GaiaUtils.GetUnixTimestamp();
                }
            }



            if (m_activatePreviewRequested && (m_activatePreviewTimeStamp + m_gaiaSettings.m_stamperAutoHidePreviewMilliseconds < GaiaUtils.GetUnixTimestamp()))
            {
                m_activatePreviewRequested = false;
                m_biomeController.m_biomePreviewDirty = true;
                m_biomeController.m_drawPreview = true;
                //force repaint
                EditorWindow view = EditorWindow.GetWindow<SceneView>();
                view.Repaint();
            }


            serializedObject.Update();
            m_editorUtils.Panel("Biome Controller", DrawBiomeControls, true);
            m_editorUtils.Panel("Spawners", DrawSpawners, true);
            m_editorUtils.Panel("ClearSpawns", DrawClearControls, false);
            m_editorUtils.Panel("Advanced", DrawAdvanced, false);

            if (m_biomeController.m_autoSpawnRequested)
            {
                GUI.enabled = true;
                if (m_editorUtils.Button("Cancel"))
                {
                    foreach (AutoSpawner autoSpawner in m_biomeController.m_autoSpawners)
                    {
                        autoSpawner.status = AutoSpawnerStatus.Done;
                        autoSpawner.spawner.CancelSpawn();
                    }
                }
            }
            else
            {
                if (m_editorUtils.Button("SpawnBiome"))
                {
                    //Check for potential missing resources in autospawners first
                    //foreach (AutoSpawner autoSpawner in m_biomeController.m_autoSpawners)
                    //{
                    //    autoSpawner.spawner.transform.position = m_biomeController.transform.position;
                    //    autoSpawner.spawner.m_settings.m_spawnRange = m_biomeController.m_settings.m_range;

                    //    //if (autoSpawner.spawner.enabled && autoSpawner.spawner.CheckForMissingResources(m_biomeController.m_autoSpawnerArea == GaiaConstants.AutoSpawnerArea.World))
                    //    //{
                    //    //    Debug.Log("Spawner " + autoSpawner.spawner.name + " is missing resources on the terrain, Biome spawning was cancelled. Please deactivate this Spawner in the Biome Spawner List, or let the Spawner add the missing resources to the terrain.");
                    //    //    return;
                    //    //}
                    //}
                    m_biomeController.m_autoSpawnRequested = true;
                    if (m_gaiaSettings.m_spawnerAutoHidePreviewMilliseconds > 0)
                    {
                        m_activatePreviewRequested = true;
                        m_activatePreviewTimeStamp = GaiaUtils.GetUnixTimestamp();
                    }
                    m_biomeController.m_drawPreview = false;
                }
            }
            serializedObject.ApplyModifiedProperties();

        }

        private void DrawAdvanced(bool helpEnabled)
        {
            m_editorUtils.Panel("RemoveForeignResources", DrawForeignResourceRemoval, false);
            m_editorUtils.Panel("AddPPVolume", DrawPostProcessing, false);
            m_editorUtils.Panel("Appearance", DrawAppearance, false);

            if (m_biomeController.m_changesMadeSinceLastSave)
            {
                GUI.backgroundColor = m_dirtyColor;
                m_editorUtils.Panel("SaveLoadChangesPending", DrawSaveAndLoad, false);
                GUI.backgroundColor = m_normalBGColor;
            }
            else
            {
                m_editorUtils.Panel("SaveAndLoad", DrawSaveAndLoad, false);
            }
        }

        private void DrawPostProcessing(bool helpEnabled)
        {
            bool currentGUIState = GUI.enabled;
#if !UNITY_POST_PROCESSING_STACK_V2
            EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("NoPPInstalled"), MessageType.Warning);
            GUI.enabled = false;
#else
            m_biomeController.m_postProcessProfile = (PostProcessProfile)m_editorUtils.ObjectField("PostProcessingProfile", m_biomeController.m_postProcessProfile, typeof(PostProcessProfile), false, helpEnabled);
            m_biomeController.m_settings.m_postProcessBlenddDistance = m_editorUtils.Slider("PostProcessingBlendDistance", m_biomeController.m_settings.m_postProcessBlenddDistance, 0f, 500f, helpEnabled);
            m_biomeController.m_ppVSpawnMode = (GaiaConstants.BiomePostProcessingVolumeSpawnMode)m_editorUtils.EnumPopup("PostProcessingSpawnMode", m_biomeController.m_ppVSpawnMode, helpEnabled);
            if (m_biomeController.m_postProcessProfile == null)
            {
                GUI.enabled = false;
            }
            if (m_editorUtils.Button("CreatePPVolume"))
            {
                //Spawn Biome PP profile in local mode only - no good results if multiple biomes spawn in world spawn mode and stack multiple PP profiles on top of each other across the entire world...
                if (m_biomeController.m_postProcessProfile != null)
                {
                    GaiaLighting.PostProcessingBiomeSpawning(m_biomeController.name, m_biomeController.m_postProcessProfile, m_biomeController.m_settings.m_range * 2f, m_biomeController.m_settings.m_postProcessBlenddDistance, m_biomeController.m_ppVSpawnMode);
                }
            }
            GUI.enabled = true;
#endif

        }

        private void DrawSpawners(bool helpEnabled)
        {
            Rect listRect = EditorGUILayout.GetControlRect(true, m_autoSpawnerReorderable.GetHeight());
            m_autoSpawnerReorderable.DoList(listRect);
            m_editorUtils.InlineHelp("AutoSpawnerHeader", helpEnabled);

            if (m_autoSpawnerReorderable.list.Count <= 0)
            {
                if (m_noSpawnersLabelStyle == null)
                {
                    m_noSpawnersLabelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
                }
                m_editorUtils.LabelField("NoAutoSpawnersYet", m_noSpawnersLabelStyle);
            }

            GUILayout.Space(10);

            if (m_editorUtils.Button("CreateNewSpawner"))
            {
                string newName = m_biomeController.name.Replace(" Biome", "") + "-New Spawner";
                List<string> allChildNames = new List<string>();
                foreach (Transform t in m_biomeController.transform)
                {
                    allChildNames.Add(t.name);
                }

                newName = ObjectNames.GetUniqueName(allChildNames.ToArray(), newName);
                GameObject spawnerObj = new GameObject(newName);
                Spawner spawner = spawnerObj.AddComponent<Spawner>();
                m_biomeController.m_autoSpawners.Add(new AutoSpawner { isActive = true, spawner = spawner});
                spawner.transform.parent = m_biomeController.transform;
                Selection.activeGameObject = spawnerObj;
                EditorGUIUtility.PingObject(spawnerObj);
            }
        }

        private void DrawSaveAndLoad(bool helpEnabled)
        {
            GUI.backgroundColor = m_normalBGColor;

            if (!string.IsNullOrEmpty(m_SaveAndLoadMessage))
            {
                EditorGUILayout.HelpBox(m_SaveAndLoadMessage, m_SaveAndLoadMessageType, true);
            }
            EditorGUILayout.BeginHorizontal();
            if (m_editorUtils.Button("LoadButton"))
            {

                string openFilePath = EditorUtility.OpenFilePanel("Load Spawner settings..", GaiaDirectories.GetSettingsDirectory(), "asset");

                bool loadConditionsMet = true;

                //Do we have a path to begin with?
                if (openFilePath == null || openFilePath == "")
                {
                    //Silently abort in this case, the user has pressed "Abort" in the File Open Dialog
                    loadConditionsMet = false;
                }

                //Look for the Assets Directory
                if (!openFilePath.Contains("Assets") && loadConditionsMet)
                {
                    m_SaveAndLoadMessage = m_editorUtils.GetContent("LoadNoAssetDirectory").text;
                    m_SaveAndLoadMessageType = MessageType.Error;
                    loadConditionsMet = false;
                }
                if (loadConditionsMet)
                {
                    openFilePath = GaiaDirectories.GetPathStartingAtAssetsFolder(openFilePath);
                    BiomePreset presetToLoad = (BiomePreset)AssetDatabase.LoadAssetAtPath(openFilePath, typeof(BiomePreset));

                    if (presetToLoad != null)
                    {
                        //Load in the resource file that was last used first

                        //settingsToLoad.m_resourcesPath = AssetDatabase.GUIDToAssetPath(settingsToLoad.m_resourcesGUID);

                        m_biomeController.LoadFromPreset(presetToLoad);
                        m_SaveAndLoadMessage = m_editorUtils.GetContent("LoadSuccessful").text;
                        m_SaveAndLoadMessageType = MessageType.Info;
                    }
                    else
                    {
                        m_SaveAndLoadMessage = m_editorUtils.GetContent("LoadFailed").text;
                        m_SaveAndLoadMessageType = MessageType.Error;
                    }
                }

            }
            if (m_editorUtils.Button("SaveButton"))
            {
                string dialogPath = m_biomeController.m_settings.m_lastSavePath;
                string filename = m_biomeController.transform.name.Replace(" Biome", "");
                if (string.IsNullOrEmpty(dialogPath))
                {
                    dialogPath = GaiaDirectories.GetUserBiomeDirectory();

                }
                else
                {
                    filename = dialogPath.Substring(dialogPath.LastIndexOf('/') + 1).Replace(".asset", "");
                }
                string saveFilePath = EditorUtility.SaveFilePanel("Save Spawner settings as..", dialogPath, filename, "asset");
                bool saveConditionsMet = true;

                //Do we have a path to begin with?
                if (saveFilePath == null || saveFilePath == "")
                {
                    //Silently abort in this case, the user has pressed "Abort" in the File Open Dialog
                    saveConditionsMet = false;
                }

                //Look for the Assets Directory
                if (!saveFilePath.Contains("Assets") && saveConditionsMet)
                {
                    m_SaveAndLoadMessage = m_editorUtils.GetContent("SaveNoAssetDirectory").text;
                    m_SaveAndLoadMessageType = MessageType.Error;
                    saveConditionsMet = false;
                }

                if (saveConditionsMet)
                {
                    saveFilePath = GaiaDirectories.GetPathStartingAtAssetsFolder(saveFilePath);

                    m_biomeController.m_settings.m_lastSavePath = saveFilePath;

                    BiomePreset biomePreset = ScriptableObject.CreateInstance<BiomePreset>();
                    bool errors = false;
                    foreach (AutoSpawner autoSpawner in m_biomeController.m_autoSpawners)
                    {
                        string spawnerSettingsSaveFilePath = autoSpawner.spawner.m_settings.lastSavePath;
                        //Is there a path provided already? If not, we create one.
                        if (string.IsNullOrEmpty(spawnerSettingsSaveFilePath))
                        {
                            spawnerSettingsSaveFilePath = GaiaDirectories.GetUserBiomeDirectory() + Path.DirectorySeparatorChar + autoSpawner.spawner.name + ".asset";
                        }
                        autoSpawner.spawner.m_settings.lastSavePath = spawnerSettingsSaveFilePath;
                        AssetDatabase.CreateAsset(autoSpawner.spawner.m_settings, spawnerSettingsSaveFilePath);
                        AssetDatabase.ImportAsset(spawnerSettingsSaveFilePath);
                        //Was saving the spawner settings under this path successful?
                        SpawnerSettings spawnerSettings = (SpawnerSettings)AssetDatabase.LoadAssetAtPath(autoSpawner.spawner.m_settings.lastSavePath, typeof(SpawnerSettings));
                        
                        if (spawnerSettings == null)
                        {
                            Debug.LogError("Error while saving the biome preset: The spawner settings for the spawner '" + autoSpawner.spawner.name + "' could not be created / found. Try to save the settings for this spawner manually before saving the biome preset.");
                            errors = true;
                        }
                        else
                        {
                            //Re-load the spawner settings to dissociate the spawner settings with the file we just saved
                            //otherwise the users will edit the scriptable object directly when altering the spawner in the scene.
                            autoSpawner.spawner.LoadSettings(spawnerSettings);
                            biomePreset.m_spawnerPresetList.Add(new BiomeSpawnerListEntry() { m_isActiveInBiome = autoSpawner.isActive, m_autoAssignPrototypes = true, m_spawnerSettings = spawnerSettings });
                        }
                    }
#if UNITY_POST_PROCESSING_STACK_V2
                    biomePreset.postProcessProfile = m_biomeController.m_postProcessProfile;
#endif

                    AssetDatabase.CreateAsset(biomePreset, saveFilePath);
                    AssetDatabase.ImportAsset(saveFilePath);

                    //Check if save was successful
                    BiomePreset biomePresetToLoad = (BiomePreset)AssetDatabase.LoadAssetAtPath(saveFilePath, typeof(BiomePreset));
                    if (biomePresetToLoad != null && !errors)
                    {
                        m_SaveAndLoadMessage = m_editorUtils.GetContent("SaveSuccessful").text;
                        m_biomeController.m_changesMadeSinceLastSave = false;
                        m_SaveAndLoadMessageType = MessageType.Info;
                        EditorGUIUtility.PingObject(biomePresetToLoad);
                        //dissociate the current stamper settings from the file we just saved, otherwise the user will continue editing the file afterwards
                        //We do this by just loading the file in again we just created
                    }
                    else
                    {
                        m_SaveAndLoadMessage = m_editorUtils.GetContent("SaveFailed").text;
                        m_SaveAndLoadMessageType = MessageType.Error;
                    }
                }

            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawForeignResourceRemoval(bool helpEnabled)
        {
            m_biomeController.m_settings.m_removeForeignGameObjects = m_editorUtils.Toggle("RemoveForeignGameObjects", m_biomeController.m_settings.m_removeForeignGameObjects, helpEnabled);
            m_biomeController.m_settings.m_removeForeignGameObjectStrength = m_editorUtils.Slider("RemoveForeignGameObjectsStrength", m_biomeController.m_settings.m_removeForeignGameObjectStrength, 0f, 1f, helpEnabled);
            m_biomeController.m_settings.m_removeForeignTrees = m_editorUtils.Toggle("RemoveForeignTrees", m_biomeController.m_settings.m_removeForeignTrees, helpEnabled);
            m_biomeController.m_settings.m_removeForeignTreesStrength = m_editorUtils.Slider("RemoveForeignTreesStrength", m_biomeController.m_settings.m_removeForeignTreesStrength, 0f, 1f, helpEnabled);
            m_biomeController.m_settings.m_removeForeignTerrainDetails = m_editorUtils.Toggle("RemoveForeignTerrainDetails", m_biomeController.m_settings.m_removeForeignTerrainDetails, helpEnabled);
            m_biomeController.m_settings.m_removeForeignTerrainDetailsStrength = m_editorUtils.Slider("RemoveForeignTerrainDetailsDensity", m_biomeController.m_settings.m_removeForeignTerrainDetailsStrength, 0f, 1f, helpEnabled);
            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            if (m_editorUtils.Button("RemoveResources"))
            {
                RemoveNonBiomeResourcesSettings removeNonBiomeResourcesSettings = ScriptableObject.CreateInstance<RemoveNonBiomeResourcesSettings>();
                removeNonBiomeResourcesSettings.m_biomeControllerSettings = m_biomeController.m_settings;

                removeNonBiomeResourcesSettings.m_spawnerSettingsList = new List<SpawnerSettings>();

                foreach (AutoSpawner autoSpawner in m_biomeController.m_autoSpawners)
                {
                    removeNonBiomeResourcesSettings.m_spawnerSettingsList.Add(autoSpawner.spawner.m_settings);
                }

                GaiaSessionManager.RemoveNonBiomeResources(removeNonBiomeResourcesSettings, true, m_biomeController);

                //if (m_biomeController.m_removeForeignGameObjects)
                //{
                //    m_biomeController.RemoveForeignGameObjects();
                //}
                //if (m_biomeController.m_removeForeignTrees)
                //{
                //    m_biomeController.RemoveForeignTrees();
                //}
                //if (m_biomeController.m_removeForeignTerrainDetails)
                //{
                //    m_biomeController.RemoveForeignTerrainDetails();
                //}
            }
        }
        private void DrawAppearance(bool showHelp)
        {
#if GAIA_PRO_PRESENT
            m_biomeController.m_loadTerrainMode = (LoadMode)m_editorUtils.EnumPopup("AutoLoadTerrains", m_biomeController.m_loadTerrainMode, showHelp);
#endif
            m_biomeController.m_showSeaLevelPlane = m_editorUtils.Toggle("ShowSeaLevelPlane", m_biomeController.m_showSeaLevelPlane, showHelp);
            m_biomeController.m_showSeaLevelinPreview = m_editorUtils.Toggle("ShowSeaLevelSpawnerPreview", m_biomeController.m_showSeaLevelinPreview, showHelp);
            m_biomeController.m_showBoundingBox = m_editorUtils.Toggle("ShowBoundingBox", m_biomeController.m_showBoundingBox, showHelp);
        }

        private void DrawBiomeControls(bool helpEnabled)
        {
            EditorGUI.BeginChangeCheck();
            if (m_biomeController.transform.name == "Custom Biome")
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("NoUniqueName"), MessageType.Warning);            
            }
            m_biomeController.transform.name = m_editorUtils.DelayedTextField("Name", m_biomeController.transform.name);

            if (m_biomeController.m_oldName != m_biomeController.transform.name)
            {
                if (m_biomeController.transform.childCount > 0)
                {
                    if (EditorUtility.DisplayDialog("Rename detected", "It looks like you renamed the biome controller - do you want to rename all child spawners that used the old name as well?", "Yes", "No"))
                    {
                        string oldBiomeString = m_biomeController.m_oldName.Replace(" Biome", "");
                        string newBiomeString = m_biomeController.transform.name.Replace(" Biome", "");
                        foreach (Transform t in m_biomeController.transform)
                        {
                            t.name = t.name.Replace(oldBiomeString, newBiomeString);
                        }
                    }
                }
                m_biomeController.m_oldName = m_biomeController.transform.name;
            }

            float oldRange = m_biomeController.m_settings.m_range;
            m_biomeController.m_settings.m_range = m_editorUtils.Slider("Range", m_biomeController.m_settings.m_range, 1, 4096, helpEnabled);
            //m_editorUtils.LabelField("SeaLevel", new GUIContent(SessionManager.GetSeaLevel().ToString() + " m"), helpEnabled);

            float maxSeaLevel = 2000f;
            if (m_biomeController.GetCurrentTerrain() != null)
            {
                maxSeaLevel = m_biomeController.GetCurrentTerrain().terrainData.size.y;
            }
            else
            {
                maxSeaLevel = SessionManager.GetSeaLevel() + 500f;
            }
            float oldSeaLevel = SessionManager.GetSeaLevel();
            float newSeaLEvel = m_editorUtils.Slider("SeaLevel", oldSeaLevel, 0, maxSeaLevel, helpEnabled);
            if (oldSeaLevel != newSeaLEvel)
            {
                SessionManager.SetSeaLevel(newSeaLEvel);
                SceneView.RepaintAll();
            }

            if (oldRange != m_biomeController.m_settings.m_range)
            {
                //Range changed, adjust the range for all autospawners associated with this biome
                foreach (AutoSpawner autoSpawner in m_biomeController.m_autoSpawners)
                {
                    autoSpawner.spawner.m_settings.m_spawnRange = m_biomeController.m_settings.m_range;
                }
            }


            if (GaiaUtils.ColorsEqual(m_biomeController.m_settings.m_visualisationColor, GaiaConstants.spawnerInitColor))
            {
                //The magic number of 0.618... equals the golden ratio to get an even distributon of colors from the available palette
                m_biomeController.m_settings.m_visualisationColor = m_gaiaSettings.m_spawnerColorGradient.Evaluate((0.618033988749895f * 1) % 1);
            }
            GUILayout.BeginHorizontal();

            m_editorUtils.Label("VisualisationColor", GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
            Color currentBGColor = GUI.backgroundColor;
            if (m_biomeController.m_drawPreview)
            {
                GUI.backgroundColor = m_biomeController.m_settings.m_visualisationColor;
            }
            GUIContent GCvisualizeIcon = GaiaEditorUtils.GetIconGUIContent("IconVisible", m_gaiaSettings.m_IconVisible, m_gaiaSettings.m_IconProVisible, m_editorUtils);

            if (m_smallButtonStyle == null)
            {
                m_smallButtonStyle = new GUIStyle(GUI.skin.button);
                m_smallButtonStyle.padding = new RectOffset(2, 2, 2, 2);
                m_smallButtonStyle.margin = new RectOffset(0, 5, 0, 2);
            }
            GUILayout.Space(-5);
            if (m_editorUtils.Button(GCvisualizeIcon, m_smallButtonStyle, GUILayout.Height(20), GUILayout.Width(20)))
            {
                m_biomeController.m_drawPreview = !m_biomeController.m_drawPreview;
                if (m_biomeController.m_drawPreview)
                {
                    m_biomeController.m_biomePreviewDirty = true;
                }
            }
            GUI.backgroundColor = currentBGColor;
            GUILayout.Space(5);
            m_biomeController.m_settings.m_visualisationColor = EditorGUILayout.ColorField(m_biomeController.m_settings.m_visualisationColor, GUILayout.MaxWidth(60));
          
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            Rect listRect;
            if (m_masksExpanded)
            {
                listRect = EditorGUILayout.GetControlRect(true, m_masksReorderable.GetHeight());
                m_masksReorderable.DoList(listRect);
                listRect.y += m_masksReorderable.GetHeight();
                listRect.y += EditorGUIUtility.singleLineHeight;
                listRect.height = m_autoSpawnerReorderable.GetHeight();
            }
            else
            {
                int oldIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 1;
                m_masksExpanded = EditorGUILayout.Foldout(m_masksExpanded, ImageMaskListEditor.PropertyCount("MaskSettings", m_biomeController.m_settings.m_imageMasks,m_editorUtils), true);
                listRect = GUILayoutUtility.GetLastRect();
                listRect.y += EditorGUIUtility.singleLineHeight * 2f;
                EditorGUI.indentLevel = oldIndent;
            }



            //EditorGUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("MaskSettings", helpEnabled);


            if (EditorGUI.EndChangeCheck())
            {
                m_biomeController.m_biomePreviewDirty = true;
                EditorUtility.SetDirty(m_biomeController);
            }

            //GUILayout.Space(EditorGUIUtility.singleLineHeight);
            //GUILayout.Space(m_autoSpawnerReorderable.GetHeight());
        }

        private void OnSceneGUI()
        {
            // dont render preview if this isnt a repaint. losing performance if we do
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }


            //reset rotation, rotation for the biomes is currently not supported because it causes too many issues
            m_biomeController.transform.rotation = new Quaternion();

            //set the preview dirty if the transform changed so it will be redrawn correctly in the new location
            //the lastXPos & lastZPos variables are a workaround, because transform.hasChanged was triggering too often
            if (m_lastXPos != m_biomeController.transform.position.x || m_lastZPos != m_biomeController.transform.position.z)
            {
                m_lastXPos = m_biomeController.transform.position.x;
                m_lastZPos = m_biomeController.transform.position.z;
                m_biomeController.m_biomePreviewDirty = true;
            }

            m_biomeController.DrawBiomePreview();
        }
    }
}
