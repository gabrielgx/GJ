using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using PWCommon2;
using UnityEditorInternal;
using Gaia.Internal;
using static Gaia.GaiaConstants;
using System.Linq;
using System.IO;
using static Gaia.FbmFractalType;
using UnityEditor.UIElements;

namespace Gaia
{
    [CustomEditor(typeof(Spawner))]
    public class SpawnerEditor : PWEditor, IPWEditor
    {
        GUIStyle m_boxStyle;
        GUIStyle m_wrapStyle;
        GUIStyle m_smallButtonStyle;
        Spawner m_spawner;
        private EditorUtils m_editorUtils;
        DateTime m_timeSinceLastUpdate = DateTime.Now;
        bool m_startedUpdates = false;
        private bool m_showTooltips = true;
        private bool m_spawnerMaskListExpanded = true;

        private bool[] m_spawnRuleMaskListExpanded;
        private bool[] m_previewImageDisplayedDuringLayout;

        private ReorderableList[] m_reorderableRuleMasksLists;


        //The Spawner Editor is a complex editor drawing settings for resources, spawner settings, reorderable mask lists, etc.
        //For all this to work it is sometimes required to store the current thing that is "Being Drawn" in a temporary variable so it becomes accessible elsewhere.
        private ImageMask[] m_maskListBeingDrawn;
        private CollisionMask[] m_collisionMaskListBeingDrawn;
        private int m_spawnRuleIndexBeingDrawn;
        private int m_spawnRuleMaskIndexBeingDrawn;
        private ResourceProtoTexture m_textureResourcePrototypeBeingDrawn;
        private ResourceProtoTree m_treeResourcePrototypeBeingDrawn;
        private ResourceProtoDetail m_terrainDetailPrototypeBeingDrawn;
        private ResourceProtoGameObject m_gameObjectResourcePrototypeBeingDrawn;
        private ResourceProtoSpawnExtension m_spawnExtensionPrototypeBeingDrawn;
        private ResourceProtoStampDistribution m_stampDistributionPrototypeBeingDrawn;
        private ResourceProtoWorldBiomeMask m_worldBiomeMaskPrototypeBeingDrawn;

        //private EditorUtilsOLD m_editorUtils = new EditorUtilsOLD();
        private UnityEditorInternal.ReorderableList m_reorderableSpawnerMaskList;

        private float m_lastXPos;
        private float m_lastZPos;

        private bool m_activatePreviewRequested;
        private long m_activatePreviewTimeStamp;
        private bool m_showStampBaseHelp;
        private bool m_showExportTerrainHelp;
        private Color visColor;

        private GUIStyle m_spawnRulesHeader;
        private GUIStyle m_singleSpawnRuleHeader;
        private GUIStyle m_spawRulesBorder;
        private GUIStyle m_noRulesLabelStyle;

        private string m_SaveAndLoadMessage = "";
        private MessageType m_SaveAndLoadMessageType;
        private GaiaSettings m_gaiaSettings;
        private GaiaSessionManager m_sessionManager;
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
        private bool m_rulePanelHelpActive;
        private List<Texture2D> m_tempTextureList = new List<Texture2D>();
        private int m_drawResourcePreviewRuleId;
        private string m_ResourceManagementMessage = "";
        private MessageType m_ResourceManagementMessageType;


        /// <summary>
        /// This is a color used to initialize all spawning rules with a progressing palette.
        /// </summary>
        private Color m_rollingColor = Color.red;

        private bool m_changesMadeSinceLastSave;
        private Color m_dirtyColor;
        private Color m_normalBGColor;

        private List<string> m_stampCategoryNames = new List<string>();
        private int[] m_stampCategoryIDArray;

        private Stamper m_baseTerrainStamper;

        private Stamper BaseTerrainStamper
        {
            get
            {
                if (m_baseTerrainStamper == null)
                {
                    CreateBaseTerrainStamper();
                }
                return m_baseTerrainStamper;
            }
            set { m_baseTerrainStamper = value; }
        }


        void OnEnable()
        {
            //Get the settings and update tooltips
            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = GaiaUtils.GetGaiaSettings();
            }
            if (m_gaiaSettings != null)
            {
                m_showTooltips = m_gaiaSettings.m_showTooltips;
            }

            //Get our spawner
            m_spawner = (Spawner)target;

            //make sure we got settings
            if (m_spawner.m_settings == null)
            {
                m_spawner.m_settings = ScriptableObject.CreateInstance<SpawnerSettings>();
                m_reorderableRuleMasksLists = null;
                serializedObject.ApplyModifiedProperties();
            }



            //Init editor utils
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            m_previewImageDisplayedDuringLayout = new bool[m_spawner.m_settings.m_spawnerRules.Count];

            CreateMaskLists();


            //Fill the enum popup for stamp categories / feature names from the stamp directory
            m_stampCategoryNames.Clear();
            var info = new DirectoryInfo(GaiaDirectories.GetStampDirectory());
            var fileInfo = info.GetDirectories();
            foreach (DirectoryInfo dir in fileInfo)
            {
                m_stampCategoryNames.Add(dir.Name);
            }

            m_stampCategoryIDArray = Enumerable
                               .Repeat(0, (int)((m_stampCategoryNames.ToArray().Length - 1) / 1) + 1)
                               .Select((tr, ti) => tr + (1 * ti))
                               .ToArray();

            //Clean up any rules that relate to missing resources
            CleanUpRules();

            //Do some simple sanity checks
            if (m_spawner.m_rndGenerator == null)
            {
                m_spawner.m_rndGenerator = new Gaia.XorshiftPlus(m_spawner.m_seed);
            }

            if (m_spawner.m_spawnFitnessAttenuator == null)
            {
                m_spawner.m_spawnFitnessAttenuator = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1.0f), new Keyframe(1f, 0.0f));
            }

            if (m_spawner.m_spawnCollisionLayers.value == 0)
            {
                m_spawner.m_spawnCollisionLayers = Gaia.TerrainHelper.GetActiveTerrainLayer();
            }

            m_lastXPos = m_spawner.transform.position.x;
            m_lastZPos = m_spawner.transform.position.z;

            if (m_spawner.m_settings.m_isWorldmapSpawner)
            {
                SessionManager.m_session.m_worldBiomeMaskSettings = m_spawner.m_settings;
                SessionManager.SaveSession();
                if (m_baseTerrainStamper == null)
                {
                    CreateBaseTerrainStamper();
                }
                m_spawner.FitToTerrain(m_spawner.m_worldMapTerrain);
            }


            if (GaiaWater.DoesWaterExist())
            {
                m_spawner.m_showSeaLevelPlane = false;
            }

            GaiaLighting.SetPostProcessingStatus(false);



            m_dirtyColor = GaiaUtils.GetColorFromHTML("#FF666666");
            m_normalBGColor = GUI.backgroundColor;
            m_spawner.UpdateMinMaxHeight();
#if GAIA_PRO_PRESENT
            m_spawner.TerrainLoader.m_isSelected = true;
#endif

            ImageMask.RefreshSpawnRuleGUIDs();
            StartEditorUpdates();
            //Force a repaint to get the stamper visuailization running immediately
            SceneView.RepaintAll();
        }


        private void CreateMaskLists()
        {
            //Create the spawner mask list
            CreateSpawnerMaskList();

            //Create the individual spawn rule mask lists
            m_spawnRuleMaskListExpanded = new bool[m_spawner.m_settings.m_spawnerRules.Count];
            m_reorderableRuleMasksLists = new ReorderableList[m_spawner.m_settings.m_spawnerRules.Count];

            for (int i = 0; i < m_spawner.m_settings.m_spawnerRules.Count; i++)
            {
                m_reorderableRuleMasksLists[i] = CreateSpawnRuleMaskList(m_reorderableRuleMasksLists[i], m_spawner.m_settings.m_spawnerRules[i].m_imageMasks);
                m_spawnRuleMaskListExpanded[i] = true;
            }
        }

        void OnDisable()
        {
            GaiaLighting.SetPostProcessingStatus(true);
#if GAIA_PRO_PRESENT
            if (m_spawner != null)
            {
                m_spawner.TerrainLoader.m_isSelected = false;
                m_spawner.UpdateAutoLoadRange();
            }
#endif
        }

        void OnDestroy()
        {
            //check if we opened a stamp selection window from this spawner, and if yes, close it down
            var allWindows = Resources.FindObjectsOfTypeAll<GaiaStampSelectorEditorWindow>();
            for (int i = allWindows.Length - 1; i >= 0; i--)
            {
                //Check General Masks first

                foreach (ImageMask imageMask in m_spawner.m_settings.m_imageMasks)
                {
                    if (allWindows[i].m_editedImageMask == imageMask)
                    {
                        allWindows[i].Close();
                    }
                }

                //Then the rule specific masks

                foreach (SpawnRule spawnRule in m_spawner.m_settings.m_spawnerRules)
                {
                    foreach (ImageMask imageMask in spawnRule.m_imageMasks)
                    {
                        if (allWindows[i].m_editedImageMask == imageMask)
                        {
                            allWindows[i].Close();
                        }
                    }
                }
            }

            for (int i = 0; i < m_tempTextureList.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(m_tempTextureList[i]);
            }

        }

        #region SPAWN RULE MASK LIST MANAGEMENT

        /// <summary>
        /// Creates the reorderable collision mask list for collision masks in the spawn rules.
        /// </summary>
        public ReorderableList CreateSpawnRuleCollisionMaskList(ReorderableList list, CollisionMask[] collisionMasks)
        {
            list = new ReorderableList(collisionMasks, typeof(CollisionMask), true, true, true, true);
            list.elementHeightCallback = OnElementHeightCollisionMaskList;
            list.drawElementCallback = DrawSpawnRuleCollisionMaskElement;
            list.drawHeaderCallback = DrawSpawnRuleCollisionMaskListHeader;
            list.onAddCallback = OnAddSpawnRuleCollisionMaskListEntry;
            list.onRemoveCallback = OnRemoveSpawnRuleCollisionMaskMaskListEntry;
            return list;
        }

        private void OnRemoveSpawnRuleCollisionMaskMaskListEntry(ReorderableList list)
        {
            //find spawn rule index & mask index which are being edited, so we know who this list of collision masks belongs to
            int maskIndex = -99;
            int spawnRuleIndex = FindSpawnRuleIndexByReorderableCollisionMaskList(list, ref maskIndex);
            m_spawner.m_settings.m_spawnerRules[spawnRuleIndex].m_imageMasks[maskIndex].m_collisionMasks = CollisionMaskListEditor.OnRemoveMaskListEntry(m_spawner.m_settings.m_spawnerRules[spawnRuleIndex].m_imageMasks[maskIndex].m_collisionMasks, list.index);
            list.list = m_spawner.m_settings.m_spawnerRules[spawnRuleIndex].m_imageMasks[maskIndex].m_collisionMasks;
        }

        private void OnAddSpawnRuleCollisionMaskListEntry(ReorderableList list)
        {
            //find spawn rule index & mask index which are being edited, so we know who this list of collision masks belongs to
            int maskIndex = -99;
            int spawnRuleIndex = FindSpawnRuleIndexByReorderableCollisionMaskList(list, ref maskIndex);
            m_spawner.m_settings.m_spawnerRules[spawnRuleIndex].m_imageMasks[maskIndex].m_collisionMasks = CollisionMaskListEditor.OnAddMaskListEntry(m_spawner.m_settings.m_spawnerRules[spawnRuleIndex].m_imageMasks[maskIndex].m_collisionMasks);
            list.list = m_spawner.m_settings.m_spawnerRules[spawnRuleIndex].m_imageMasks[maskIndex].m_collisionMasks;
        }

        private void DrawSpawnRuleCollisionMaskListHeader(Rect rect)
        {
            m_spawner.m_settings.m_spawnerRules[m_spawnRuleIndexBeingDrawn].m_imageMasks[m_spawnRuleMaskIndexBeingDrawn].m_collisionMaskExpanded = CollisionMaskListEditor.DrawFilterListHeader(rect, m_spawner.m_settings.m_spawnerRules[m_spawnRuleIndexBeingDrawn].m_imageMasks[m_spawnRuleMaskIndexBeingDrawn].m_collisionMaskExpanded, m_spawner.m_settings.m_spawnerRules[m_spawnRuleIndexBeingDrawn].m_imageMasks[m_spawnRuleMaskIndexBeingDrawn].m_collisionMasks, m_editorUtils);
        }

        private void DrawSpawnRuleCollisionMaskElement(Rect rect, int index, bool isActive, bool isFocused)
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




        /// <summary>
        /// Creates the reorderable mask list for the individual spawn rules.
        /// </summary>
        private ReorderableList CreateSpawnRuleMaskList(ReorderableList list, ImageMask[] imageMasks)
        {
            list = new UnityEditorInternal.ReorderableList(imageMasks, typeof(ImageMask), true, true, true, true);
            list.elementHeightCallback = OnElementHeightSpawnRuleMaskList;
            list.drawElementCallback = DrawSpawnRuleMaskListElement;
            list.drawHeaderCallback = DrawSpawnRuleMaskListHeader;
            list.onAddCallback = OnAddSpawnRuleMaskListEntry;
            list.onRemoveCallback = OnRemoveSpawnRuleMaskListEntry;
            list.onReorderCallback = OnReorderSpawnRuleMaskList;

            foreach (ImageMask mask in imageMasks)
            {
                mask.m_reorderableCollisionMaskList = CreateSpawnRuleCollisionMaskList(mask.m_reorderableCollisionMaskList, mask.m_collisionMasks);
            }

            return list;
        }

        private float OnElementHeightSpawnRuleMaskList(int index)
        {
            if (m_maskListBeingDrawn.Length > 0)
            {
                return ImageMaskListEditor.OnElementHeight(index, m_maskListBeingDrawn[index]);
            }
            else
            {
                return 0f;
            }
        }

        private void OnReorderSpawnRuleMaskList(ReorderableList list)
        {
            m_spawner.m_spawnPreviewDirty = true;
            m_spawner.SetWorldBiomeMasksDirty();
            m_spawner.DrawSpawnerPreview();
        }

        private void OnRemoveSpawnRuleMaskListEntry(ReorderableList list)
        {
            //find spawn rule index that is being edited
            int spawnRuleIndex = FindSpawnRuleIndexByReorderableMaskList(list);
            m_spawner.m_settings.m_spawnerRules[spawnRuleIndex].m_imageMasks = ImageMaskListEditor.OnRemoveMaskListEntry(m_spawner.m_settings.m_spawnerRules[spawnRuleIndex].m_imageMasks, list.index);
            list.list = m_spawner.m_settings.m_spawnerRules[spawnRuleIndex].m_imageMasks;
        }

        private void OnAddSpawnRuleMaskListEntry(ReorderableList list)
        {
            //find spawn rule index that is being edited
            int spawnRuleIndex = FindSpawnRuleIndexByReorderableMaskList(list);
            m_spawner.m_settings.m_spawnerRules[spawnRuleIndex].m_imageMasks = ImageMaskListEditor.OnAddMaskListEntry(m_spawner.m_settings.m_spawnerRules[spawnRuleIndex].m_imageMasks, m_spawner.m_maxWorldHeight, m_spawner.m_minWorldHeight, SessionManager.m_session.m_seaLevel);
            //set up the new collision mask inside the newly added mask
            ImageMask[] maskList = m_spawner.m_settings.m_spawnerRules[spawnRuleIndex].m_imageMasks;
            ImageMask lastElement = maskList[maskList.Length - 1];
            lastElement.m_reorderableCollisionMaskList = CreateSpawnRuleCollisionMaskList(lastElement.m_reorderableCollisionMaskList, lastElement.m_collisionMasks);
            list.list = maskList;
        }


        private void DrawSpawnRuleMaskListHeader(Rect rect)
        {
            m_spawnRuleMaskListExpanded[m_spawnRuleIndexBeingDrawn] = ImageMaskListEditor.DrawFilterListHeader(rect, m_spawnRuleMaskListExpanded[m_spawnRuleIndexBeingDrawn], m_spawner.m_settings.m_spawnerRules[m_spawnRuleIndexBeingDrawn].m_imageMasks, m_editorUtils);
        }

        int FindSpawnRuleIndexByReorderableMaskList(ReorderableList maskList)
        {
            //find texture index that is being edited
            int spawnRuleIndex = -99;

            for (int i = 0; i < m_reorderableRuleMasksLists.Length; i++)
            {
                if (m_reorderableRuleMasksLists[i] == maskList)
                {
                    spawnRuleIndex = i;
                }
            }
            return spawnRuleIndex;
        }

        int FindSpawnRuleIndexByReorderableCollisionMaskList(ReorderableList collisionMaskList, ref int maskIndex)
        {
            //find texture index that is being edited
            int spawnRuleIndex = -99;

            for (int i = 0; i < m_reorderableRuleMasksLists.Length; i++)
            {
                for (int j = 0; j < m_reorderableRuleMasksLists[i].list.Count; j++)
                {
                    if (((ImageMask)m_reorderableRuleMasksLists[i].list[j]).m_reorderableCollisionMaskList == collisionMaskList)
                    {
                        maskIndex = j;
                        spawnRuleIndex = i;
                    }
                }
            }
            return spawnRuleIndex;
        }

        private void DrawSpawnRuleMaskListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (m_maskListBeingDrawn != null && m_maskListBeingDrawn.Length > index)
            {

                ImageMask copiedImageMask = SessionManager.m_copiedImageMask;

                m_spawnRuleMaskIndexBeingDrawn = index;
                //bool isCopiedMask = m_maskListBeingDrawn[index] != null && m_maskListBeingDrawn[index] == m_copiedImageMask;
                MaskListButtonCommand mlbc = ImageMaskListEditor.DrawMaskListElement(rect, index, m_maskListBeingDrawn, ref m_collisionMaskListBeingDrawn, m_editorUtils, Terrain.activeTerrain, false, copiedImageMask, m_spawnRulesHeader.normal.background, m_gaiaSettings, SessionManager);

                switch (mlbc)
                {
                    case MaskListButtonCommand.Delete:
                        foreach (ReorderableList rl in m_reorderableRuleMasksLists)
                        {
                            if (rl.list == m_maskListBeingDrawn)
                            {
                                rl.index = index;
                                OnRemoveSpawnRuleMaskListEntry(rl);
                            }
                        }

                        break;
                    case MaskListButtonCommand.Duplicate:
                        foreach (ReorderableList rl in m_reorderableRuleMasksLists)
                        {
                            if (rl.list == m_maskListBeingDrawn)
                            {

                                ImageMask newImageMask = ImageMask.Clone(m_maskListBeingDrawn[index]);
                                m_spawner.m_settings.m_spawnerRules[m_spawnRuleIndexBeingDrawn].m_imageMasks = GaiaUtils.InsertElementInArray(m_spawner.m_settings.m_spawnerRules[m_spawnRuleIndexBeingDrawn].m_imageMasks, newImageMask, index + 1);
                                rl.list = m_spawner.m_settings.m_spawnerRules[m_spawnRuleIndexBeingDrawn].m_imageMasks;
                                m_spawner.m_settings.m_spawnerRules[m_spawnRuleIndexBeingDrawn].m_imageMasks[index + 1].m_reorderableCollisionMaskList = CreateSpawnRuleCollisionMaskList(m_spawner.m_settings.m_spawnerRules[m_spawnRuleIndexBeingDrawn].m_imageMasks[index + 1].m_reorderableCollisionMaskList, m_spawner.m_settings.m_spawnerRules[m_spawnRuleIndexBeingDrawn].m_imageMasks[index + 1].m_collisionMasks);
                                serializedObject.ApplyModifiedProperties();
                            }
                        }

                        break;
                    case MaskListButtonCommand.Copy:
                        SessionManager.m_copiedImageMask = m_maskListBeingDrawn[index];
                        break;
                    case MaskListButtonCommand.Paste:
                        m_maskListBeingDrawn[index] = ImageMask.Clone(copiedImageMask);
                        //Rebuild collsion mask list with new content from the cloning
                        m_maskListBeingDrawn[index].m_reorderableCollisionMaskList = CreateSpawnRuleCollisionMaskList(m_maskListBeingDrawn[index].m_reorderableCollisionMaskList, m_maskListBeingDrawn[index].m_collisionMasks);
                        SessionManager.m_copiedImageMask = null;
                        break;

                }


            }
        }

        #endregion

        #region SPAWNER MASK LIST MANAGEMENT

        /// <summary>
        /// Creates the reorderable mask list for the spawner itself.
        /// </summary>
        private void CreateSpawnerMaskList()
        {
            m_reorderableSpawnerMaskList = new UnityEditorInternal.ReorderableList(m_spawner.m_settings.m_imageMasks, typeof(ImageMask), true, true, true, true);
            m_reorderableSpawnerMaskList.elementHeightCallback = OnElementHeightSpawnerMaskList;
            m_reorderableSpawnerMaskList.drawElementCallback = DrawSpawnerMaskListElement;
            m_reorderableSpawnerMaskList.drawHeaderCallback = DrawSpawnerMaskListHeader;
            m_reorderableSpawnerMaskList.onAddCallback = OnAddSpawnerMaskListEntry;
            m_reorderableSpawnerMaskList.onRemoveCallback = OnRemoveSpawnerMaskListEntry;
            m_reorderableSpawnerMaskList.onReorderCallback = OnReorderSpawnerMaskList;

            foreach (ImageMask mask in m_spawner.m_settings.m_imageMasks)
            {
                mask.m_reorderableCollisionMaskList = CreateSpawnerCollisionMaskList(mask.m_reorderableCollisionMaskList, mask.m_collisionMasks);
            }
        }

        private float OnElementHeightSpawnerMaskList(int index)
        {
            return ImageMaskListEditor.OnElementHeight(index, m_spawner.m_settings.m_imageMasks[index]);
        }

        private void OnReorderSpawnerMaskList(ReorderableList list)
        {
            m_spawner.m_spawnPreviewDirty = true;
            m_spawner.SetWorldBiomeMasksDirty();
            m_spawner.DrawSpawnerPreview();
        }

        private void OnRemoveSpawnerMaskListEntry(ReorderableList list)
        {
            m_spawner.m_settings.m_imageMasks = ImageMaskListEditor.OnRemoveMaskListEntry(m_spawner.m_settings.m_imageMasks, list.index);
            list.list = m_spawner.m_settings.m_imageMasks;
        }

        private void OnAddSpawnerMaskListEntry(ReorderableList list)
        {
            m_spawner.m_settings.m_imageMasks = ImageMaskListEditor.OnAddMaskListEntry(m_spawner.m_settings.m_imageMasks, m_spawner.m_maxWorldHeight, m_spawner.m_minWorldHeight, SessionManager.m_session.m_seaLevel);
            ImageMask lastElement = m_spawner.m_settings.m_imageMasks[m_spawner.m_settings.m_imageMasks.Length - 1];
            lastElement.m_reorderableCollisionMaskList = CreateSpawnerCollisionMaskList(lastElement.m_reorderableCollisionMaskList, lastElement.m_collisionMasks);
            list.list = m_spawner.m_settings.m_imageMasks;
        }

        private void DrawSpawnerMaskListHeader(Rect rect)
        {
            m_spawnerMaskListExpanded = ImageMaskListEditor.DrawFilterListHeader(rect, m_spawnerMaskListExpanded, m_spawner.m_settings.m_imageMasks, m_editorUtils);
        }

        private void DrawSpawnerMaskListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            ImageMask copiedImageMask = SessionManager.m_copiedImageMask;
            //bool isCopiedMask = m_spawner.m_settings.m_imageMasks[index] != null && m_spawner.m_settings.m_imageMasks[index] == m_copiedImageMask;
            MaskListButtonCommand mlbc = ImageMaskListEditor.DrawMaskListElement(rect, index, m_spawner.m_settings.m_imageMasks, ref m_collisionMaskListBeingDrawn, m_editorUtils, Terrain.activeTerrain, false, copiedImageMask, m_spawnRulesHeader.normal.background, m_gaiaSettings, SessionManager);
            switch (mlbc)
            {
                case MaskListButtonCommand.Delete:
                    m_reorderableSpawnerMaskList.index = index;
                    OnRemoveSpawnerMaskListEntry(m_reorderableSpawnerMaskList);
                    break;
                case MaskListButtonCommand.Duplicate:
                    ImageMask newImageMask = ImageMask.Clone(m_spawner.m_settings.m_imageMasks[index]);
                    m_spawner.m_settings.m_imageMasks = GaiaUtils.InsertElementInArray(m_spawner.m_settings.m_imageMasks, newImageMask, index + 1);
                    m_reorderableSpawnerMaskList.list = m_spawner.m_settings.m_imageMasks;
                    m_spawner.m_settings.m_imageMasks[index + 1].m_reorderableCollisionMaskList = CreateSpawnerCollisionMaskList(m_spawner.m_settings.m_imageMasks[index + 1].m_reorderableCollisionMaskList, m_spawner.m_settings.m_imageMasks[index + 1].m_collisionMasks);
                    serializedObject.ApplyModifiedProperties();

                    break;
                case MaskListButtonCommand.Copy:
                    SessionManager.m_copiedImageMask = m_spawner.m_settings.m_imageMasks[index];
                    break;
                case MaskListButtonCommand.Paste:
                    m_spawner.m_settings.m_imageMasks[index] = ImageMask.Clone(copiedImageMask);
                    //Rebuild collsion mask list with new content from the cloning
                    m_spawner.m_settings.m_imageMasks[index].m_reorderableCollisionMaskList = CreateSpawnerCollisionMaskList(m_spawner.m_settings.m_imageMasks[index].m_reorderableCollisionMaskList, m_spawner.m_settings.m_imageMasks[index].m_collisionMasks);
                    SessionManager.m_copiedImageMask = null;
                    break;

            }
        }



        /// <summary>
        /// Creates the reorderable collision mask list for collision masks in the spawner itself.
        /// </summary>
        public ReorderableList CreateSpawnerCollisionMaskList(ReorderableList list, CollisionMask[] collisionMasks)
        {
            list = new ReorderableList(collisionMasks, typeof(CollisionMask), true, true, true, true);
            list.elementHeightCallback = OnElementHeightCollisionMaskList;
            list.drawElementCallback = DrawSpawnerCollisionMaskElement;
            list.drawHeaderCallback = DrawSpawnerCollisionMaskListHeader;
            list.onAddCallback = OnAddSpawnerCollisionMaskListEntry;
            list.onRemoveCallback = OnRemoveSpawnerCollisionMaskMaskListEntry;
            return list;
        }

        private void OnRemoveSpawnerCollisionMaskMaskListEntry(ReorderableList list)
        {
            //look up the collision mask in the spawner's mask list
            foreach (ImageMask imagemask in m_spawner.m_settings.m_imageMasks)
            {
                if (imagemask.m_reorderableCollisionMaskList == list)
                {
                    imagemask.m_collisionMasks = CollisionMaskListEditor.OnRemoveMaskListEntry(imagemask.m_collisionMasks, list.index);
                    list.list = imagemask.m_collisionMasks;
                    return;
                }
            }
        }

        private void OnAddSpawnerCollisionMaskListEntry(ReorderableList list)
        {
            //look up the collision mask in the spawner's mask list
            foreach (ImageMask imagemask in m_spawner.m_settings.m_imageMasks)
            {
                if (imagemask.m_reorderableCollisionMaskList == list)
                {
                    imagemask.m_collisionMasks = CollisionMaskListEditor.OnAddMaskListEntry(imagemask.m_collisionMasks);
                    list.list = imagemask.m_collisionMasks;
                    return;
                }
            }
        }

        private void DrawSpawnerCollisionMaskListHeader(Rect rect)
        {
            foreach (ImageMask imagemask in m_spawner.m_settings.m_imageMasks)
            {
                if (imagemask.m_collisionMasks == m_collisionMaskListBeingDrawn)
                {
                    imagemask.m_collisionMaskExpanded = CollisionMaskListEditor.DrawFilterListHeader(rect, imagemask.m_collisionMaskExpanded, imagemask.m_collisionMasks, m_editorUtils);
                }
            }
        }

        private void DrawSpawnerCollisionMaskElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (m_collisionMaskListBeingDrawn != null && m_collisionMaskListBeingDrawn.Length > index && m_collisionMaskListBeingDrawn[index] != null)
            {
                CollisionMaskListEditor.DrawMaskListElement(rect, index, m_collisionMaskListBeingDrawn[index], m_editorUtils, Terrain.activeTerrain, GaiaConstants.FeatureOperation.Contrast);
            }
        }

        #endregion

        /// <summary>
        /// Start editor updates
        /// </summary>
        public void StartEditorUpdates()
        {
            if (!m_startedUpdates)
            {
                m_startedUpdates = true;
                EditorApplication.update += EditorUpdate;
            }
        }

        /// <summary>
        /// Stop editor updates
        /// </summary>
        public void StopEditorUpdates()
        {
            if (m_startedUpdates)
            {
                m_startedUpdates = false;
                EditorApplication.update -= EditorUpdate;
            }
        }

        /// <summary>
        /// This is used just to force the editor to repaint itself
        /// </summary>
        void EditorUpdate()
        {
            if (m_spawner != null)
            {
                if (m_spawner.m_updateCoroutine != null)
                {
                    if ((DateTime.Now - m_timeSinceLastUpdate).TotalMilliseconds > 500)
                    {
                        //Debug.Log("Active repainting spawner " + m_spawner.gameObject.name);
                        m_timeSinceLastUpdate = DateTime.Now;
                        Repaint();
                    }
                }
                else
                {
                    if ((DateTime.Now - m_timeSinceLastUpdate).TotalSeconds > 5)
                    {
                        //Debug.Log("Inactive repainting spawner " + m_spawner.gameObject.name);
                        m_timeSinceLastUpdate = DateTime.Now;
                        Repaint();
                    }
                }
            }
        }

        public void OnSceneGUI()
        {
            // dont render preview if this isnt a repaint. losing performance if we do
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            //reset rotation, rotation for the spawner is currently not supported because it causes too many issues
            m_spawner.transform.rotation = new Quaternion();

            //set the preview dirty if the transform changed so it will be redrawn correctly in the new location
            //the lastXPos & lastZPos variables are a workaround, because transform.hasChanged was triggering too often
            if (m_lastXPos != m_spawner.transform.position.x || m_lastZPos != m_spawner.transform.position.z)
            {
                m_lastXPos = m_spawner.transform.position.x;
                m_lastZPos = m_spawner.transform.position.z;
                m_spawner.m_spawnPreviewDirty = true;
                m_spawner.SetWorldBiomeMasksDirty();
            }

            //World Biome Mask cubes
            //if (m_spawner.m_settings.m_isWorldmapSpawner && m_spawner.m_settings.m_worldmapSpawnerType == WorldmapSpawnerType.WorldBiomeMasks)
            //{
            //    foreach (SpawnRule sr in m_spawner.m_settings.m_spawnerRules)
            //    {
            //        Handles.color = sr.m_visualisationColor;

            //        Vector3 center = m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes[sr.m_resourceIdx].m_center - SessionManager.GetOrigin();
            //        float size = (float)m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes[sr.m_resourceIdx].m_range * 2f;
            //        Handles.DrawWireCube(center, new Vector3(size, size, size));
            //        Handles.DrawWireDisc(center, Vector3.up, 20f);
            //        Handles.Label(center + new Vector3(0f,100f,0f), m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes[sr.m_resourceIdx].m_name);
            //    }
            //}

            m_spawner.DrawSpawnerPreview();

            if (m_spawner.m_baseTerrainSettings.m_drawPreview && m_spawner.m_settings.m_isWorldmapSpawner)
            {
                BaseTerrainStamper.DrawStampPreview();
            }

        }

        /// <summary>
        /// Draw the UI
        /// </summary>
        public override void OnInspectorGUI()
        {

            if (m_spawner.m_ExportRunning)
            {
                GUI.enabled = false;
            }
            else
            {
                GUI.enabled = true;
            }


            if (m_spawRulesBorder == null || m_spawRulesBorder.normal.background == null)
            {
                m_spawRulesBorder = new GUIStyle(GUI.skin.box);
                if (EditorGUIUtility.isProSkin)
                {
                    m_spawRulesBorder.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML("383838"), GaiaUtils.GetColorFromHTML("000000"), new List<Texture2D>());
                }
                else
                {
                    m_spawRulesBorder.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML("cdced0"), GaiaUtils.GetColorFromHTML("000000"), new List<Texture2D>());
                }
            }


            if (m_spawnRulesHeader == null || m_singleSpawnRuleHeader == null || m_spawnRulesHeader.normal.background == null || m_singleSpawnRuleHeader.normal.background == null)
            {
                m_spawnRulesHeader = new GUIStyle();
                m_spawnRulesHeader.overflow = new RectOffset(2, 2, 2, 2);
                m_singleSpawnRuleHeader = new GUIStyle(m_spawnRulesHeader);

                // Setup colors for Unity Pro
                if (EditorGUIUtility.isProSkin)
                {
                    m_spawnRulesHeader.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML("2d2d2dff"), m_tempTextureList);
                    m_singleSpawnRuleHeader.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML("2d2d4cff"), m_tempTextureList);
                }
                // or Unity Personal
                else
                {
                    m_spawnRulesHeader.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML("a2a2a2ff"), m_tempTextureList);
                    m_singleSpawnRuleHeader.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML("a2a2c1ff"), m_tempTextureList);
                }
            }

            if (m_smallButtonStyle == null)
            {
                m_smallButtonStyle = new GUIStyle(GUI.skin.button);
                m_smallButtonStyle.padding = new RectOffset(2, 2, 2, 2);
                m_smallButtonStyle.margin = new RectOffset(5, 5, 2, 2);
            }

            m_spawner.m_settings.m_spawnRange = Mathf.Clamp(m_spawner.m_settings.m_spawnRange, 1f, m_spawner.GetMaxSpawnerRange());

            //Handle terrain auto-loading
            m_spawner.UpdateAutoLoadRange();



            //Reset Resource Preview
            m_drawResourcePreviewRuleId = -99;
            //Reset reorderable masks lists in case the spawner was reset etc.
            if (m_spawner.m_settings.m_spawnerRules.Count == 0 && m_reorderableRuleMasksLists.Count() > 0)
            {
                m_reorderableRuleMasksLists = new ReorderableList[0];
            }

            //Get our spawner
            m_spawner = (Spawner)target;

            //Init editor utils
            m_editorUtils.Initialize();
            serializedObject.Update();

            if (m_activatePreviewRequested && (m_activatePreviewTimeStamp + m_gaiaSettings.m_stamperAutoHidePreviewMilliseconds < GaiaUtils.GetUnixTimestamp()))
            {
                m_activatePreviewRequested = false;
                m_spawner.m_drawPreview = true;
                //force repaint
                EditorWindow view = EditorWindow.GetWindow<SceneView>();
                view.Repaint();
            }

            //Check if sea level changed
            if (m_spawner.m_seaLevel != SessionManager.GetSeaLevel())
            {
                //Dirty the preview to force it to be refreshed according to the new sea level
                m_spawner.m_spawnPreviewDirty = true;
                m_spawner.m_seaLevel = SessionManager.GetSeaLevel();
                m_spawner.SetWorldBiomeMasksDirty();
            }



            //Disable if spawning
            if (m_spawner.m_spawnProgress > 0f)
            {
                GUI.enabled = false;
            }


            EditorGUI.BeginChangeCheck();

            if (m_spawner.m_settings.m_isWorldmapSpawner)
            {
                m_spawner.m_createBaseTerrainUnfolded = DrawWorldDesignerPanel("CreateBaseTerrain", DrawBaseTerrainStamp, ref m_showStampBaseHelp, m_spawner.m_createBaseTerrainUnfolded);
                bool currentGUIState = GUI.enabled;
                if (!m_spawner.m_baseTerrainStamped)
                {
                    GUI.enabled = false;
                }
                DrawSpawnRulesPanel("SpawnStamps", DrawStampSpawnRules, m_spawner.m_baseTerrainStamped);
                m_spawner.m_exportTerrainUnfolded = DrawWorldDesignerPanel("ExportTerrain", DrawExportTerrain, ref m_showExportTerrainHelp, m_spawner.m_exportTerrainUnfolded);
                m_spawner.m_worldBiomeMasksUnfolded = DrawSpawnRulesPanel("DefineWorldBiomeMasks", DrawBiomeMaskSpawnRules, m_spawner.m_worldBiomeMasksUnfolded);
                GUI.enabled = currentGUIState;
            }
            else
            {
                //Regular Spawner
                m_editorUtils.Panel("SpawnSettings", DrawSpawnSettings, true);
                GUILayout.Space(10);
                DrawSpawnRulesPanel("SpawnRules", DrawRegularSpawnRules);
                GUILayout.Space(10);
                m_editorUtils.Panel("ClearSpawns", DrawClearControls, false);
            }
            //m_editorUtils.Panel("Statistics", DrawStatistics, false);
            //GUILayout.Space(10);


            //GUILayout.BeginVertical();
            //GUILayout.Space(20);
            //bool showGizmos = EditorGUILayout.Toggle(GetLabel("Show Gizmos"), m_spawner.m_showGizmos);
            //bool showStatistics = m_spawner.m_showStatistics = EditorGUILayout.Toggle(GetLabel("Show Statistics"), m_spawner.m_showStatistics);
            //bool showTerrainHelper = m_spawner.m_showTerrainHelper = EditorGUILayout.Toggle(GetLabel("Show Terrain Helper"), m_spawner.m_showTerrainHelper);
            //GUILayout.EndVertical();

            //Check for changes, make undo record, make changes and let editor know we are dirty
            if (EditorGUI.EndChangeCheck())
            {
                m_changesMadeSinceLastSave = true;
                Undo.RecordObject(m_spawner, "Made changes");
                m_spawner.m_spawnPreviewDirty = true;
                m_spawner.SetWorldBiomeMasksDirty();
                //m_spawner.m_showGizmos = showGizmos;
                //m_spawner.m_showStatistics = showStatistics;
                //m_spawner.m_showTerrainHelper = showTerrainHelper;


                if (m_spawner.m_imageMask != null)
                {
                    GaiaUtils.MakeTextureReadable(m_spawner.m_imageMask);
                    GaiaUtils.MakeTextureUncompressed(m_spawner.m_imageMask);
                }
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(m_spawner);
                EditorUtility.SetDirty(m_spawner.m_settings);


            }

            //m_editorUtils.Panel("ResourceManagement", DrawResourceManagement, false);
            //GUILayout.Space(10);

            m_editorUtils.Panel("Advanced", DrawAdvanced, false);
            if (!m_spawner.m_settings.m_isWorldmapSpawner)
            {
                m_editorUtils.Panel("SpawnControl", DrawSpawnControls, true);
            }
        }

        private void DrawAdvanced(bool helpEnabled)
        {
            if (m_changesMadeSinceLastSave)
            {
                GUI.backgroundColor = m_dirtyColor;
                m_editorUtils.Panel("SaveLoadChangesPending", DrawSaveAndLoad, (m_spawner.m_createdfromBiomePreset || m_spawner.m_createdFromGaiaManager) ? true : false);
            }
            else
            {
                m_editorUtils.Panel("SaveLoad", DrawSaveAndLoad, (m_spawner.m_createdfromBiomePreset || m_spawner.m_createdFromGaiaManager) ? true : false);
            }
            GUI.backgroundColor = m_normalBGColor;
            m_editorUtils.Panel("Appearance", DrawAppearance, false);
        }

        private bool DrawWorldDesignerPanel(string nameKey, Action<bool> contentMethod, ref bool showHelp, bool unfolded = true)
        {
            GUIContent panelLabel = m_editorUtils.GetContent(nameKey);

            //Panel Label
            GUIStyle panelLabelStyle = new GUIStyle(GUI.skin.label);
            panelLabelStyle.normal.textColor = GUI.skin.label.normal.textColor;
            panelLabelStyle.fontStyle = FontStyle.Bold;
            panelLabelStyle.normal.background = GUI.skin.label.normal.background;

            // Panel Frame
            GUIStyle panelFrameStyle = new GUIStyle(GUI.skin.box);
            panelFrameStyle.normal.textColor = GUI.skin.label.normal.textColor;
            panelFrameStyle.fontStyle = FontStyle.Bold;
            panelFrameStyle.alignment = TextAnchor.UpperLeft;

            // Panel
            GUIStyle panelStyle = new GUIStyle(GUI.skin.label);
            panelStyle.normal.textColor = GUI.skin.label.normal.textColor;
            panelStyle.alignment = TextAnchor.UpperLeft;


            bool helpActive = m_rulePanelHelpActive;

            GUILayout.BeginVertical(m_spawRulesBorder);
            {
                GUILayout.BeginHorizontal(m_spawnRulesHeader);
                {
                    unfolded = GUILayout.Toggle(unfolded, unfolded ? "-" : "+", panelLabelStyle, GUILayout.MinWidth(14));
                    GUILayout.Space(-5f);
                    unfolded = GUILayout.Toggle(unfolded, panelLabel, panelLabelStyle);
                    GUILayout.FlexibleSpace();
                    m_editorUtils.HelpToggle(ref showHelp);

                }
                GUILayout.EndHorizontal();

                if (helpActive)
                {
                    GUILayout.Space(2f);
                    m_editorUtils.InlineHelp(nameKey, helpActive);
                }

                if (unfolded)
                {
                    GUILayout.BeginVertical(panelStyle);
                    {
                        contentMethod(showHelp);
                    }
                    GUILayout.EndVertical();
                }
            }
            if (unfolded)
            {
                //Footer - repeat the buttons so they are easily accessible anywhere
                GUILayout.BeginHorizontal(m_spawnRulesHeader);
                {

                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            m_rulePanelHelpActive = helpActive;
            return unfolded;
        }

        void DrawBaseTerrainStamp(bool showHelp)
        {
            BaseTerrainSettings bts = m_spawner.m_baseTerrainSettings;
            EditorGUI.BeginChangeCheck();
            //bts.m_drawPreview = m_editorUtils.Toggle("BaseTerrainShowPreview", bts.m_drawPreview, showHelp);
            bts.m_borderStyle = (GeneratorBorderStyle)m_editorUtils.EnumPopup("BaseTerrainBorderStyle", bts.m_borderStyle, showHelp);
            DrawSeaLevelSlider(showHelp);
            bts.m_heightScale = m_editorUtils.Slider("BaseTerrainHeightScale", bts.m_heightScale, 0f, 100f, showHelp);
            bts.m_baseLevel = m_editorUtils.Slider("BaseTerrainBaseHeight", bts.m_baseLevel, 0f, 1000f, showHelp);
            bts.m_smoothness = m_editorUtils.Slider("BaseTerrainSmoothness", bts.m_smoothness, 0f, 4f, showHelp);

            EditorGUI.indentLevel++;
            bts.m_shapeFoldedOut = m_editorUtils.Foldout(bts.m_shapeFoldedOut, "BaseTerrainHeadingTerrainShape");
            if (bts.m_shapeFoldedOut)
            {
                bts.m_shapeNoiseStyle = (NoiseTypeName)m_editorUtils.EnumPopup("BaseTerrainNoiseStyle", bts.m_shapeNoiseStyle, showHelp);
                bts.m_shapeStrength = m_editorUtils.Slider("BaseTerrainStrength", bts.m_shapeStrength, 0f, 1f, showHelp);
                bts.m_shapeSteepness = m_editorUtils.Slider("BaseTerrainCutoff", bts.m_shapeSteepness, 0f, 1f, showHelp);
                bts.m_shapeOffset = m_editorUtils.Vector3Field("BaseTerrainOffset", bts.m_shapeOffset, showHelp);
                bts.m_shapeSize = m_editorUtils.Slider("BaseTerrainSize", bts.m_shapeSize, 0f, 10f, showHelp);
                bts.m_shapeGranularity = m_editorUtils.Slider("BaseTerrainGranularity", bts.m_shapeGranularity, 0f, 4f, showHelp);
            }
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel++;
            bts.m_mountainsFoldedOut = m_editorUtils.Foldout(bts.m_mountainsFoldedOut, "BaseTerrainHeadingMountains");
            if (bts.m_mountainsFoldedOut)
            {
                bts.m_mountainNoiseStyle = (NoiseTypeName)m_editorUtils.EnumPopup("BaseTerrainNoiseStyle", bts.m_mountainNoiseStyle, showHelp);
                bts.m_mountainStrength = m_editorUtils.Slider("BaseTerrainStrength", bts.m_mountainStrength, 0f, 1f, showHelp);
                bts.m_mountainSteepness = m_editorUtils.Slider("BaseTerrainCutoff", bts.m_mountainSteepness, 0f, 1f, showHelp);
                bts.m_mountainOffset = m_editorUtils.Vector3Field("BaseTerrainOffset", bts.m_mountainOffset, showHelp);
                bts.m_mountainSize = m_editorUtils.Slider("BaseTerrainSize", bts.m_mountainSize, 0f, 10f, showHelp);
                bts.m_mountainGranularity = m_editorUtils.Slider("BaseTerrainGranularity", bts.m_mountainGranularity, 0f, 4f, showHelp);
            }
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel++;
            bts.m_valleysFoldedOut = m_editorUtils.Foldout(bts.m_valleysFoldedOut, "BaseTerrainHeadingValleys");
            if (bts.m_valleysFoldedOut)
            {
                bts.m_valleyNoiseStyle = (NoiseTypeName)m_editorUtils.EnumPopup("BaseTerrainNoiseStyle", bts.m_valleyNoiseStyle, showHelp);
                bts.m_valleyStrength = m_editorUtils.Slider("BaseTerrainStrength", bts.m_valleyStrength, 0f, 1f, showHelp);
                bts.m_valleySteepness = m_editorUtils.Slider("BaseTerrainCutoff", bts.m_valleySteepness, 0f, 1f, showHelp);
                bts.m_valleyOffset = m_editorUtils.Vector3Field("BaseTerrainOffset", bts.m_valleyOffset, showHelp);
                bts.m_valleySize = m_editorUtils.Slider("BaseTerrainSize", bts.m_valleySize, 0f, 10f, showHelp);
                bts.m_valleyGranularity = m_editorUtils.Slider("BaseTerrainGranularity", bts.m_valleyGranularity, 0f, 4f, showHelp);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.BeginHorizontal();
            {
                Color currentColor = GUI.backgroundColor;
                if (bts.m_drawPreview)
                {
                    GUI.backgroundColor = Color.red;
                }

                if (m_editorUtils.Button("BaseTerrainShowPreview"))
                {
                    bts.m_drawPreview = !bts.m_drawPreview;
                    m_baseTerrainStamper.m_drawPreview = bts.m_drawPreview;
                }
                GUI.backgroundColor = currentColor;
                //m_spawner.m_baseTerrainSettings.m_terrainShapeStyle =
                if (EditorGUI.EndChangeCheck())
                {
                    if (m_spawner.m_baseTerrainSettings.m_drawPreview)
                    {
                        UpdateBaseTerrainStamper(bts);
                    }

                }



                if (m_editorUtils.Button("BaseTerrainButtonStamp"))
                {
                    BaseTerrainStamper.Stamp();
                    m_spawner.m_baseTerrainStamped = true;
                    m_spawner.m_createBaseTerrainUnfolded = false;
                    m_spawner.m_spawnStampsPanelUnfolded = true;
                    m_spawner.m_baseTerrainSettings.m_drawPreview = false;
                    m_baseTerrainStamper.m_drawPreview = false;
                    m_spawner.UpdateMinMaxHeight();
                    serializedObject.ApplyModifiedProperties();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (m_editorUtils.Button("BaseTerrainButtonFlatten"))
            {
                GaiaSessionManager.FlattenTerrain(new List<string> { TerrainHelper.GetWorldMapTerrain().name }, true);
            }
        }


        private void UpdateBaseTerrainStamper(BaseTerrainSettings bts)
        {
            BaseTerrainStamper.transform.position = new Vector3(BaseTerrainStamper.transform.position.x, bts.m_baseLevel, BaseTerrainStamper.transform.position.z);
            //Setup the existing masks according to the parameters
            BaseTerrainStamper.transform.localScale = new Vector3(BaseTerrainStamper.transform.localScale.x, bts.m_heightScale, BaseTerrainStamper.transform.localScale.z);
            //Terrain shape noise mask

            ApplyNoiseSettingsToImageMask(BaseTerrainStamper.m_settings.m_imageMasks[0], bts.m_shapeNoiseStyle.ToString(), bts.m_shapeSize, bts.m_shapeStrength, bts.m_shapeSteepness, bts.m_shapeOffset, bts.m_shapeGranularity);
            ApplyNoiseSettingsToImageMask(BaseTerrainStamper.m_settings.m_imageMasks[1], bts.m_mountainNoiseStyle.ToString(), bts.m_mountainSize, bts.m_mountainStrength, bts.m_mountainSteepness, bts.m_mountainOffset, bts.m_mountainGranularity);
            ApplyNoiseSettingsToImageMask(BaseTerrainStamper.m_settings.m_imageMasks[2], bts.m_valleyNoiseStyle.ToString(), bts.m_valleySize, bts.m_valleyStrength, bts.m_valleySteepness, bts.m_valleyOffset, bts.m_valleyGranularity);

            //Mountain mask
            BaseTerrainStamper.m_settings.m_imageMasks[1].m_blendMode = ImageMaskBlendMode.Add;

            //Valley mask
            BaseTerrainStamper.m_settings.m_imageMasks[2].m_blendMode = ImageMaskBlendMode.Subtract;


            //Set up the distance mask according to border style
            ImageMask distanceMask = m_baseTerrainStamper.m_settings.m_imageMasks[3];
            if (bts.m_borderStyle == GeneratorBorderStyle.None)
            {
                distanceMask.m_active = false;
                distanceMask.m_strengthTransformCurve = ImageMask.NewAnimCurveStraightUpwards(1f);
            }
            else
            {
                distanceMask.m_active = true;
                if (bts.m_borderStyle == GeneratorBorderStyle.Water)
                {
                    distanceMask.m_distanceMaskCurve = ImageMask.NewAnimCurveWaterBorder();
                    distanceMask.m_strengthTransformCurve = ImageMask.NewAnimCurveStraightUpwards(1f);
                }
                else
                {
                    distanceMask.m_distanceMaskCurve = ImageMask.NewAnimCurveMountainBorderDistance();
                    distanceMask.m_strengthTransformCurve = ImageMask.NewAnimCurveMountainBorderStrength();
                }
            }

            ImageMask smoothnessMask = BaseTerrainStamper.m_settings.m_imageMasks[4];
            smoothnessMask.m_smoothBlurRadius = bts.m_smoothness;

            //m_baseTerrainStamper.m_settings.m_imageMasks[0].m_noiseSettings.domainSettings.noiseTypeName = m_spawner.m_baseTerrainSettings.m_shapeNoiseStyle.ToString();
            BaseTerrainStamper.m_stampDirty = true;
        }

        private void CreateBaseTerrainStamper()
        {
            GameObject stamperGO = WorldMap.GetOrCreateWorldMapStamper();
            if (stamperGO != null)
            {
                m_baseTerrainStamper = stamperGO.GetComponent<Stamper>();
                m_baseTerrainStamper.FitToTerrain(TerrainHelper.GetWorldMapTerrain());
                m_baseTerrainStamper.SetBaseTerrainStandardMasks();
                UpdateBaseTerrainStamper(m_spawner.m_baseTerrainSettings);
            }
        }

        private void ApplyNoiseSettingsToImageMask(ImageMask imageMask, string noiseStyle, float size, float strength, float cutoff, Vector3 offset, float granularity)
        {
            imageMask.m_gaiaNoiseSettings.m_noiseTypeName = noiseStyle;
            imageMask.m_gaiaNoiseSettings.m_scale = new Vector3(10f - size, 1f, 10f - size);
            imageMask.m_gaiaNoiseSettings.m_translation = offset;
            ImageMaskListEditor.InitNoiseMask(imageMask);
            IFractalType fractalType = NoiseLib.GetFractalTypeInstance(imageMask.noiseSettingsGUI.fractalTypeName.stringValue);
            // deserialize string
            FbmFractalInput fbm = (FbmFractalInput)fractalType.FromSerializedString(imageMask.m_noiseSettings.domainSettings.fractalTypeParams);
            fbm.amplitude = strength;
            fbm.lacunarity = granularity;
            imageMask.m_noiseSettings.transformSettings.translation = offset;
            imageMask.m_noiseSettings.domainSettings.fractalTypeParams = fractalType.ToSerializedString(fbm);
            imageMask.m_noiseSettings.transformSettings.scale = new Vector3(10f - size, 1f, 10f - size);
            imageMask.m_noiseSettings.domainSettings.noiseTypeName = noiseStyle;
            imageMask.noiseSettingsGUI.noiseTypeName.stringValue = noiseStyle;
            imageMask.m_strengthTransformCurve = ImageMask.NewAnimCurveDefaultNoise((1f - cutoff) / 2f);
        }

        private void DrawExportTerrain(bool showHelp)
        {
            if (m_spawner.HasWorldSizeChanged())
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("ExportTerrainSizeSettingsChanged"), MessageType.Error);
            }

            //if (m_spawner.HasHeightmapResolutionChanged())
            //{
            //    EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("ExportTerrainResolutionSettingsChanged"), MessageType.Warning);
            //}

            bool currentGUIState = GUI.enabled;
            bool terrainExists = true;
            BoundsDouble bounds = new BoundsDouble();
            TerrainHelper.GetTerrainBounds(ref bounds);
            if (bounds.size.x <= 0)
            {
                terrainExists = false;
            }
            if (terrainExists)
            {
                m_spawner.m_useExistingTerrainForWorldMapExport = m_editorUtils.Toggle("ExportTerrainUseExistingTerrain", m_spawner.m_useExistingTerrainForWorldMapExport, showHelp);
            }
            else
            {
                m_spawner.m_useExistingTerrainForWorldMapExport = false;
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("ExportTerrainNewTerrainMessage"), MessageType.Info);
            }

            if (!m_spawner.m_useExistingTerrainForWorldMapExport)
            {
                m_editorUtils.Heading("ExportTerrainWorldCreationSettings");
                WorldCreationSettings wcs = m_spawner.m_worldCreationSettings;
                wcs.m_targeSizePreset = (GaiaConstants.EnvironmentSizePreset)m_editorUtils.EnumPopup("ExportTerrainWorldSize", wcs.m_targeSizePreset, showHelp);

                //Set the world tile size if the user chose one of the presets
                switch (wcs.m_targeSizePreset)
                {
                    case GaiaConstants.EnvironmentSizePreset.Tiny:
                        m_spawner.m_worldTileSize = GaiaConstants.EnvironmentSize.Is256MetersSq;
                        break;
                    case GaiaConstants.EnvironmentSizePreset.Small:
                        m_spawner.m_worldTileSize = GaiaConstants.EnvironmentSize.Is512MetersSq;
                        break;
                    case GaiaConstants.EnvironmentSizePreset.Medium:
                        m_spawner.m_worldTileSize = GaiaConstants.EnvironmentSize.Is1024MetersSq;
                        break;
                    case GaiaConstants.EnvironmentSizePreset.Large:
                        m_spawner.m_worldTileSize = GaiaConstants.EnvironmentSize.Is2048MetersSq;
                        break;
                    case GaiaConstants.EnvironmentSizePreset.XLarge:
                        m_spawner.m_worldTileSize = GaiaConstants.EnvironmentSize.Is4096MetersSq;
                        break;
                }

                if (wcs.m_targeSizePreset != EnvironmentSizePreset.Custom)
                {
                    wcs.m_xTiles = 1;
                    wcs.m_zTiles = 1;
                    wcs.m_tileSize = GaiaUtils.EnvironmentSizeToInt(m_spawner.m_worldTileSize);
                    wcs.m_createInScene = false;
                    wcs.m_autoUnloadScenes = false;
                    wcs.m_applyFloatingPointFix = false;
                }

                EditorGUI.indentLevel += 2;
                m_spawner.m_worldSizeAdvancedUnfolded = m_editorUtils.Foldout("ExportTerrainAdvanced", m_spawner.m_worldSizeAdvancedUnfolded, showHelp);
                if (m_spawner.m_worldSizeAdvancedUnfolded || wcs.m_targeSizePreset == EnvironmentSizePreset.Custom)
                {
                    m_spawner.m_worldSizeAdvancedUnfolded = true;
                    EditorGUI.BeginChangeCheck();
                    wcs.m_xTiles = m_editorUtils.IntField("ExportTerrainXTiles", wcs.m_xTiles, showHelp);
                    wcs.m_zTiles = m_editorUtils.IntField("ExportTerrainZTiles", wcs.m_zTiles, showHelp);
                    if (m_spawner.m_worldCreationSettings.m_xTiles != m_spawner.m_worldCreationSettings.m_zTiles)
                    {
                        EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("ExportTerrainNotSquareWarning"), MessageType.Warning);
                    }
                    m_spawner.m_worldTileSize = (GaiaConstants.EnvironmentSize)m_editorUtils.EnumPopup("ExportTerrainTileSize", m_spawner.m_worldTileSize, showHelp);
                    wcs.m_tileSize = GaiaUtils.EnvironmentSizeToInt(m_spawner.m_worldTileSize);
#if !GAIA_PRO_PRESENT
                    GUI.enabled = false;
#endif
                    wcs.m_createInScene = m_editorUtils.Toggle("ExportTerrainCreateInScene", wcs.m_createInScene, showHelp);
                    wcs.m_autoUnloadScenes = m_editorUtils.Toggle("ExportTerrainUnloadScenes", wcs.m_autoUnloadScenes, showHelp);
                    wcs.m_applyFloatingPointFix = m_editorUtils.Toggle("ExportTerrainFloatingPointFix", wcs.m_applyFloatingPointFix, showHelp);
#if !GAIA_PRO_PRESENT
                    wcs.m_createInScene = false;
                    wcs.m_autoUnloadScenes = false;
                    wcs.m_applyFloatingPointFix = false;
                    GUI.enabled = currentGUIState;
#endif
                    if (EditorGUI.EndChangeCheck())
                    {
                        wcs.m_targeSizePreset = EnvironmentSizePreset.Custom;
                    }
                }

                EditorGUI.indentLevel -= 2;
                wcs.m_qualityPreset = (EnvironmentTarget)m_editorUtils.EnumPopup("ExportTerrainQuality", wcs.m_qualityPreset, showHelp);
                switch (wcs.m_qualityPreset)
                {
                    case EnvironmentTarget.UltraLight:
                        m_gaiaSettings.m_currentDefaults = m_gaiaSettings.m_ultraLightDefaults;
                        m_gaiaSettings.m_currentWaterPrefabName = m_gaiaSettings.m_waterMobilePrefabName;
                        break;
                    case EnvironmentTarget.MobileAndVR:
                        m_gaiaSettings.m_currentDefaults = m_gaiaSettings.m_mobileDefaults;
                        m_gaiaSettings.m_currentWaterPrefabName = m_gaiaSettings.m_waterMobilePrefabName;
                        break;
                    case EnvironmentTarget.Desktop:
                        m_gaiaSettings.m_currentDefaults = m_gaiaSettings.m_desktopDefaults;
                        m_gaiaSettings.m_currentWaterPrefabName = m_gaiaSettings.m_waterPrefabName;
                        break;
                    case EnvironmentTarget.PowerfulDesktop:
                        m_gaiaSettings.m_currentDefaults = m_gaiaSettings.m_powerDesktopDefaults;
                        m_gaiaSettings.m_currentWaterPrefabName = m_gaiaSettings.m_waterPrefabName;
                        break;
                }
                wcs.m_gaiaDefaults = m_gaiaSettings.m_currentDefaults;
                EditorGUI.indentLevel += 2;

                m_gaiaSettings.m_currentDefaults.m_terrainSize = wcs.m_tileSize;

                GaiaUtils.SetSettingsForEnvironment(m_gaiaSettings, wcs.m_qualityPreset);

                m_spawner.m_qualityPresetAdvancedUnfolded = m_editorUtils.Foldout("ExportTerrainAdvanced", m_spawner.m_qualityPresetAdvancedUnfolded, showHelp);

                if (m_spawner.m_qualityPresetAdvancedUnfolded || wcs.m_qualityPreset == GaiaConstants.EnvironmentTarget.Custom)
                {
                    m_spawner.m_qualityPresetAdvancedUnfolded = true;
                    EditorGUI.BeginChangeCheck();
                    m_gaiaSettings.m_currentDefaults.m_heightmapResolution = (int)(HeightmapResolution)m_editorUtils.EnumPopup("ExportTerrainHeightmapResolution", (HeightmapResolution)m_gaiaSettings.m_currentDefaults.m_heightmapResolution, showHelp);
                    m_gaiaSettings.m_currentDefaults.m_controlTextureResolution = (int)(TerrainTextureResolution)m_editorUtils.EnumPopup("ExportTerrainControlTextureResolution", (TerrainTextureResolution)m_gaiaSettings.m_currentDefaults.m_controlTextureResolution, showHelp);
                    m_gaiaSettings.m_currentDefaults.m_baseMapSize = (int)(TerrainTextureResolution)m_editorUtils.EnumPopup("ExportTerrainBaseMapResolution", (TerrainTextureResolution)m_gaiaSettings.m_currentDefaults.m_baseMapSize, showHelp);
                    m_gaiaSettings.m_currentDefaults.m_detailResolutionPerPatch = m_editorUtils.IntField("ExportTerrainDetailResolutionPerPatch", m_gaiaSettings.m_currentDefaults.m_detailResolutionPerPatch, showHelp);
                    m_gaiaSettings.m_currentDefaults.m_detailResolution = m_editorUtils.IntField("ExportTerrainDetailResolution", m_gaiaSettings.m_currentDefaults.m_detailResolution, showHelp);
                    if (EditorGUI.EndChangeCheck())
                    {
                        wcs.m_qualityPreset = EnvironmentTarget.Custom;
                    }
                }
                EditorGUI.indentLevel -= 2;
            }

            EditorGUI.indentLevel++;
            GUILayout.Space(10);
            m_editorUtils.Heading("ExportTerrainStampOperationsHeading");
            m_spawner.m_stampOperationsFoldedOut = m_editorUtils.Foldout(m_spawner.m_stampOperationsFoldedOut, "ExportTerrainStampOperations", showHelp);

            if (m_spawner.m_stampOperationsFoldedOut)
            {
                int deletionIndex = -99;
                EditorGUI.indentLevel++;
                for (int i = 0; i < m_spawner.m_worldMapStamperSettings.Count; i++)
                {
                    StamperSettings stamperSettings = m_spawner.m_worldMapStamperSettings[i];

                    //Get the operation + the first image mask as a name
                    string name = stamperSettings.m_operation.ToString();
                    if (stamperSettings.m_imageMasks.Length > 0)
                    {
                        switch (stamperSettings.m_imageMasks[0].m_operation)
                        {
                            case ImageMaskOperation.ImageMask:
                                name += " " + stamperSettings.m_imageMasks[0].m_imageMaskTexture.name;
                                break;
                            default:
                                name += " " + stamperSettings.m_imageMasks[0].m_operation.ToString();
                                break;
                        }
                    }

                    stamperSettings.m_isFoldedOut = m_editorUtils.Foldout(stamperSettings.m_isFoldedOut, new GUIContent(name), showHelp);
                    GUILayout.BeginHorizontal();
                    if (stamperSettings.m_isFoldedOut)
                    {
                        if (m_editorUtils.Button("StampOperationSelect"))
                        {
                            WorldMapStampToken[] allTokens = Resources.FindObjectsOfTypeAll<WorldMapStampToken>();
                            WorldMapStampToken token = allTokens.Where(x => x.m_connectedStamperSettings == stamperSettings).First();
                            if (token != null)
                            {
                                Selection.activeGameObject = token.gameObject;
                            }
                        }
                        if (m_editorUtils.Button("StampOperationDelete"))
                        {
                            deletionIndex = i;
                        }
                    }

                    if (deletionIndex != -99)
                    {
                        WorldMapStampToken[] allTokens = Resources.FindObjectsOfTypeAll<WorldMapStampToken>();
                        WorldMapStampToken token = allTokens.Where(x => x.m_connectedStamperSettings == stamperSettings).First();
                        if (token != null)
                        {
                            DestroyImmediate(token.gameObject);
                        }
                        m_spawner.m_worldMapStamperSettings.RemoveAt(deletionIndex);
                    }
                    GUILayout.EndHorizontal();
                }
                if (m_editorUtils.ButtonAutoIndent("DeleteAllOperations"))
                {
                    Transform tokenTarget = m_spawner.m_worldMapTerrain.transform.Find(GaiaConstants.worldMapStampTokenSpawnTarget);
                    if (tokenTarget != null)
                    {
                        DestroyImmediate(tokenTarget.gameObject);
                    }
                    m_spawner.m_worldMapStamperSettings.Clear();
                }
            }
            EditorGUI.indentLevel--;

            if (m_spawner.m_ExportRunning)
            {
                //always allow cancel
                GUI.enabled = true;
                if (m_editorUtils.ButtonAutoIndent("CancelExport"))
                {
                    SessionManager.m_massStamperSettingsIndex = int.MaxValue;
                    m_spawner.m_ExportRunning = false;
                }
                GUI.enabled = currentGUIState;
            }
            else
            {
                if (m_spawner.m_worldCreationSettings.m_xTiles != m_spawner.m_worldCreationSettings.m_zTiles)
                {
                    EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("ExportTerrainNotSquareWarning"), MessageType.Warning);
                    GUI.enabled = false;
                }

                if (m_editorUtils.ButtonAutoIndent("ExportTerrainButtonExport"))
                {
                    try
                    {
                        if (!m_spawner.m_useExistingTerrainForWorldMapExport)
                        {
                            bool hasTerrains = false;
                            if (GaiaUtils.GetTerrainObject(false) != null || GaiaUtils.HasDynamicLoadedTerrains())
                            {
                                hasTerrains = true;
                            }

                            if (hasTerrains && !EditorUtility.DisplayDialog("Create new World", "WARNING: You are about to export the world map to a new terrain setup. This will delete all your existing terrains and build a new world from scratch. Continue?", "OK", "Cancel"))
                            {
                                return;
                            }
                            GaiaSessionManager.ClearWorld(true);
                            GaiaTerrainLoaderManager.Instance.SwitchToLocalMap();
                            GaiaTerrainLoaderManager.Instance.UnloadAll();
                            ProgressBar.Show(ProgressBarPriority.WorldCreation, "Export To World", "Creating Terrain Tiles for the Export...");

                            //Shift everything back to 0,0,0 if required
                            if (GaiaUtils.HasDynamicLoadedTerrains())
                            {
                                GaiaTerrainLoaderManager.Instance.SetOrigin(Vector3Double.zero);
                            }
                            //Create a new world according to the settings set by the user
                            WorldCreationSettings wcs = m_spawner.m_worldCreationSettings;
                            float worldsizeX = wcs.m_xTiles * wcs.m_tileSize;
                            wcs.m_tileHeight = m_spawner.m_worldMapTerrain.terrainData.size.y / (m_spawner.m_worldMapTerrain.terrainData.size.x / worldsizeX);
                            wcs.m_seaLevel = Mathf.RoundToInt(SessionManager.GetSeaLevel(true) * (m_spawner.m_worldMapTerrain.terrainData.size.x / worldsizeX));

                            //Subscribe for the world creation event so we can wait with the export until the world was created in the coroutine
                            GaiaSessionManager.OnWorldCreated -= DoExport;
                            GaiaSessionManager.OnWorldCreated += DoExport;
                            GaiaSessionManager.CreateWorld(m_spawner.m_worldCreationSettings, true);
                            m_spawner.m_ExportRunning = true;

                        }
                        else
                        {
                            if (!EditorUtility.DisplayDialog("Export World", "WARNING: You are about to export the world map and the spawned stamps over your existing terrain setup. This will replace the existing height information on all terrains in the scene and can greatly change the look of your terrains. Continue?", "OK", "Cancel"))
                            {
                                return;
                            }
                            //Shift everything back to 0,0,0 if required
                            if (GaiaUtils.HasDynamicLoadedTerrains())
                            {
                                GaiaTerrainLoaderManager.Instance.SetOrigin(Vector3Double.zero);
                            }
                            //We do not need to wait for terrain creation, but can do the export right away.
                            m_spawner.m_ExportRunning = true;
                            DoExport();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Error while exporting terrain from world map, Exception: " + ex.Message + " Stack Trace: " + ex.StackTrace);
                    }
                }
                GUI.enabled = currentGUIState;
            }
            GUILayout.Space(20);

            if (!GaiaUtils.HasTerrains())
            {
                GUI.enabled = false;
            }
            if (m_editorUtils.ButtonAutoIndent("WorldMapToTerrains"))
            {
                GameObject wmeGO = GaiaUtils.GetOrCreateWorldDesigner();
                if (wmeGO != null)
                {
                    wmeGO.GetComponent<WorldMap>().SyncWorldMapToLocalMap();
                }
                else
                {
                    Debug.LogError("Could not find World map editor in this scene!");
                }
            }
            if (m_editorUtils.ButtonAutoIndent("TerrainsToWorldMap"))
            {
                GameObject wmeGO = GaiaUtils.GetOrCreateWorldDesigner();
                if (wmeGO != null)
                {
                    wmeGO.GetComponent<WorldMap>().SyncLocalMapToWorldMap();
                }
                else
                {
                    Debug.LogError("Could not find World map editor in this scene!");
                }
            }
            GUI.enabled = currentGUIState;

        }

        private void DoExport()
        {
            try
            {

                WorldCreationSettings wcs = m_spawner.m_worldCreationSettings;
                float worldsizeX = wcs.m_xTiles * wcs.m_tileSize;
                //Calculate relative size and heightmap res values for the session
                GaiaTerrainLoaderManager.Instance.TerrainSceneStorage.m_worldMaprelativeSize = m_spawner.m_worldMapTerrain.terrainData.size.x / worldsizeX;

                GaiaDefaults defaults = (wcs.m_gaiaDefaults == null) ? m_gaiaSettings.m_currentDefaults : wcs.m_gaiaDefaults;

                GaiaTerrainLoaderManager.Instance.TerrainSceneStorage.m_worldMapRelativeHeightmapPixels = ((worldsizeX / wcs.m_tileSize) * defaults.m_heightmapResolution / defaults.m_heightmapResolution);

                //BoundsDouble newWorldBounds = new BoundsDouble();
                //TerrainHelper.GetTerrainBounds(ref newWorldBounds);
                //m_spawner.m_worldMapTerrain.transform.position = newWorldBounds.center + new Vector3Double(newWorldBounds.extents.x + m_spawner.m_worldCreationSettings.m_tileSize, -newWorldBounds.center.y, -m_spawner.m_worldMapTerrain.terrainData.size.z / 2f);
                m_spawner.FitToTerrain(m_spawner.m_worldMapTerrain);
                m_spawner.m_useExistingTerrainForWorldMapExport = true;


                GaiaTerrainLoaderManager.Instance.SwitchToLocalMap();
                GaiaTerrainLoaderManager.Instance.UnloadAll();
                ////m_spawner.StoreHeightmapResolution();
                ProgressBar.Show(ProgressBarPriority.WorldCreation, "Export To World", "Syncing World Map to World...");
                GameObject wmeGO = GaiaUtils.GetOrCreateWorldDesigner();
                wmeGO.GetComponent<WorldMap>().SyncWorldMapToLocalMap();
                ProgressBar.Show(ProgressBarPriority.WorldCreation, "Export To World", "Updating Stamps...");
                foreach (WorldMapStampToken token in Resources.FindObjectsOfTypeAll<WorldMapStampToken>())
                {
                    token.SyncLocationToStamperSettings();
                }

                if (m_spawner.m_worldMapStamperSettings.Count > 0)
                {
                    ProgressBar.Show(ProgressBarPriority.WorldCreation, "Export To World", "Stamping...");
                    GaiaSessionManager.OnMassStampingFinished -= OnMassStampingFinished;
                    GaiaSessionManager.OnMassStampingFinished += OnMassStampingFinished;
                    GaiaSessionManager.MassStamp(m_spawner.m_worldMapStamperSettings, true);
                    m_spawner.m_worldBiomeMasksUnfolded = true;
                }
                else
                {
                    GaiaTerrainLoaderManager.Instance.UpdateTerrainLoadState();
                    ProgressBar.Clear(ProgressBarPriority.WorldCreation);
                    m_spawner.m_ExportRunning = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error while exporting terrain from world map, Exception: " + ex.Message + " Stack Trace: " + ex.StackTrace);
                ProgressBar.Clear(ProgressBarPriority.WorldCreation);
            }
            finally
            {
                GaiaSessionManager.OnWorldCreated -= DoExport;
            }
        }

        private void OnMassStampingFinished()
        {
            m_spawner.m_ExportRunning = false;
            ProgressBar.Clear(ProgressBarPriority.WorldCreation);
            GaiaSessionManager.OnMassStampingFinished -= OnMassStampingFinished;
        }

        private void DrawAppearance(bool showHelp)
        {
            if (!m_spawner.m_settings.m_isWorldmapSpawner)
            {
#if GAIA_PRO_PRESENT
                m_spawner.m_loadTerrainMode = (LoadMode)m_editorUtils.EnumPopup("AutoLoadTerrains", m_spawner.m_loadTerrainMode, showHelp);
#endif
            }
            m_spawner.m_showSeaLevelPlane = m_editorUtils.Toggle("ShowSeaLevelPlane", m_spawner.m_showSeaLevelPlane, showHelp);
            m_spawner.m_showSeaLevelinStampPreview = m_editorUtils.Toggle("ShowSeaLevelSpawnerPreview", m_spawner.m_showSeaLevelinStampPreview, showHelp);
            //Color gizmoColour = EditorGUILayout.ColorField(GetLabel("Gizmo Colour"), m_stamper.m_gizmoColour);
            //alwaysShow = m_editorUtils.Toggle("AlwaysShowStamper", m_stamper.m_alwaysShow, showHelp);
            m_spawner.m_showBoundingBox = m_editorUtils.Toggle("ShowBoundingBox", m_spawner.m_showBoundingBox, showHelp);
            //showRulers = m_stamper.m_showRulers = m_editorUtils.Toggle("ShowRulers", m_stamper.m_showRulers, showHelp);
            //bool showTerrainHelper = m_stamper.m_showTerrainHelper = EditorGUILayout.Toggle(GetLabel("Show Terrain Helper"), m_stamper.m_showTerrainHelper);
        }

        private void DrawSeaLevelSlider(bool showHelp)
        {
            //m_editorUtils.LabelField("SeaLevel", new GUIContent(SessionManager.GetSeaLevel().ToString() + " m"), showHelp);

            float maxSeaLevel = 2000f;
            if (m_spawner.GetCurrentTerrain() != null)
            {
                maxSeaLevel = m_spawner.GetCurrentTerrain().terrainData.size.y;
            }
            else
            {
                maxSeaLevel = SessionManager.GetSeaLevel(m_spawner.m_settings.m_isWorldmapSpawner) + 500f;
            }

            float oldSeaLevel = SessionManager.GetSeaLevel(m_spawner.m_settings.m_isWorldmapSpawner);
            float newSeaLEvel = oldSeaLevel;
            newSeaLEvel = m_editorUtils.Slider("SeaLevel", newSeaLEvel, 0, maxSeaLevel, showHelp);
            if (newSeaLEvel != oldSeaLevel)
            {
                //update session with new sea level
                SessionManager.SetSeaLevel(newSeaLEvel, m_spawner.m_settings.m_isWorldmapSpawner);

                SceneView.RepaintAll();
            }
            //update associated base terrain stamper (if any)
            if (m_baseTerrainStamper != null)
            {
                m_baseTerrainStamper.m_seaLevel = newSeaLEvel;
            }
        }

        private void DrawGameObjectDropBox()
        {
            if (m_boxStyle == null || m_boxStyle.normal.background == null)
            {
                m_boxStyle = new GUIStyle(m_spawRulesBorder);
                m_boxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                m_boxStyle.fontStyle = FontStyle.Bold;
                m_boxStyle.alignment = TextAnchor.UpperLeft;
            }

            if (m_ResourceManagementMessage != "")
            {
                EditorGUILayout.HelpBox(m_ResourceManagementMessage, m_ResourceManagementMessageType, true);
            }

            Event evt = Event.current;
            Rect drop_area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(drop_area, m_editorUtils.GetTextValue("DropAreaText"), m_boxStyle);

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!drop_area.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        //Work out if we have prefab instances or prefab objects
                        bool havePrefabInstances = false;
                        foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                        {
                            PrefabAssetType pt = PrefabUtility.GetPrefabAssetType(dragged_object);

                            if (pt == PrefabAssetType.Regular || pt == PrefabAssetType.Model)
                            {
                                havePrefabInstances = true;
                                break;
                            }
                        }

                        if (havePrefabInstances)
                        {
                            List<GameObject> prototypes = new List<GameObject>();

                            foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                            {
                                PrefabAssetType pt = PrefabUtility.GetPrefabAssetType(dragged_object);

                                if (pt == PrefabAssetType.Regular || pt == PrefabAssetType.Model)
                                {
                                    prototypes.Add(dragged_object as GameObject);
                                }
                                else
                                {
                                    m_ResourceManagementMessageType = MessageType.Error;
                                    m_ResourceManagementMessage = m_editorUtils.GetTextValue("DropAreaOnlyPrefabInstances");
                                }
                            }

                            //Same them as a single entity
                            if (prototypes.Count > 0)
                            {
                                m_spawner.m_settings.m_resources.AddGameObject(prototypes);

                                //Create a new rule from the newly added gameobject
                                AddNewRule(SpawnerResourceType.GameObject, m_spawner.m_settings.m_resources.m_gameObjectPrototypes.Length - 1);

                            }
                        }
                        else
                        {
                            foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                            {
                                if (PrefabUtility.GetPrefabAssetType(dragged_object) == PrefabAssetType.Regular)
                                {
                                    m_spawner.m_settings.m_resources.AddGameObject(dragged_object as GameObject);
                                    AddNewRule(SpawnerResourceType.GameObject, m_spawner.m_settings.m_resources.m_gameObjectPrototypes.Length - 1);
                                }
                                else
                                {
                                    //Debug.LogWarning("You may only add prefabs or game objects attached to prefabs!");
                                    m_ResourceManagementMessageType = MessageType.Error;
                                    m_ResourceManagementMessage = m_editorUtils.GetTextValue("DropAreaOnlyPrefabsOrGameObjects");
                                }
                            }
                        }

                    }
                    break;

            }
        }

        private void DrawSpawnControls(bool helpEnabled)
        {
            GUILayout.Space(5);
            //Ragardless, re-enable the spawner controls
            GUI.enabled = true;

            //Display progress
            if (m_spawner.m_spawnProgress > 0f && m_spawner.m_spawnProgress < 1f)
            {
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(GetLabel("Cancel")))
                {
                    m_spawner.CancelSpawn();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                GUILayout.EndVertical();
                //ProgressBar(string.Format("Progress ({0:0.0}%)", m_spawner.m_spawnProgress * 100f), m_spawner.m_spawnProgress);
            }
            else
            {
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                //if (GUILayout.Button(GetLabel("Ground")))
                //{
                //    m_spawner.GroundToTerrain();
                //}
                if (GUILayout.Button(GetLabel("Fit To Terrain")))
                {
                    m_spawner.FitToTerrain();
                }
                if (Terrain.activeTerrains.Length > 1)
                {
                    if (GUILayout.Button(GetLabel("Fit To World")))
                    {
                        m_spawner.FitToAllTerrains();
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                //highlight the currently active preview.
                //if (m_spawner.m_drawPreview)
                //{
                //    GUI.backgroundColor = Color.red;
                //}
                //if (GUILayout.Button(GetLabel("Preview")))
                //{
                //    //as soon as the user interacts with the button, the user is in control, no need for auto activate anymore
                //    m_activatePreviewRequested = false;
                //    m_spawner.TogglePreview();
                //    //force repaint
                //    EditorWindow view = EditorWindow.GetWindow<SceneView>();
                //    view.Repaint();
                //}
                //if (GUILayout.Button(GetLabel("Preview")))
                //{
                //    foreach (ResourceProtoGameObject proto in m_spawner.m_settings.m_resources.m_gameObjectPrototypes)
                //    {
                //        foreach (ResourceProtoGameObjectInstance instance in proto.m_instances)
                //        {
                //            instance.m_minSpawnOffsetY = 0;
                //            instance.m_maxSpawnOffsetY = 0;
                //        }
                //    }

                //}


                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = m_normalBGColor;
                GUI.enabled = true;
                if (GUILayout.Button(GetLabel("Spawn Local")))
                {
                    Spawn(false);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button(GetLabel("Spawn World")))
                {
                    Spawn(true);
                    GUIUtility.ExitGUI();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                GUILayout.EndVertical();
            }

            //if (GUILayout.Button("Deactivate"))
            //{
            //    spawner.DeactivateAllInstances();
            //}

            GUI.enabled = true;
        }

        private void DrawClearControls(bool helpEnabled)
        {
            GUILayout.BeginVertical();
            m_editorUtils.Label("ClearSpawnsLabel");
            m_spawner.m_settings.m_clearSpawnsToggleTrees = m_editorUtils.Toggle("ClearSpawnsToggleTrees", m_spawner.m_settings.m_clearSpawnsToggleTrees, helpEnabled);
            m_spawner.m_settings.m_clearSpawnsToggleDetails = m_editorUtils.Toggle("ClearSpawnsToggleDetails", m_spawner.m_settings.m_clearSpawnsToggleDetails, helpEnabled);
            m_spawner.m_settings.m_clearSpawnsToggleGOs = m_editorUtils.Toggle("ClearSpawnsToggleGOs", m_spawner.m_settings.m_clearSpawnsToggleGOs, helpEnabled);
            m_spawner.m_settings.m_clearSpawnsToggleSpawnExtensions = m_editorUtils.Toggle("ClearSpawnsToggleSpawnExtensions", m_spawner.m_settings.m_clearSpawnsToggleSpawnExtensions, helpEnabled);
            m_spawner.m_settings.m_clearSpawnsFrom = (ClearSpawnFrom)m_editorUtils.EnumPopup("ClearSpawnsFrom", m_spawner.m_settings.m_clearSpawnsFrom, helpEnabled);
            m_spawner.m_settings.m_clearSpawnsFor = (ClearSpawnFor)m_editorUtils.EnumPopup("ClearSpawnsFor", m_spawner.m_settings.m_clearSpawnsFor, helpEnabled);


            GUILayout.BeginHorizontal();
            if (GUILayout.Button(m_editorUtils.GetContent("ClearSpawnsButton")))
            {
                ClearOperationSettings clearOperationSettings = ScriptableObject.CreateInstance<ClearOperationSettings>();
                clearOperationSettings.m_clearTrees = m_spawner.m_settings.m_clearSpawnsToggleTrees;
                clearOperationSettings.m_clearTerrainDetails = m_spawner.m_settings.m_clearSpawnsToggleDetails;
                clearOperationSettings.m_clearGameObjects = m_spawner.m_settings.m_clearSpawnsToggleGOs;
                clearOperationSettings.m_clearSpawnExtensions = m_spawner.m_settings.m_clearSpawnsToggleSpawnExtensions;
                clearOperationSettings.m_clearSpawnFrom = m_spawner.m_settings.m_clearSpawnsFrom;
                clearOperationSettings.m_clearSpawnFor = m_spawner.m_settings.m_clearSpawnsFor;

                GaiaSessionManager.ClearSpawns(clearOperationSettings, m_spawner.m_settings, true, m_spawner);


            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        //private void DrawStatistics(bool obj)
        //{

        //    m_editorUtils.LabelField("ActiveRules", GetLabel(m_spawner.m_settings.m_spawnerRules.Where(x => x.m_isActive == true).Count().ToString()));
        //    m_editorUtils.LabelField("InactiveRules", GetLabel(m_spawner.m_settings.m_spawnerRules.Where(x=>x.m_isActive!=true).Count().ToString()));
        //    //EditorGUILayout.LabelField(GetLabel("TOTAL Rules"), GetLabel(m_spawner.m_totalRuleCnt.ToString()));
        //    GUILayout.Space(8);
        //    m_editorUtils.LabelField("InstanceCount");
        //    foreach (SpawnRule rule in m_spawner.m_settings.m_spawnerRules)
        //    {
        //        EditorGUILayout.LabelField(new GUIContent(rule.m_resourceIdx.ToString() + " "+ rule.m_name), GetLabel(rule.m_spawnedInstances.ToString()));
        //    }
        //    //EditorGUILayout.LabelField(GetLabel("Active Instances"), GetLabel(m_spawner.m_activeInstanceCnt.ToString()));
        //    //EditorGUILayout.LabelField(GetLabel("Inactive Instances"), GetLabel(m_spawner.m_inactiveInstanceCnt.ToString()));
        //    //EditorGUILayout.LabelField(GetLabel("TOTAL Instances"), GetLabel(m_spawner.m_totalInstanceCnt.ToString()));
        //    //EditorGUILayout.LabelField(GetLabel("MAX INSTANCES"), GetLabel(m_spawner.m_maxInstanceCnt.ToString()));

        //}

        private bool DrawSpawnRulesPanel(string nameKey, Action<bool> contentMethod, bool unfolded = true)
        {
            GUIContent panelLabel = m_editorUtils.GetContent(nameKey);

            //Panel Label
            GUIStyle panelLabelStyle = new GUIStyle(GUI.skin.label);
            panelLabelStyle.normal.textColor = GUI.skin.label.normal.textColor;
            panelLabelStyle.fontStyle = FontStyle.Bold;
            panelLabelStyle.normal.background = GUI.skin.label.normal.background;

            // Panel Frame
            GUIStyle panelFrameStyle = new GUIStyle(GUI.skin.box);
            panelFrameStyle.normal.textColor = GUI.skin.label.normal.textColor;
            panelFrameStyle.fontStyle = FontStyle.Bold;
            panelFrameStyle.alignment = TextAnchor.UpperLeft;

            // Panel
            GUIStyle panelStyle = new GUIStyle(GUI.skin.label);
            panelStyle.normal.textColor = GUI.skin.label.normal.textColor;
            panelStyle.alignment = TextAnchor.UpperLeft;

            switch (contentMethod.Method.Name)
            {
                case "DrawBiomeMaskSpawnRules":
                    unfolded = m_spawner.m_settings.m_spawnerRules.Where(x => x.m_resourceType == SpawnerResourceType.WorldBiomeMask).Count() > 0 ? m_spawner.m_worldBiomeMasksUnfolded : true;
                    break;
                case "DrawStampSpawnRules":
                    unfolded = m_spawner.m_settings.m_spawnerRules.Where(x => x.m_resourceType == SpawnerResourceType.StampDistribution).Count() > 0 ? m_spawner.m_spawnStampsPanelUnfolded : true;
                    break;
                default:
                    //Always unfold if we have no rules yet, to show the prompt to add new stuff
                    if (!m_spawner.m_settings.m_isWorldmapSpawner)
                    {
                        unfolded = m_spawner.m_settings.m_spawnerRules.Count > 0 ? m_spawner.m_rulePanelUnfolded : true;
                    }
                    else
                    {
                        unfolded = m_spawner.m_rulePanelUnfolded;
                    }
                    break;
            }




            bool helpActive = m_rulePanelHelpActive;



            GUILayout.BeginVertical(m_spawRulesBorder);
            {
                GUILayout.BeginHorizontal(m_spawnRulesHeader);
                {
                    unfolded = GUILayout.Toggle(unfolded, unfolded ? "-" : "+", panelLabelStyle, GUILayout.MinWidth(14));
                    GUILayout.Space(-5f);
                    unfolded = GUILayout.Toggle(unfolded, panelLabel, panelLabelStyle);
                    GUILayout.FlexibleSpace();
                    GUIContent GCvisualizeIcon = null;
                    bool currentGUIState = GUI.enabled;
                    if (m_spawner.m_previewRuleIds.Count == 0)
                    {
                        GUI.enabled = false;
                    }

                    if (m_spawner.m_drawPreview)
                    {
                        GCvisualizeIcon = GaiaEditorUtils.GetIconGUIContent("IconVisibleDisabled", m_gaiaSettings.m_IconVisibleDisabled, m_gaiaSettings.m_IconProVisibleDisabled, m_editorUtils);
                    }
                    else
                    {
                        GCvisualizeIcon = GaiaEditorUtils.GetIconGUIContent("IconVisibleDisabled", m_gaiaSettings.m_IconVisible, m_gaiaSettings.m_IconProVisible, m_editorUtils);
                    }
                    if (m_editorUtils.Button(GCvisualizeIcon, m_smallButtonStyle, GUILayout.Height(20), GUILayout.Width(20)))
                    {
                        //as soon as the user interacts with the button, the user is in control, no need for auto activate anymore
                        m_activatePreviewRequested = false;
                        m_spawner.TogglePreview();
                        //force repaint
                        EditorWindow view = EditorWindow.GetWindow<SceneView>();
                        view.Repaint();
                    }
                    GUI.enabled = currentGUIState;
                    GUILayout.Space(5);
                    if (m_editorUtils.Button(GetLabel("+"), GUILayout.Height(20)))
                    {

                        switch (contentMethod.Method.Name)
                        {
                            case "DrawBiomeMaskSpawnRules":
                                AddNewRule(SpawnerResourceType.WorldBiomeMask);
                                break;
                            case "DrawStampSpawnRules":
                                AddNewRule(SpawnerResourceType.StampDistribution);
                                break;
                            default:
                                AddNewRule(SpawnerResourceType.GameObject);
                                break;
                        }



                        //Always unfold when adding a new rule, we want to see & edit it
                        unfolded = true;
                    }




                    if (m_editorUtils.Button(GetLabel("A"), GUILayout.Height(20)))
                    {
                        for (int idx = 0; idx < m_spawner.m_settings.m_spawnerRules.Count; idx++)
                        {
                            m_spawner.m_settings.m_spawnerRules[idx].m_isActive = true;
                        }
                    }


                    if (m_editorUtils.Button(GetLabel("I"), GUILayout.Height(20)))
                    {
                        for (int idx = 0; idx < m_spawner.m_settings.m_spawnerRules.Count; idx++)
                        {
                            m_spawner.m_settings.m_spawnerRules[idx].m_isActive = false;
                        }
                    }
                    if (m_editorUtils.Button("X All", GUILayout.Height(20)))
                    {
                        if (EditorUtility.DisplayDialog("Delete All Rules ?", "Are you sure you want to delete all rules - this can not be undone ?", "Yes", "No"))
                        {
                            m_spawner.m_settings.m_spawnerRules.Clear();
                            m_reorderableRuleMasksLists = new ReorderableList[0];
                            PruneResources();
                        }
                    }

                    //EditorGUILayout.EndVertical();
                    //m_editorUtils.HelpToggle(ref helpActive);
                }
                GUILayout.EndHorizontal();

                if (helpActive)
                {
                    GUILayout.Space(2f);
                    m_editorUtils.InlineHelp(nameKey, helpActive);
                }

                if (unfolded)
                {
                    GUILayout.BeginVertical(panelStyle);
                    {
                        bool noRulesCheck = false;

                        switch (contentMethod.Method.Name)
                        {
                            case "DrawBiomeMaskSpawnRules":
                                noRulesCheck = m_spawner.m_settings.m_spawnerRules.Where(x => x.m_resourceType == SpawnerResourceType.WorldBiomeMask).Count() == 0;
                                break;
                            case "DrawStampSpawnRules":
                                noRulesCheck = m_spawner.m_settings.m_spawnerRules.Where(x => x.m_resourceType == SpawnerResourceType.StampDistribution).Count() == 0;
                                break;
                            default:
                                noRulesCheck = m_spawner.m_settings.m_spawnerRules.Count == 0;
                                break;
                        }



                        if (noRulesCheck)
                        {
                            GUILayout.Space(20);
                            //No rules yet, display a message to prompt the user to add a first resource
                            if (m_noRulesLabelStyle == null)
                            {
                                m_noRulesLabelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
                            }
                            switch (contentMethod.Method.Name)
                            {
                                case "DrawBiomeMaskSpawnRules":
                                    m_editorUtils.LabelField("NoBiomeMaskRulesYet", m_noRulesLabelStyle);
                                    break;
                                case "DrawStampSpawnRules":
                                    m_editorUtils.LabelField("NoStampRulesYet", m_noRulesLabelStyle);
                                    break;
                                default:
                                    m_editorUtils.LabelField("NoRulesYet", m_noRulesLabelStyle);
                                    break;
                            }


                            GUILayout.Space(10);

                            switch (contentMethod.Method.Name)
                            {
                                case "DrawBiomeMaskSpawnRules":
                                    if (m_editorUtils.Button("CreateFirstBiomeMaskRule"))
                                    {
                                        AddNewRule(SpawnerResourceType.WorldBiomeMask);
                                    }
                                    break;
                                case "DrawStampSpawnRules":
                                    if (m_editorUtils.Button("CreateFirstStampRule"))
                                    {
                                        AddNewRule(SpawnerResourceType.StampDistribution);
                                    }
                                    break;
                                default:
                                    if (m_editorUtils.Button("CreateFirstRule"))
                                    {
                                        AddNewRule(SpawnerResourceType.GameObject);
                                    }
                                    break;
                            }

                            //Always unfold when adding a new rule, we want to see & edit it
                            unfolded = true;
                            GUILayout.Space(20);
                        }
                        else
                        {
                            GUILayout.Space(10);
                            EditorGUI.indentLevel++;
                            contentMethod.Invoke(helpActive);
                            EditorGUI.indentLevel--;
                            GUILayout.Space(10);
                        }
                        if (!m_spawner.m_settings.m_isWorldmapSpawner)
                        {
                            DrawGameObjectDropBox();
                        }
                        GUILayout.Space(10);
                    }
                    GUILayout.EndVertical();
                }
            }
            if (unfolded)
            {
                //Footer - repeat the buttons so they are easily accessible anywhere
                GUILayout.BeginHorizontal(m_spawnRulesHeader);
                {
                    //unfolded = GUILayout.Toggle(unfolded, unfolded ? "-" : "+", panelLabelStyle, GUILayout.MinWidth(14));
                    //GUILayout.Space(-5f);
                    //unfolded = GUILayout.Toggle(unfolded, panelLabel, panelLabelStyle);
                    GUILayout.FlexibleSpace();
                    GUIContent GCvisualizeIcon = null;
                    bool currentGUIState = GUI.enabled;
                    if (m_spawner.m_previewRuleIds.Count == 0)
                    {
                        GUI.enabled = false;
                    }

                    if (m_spawner.m_drawPreview)
                    {
                        GCvisualizeIcon = GaiaEditorUtils.GetIconGUIContent("IconVisibleDisabled", m_gaiaSettings.m_IconVisibleDisabled, m_gaiaSettings.m_IconProVisibleDisabled, m_editorUtils);
                    }
                    else
                    {
                        GCvisualizeIcon = GaiaEditorUtils.GetIconGUIContent("IconVisibleDisabled", m_gaiaSettings.m_IconVisible, m_gaiaSettings.m_IconProVisible, m_editorUtils);
                    }
                    if (m_editorUtils.Button(GCvisualizeIcon, m_smallButtonStyle, GUILayout.Height(20), GUILayout.Width(20)))
                    {
                        //as soon as the user interacts with the button, the user is in control, no need for auto activate anymore
                        m_activatePreviewRequested = false;
                        m_spawner.TogglePreview();
                        //force repaint
                        EditorWindow view = EditorWindow.GetWindow<SceneView>();
                        view.Repaint();
                    }
                    GUI.enabled = currentGUIState;
                    GUILayout.Space(5);
                    if (m_editorUtils.Button(GetLabel("+"), GUILayout.Height(20)))
                    {

                        switch (contentMethod.Method.Name)
                        {
                            case "DrawBiomeMaskSpawnRules":
                                AddNewRule(SpawnerResourceType.WorldBiomeMask);
                                break;
                            case "DrawStampSpawnRules":
                                AddNewRule(SpawnerResourceType.StampDistribution);
                                break;
                            default:
                                AddNewRule(SpawnerResourceType.GameObject);
                                break;
                        }



                        //Always unfold when adding a new rule, we want to see & edit it
                        unfolded = true;
                    }




                    if (m_editorUtils.Button(GetLabel("A"), GUILayout.Height(20)))
                    {
                        for (int idx = 0; idx < m_spawner.m_settings.m_spawnerRules.Count; idx++)
                        {
                            m_spawner.m_settings.m_spawnerRules[idx].m_isActive = true;
                        }
                    }


                    if (m_editorUtils.Button(GetLabel("I"), GUILayout.Height(20)))
                    {
                        for (int idx = 0; idx < m_spawner.m_settings.m_spawnerRules.Count; idx++)
                        {
                            m_spawner.m_settings.m_spawnerRules[idx].m_isActive = false;
                        }
                    }
                    if (m_editorUtils.Button("X All", GUILayout.Height(20)))
                    {
                        if (EditorUtility.DisplayDialog("Delete All Rules ?", "Are you sure you want to delete all rules - this can not be undone ?", "Yes", "No"))
                        {
                            m_spawner.m_settings.m_spawnerRules.Clear();
                            m_reorderableRuleMasksLists = new ReorderableList[0];
                            PruneResources();
                        }
                    }

                    //EditorGUILayout.EndVertical();
                    //m_editorUtils.HelpToggle(ref helpActive);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            switch (contentMethod.Method.Name)
            {
                case "DrawBiomeMaskSpawnRules":
                    m_spawner.m_biomeMaskPanelUnfolded = unfolded;
                    break;
                case "DrawStampSpawnRules":
                    m_spawner.m_spawnStampsPanelUnfolded = unfolded;
                    break;
                default:
                    m_spawner.m_rulePanelUnfolded = unfolded;
                    break;
            }


            m_rulePanelHelpActive = helpActive;
            return unfolded;
        }

        private void AddNewRule(SpawnerResourceType resourceType, int resourceID = -99)
        {
            SpawnRule newRule = new SpawnRule();

            //no resource ID given? Create a new one
            if (resourceID == -99)
            {
                newRule.m_name = "New Spawn Rule Entry";

                if (m_spawner.m_settings.m_isWorldmapSpawner)
                {
                    newRule.m_resourceType = resourceType;
                }
                else
                {
                    //check the previous rule, if any. We assume the user wants to create another rule of the same resource type.
                    if (m_spawner.m_settings.m_spawnerRules.Count >= 1)
                    {
                        newRule.m_resourceType = m_spawner.m_settings.m_spawnerRules[m_spawner.m_settings.m_spawnerRules.Count - 1].m_resourceType;
                    }
                }
                //add a new resource prototype as well, we assume the user wants to add a new resource rather than re-use one
                switch (newRule.m_resourceType)
                {
                    case SpawnerResourceType.TerrainTexture:
                        newRule.m_resourceIdx = AddNewTextureResource();
                        ImageMask.RefreshSpawnRuleGUIDs();
                        break;
                    case SpawnerResourceType.TerrainTree:
                        newRule.m_resourceIdx = AddNewTreeResource();
                        break;
                    case SpawnerResourceType.TerrainDetail:
                        newRule.m_resourceIdx = AddNewTerrainDetailResource();
                        break;
                    case SpawnerResourceType.GameObject:
                        newRule.m_resourceIdx = AddNewGameObjectResource();
                        break;
                    case SpawnerResourceType.SpawnExtension:
                        newRule.m_resourceIdx = AddNewSpawnExtensionResource();
                        break;
                    case SpawnerResourceType.StampDistribution:
                        newRule.m_resourceIdx = AddNewStampDistributionResource();
                        break;
                    case SpawnerResourceType.WorldBiomeMask:
                        newRule.m_resourceIdx = AddNewWorldBiomeMaskResource();
                        break;
                }
            }
            else
            {
                //use existing resource type / id
                newRule.m_resourceType = resourceType;
                switch (resourceType)
                {
                    case SpawnerResourceType.TerrainTexture:
                        newRule.m_name = m_spawner.m_settings.m_resources.m_texturePrototypes[resourceID].m_name;
                        break;
                    case SpawnerResourceType.TerrainTree:
                        newRule.m_name = m_spawner.m_settings.m_resources.m_treePrototypes[resourceID].m_name;
                        break;
                    case SpawnerResourceType.TerrainDetail:
                        newRule.m_name = m_spawner.m_settings.m_resources.m_detailPrototypes[resourceID].m_name;
                        break;
                    case SpawnerResourceType.GameObject:
                        newRule.m_name = m_spawner.m_settings.m_resources.m_gameObjectPrototypes[resourceID].m_name;
                        break;
                    case SpawnerResourceType.SpawnExtension:
                        newRule.m_name = m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes[resourceID].m_name;
                        break;
                    case SpawnerResourceType.StampDistribution:
                        newRule.m_name = m_spawner.m_settings.m_resources.m_stampDistributionPrototypes[resourceID].m_name;
                        break;
                    case SpawnerResourceType.WorldBiomeMask:
                        newRule.m_name = m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes[resourceID].m_name;
                        break;
                }
                newRule.m_resourceIdx = resourceID;
            }

            newRule.m_imageMasks = new ImageMask[0];
            //always fold out the resource settings for new rules so user can edit directly
            newRule.m_isFoldedOut = true;
            newRule.m_resourceSettingsFoldedOut = true;
            m_spawner.m_rulePanelUnfolded = true;
            m_spawner.m_settings.m_spawnerRules.Add(newRule);
            m_previewImageDisplayedDuringLayout = GaiaUtils.AddElementToArray(m_previewImageDisplayedDuringLayout, false);
            AddNewMaskList();

        }

        private void DrawSpawnSettings(bool helpEnabled)
        {
            if (m_spawner.transform.name.Contains("New Spawner"))
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("NoUniqueName"), MessageType.Warning);
            }
            m_spawner.transform.name = m_editorUtils.DelayedTextField("Name", m_spawner.transform.name);
            m_spawner.m_settings.m_spawnRange = m_editorUtils.Slider("Range", m_spawner.m_settings.m_spawnRange, 1, m_spawner.GetMaxSpawnerRange(), helpEnabled);
            DrawSeaLevelSlider(helpEnabled);
            m_spawner.m_settings.spawnMode = (SpawnMode)m_editorUtils.EnumPopup("SpawnMode", m_spawner.m_settings.spawnMode, helpEnabled);
            m_spawner.m_settings.m_generateRandomSeed = m_editorUtils.Toggle("GenerateRandomSeed", m_spawner.m_settings.m_generateRandomSeed, helpEnabled);
            bool guiEnabled = GUI.enabled;
            GUI.enabled = !m_spawner.m_settings.m_generateRandomSeed;
            m_spawner.m_settings.m_randomSeed = m_editorUtils.IntField("RandomSeed", m_spawner.m_settings.m_randomSeed, helpEnabled);
            GUI.enabled = guiEnabled;
            ImageMaskListEditor.DrawMaskList(ref m_spawnerMaskListExpanded, m_reorderableSpawnerMaskList, m_editorUtils);
            m_editorUtils.InlineHelp("SpawnerMask", helpEnabled);
        }


        private void DrawRegularSpawnRules(bool helpEnabled)
        {
            for (int ruleIdx = 0; ruleIdx < m_spawner.m_settings.m_spawnerRules.Count; ruleIdx++)
            {
                m_spawnRuleIndexBeingDrawn = ruleIdx;
                SpawnRule rule = m_spawner.m_settings.m_spawnerRules[ruleIdx];
                if (rule.m_resourceType == SpawnerResourceType.GameObject ||
                    rule.m_resourceType == SpawnerResourceType.SpawnExtension ||
                    rule.m_resourceType == SpawnerResourceType.TerrainDetail ||
                    rule.m_resourceType == SpawnerResourceType.TerrainTree ||
                    rule.m_resourceType == SpawnerResourceType.TerrainTexture)
                {
                    DrawSingleRulePanel(rule, ruleIdx);
                }
            } //for
        }

        private void DrawStampSpawnRules(bool helpEnabled)
        {
            for (int ruleIdx = 0; ruleIdx < m_spawner.m_settings.m_spawnerRules.Count; ruleIdx++)
            {
                m_spawnRuleIndexBeingDrawn = ruleIdx;
                SpawnRule rule = m_spawner.m_settings.m_spawnerRules[ruleIdx];
                if (rule.m_resourceType == SpawnerResourceType.StampDistribution)
                {
                    DrawSingleRulePanel(rule, ruleIdx);
                }
            } //for

            if (m_spawner.HasWorldSizeChanged())
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("ExportTerrainSizeSettingsChanged"), MessageType.Error);

            }
            //if (m_spawner.HasHeightmapResolutionChanged())
            //{
            //    GUILayout.Space(10);
            //    EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("ExportTerrainResolutionSettingsChanged"), MessageType.Warning);
            //}

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (m_editorUtils.Button("ButtonSpawnStamps"))
            {
                GaiaTerrainLoaderManager.Instance.SwitchToWorldMap();
                Spawn(false);
                m_spawner.StoreWorldSize();
                m_spawner.m_exportTerrainUnfolded = true;
            }
            if (m_editorUtils.Button("ButtonClearStamps"))
            {
                ClearOperationSettings clearOperationSettings = ScriptableObject.CreateInstance<ClearOperationSettings>();
                clearOperationSettings.m_clearTrees = false;
                clearOperationSettings.m_clearTerrainDetails = false;
                clearOperationSettings.m_clearGameObjects = false;
                clearOperationSettings.m_clearSpawnExtensions = false;
                clearOperationSettings.m_clearStamps = true;
                clearOperationSettings.m_clearSpawnFrom = ClearSpawnFrom.OnlyThisSpawner;
                clearOperationSettings.m_clearSpawnFor = ClearSpawnFor.AllTerrains;

                GaiaSessionManager.ClearSpawns(clearOperationSettings, m_spawner.m_settings, true, m_spawner);

                m_spawner.m_worldMapStamperSettings.Clear();

                foreach (SpawnRule rule in m_spawner.m_settings.m_spawnerRules)
                {
                    if (rule.m_resourceType == GaiaConstants.SpawnerResourceType.StampDistribution)
                    {
                        rule.m_spawnedInstances = 0;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBiomeMaskSpawnRules(bool helpEnabled)
        {
            for (int ruleIdx = 0; ruleIdx < m_spawner.m_settings.m_spawnerRules.Count; ruleIdx++)
            {
                m_spawnRuleIndexBeingDrawn = ruleIdx;
                SpawnRule rule = m_spawner.m_settings.m_spawnerRules[ruleIdx];
                if (rule.m_resourceType == SpawnerResourceType.WorldBiomeMask)
                {
                    DrawSingleRulePanel(rule, ruleIdx);
                }
            } //for

            GUILayout.Space(10);
        }


        //private void DrawSpawnRules(bool helpEnabled, SpawnerResourceType resourceType)
        //{
        //    for (int ruleIdx = 0; ruleIdx < m_spawner.m_settings.m_spawnerRules.Count; ruleIdx++)
        //    {
        //        m_spawnRuleIndexBeingDrawn = ruleIdx;
        //        SpawnRule rule = m_spawner.m_settings.m_spawnerRules[ruleIdx];

        //        DrawSingleRulePanel(rule, ruleIdx);

        //    } //for
        //}

        private bool DrawSingleRulePanel(SpawnRule rule, int ruleIdx)
        {
            string instanceCount = "";

            if (rule.m_resourceType != SpawnerResourceType.TerrainTexture)
            {
                instanceCount = "(" + rule.m_spawnedInstances + ")";
            }


            GUIContent panelLabel = new GUIContent(instanceCount + " " + rule.m_name);

            //Panel Label
            GUIStyle panelLabelStyle = new GUIStyle(GUI.skin.label);
            panelLabelStyle.normal.textColor = GUI.skin.label.normal.textColor;
            panelLabelStyle.fontStyle = FontStyle.Bold;
            panelLabelStyle.normal.background = GUI.skin.label.normal.background;

            //Preview Image
            GUIStyle previewImageStyle = new GUIStyle();
            //previewImageStyle.alignment = TextAnchor.MiddleCenter;
            //previewImageStyle.fixedWidth = 20;
            //previewImageStyle.fixedHeight = 20;
            previewImageStyle.padding = new RectOffset(0, 0, 0, 2);

            // Panel
            GUIStyle panelStyle = new GUIStyle(GUI.skin.label);
            panelStyle.normal.textColor = GUI.skin.label.normal.textColor;
            panelStyle.alignment = TextAnchor.UpperLeft;

            bool unfolded = rule.m_isFoldedOut;
            bool helpEnabled = rule.m_isHelpActive;
            bool originalGUIState = GUI.enabled;


            GUILayout.BeginVertical(m_spawRulesBorder);
            {
                GUILayout.BeginHorizontal(m_singleSpawnRuleHeader);
                {
                    //Rect rect = EditorGUILayout.GetControlRect();
                    unfolded = GUILayout.Toggle(unfolded, unfolded ? "-" : "+", panelLabelStyle, GUILayout.MinWidth(10));



                    rule.m_isActive = GUILayout.Toggle(rule.m_isActive, "");


                    GUI.enabled = rule.m_isActive && GUI.enabled;
                    bool ruleGUIState = GUI.enabled;

                    Texture2D resourceTexture = GetSpawnRulePreviewTexture(rule, m_spawner.m_settings.m_resources);


                    //We only should display the texture if it is not null, but also make sure it was displayed during the layout event already.
                    //If the texture is being loaded between EventType.Layout and EventType.Repaint, a new control would be created during Repaint 
                    //which will result in an error message.
                    if (resourceTexture != null && (Event.current.type == EventType.Layout || m_previewImageDisplayedDuringLayout[ruleIdx]))
                    {
                        GUILayout.Space(-15);
                        EditorGUILayout.LabelField(new GUIContent(resourceTexture), previewImageStyle, GUILayout.MaxWidth(30));
                        if (Event.current.type == EventType.Layout)
                        {
                            m_previewImageDisplayedDuringLayout[ruleIdx] = true;
                        }
                    }

                    Rect imageRect = GUILayoutUtility.GetLastRect();
                    imageRect.width = 20;
                    imageRect.height = 20;
                    //EditorGUIUtility.AddCursorRect(imageRect, MouseCursor.Zoom);
                    //if (imageRect.Contains(Event.current.mousePosition))
                    //{
                    //    m_drawResourcePreviewRuleId = ruleIdx;
                    //    //unfold so we can see the larger preview actually
                    //    unfolded = true;
                    //}
                    unfolded = GUILayout.Toggle(unfolded, panelLabel, panelLabelStyle, GUILayout.MinWidth(0));
                    GUILayout.FlexibleSpace();

                    //Deactivate upwards button for first position in the rule list
                    if (ruleIdx == 0)
                    {
                        GUI.enabled = false;
                    }

                    float smallButtonSize = 20;
                    GUIContent GCupIcon = GaiaEditorUtils.GetIconGUIContent("IconUp", m_gaiaSettings.m_IconUp, m_gaiaSettings.m_IconProUp, m_editorUtils);
                    if (m_editorUtils.Button(GCupIcon, m_smallButtonStyle, GUILayout.Height(smallButtonSize), GUILayout.Width(smallButtonSize)))
                    {
                        SwapRules(ruleIdx - 1, ruleIdx);
                    }

                    GUI.enabled = ruleGUIState;

                    //Deactivate downwards button for last position in the rule list
                    if (ruleIdx == m_spawner.m_settings.m_spawnerRules.Count() - 1)
                    {
                        GUI.enabled = false;
                    }


                    GUIContent GCdownIcon = GaiaEditorUtils.GetIconGUIContent("IconDown", m_gaiaSettings.m_IconDown, m_gaiaSettings.m_IconProDown, m_editorUtils);
                    if (m_editorUtils.Button(GCdownIcon, m_smallButtonStyle, GUILayout.Height(smallButtonSize), GUILayout.Width(smallButtonSize)))
                    {
                        SwapRules(ruleIdx, ruleIdx + 1);
                    }
                    GUI.enabled = ruleGUIState;

                    GUIContent GCduplicateIcon = GaiaEditorUtils.GetIconGUIContent("IconDuplicate", m_gaiaSettings.m_IconDuplicate, m_gaiaSettings.m_IconProDuplicate, m_editorUtils);
                    if (m_editorUtils.Button(GCduplicateIcon, m_smallButtonStyle, GUILayout.Height(smallButtonSize), GUILayout.Width(smallButtonSize)))
                    {
                        DuplicateRule(ruleIdx);
                    }

                    DrawVisualiseButton(ruleIdx, m_smallButtonStyle, smallButtonSize);

                    //Unless the spawner is spawning, still offer to delete even if the rule is inactive
                    if (originalGUIState)
                    {
                        GUI.enabled = true;
                    }
                    GUIContent GCremoveIcon = GaiaEditorUtils.GetIconGUIContent("IconRemove", m_gaiaSettings.m_IconRemove, m_gaiaSettings.m_IconProRemove, m_editorUtils);
                    if (m_editorUtils.Button(GCremoveIcon, m_smallButtonStyle, GUILayout.Height(smallButtonSize), GUILayout.Width(smallButtonSize)))
                    {
                        m_spawner.m_settings.m_spawnerRules.Remove(rule);
                        RemoveMaskList(ruleIdx);
                        m_previewImageDisplayedDuringLayout = GaiaUtils.RemoveArrayIndexAt(m_previewImageDisplayedDuringLayout, ruleIdx);
                        PruneResources();
                        CleanPreviewRuleIDs();
                    }

                    GUI.enabled = originalGUIState && rule.m_isActive;

                    //EditorGUILayout.EndVertical();
                    m_editorUtils.HelpToggle(ref helpEnabled);

                }
                GUILayout.EndHorizontal();

                //if (helpActive)
                //{
                //    GUILayout.Space(2f);
                //    m_editorUtils.InlineHelp(nameKey, helpActive);
                //}

                if (unfolded)
                {
                    GUILayout.BeginVertical(panelStyle);
                    {
                        if (rule != null && m_spawner.m_settings.m_spawnerRules.Count > ruleIdx)
                        {
                            DrawSingleRule(rule, ruleIdx, helpEnabled);
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            //Leave a little space between the rules, but no extra space behind the last rule
            if (ruleIdx < m_spawner.m_settings.m_spawnerRules.Count - 1)
            {
                GUILayout.Space(8);
            }

            rule.m_isFoldedOut = unfolded;
            rule.m_isHelpActive = helpEnabled;
            GUI.enabled = originalGUIState;
            return unfolded;
        }


        /// <summary>
        /// Swaps out the position / index of two spawn rules in the stack
        /// </summary>
        /// <param name="firstRuleID"></param>
        /// <param name="secondRuleID"></param>
        private void SwapRules(int firstRuleID, int secondRuleID)
        {
            //Check if both ids are actually in index first
            int maxIndex = m_spawner.m_settings.m_spawnerRules.Count() - 1;
            if (firstRuleID > maxIndex || secondRuleID > maxIndex || firstRuleID < 0 || secondRuleID < 0)
            {
                Debug.LogError("Could not swap rules: First Index " + firstRuleID.ToString() + " or Second Index " + secondRuleID.ToString() + " are out of bounds.");
                return;
            }

            SpawnRule tempRule = m_spawner.m_settings.m_spawnerRules[firstRuleID];
            m_spawner.m_settings.m_spawnerRules[firstRuleID] = m_spawner.m_settings.m_spawnerRules[secondRuleID];
            m_spawner.m_settings.m_spawnerRules[secondRuleID] = tempRule;

            m_reorderableRuleMasksLists = GaiaUtils.SwapElementsInArray(m_reorderableRuleMasksLists, firstRuleID, secondRuleID);

            m_spawnRuleMaskListExpanded = GaiaUtils.SwapElementsInArray(m_spawnRuleMaskListExpanded, firstRuleID, secondRuleID);
            m_previewImageDisplayedDuringLayout = GaiaUtils.SwapElementsInArray(m_previewImageDisplayedDuringLayout, firstRuleID, secondRuleID);

            //Swap preview rule indexes as well, if affected
            bool visChange = false;
            for (int i = 0; i < m_spawner.m_previewRuleIds.Count(); i++)
            {
                if (m_spawner.m_previewRuleIds[i] == secondRuleID)
                {
                    m_spawner.m_previewRuleIds[i] = firstRuleID;
                    visChange = true;
                }
                //the else here is important!
                else if (m_spawner.m_previewRuleIds[i] == firstRuleID)
                {
                    m_spawner.m_previewRuleIds[i] = secondRuleID;
                    visChange = true;
                }

                //If the order of visualized rules has changed, we need to update the visualization as well
                if (visChange)
                {
                    m_spawner.m_previewRuleIds.Sort();
                    m_spawner.m_spawnPreviewDirty = true;
                    m_spawner.SetWorldBiomeMasksDirty();
                }
            }

        }


        /// <summary>
        /// Duplicates a spawn rule in the stack
        /// </summary>
        /// <param name="spawnRuleId"></param>
        private void DuplicateRule(int spawnRuleId)
        {
            SpawnRule sourceRule = m_spawner.m_settings.m_spawnerRules[spawnRuleId];
            SpawnRule newRule = new SpawnRule();

            //Create a copy of the used resource, from user feedback it is more common that users want
            //an actual copy of the resource rather than editing the same resource from two different rules.
            switch (sourceRule.m_resourceType)
            {
                case SpawnerResourceType.GameObject:
                    newRule.m_resourceIdx = AddNewGameObjectResource();
                    GaiaUtils.CopyFields(m_spawner.m_settings.m_resources.m_gameObjectPrototypes[sourceRule.m_resourceIdx], m_spawner.m_settings.m_resources.m_gameObjectPrototypes[newRule.m_resourceIdx]);
                    m_spawner.m_settings.m_resources.m_gameObjectPrototypes[newRule.m_resourceIdx].m_name += " Copy";
                    break;
                case SpawnerResourceType.SpawnExtension:
                    newRule.m_resourceIdx = AddNewSpawnExtensionResource();
                    GaiaUtils.CopyFields(m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes[sourceRule.m_resourceIdx], m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes[newRule.m_resourceIdx]);
                    m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes[newRule.m_resourceIdx].m_name += " Copy";
                    break;
                case SpawnerResourceType.TerrainDetail:
                    newRule.m_resourceIdx = AddNewTerrainDetailResource();
                    GaiaUtils.CopyFields(m_spawner.m_settings.m_resources.m_detailPrototypes[sourceRule.m_resourceIdx], m_spawner.m_settings.m_resources.m_detailPrototypes[newRule.m_resourceIdx]);
                    m_spawner.m_settings.m_resources.m_detailPrototypes[newRule.m_resourceIdx].m_name += " Copy";
                    break;
                case SpawnerResourceType.TerrainTexture:
                    newRule.m_resourceIdx = AddNewTextureResource();
                    GaiaUtils.CopyFields(m_spawner.m_settings.m_resources.m_texturePrototypes[sourceRule.m_resourceIdx], m_spawner.m_settings.m_resources.m_texturePrototypes[newRule.m_resourceIdx]);
                    m_spawner.m_settings.m_resources.m_texturePrototypes[newRule.m_resourceIdx].m_name += " Copy";
                    break;
                case SpawnerResourceType.TerrainTree:
                    newRule.m_resourceIdx = AddNewTreeResource();
                    GaiaUtils.CopyFields(m_spawner.m_settings.m_resources.m_treePrototypes[sourceRule.m_resourceIdx], m_spawner.m_settings.m_resources.m_treePrototypes[newRule.m_resourceIdx]);
                    m_spawner.m_settings.m_resources.m_treePrototypes[newRule.m_resourceIdx].m_name += " Copy";
                    break;
                case SpawnerResourceType.StampDistribution:
                    newRule.m_resourceIdx = AddNewStampDistributionResource();
                    GaiaUtils.CopyFields(m_spawner.m_settings.m_resources.m_stampDistributionPrototypes[sourceRule.m_resourceIdx], m_spawner.m_settings.m_resources.m_stampDistributionPrototypes[newRule.m_resourceIdx]);
                    //Deep copy of feature list
                    m_spawner.m_settings.m_resources.m_stampDistributionPrototypes[newRule.m_resourceIdx].m_featureSettings = m_spawner.m_settings.m_resources.m_stampDistributionPrototypes[sourceRule.m_resourceIdx].m_featureSettings.Select(x => new StampFeatureSettings(x)).ToList();
                    m_spawner.m_settings.m_resources.m_stampDistributionPrototypes[newRule.m_resourceIdx].m_name += " Copy";
                    break;
                case SpawnerResourceType.WorldBiomeMask:
                    newRule.m_resourceIdx = AddNewWorldBiomeMaskResource();
                    GaiaUtils.CopyFields(m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes[sourceRule.m_resourceIdx], m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes[newRule.m_resourceIdx]);
                    m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes[newRule.m_resourceIdx].m_name += " Copy";
                    break;
            }




            //GaiaUtils.CopyFields(m_spawner.m_settings.m_spawnerRules[spawnRuleId], newRule);
            newRule.m_resourceType = sourceRule.m_resourceType;

            newRule.m_name = sourceRule.m_name + " Copy";
            newRule.m_locationIncrementMin = sourceRule.m_locationIncrementMin;
            newRule.m_locationIncrementMax = sourceRule.m_locationIncrementMax;
            newRule.m_jitterPercent = sourceRule.m_jitterPercent;
            newRule.m_minRequiredFitness = sourceRule.m_minRequiredFitness;
            newRule.m_minDirection = sourceRule.m_minDirection;
            newRule.m_maxDirection = sourceRule.m_maxDirection;
            newRule.m_boundsCheckQuality = sourceRule.m_boundsCheckQuality;
            newRule.m_goSpawnTarget = sourceRule.m_goSpawnTarget;

            newRule.m_isFoldedOut = sourceRule.m_isFoldedOut;
            newRule.m_resourceSettingsFoldedOut = sourceRule.m_resourceSettingsFoldedOut;
            m_spawner.m_settings.m_spawnerRules.Insert(spawnRuleId + 1, newRule);

            newRule.m_imageMasks = new ImageMask[sourceRule.m_imageMasks.Count()];

            //Create a deep copy for each mask in the list > we want source & target to be independent from each other
            for (int i = 0; i < newRule.m_imageMasks.Count(); i++)
            {
                newRule.m_imageMasks[i] = ImageMask.Clone(m_spawner.m_settings.m_spawnerRules[spawnRuleId].m_imageMasks[i]);
                newRule.m_imageMasks[i].m_reorderableCollisionMaskList = CreateSpawnRuleCollisionMaskList(newRule.m_imageMasks[i].m_reorderableCollisionMaskList, newRule.m_imageMasks[i].m_collisionMasks);

            }

            //Insert the new mask list
            ReorderableList newMaskList = new ReorderableList(newRule.m_imageMasks, typeof(ImageMask));
            newMaskList = CreateSpawnRuleMaskList(newMaskList, newRule.m_imageMasks);
            m_reorderableRuleMasksLists = GaiaUtils.InsertElementInArray(m_reorderableRuleMasksLists, newMaskList, spawnRuleId + 1);

            //Insert the expanded flag
            m_spawnRuleMaskListExpanded = GaiaUtils.InsertElementInArray(m_spawnRuleMaskListExpanded, false, spawnRuleId + 1);

            //Insert the flag for preview image
            m_previewImageDisplayedDuringLayout = GaiaUtils.InsertElementInArray(m_previewImageDisplayedDuringLayout, false, spawnRuleId + 1);
        }

        /// <summary>
        /// Removes all Resource entries that are not in use by any rule anymore, this prevents build up of unused resource entries
        /// </summary>
        private void PruneResources()
        {
            GaiaResource resource = m_spawner.m_settings.m_resources;
            for (int i = resource.m_texturePrototypes.Length - 1; i >= 0; i--)
            {
                if (m_spawner.m_settings.m_spawnerRules.Find(x => x.m_resourceType == SpawnerResourceType.TerrainTexture && x.m_resourceIdx == i) == null)
                {
                    resource.m_texturePrototypes = GaiaUtils.RemoveArrayIndexAt(resource.m_texturePrototypes, i);
                    m_spawner.CorrectIndicesAfteResourceDeletion(SpawnerResourceType.TerrainTexture, i);
                }
            }
            for (int i = resource.m_treePrototypes.Length - 1; i >= 0; i--)
            {
                if (m_spawner.m_settings.m_spawnerRules.Find(x => x.m_resourceType == SpawnerResourceType.TerrainTree && x.m_resourceIdx == i) == null)
                {
                    resource.m_treePrototypes = GaiaUtils.RemoveArrayIndexAt(resource.m_treePrototypes, i);
                    m_spawner.CorrectIndicesAfteResourceDeletion(SpawnerResourceType.TerrainTree, i);
                }
            }
            for (int i = resource.m_detailPrototypes.Length - 1; i >= 0; i--)
            {
                if (m_spawner.m_settings.m_spawnerRules.Find(x => x.m_resourceType == SpawnerResourceType.TerrainDetail && x.m_resourceIdx == i) == null)
                {
                    resource.m_detailPrototypes = GaiaUtils.RemoveArrayIndexAt(resource.m_detailPrototypes, i);
                    m_spawner.CorrectIndicesAfteResourceDeletion(SpawnerResourceType.TerrainDetail, i);
                }
            }
            for (int i = resource.m_gameObjectPrototypes.Length - 1; i >= 0; i--)
            {
                if (m_spawner.m_settings.m_spawnerRules.Find(x => x.m_resourceType == SpawnerResourceType.GameObject && x.m_resourceIdx == i) == null)
                {
                    resource.m_gameObjectPrototypes = GaiaUtils.RemoveArrayIndexAt(resource.m_gameObjectPrototypes, i);
                    m_spawner.CorrectIndicesAfteResourceDeletion(SpawnerResourceType.GameObject, i);
                }
            }
            for (int i = resource.m_spawnExtensionPrototypes.Length - 1; i >= 0; i--)
            {
                if (m_spawner.m_settings.m_spawnerRules.Find(x => x.m_resourceType == SpawnerResourceType.SpawnExtension && x.m_resourceIdx == i) == null)
                {
                    resource.m_spawnExtensionPrototypes = GaiaUtils.RemoveArrayIndexAt(resource.m_spawnExtensionPrototypes, i);
                    m_spawner.CorrectIndicesAfteResourceDeletion(SpawnerResourceType.SpawnExtension, i);
                }
            }
            for (int i = resource.m_stampDistributionPrototypes.Length - 1; i >= 0; i--)
            {
                if (m_spawner.m_settings.m_spawnerRules.Find(x => x.m_resourceType == SpawnerResourceType.StampDistribution && x.m_resourceIdx == i) == null)
                {
                    resource.m_stampDistributionPrototypes = GaiaUtils.RemoveArrayIndexAt(resource.m_stampDistributionPrototypes, i);
                    m_spawner.CorrectIndicesAfteResourceDeletion(SpawnerResourceType.StampDistribution, i);
                }
            }
            for (int i = resource.m_worldBiomeMaskPrototypes.Length - 1; i >= 0; i--)
            {
                if (m_spawner.m_settings.m_spawnerRules.Find(x => x.m_resourceType == SpawnerResourceType.WorldBiomeMask && x.m_resourceIdx == i) == null)
                {
                    resource.m_worldBiomeMaskPrototypes = GaiaUtils.RemoveArrayIndexAt(resource.m_worldBiomeMaskPrototypes, i);
                    m_spawner.CorrectIndicesAfteResourceDeletion(SpawnerResourceType.WorldBiomeMask, i);
                }
            }
        }

        private void DrawVisualiseButton(int spawnRuleID, GUIStyle buttonStyle, float smallButtonSize)
        {
            Color currentBGColor = GUI.backgroundColor;
            if (m_spawner.m_previewRuleIds.Contains(spawnRuleID) && m_spawner.m_drawPreview)
            {
                GUI.backgroundColor = m_spawner.m_settings.m_spawnerRules[spawnRuleID].m_visualisationColor;
            }

            GUIContent GCvisualizeIcon = GaiaEditorUtils.GetIconGUIContent("IconVisible", m_gaiaSettings.m_IconVisible, m_gaiaSettings.m_IconProVisible, m_editorUtils);
            GUILayout.Space(5);
            if (m_editorUtils.Button(GCvisualizeIcon, buttonStyle, GUILayout.Height(smallButtonSize), GUILayout.Width(smallButtonSize)))

            {
                //is this rule being shown already? then only remove this rule
                if (m_spawner.m_previewRuleIds.Contains(spawnRuleID) && m_spawner.m_drawPreview)
                {
                    m_spawner.m_previewRuleIds.Remove(spawnRuleID);
                    if (m_spawner.m_previewRuleIds.Count <= 0)
                        m_spawner.m_drawPreview = false;
                }
                else
                {
                    if (m_spawner.m_drawPreview)
                    {
                        //this rule needs to be added for visualisation, would we exceed the maximum allowed number?
                        if (m_spawner.m_previewRuleIds.Count() >= GaiaConstants.maxPreviewedTextures)
                        {
                            //Yes, kick lowest rule in stack out first
                            m_spawner.m_previewRuleIds.RemoveAt(0);
                        }

                        //mark this rule for visualisation
                        m_spawner.m_previewRuleIds.Add(spawnRuleID);
                        //Sort the rules ascending, important because the lower rules should overwrite the earlier ones.
                        m_spawner.m_previewRuleIds.Sort();
                    }
                    else
                    {
                        //the spawner was currently not displaying the preview. Throw out any old rules first, and start fresh
                        m_spawner.m_previewRuleIds.Clear();
                        m_spawner.m_previewRuleIds.Add(spawnRuleID);
                        m_spawner.m_drawPreview = true;
                    }
                }

            }

            GUI.backgroundColor = currentBGColor;

            Color currentVisColor = m_spawner.m_settings.m_spawnerRules[spawnRuleID].m_visualisationColor;
            if (GaiaUtils.ColorsEqual(currentVisColor, GaiaConstants.spawnerInitColor))
            {
                //The magic number of 0.618... equals the golden ratio to get an even distributon of colors from the available palette
                currentVisColor = m_gaiaSettings.m_spawnerColorGradient.Evaluate((0.618033988749895f * spawnRuleID) % 1);
            }
            GUILayout.Space(-15);
            m_spawner.m_settings.m_spawnerRules[spawnRuleID].m_visualisationColor = EditorGUILayout.ColorField(currentVisColor, GUILayout.MaxWidth(60));
            GUILayout.Space(5);
        }

        private void DrawSingleRule(SpawnRule rule, int spawnRuleID, bool helpEnabled)
        {
            if (spawnRuleID == m_drawResourcePreviewRuleId)
            {
                Texture2D resourceTexture = GetSpawnRulePreviewTexture(rule, m_spawner.m_settings.m_resources);

                if (resourceTexture != null)
                {
                    //Rect previewRect = EditorGUILayout.GetControlRect();
                    //EditorGUI.LabelField(new Rect(previewRect.x, previewRect.y, EditorGUIUtility.labelWidth, previewRect.height), label);
                    m_editorUtils.Image(resourceTexture);
                    //GUILayout.Space(100);
                }
            }

            //Don't draw a resource foldout for world biome masks - the resource settings are just the name & biome preset, no foldout needed.
            if (rule.m_resourceType != SpawnerResourceType.WorldBiomeMask)
            {
                rule.m_resourceSettingsFoldedOut = m_editorUtils.Foldout(rule.m_resourceSettingsFoldedOut, "ResourceSettings");
            }
            else
            {
                rule.m_resourceSettingsFoldedOut = true;
            }

            if (rule.m_resourceSettingsFoldedOut)
            {
                DrawRuleResourceSettings(rule, spawnRuleID, helpEnabled);
            }


            float maxLocationIncrement = 100f;
            maxLocationIncrement = m_spawner.m_lastActiveTerrainSize * 0.5f;
            if (m_spawner.m_settings.m_isWorldmapSpawner)
            {
                maxLocationIncrement = m_spawner.m_worldMapTerrain.terrainData.size.x * 0.5f;
            }

            if (rule.m_resourceType == GaiaConstants.SpawnerResourceType.TerrainTree)
            {
                m_editorUtils.MinMaxSliderWithFields("TreeProtoLocationIncrement", ref rule.m_locationIncrementMin, ref rule.m_locationIncrementMax, GaiaConstants.minlocationIncrement, maxLocationIncrement, helpEnabled);
                if (rule.m_locationIncrementMin < 1f)
                {
                    EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("ProtoSmallLocationIncrementWarning"), MessageType.Warning);
                }
                rule.m_jitterPercent = m_editorUtils.Slider("TreeProtoJitterPercent", rule.m_jitterPercent, 0f, 1f, helpEnabled);
                rule.m_minRequiredFitness = m_editorUtils.Slider("GameObjectMinFitness", rule.m_minRequiredFitness, 0, 1, helpEnabled);
            }
            if ((rule.m_resourceType == GaiaConstants.SpawnerResourceType.GameObject) || (rule.m_resourceType == GaiaConstants.SpawnerResourceType.SpawnExtension))
            {
                m_editorUtils.MinMaxSliderWithFields("TreeProtoLocationIncrement", ref rule.m_locationIncrementMin, ref rule.m_locationIncrementMax, GaiaConstants.minlocationIncrement, maxLocationIncrement, helpEnabled);
                if (rule.m_locationIncrementMin < 1f)
                {
                    EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("ProtoSmallLocationIncrementWarning"), MessageType.Warning);
                }
                rule.m_jitterPercent = m_editorUtils.Slider("TreeProtoJitterPercent", rule.m_jitterPercent, 0f, 1f, helpEnabled);
                rule.m_minRequiredFitness = m_editorUtils.Slider("GameObjectMinFitness", rule.m_minRequiredFitness, 0, 1, helpEnabled);
            }
            if (rule.m_resourceType == GaiaConstants.SpawnerResourceType.StampDistribution)
            {
                m_editorUtils.MinMaxSliderWithFields("StampProtoLocationIncrement", ref rule.m_locationIncrementMin, ref rule.m_locationIncrementMax, GaiaConstants.minlocationIncrement, 100f, helpEnabled);
                if (rule.m_locationIncrementMin < 1f)
                {
                    EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("ProtoSmallLocationIncrementWarning"), MessageType.Warning);
                }
                rule.m_jitterPercent = m_editorUtils.Slider("TreeProtoJitterPercent", rule.m_jitterPercent, 0f, 1f, helpEnabled);
            }

            if (rule.m_resourceType == GaiaConstants.SpawnerResourceType.TerrainDetail)
            {
                rule.m_terrainDetailDensity = m_editorUtils.IntSlider("DetailProtoDensity", rule.m_terrainDetailDensity, 1, 50, helpEnabled);
                rule.m_terrainDetailMinFitness = m_editorUtils.Slider("DetailMinimumFitness", rule.m_terrainDetailMinFitness, 0, 1, helpEnabled);
                rule.m_terrainDetailFitnessBeginFadeOut = m_editorUtils.Slider("DetailFitnessBeginFadeOut", rule.m_terrainDetailFitnessBeginFadeOut, 0, 1, helpEnabled);
            }


            //if (m_spawner.m_showStatistics && rule.m_resourceType != GaiaConstants.SpawnerResourceType.TerrainTexture)
            //{
            //    EditorGUILayout.LabelField(GetLabel("Instances Spawned"), new GUIContent(rule.m_activeInstanceCnt.ToString()));
            //}

            //Direction control for spawned POI
            if (rule.m_resourceType == GaiaConstants.SpawnerResourceType.GameObject || rule.m_resourceType == GaiaConstants.SpawnerResourceType.SpawnExtension)
            {
                m_editorUtils.MinMaxSliderWithFields("GameObjectProtoMinMaxDirection", ref rule.m_minDirection, ref rule.m_maxDirection, 0f, 360f, helpEnabled);
                rule.m_boundsCheckQuality = m_editorUtils.Slider("GameObjectProtoBoundsCheckQuality", rule.m_boundsCheckQuality, 1f, 100f, helpEnabled);
                rule.m_goSpawnTargetMode = (SpawnerTargetMode)m_editorUtils.EnumPopup("GameObjectSpawnerTargetMode", rule.m_goSpawnTargetMode, helpEnabled);
                if (rule.m_goSpawnTargetMode == SpawnerTargetMode.SingleTransform)
                {
                    rule.m_goSpawnTarget = (Transform)m_editorUtils.ObjectField("GameObjectSpawnTarget", rule.m_goSpawnTarget, typeof(Transform), true);
                }
                else
                {
                    rule.m_terrainGOSpawnTargetName = m_editorUtils.TextField("GameObjectTerrainSpawnTargetName", rule.m_terrainGOSpawnTargetName, helpEnabled);
                }
            }

            m_maskListBeingDrawn = m_spawner.m_settings.m_spawnerRules[spawnRuleID].m_imageMasks;
            EditorGUI.indentLevel++;

            if (m_spawnRuleMaskListExpanded.Count() > spawnRuleID && m_reorderableRuleMasksLists.Count() > spawnRuleID)
            {
                ImageMaskListEditor.DrawMaskList(ref m_spawnRuleMaskListExpanded[spawnRuleID], m_reorderableRuleMasksLists[spawnRuleID], m_editorUtils);
            }
            EditorGUI.indentLevel--;

            if (rule.m_resourceType == SpawnerResourceType.WorldBiomeMask)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (m_editorUtils.Button("ButtonCreateBiome"))
                    {
                        BoundsDouble bounds = new BoundsDouble();
                        TerrainHelper.GetTerrainBounds(ref bounds);
                        SpawnRule[] allBiomeMaskRules = m_spawner.m_settings.m_spawnerRules.Where(x => x.m_resourceType == SpawnerResourceType.WorldBiomeMask).ToArray();
                        BiomePreset biomePreset = m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes[rule.m_resourceIdx].m_biomePreset;

                        if (biomePreset != null)
                        {
                            //Try to find an already existing biome preset in the scene
                            GameObject biomeGO = GameObject.Find(biomePreset.name + " Biome");
                            BiomeController biomeController = null;
                            if (biomeGO == null)
                            {
                                biomeController = biomePreset.CreateBiome(false);
                                biomeGO = biomeController.gameObject;
                            }
                            else
                            {
                                biomeController = biomeGO.GetComponent<BiomeController>();
                            }
                            rule.m_createdBiomeController = biomeController;
                            biomeGO.transform.position = bounds.center;
                            biomeController.m_settings.m_range = Mathf.Round((m_spawner.m_worldCreationSettings.m_tileSize * 1.9f) / 2f);
                            biomeController.m_autoSpawnerArea = AutoSpawnerArea.World;
                            biomeController.m_settings.m_imageMasks = new ImageMask[1] { new ImageMask { m_operation = ImageMaskOperation.WorldBiomeMask, m_selectedWorldBiomeMaskGUID = rule.GUID } };
                            EditorGUIUtility.PingObject(biomeController);
                            GUIUtility.ExitGUI();
                        }

                    }
                    bool currentGUIState = GUI.enabled;
                    if (rule.m_createdBiomeController == null)
                    {
                        GUI.enabled = false;
                    }

                    if (m_editorUtils.Button("ButtonSpawnBiome"))
                    {
                        if (rule.m_createdBiomeController != null)
                        {
                            rule.m_createdBiomeController.m_autoSpawnRequested = true;
                            rule.m_createdBiomeController.m_drawPreview = false;
                            Selection.activeObject = rule.m_createdBiomeController.gameObject;
                        }
                    }
                    GUI.enabled = currentGUIState;
                }
                EditorGUILayout.EndHorizontal();

            }


        }

        private void DrawRuleResourceSettings(SpawnRule rule, int spawnRuleID, bool helpEnabled)
        {
            //Resource type selection is only available for regular spawners
            if (!m_spawner.m_settings.m_isWorldmapSpawner)
            {
                //We do not want to expose all possible resource types on the UI, we therefore cast into a "censored" enum to display on the UI and then cast the selected value back.
                RegularSpawnerResourceType selectedValue = (RegularSpawnerResourceType)(int)rule.m_resourceType;
                selectedValue = (Gaia.GaiaConstants.RegularSpawnerResourceType)EditorGUILayout.EnumPopup(GetLabel("Resource Type"), selectedValue);
                rule.m_resourceType = (SpawnerResourceType)(int)selectedValue;
            }
            //else
            //{
            //    switch (m_spawner.m_settings.m_worldmapSpawnerType)
            //    {
            //        case WorldmapSpawnerType.WorldGenerator:
            //            rule.m_resourceType = SpawnerResourceType.StampDistribution;
            //            break;
            //        case WorldmapSpawnerType.WorldBiomeMasks:
            //            rule.m_resourceType = SpawnerResourceType.WorldBiomeMask;
            //            break;
            //    }

            //}

            GUIContent[] assetChoices = null;
            switch (rule.m_resourceType)
            {
                case GaiaConstants.SpawnerResourceType.TerrainTexture:
                    {
                        assetChoices = new GUIContent[m_spawner.m_settings.m_resources.m_texturePrototypes.Length + 1];
                        for (int assetIdx = 0; assetIdx < m_spawner.m_settings.m_resources.m_texturePrototypes.Length; assetIdx++)
                        {
                            assetChoices[assetIdx] = new GUIContent(m_spawner.m_settings.m_resources.m_texturePrototypes[assetIdx].m_name);
                        }

                        assetChoices[assetChoices.Length - 1] = m_editorUtils.GetContent("AddNewTexture");

                        break;
                    }
                case GaiaConstants.SpawnerResourceType.TerrainDetail:
                    {
                        assetChoices = new GUIContent[m_spawner.m_settings.m_resources.m_detailPrototypes.Length + 1];
                        for (int assetIdx = 0; assetIdx < m_spawner.m_settings.m_resources.m_detailPrototypes.Length; assetIdx++)
                        {
                            assetChoices[assetIdx] = new GUIContent(m_spawner.m_settings.m_resources.m_detailPrototypes[assetIdx].m_name);
                        }
                        assetChoices[assetChoices.Length - 1] = m_editorUtils.GetContent("AddNewTerrainDetail");
                        break;
                    }
                case GaiaConstants.SpawnerResourceType.TerrainTree:
                    {
                        assetChoices = new GUIContent[m_spawner.m_settings.m_resources.m_treePrototypes.Length + 1];
                        for (int assetIdx = 0; assetIdx < m_spawner.m_settings.m_resources.m_treePrototypes.Length; assetIdx++)
                        {
                            assetChoices[assetIdx] = new GUIContent(m_spawner.m_settings.m_resources.m_treePrototypes[assetIdx].m_name);
                        }
                        assetChoices[assetChoices.Length - 1] = m_editorUtils.GetContent("AddNewTree");
                        break;
                    }
                case GaiaConstants.SpawnerResourceType.GameObject:
                    {
                        assetChoices = new GUIContent[m_spawner.m_settings.m_resources.m_gameObjectPrototypes.Length + 1];
                        for (int assetIdx = 0; assetIdx < m_spawner.m_settings.m_resources.m_gameObjectPrototypes.Length; assetIdx++)
                        {
                            assetChoices[assetIdx] = new GUIContent(m_spawner.m_settings.m_resources.m_gameObjectPrototypes[assetIdx].m_name);
                        }
                        assetChoices[assetChoices.Length - 1] = m_editorUtils.GetContent("AddNewGameObject");
                        break;
                    }
                case GaiaConstants.SpawnerResourceType.SpawnExtension:
                    {
                        assetChoices = new GUIContent[m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes.Length + 1];
                        for (int assetIdx = 0; assetIdx < m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes.Length; assetIdx++)
                        {
                            assetChoices[assetIdx] = new GUIContent(m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes[assetIdx].m_name);
                        }
                        assetChoices[assetChoices.Length - 1] = m_editorUtils.GetContent("AddNewSpawnExtension");
                        break;
                    }
                case GaiaConstants.SpawnerResourceType.StampDistribution:
                    {
                        assetChoices = new GUIContent[m_spawner.m_settings.m_resources.m_stampDistributionPrototypes.Length + 1];
                        for (int assetIdx = 0; assetIdx < m_spawner.m_settings.m_resources.m_stampDistributionPrototypes.Length; assetIdx++)
                        {
                            assetChoices[assetIdx] = new GUIContent(m_spawner.m_settings.m_resources.m_stampDistributionPrototypes[assetIdx].m_name);
                        }
                        assetChoices[assetChoices.Length - 1] = m_editorUtils.GetContent("AddNewStampDistribution");
                        break;
                    }
                case GaiaConstants.SpawnerResourceType.WorldBiomeMask:
                    {
                        assetChoices = new GUIContent[m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes.Length + 1];
                        for (int assetIdx = 0; assetIdx < m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes.Length; assetIdx++)
                        {
                            assetChoices[assetIdx] = new GUIContent(m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes[assetIdx].m_name);
                        }
                        assetChoices[assetChoices.Length - 1] = m_editorUtils.GetContent("AddNewWorldBiomeMask");
                        break;
                    }

                    /*
                default:
                    {
                        assetChoices = new GUIContent[m_spawner.m_settings.m_resources.m_stampPrototypes.Length];
                        for (int assetIdx = 0; assetIdx < m_spawner.m_settings.m_resources.m_stampPrototypes.Length; assetIdx++)
                        {
                            assetChoices[assetIdx] = new GUIContent(m_spawner.m_settings.m_resources.m_stampPrototypes[assetIdx].m_name);
                        }
                        break;
                    } */
            }

            //Only allow resource selection on regular spawners, makes no sense for world map spawners
            if (!m_spawner.m_settings.m_isWorldmapSpawner)
            {
                rule.m_resourceIdx = EditorGUILayout.Popup(GetLabel("Selected Resource"), rule.m_resourceIdx, assetChoices);
                rule.m_resourceIdx = Mathf.Clamp(rule.m_resourceIdx, 0, assetChoices.Length - 1);
            }


            switch (rule.m_resourceType)
            {
                case GaiaConstants.SpawnerResourceType.TerrainTexture:
                    //user wants a new resource? Create one on the fly
                    if (rule.m_resourceIdx == assetChoices.Length - 1)
                    {
                        AddNewTextureResource();
                    }

                    rule.m_name = m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_name;
                    break;
                case GaiaConstants.SpawnerResourceType.TerrainDetail:
                    //user wants a new resource? Create one on the fly
                    if (rule.m_resourceIdx == assetChoices.Length - 1)
                    {
                        AddNewTerrainDetailResource();
                    }
                    rule.m_name = m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_name;
                    break;
                case GaiaConstants.SpawnerResourceType.TerrainTree:
                    //user wants a new resource? Create one on the fly
                    if (rule.m_resourceIdx == assetChoices.Length - 1)
                    {
                        AddNewTreeResource();
                    }
                    rule.m_name = m_spawner.m_settings.m_resources.m_treePrototypes[rule.m_resourceIdx].m_name;
                    break;
                case GaiaConstants.SpawnerResourceType.GameObject:
                    //user wants a new resource? Create one on the fly
                    if (rule.m_resourceIdx == assetChoices.Length - 1)
                    {
                        AddNewGameObjectResource();
                    }

                    rule.m_name = m_spawner.m_settings.m_resources.m_gameObjectPrototypes[rule.m_resourceIdx].m_name;

                    //See if we can find a custom fitness
                    if (m_spawner.m_settings.m_resources.m_gameObjectPrototypes[rule.m_resourceIdx].m_instances.Length > 0)
                    {
                        GameObject go = m_spawner.m_settings.m_resources.m_gameObjectPrototypes[rule.m_resourceIdx].m_instances[0].m_desktopPrefab;
                        bool gotExtension = false;
                        //if (go.GetComponent<ISpawnRuleExtension>() != null)
                        //{
                        //    gotExtension = true;
                        //}
                        //else
                        //{
                        //    if (go.GetComponentInChildren<ISpawnRuleExtension>() != null)
                        //    {
                        //        gotExtension = true;
                        //    }
                        //}
                        if (gotExtension)
                        {
                            Debug.Log("Got a spawn rule extension on " + go.name);
                        }
                    }
                    break;
                case GaiaConstants.SpawnerResourceType.SpawnExtension:
                    //user wants a new resource? Create one on the fly
                    if (rule.m_resourceIdx == assetChoices.Length - 1)
                    {
                        AddNewSpawnExtensionResource();
                    }

                    rule.m_name = m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes[rule.m_resourceIdx].m_name;
                    break;
                case SpawnerResourceType.StampDistribution:

                    if (rule.m_resourceIdx == assetChoices.Length - 1)
                    {
                        AddNewStampDistributionResource();
                    }

                    rule.m_name = m_spawner.m_settings.m_resources.m_stampDistributionPrototypes[rule.m_resourceIdx].m_name;
                    break;
                case SpawnerResourceType.WorldBiomeMask:

                    if (rule.m_resourceIdx == assetChoices.Length - 1)
                    {
                        AddNewWorldBiomeMaskResource();
                    }

                    rule.m_name = m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes[rule.m_resourceIdx].m_name;
                    break;
                    /*
                default:
                    {
                        rule.m_name = m_spawner.m_settings.m_resources.m_stampPrototypes[rule.m_resourceIdx].m_name;
                        break;
                    } */
            }

            //Check to see if we can use extended fitness and spawner

            //rule.m_minViableFitness = EditorGUILayout.Slider(GetLabel("Min Viable Fitness"), rule.m_minViableFitness, 0f, 1f);
            //rule.m_failureRate = EditorGUILayout.Slider(GetLabel("Failure Rate"), rule.m_failureRate, 0f, 1f);
            //rule.m_maxInstances = (ulong)EditorGUILayout.LongField(GetLabel("Max Instances"), (long)rule.m_maxInstances);
            //rule.m_ignoreMaxInstances = EditorGUILayout.Toggle(GetLabel("Ignore Max Instances"), rule.m_ignoreMaxInstances);



            if (rule.m_resourceType == GaiaConstants.SpawnerResourceType.TerrainTexture)
            {
                m_textureResourcePrototypeBeingDrawn = m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx];
                //cache the textures to notice if the user changes those, if yes we need to immediately trigger a refresh to switch out the texture on terrain as well
                Texture2D oldDiffuse = m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_texture;
                Texture2D oldNormal = m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_normal;
                Texture2D oldMaskMap = m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_maskmap;
#if SUBSTANCE_PLUGIN_ENABLED
                Substance.Game.Substance substanceMaterial = m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_substanceMaterial;
                int substanceIndex = m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].substanceSourceIndex;
                //m_editorUtils.Panel("Texture Prototype Settings", DrawTexturePrototype, false);
                DrawTexturePrototype(helpEnabled);

                if (m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_substanceMaterial != null)
                {
                    if (substanceMaterial != m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_substanceMaterial || substanceIndex != m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].substanceSourceIndex)
                    {
                        //prototype has changed, refresh on terrain & update resource & rule name
                        RefreshTerrainPrototype(spawnRuleID, oldDiffuse, null, substanceIndex);
                        m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_name = m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_substanceMaterial.name;
                        rule.m_name = m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_name;
                        m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_texture = GetSubstanceTexture(m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx], "baseColor");
                        m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_normal = GetSubstanceTexture(m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx], "normal");
                        m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_maskmap = GetSubstanceTexture(m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx], "mask");
                    }
                }
                else
                {
                    if (oldDiffuse != m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_texture || oldNormal != m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_normal || oldMaskMap != m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_maskmap)
                    {
                        //prototype has changed, refresh on terrain & update resource & rule name
                        RefreshTerrainPrototype(spawnRuleID, oldDiffuse);
                        m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_name = m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_texture.name;
                        rule.m_name = m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_name;
                    }
                }

#else
                //m_editorUtils.Panel("Texture Prototype Settings", DrawTexturePrototype, false);
                DrawTexturePrototype(helpEnabled);

                if (oldDiffuse != m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_texture || oldNormal != m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_normal || oldMaskMap != m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_maskmap)
                {
                    //prototype has changed, refresh on terrain & update resource & rule name
                    RefreshTerrainPrototype(spawnRuleID, oldDiffuse);
                    m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_name = m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_texture.name;
                    rule.m_name = m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_name;
                }
#endif

#if SUBSTANCE_PLUGIN_ENABLED
                substanceMaterial = null;
#endif
                oldDiffuse = null;
                oldNormal = null;
                oldMaskMap = null;

            }

            if (rule.m_resourceType == GaiaConstants.SpawnerResourceType.TerrainDetail)
            {
                Texture2D oldDetailTexture = m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailTexture;
                GameObject oldPrefab = m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailProtoype;
                DetailRenderMode oldRenderMode = m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_renderMode;
                m_terrainDetailPrototypeBeingDrawn = m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx];


                DrawTerrainDetailPrototype(helpEnabled);

                if (oldRenderMode != m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_renderMode ||
                    oldDetailTexture != m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailTexture ||
                    oldPrefab != m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailProtoype)
                {
                    //Texture was changed? Set detail prototype to null to prevent it remaining in the prototype on the terrain
                    if (oldDetailTexture != m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailTexture)
                    {
                        m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailProtoype = null;
                    }
                    else
                    {
                        //Prototype object was changed? Set texture to null to prevent it remaining in the prototype on the terrain
                        if (oldPrefab != m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailProtoype)
                        {
                            m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailTexture = null;
                        }
                    }

                    //prototype has changed, refresh on terrain & update resource & rule name
                    RefreshTerrainPrototype(spawnRuleID, oldDetailTexture, oldPrefab);

                    switch (m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_renderMode)
                    {
                        case DetailRenderMode.GrassBillboard:
                            if (m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailTexture != null)
                            {
                                m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_name = m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailTexture.name;
                            }
                            break;
                        case DetailRenderMode.VertexLit:
                            if (m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailProtoype != null)
                            {
                                m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_name = m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailProtoype.name;
                            }
                            break;
                        case DetailRenderMode.Grass:
                            if (oldDetailTexture != m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailTexture && m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailTexture != null)
                            {
                                m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_name = m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailTexture.name;
                                m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailProtoype = null;
                            }
                            else
                            {
                                if (m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailProtoype != null)
                                {
                                    m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_name = m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailProtoype.name;
                                    m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_detailTexture = null;
                                }
                            }
                            break;
                    }
                    rule.m_name = m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_name;
                }
                oldDetailTexture = null;
                oldPrefab = null;


            }

            if (rule.m_resourceType == GaiaConstants.SpawnerResourceType.TerrainTree)
            {
                GameObject oldDesktopPrefab = m_spawner.m_settings.m_resources.m_treePrototypes[rule.m_resourceIdx].m_desktopPrefab;
                m_treeResourcePrototypeBeingDrawn = m_spawner.m_settings.m_resources.m_treePrototypes[rule.m_resourceIdx];

                DrawTreePrototype(helpEnabled);

                if (oldDesktopPrefab != m_spawner.m_settings.m_resources.m_treePrototypes[rule.m_resourceIdx].m_desktopPrefab)
                {
                    RefreshTerrainPrototype(spawnRuleID, null, oldDesktopPrefab);
                    m_spawner.m_settings.m_resources.m_treePrototypes[rule.m_resourceIdx].m_name = m_spawner.m_settings.m_resources.m_treePrototypes[rule.m_resourceIdx].m_desktopPrefab.name;
                    rule.m_name = m_spawner.m_settings.m_resources.m_treePrototypes[rule.m_resourceIdx].m_name;
                }

            }

            if (rule.m_resourceType == GaiaConstants.SpawnerResourceType.GameObject)
            {
                m_gameObjectResourcePrototypeBeingDrawn = m_spawner.m_settings.m_resources.m_gameObjectPrototypes[rule.m_resourceIdx];

                DrawGameObjectPrototype(helpEnabled);

            }

            if (rule.m_resourceType == GaiaConstants.SpawnerResourceType.SpawnExtension)
            {
                m_spawnExtensionPrototypeBeingDrawn = m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes[rule.m_resourceIdx];

                DrawSpawnerExtensionPrototype(helpEnabled);

            }

            if (rule.m_resourceType == GaiaConstants.SpawnerResourceType.StampDistribution)
            {
                m_stampDistributionPrototypeBeingDrawn = m_spawner.m_settings.m_resources.m_stampDistributionPrototypes[rule.m_resourceIdx];

                DrawStampDistributionPrototype(helpEnabled);

            }

            if (rule.m_resourceType == GaiaConstants.SpawnerResourceType.WorldBiomeMask)
            {
                m_worldBiomeMaskPrototypeBeingDrawn = m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes[rule.m_resourceIdx];

                DrawWorldBiomeMaskPrototype(helpEnabled);

            }
        }

        private int AddNewGameObjectResource()
        {
            string newGameObjectName = m_editorUtils.GetTextValue("NewGameObject");
            int nextNewNumber = m_spawner.m_settings.m_resources.m_gameObjectPrototypes.Where(x => x.m_name.StartsWith(newGameObjectName)).Count() + 1;
            m_spawner.m_settings.m_resources.m_gameObjectPrototypes = GaiaUtils.AddElementToArray(m_spawner.m_settings.m_resources.m_gameObjectPrototypes, new ResourceProtoGameObject() { m_name = newGameObjectName + " " + nextNewNumber.ToString() });
            return m_spawner.m_settings.m_resources.m_gameObjectPrototypes.Count() - 1;
        }

        private int AddNewSpawnExtensionResource()
        {
            string newSpawnExtension = m_editorUtils.GetTextValue("NewSpawnExtension");
            int nextNewNumber = m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes.Where(x => x.m_name.StartsWith(newSpawnExtension)).Count() + 1;
            m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes = GaiaUtils.AddElementToArray(m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes, new ResourceProtoSpawnExtension() { m_name = newSpawnExtension + " " + nextNewNumber.ToString() });
            return m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes.Count() - 1;
        }

        private int AddNewStampDistributionResource()
        {
            string newStampDistribution = m_editorUtils.GetTextValue("NewStampDistribution");
            int nextNewNumber = m_spawner.m_settings.m_resources.m_stampDistributionPrototypes.Where(x => x.m_name.StartsWith(newStampDistribution)).Count() + 1;
            m_spawner.m_settings.m_resources.m_stampDistributionPrototypes = GaiaUtils.AddElementToArray(m_spawner.m_settings.m_resources.m_stampDistributionPrototypes, new ResourceProtoStampDistribution() { m_name = newStampDistribution + " " + nextNewNumber.ToString() });
            return m_spawner.m_settings.m_resources.m_stampDistributionPrototypes.Count() - 1;
        }

        private int AddNewWorldBiomeMaskResource()
        {
            string newWorldBiomeMask = m_editorUtils.GetTextValue("NewWorldBiomeMask");
            int nextNewNumber = m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes.Where(x => x.m_name.StartsWith(newWorldBiomeMask)).Count() + 1;
            m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes = GaiaUtils.AddElementToArray(m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes, new ResourceProtoWorldBiomeMask() { m_name = newWorldBiomeMask + " " + nextNewNumber.ToString() });
            return m_spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes.Count() - 1;
        }

        private int AddNewTreeResource()
        {
            string newTreeName = m_editorUtils.GetTextValue("NewTree");
            int nextNewNumber = m_spawner.m_settings.m_resources.m_treePrototypes.Where(x => x.m_name.StartsWith(newTreeName)).Count() + 1;
            m_spawner.m_settings.m_resources.m_treePrototypes = GaiaUtils.AddElementToArray(m_spawner.m_settings.m_resources.m_treePrototypes, new ResourceProtoTree() { m_name = newTreeName + " " + nextNewNumber.ToString() });
            return m_spawner.m_settings.m_resources.m_treePrototypes.Count() - 1;
        }

        private int AddNewTerrainDetailResource()
        {
            string newDetailName = m_editorUtils.GetTextValue("NewTerrainDetail");
            int nextNewNumber = m_spawner.m_settings.m_resources.m_detailPrototypes.Where(x => x.m_name.StartsWith(newDetailName)).Count() + 1;
            m_spawner.m_settings.m_resources.m_detailPrototypes = GaiaUtils.AddElementToArray(m_spawner.m_settings.m_resources.m_detailPrototypes, new ResourceProtoDetail() { m_name = newDetailName + " " + nextNewNumber.ToString() });
            return m_spawner.m_settings.m_resources.m_detailPrototypes.Count() - 1;
        }

        private int AddNewTextureResource()
        {
            string newTextureName = m_editorUtils.GetTextValue("NewTexture");
            int nextNewNumber = m_spawner.m_settings.m_resources.m_texturePrototypes.Where(x => x.m_name.StartsWith(newTextureName)).Count() + 1;
            m_spawner.m_settings.m_resources.m_texturePrototypes = GaiaUtils.AddElementToArray(m_spawner.m_settings.m_resources.m_texturePrototypes, new ResourceProtoTexture() { m_name = newTextureName + " " + nextNewNumber.ToString() });
            return m_spawner.m_settings.m_resources.m_texturePrototypes.Count() - 1;
        }

        public static Texture2D GetSpawnRulePreviewTexture(SpawnRule rule, GaiaResource resource)
        {
            Texture2D resourceTexture = null;

            //draw preview
            switch (rule.m_resourceType)
            {
                case GaiaConstants.SpawnerResourceType.TerrainTexture:
                    if (rule.m_resourceIdx < resource.m_texturePrototypes.Length && resource.m_texturePrototypes[rule.m_resourceIdx] != null)
                    {
                        resourceTexture = AssetPreview.GetAssetPreview(resource.m_texturePrototypes[rule.m_resourceIdx].m_texture);
                    }
                    break;
                case GaiaConstants.SpawnerResourceType.TerrainDetail:
                    if (rule.m_resourceIdx < resource.m_detailPrototypes.Length && resource.m_detailPrototypes[rule.m_resourceIdx] != null)
                    {
                        ResourceProtoDetail protoDetail = resource.m_detailPrototypes[rule.m_resourceIdx];
                        switch (protoDetail.m_renderMode)
                        {
                            case DetailRenderMode.Grass:
                                resourceTexture = protoDetail.m_detailTexture;
                                break;
                            case DetailRenderMode.GrassBillboard:
                                resourceTexture = protoDetail.m_detailTexture;
                                break;
                            case DetailRenderMode.VertexLit:
                                resourceTexture = AssetPreview.GetAssetPreview(protoDetail.m_detailProtoype);
                                break;
                        }
                    }
                    break;

                case GaiaConstants.SpawnerResourceType.TerrainTree:
                    if (rule.m_resourceIdx < resource.m_treePrototypes.Length && resource.m_treePrototypes[rule.m_resourceIdx] != null)
                    {
                        GameObject protoTree = resource.m_treePrototypes[rule.m_resourceIdx].m_desktopPrefab;
                        resourceTexture = AssetPreview.GetAssetPreview(protoTree);
                    }
                    break;
                case GaiaConstants.SpawnerResourceType.GameObject:
                    if (rule.m_resourceIdx < resource.m_gameObjectPrototypes.Length && resource.m_gameObjectPrototypes[rule.m_resourceIdx] != null)
                    {
                        //Get the first instance as preview object for now
                        if (resource.m_gameObjectPrototypes[rule.m_resourceIdx].m_instances.Length > 0)
                        {
                            GameObject protoGameObject = resource.m_gameObjectPrototypes[rule.m_resourceIdx].m_instances[0].m_desktopPrefab;
                            resourceTexture = AssetPreview.GetAssetPreview(protoGameObject);
                        }
                    }
                    break;
            }

            return resourceTexture;
        }

        private void CleanPreviewRuleIDs()
        {
            for (int i = m_spawner.m_previewRuleIds.Count - 1; i >= 0; i--)
            {
                if (m_spawner.m_previewRuleIds[i] > m_spawner.m_settings.m_spawnerRules.Count - 1)
                {
                    m_spawner.m_previewRuleIds.RemoveAt(i);
                }
            }
        }

        private void RemoveMaskList(int ruleIdx)
        {
            if (ruleIdx < 0 || ruleIdx >= m_reorderableRuleMasksLists.Length)
                return;
            ReorderableList[] newList = new ReorderableList[m_reorderableRuleMasksLists.Length - 1];
            for (int i = 0; i < newList.Length; ++i)
            {
                if (i < ruleIdx)
                {
                    newList[i] = m_reorderableRuleMasksLists[i];
                }
                else if (i >= ruleIdx)
                {
                    newList[i] = m_reorderableRuleMasksLists[i + 1];
                }
            }
            m_reorderableRuleMasksLists = newList;

            if (ruleIdx >= m_spawnRuleMaskListExpanded.Length)
                return;

            bool[] newExpandedList = new bool[m_spawnRuleMaskListExpanded.Length - 1];
            for (int i = 0; i < newExpandedList.Length; ++i)
            {
                if (i < ruleIdx)
                {
                    newExpandedList[i] = m_spawnRuleMaskListExpanded[i];
                }
                else if (i >= ruleIdx)
                {
                    newExpandedList[i] = m_spawnRuleMaskListExpanded[i + 1];
                }
            }
            m_spawnRuleMaskListExpanded = newExpandedList;
        }

        private void AddNewMaskList()
        {
            ReorderableList[] newList = new ReorderableList[m_reorderableRuleMasksLists.Length + 1];
            for (int i = 0; i < m_reorderableRuleMasksLists.Length; ++i)
            {
                newList[i] = m_reorderableRuleMasksLists[i];
            }
            newList[newList.Length - 1] = CreateSpawnRuleMaskList(newList[newList.Length - 1], m_spawner.m_settings.m_spawnerRules[newList.Length - 1].m_imageMasks);
            m_reorderableRuleMasksLists = newList;

            bool[] newExpandedList = new bool[m_spawnRuleMaskListExpanded.Length + 1];
            for (int i = 0; i < m_spawnRuleMaskListExpanded.Length; ++i)
            {
                newExpandedList[i] = m_spawnRuleMaskListExpanded[i];
            }

            newExpandedList[newExpandedList.Length - 1] = true;
            m_spawnRuleMaskListExpanded = newExpandedList;
        }

        private void RefreshTerrainPrototype(int spawnRuleID, Texture2D oldTexture = null, GameObject oldGameObject = null, int substanceIdx = 0, bool isUserRefresh = false)
        {
            Terrain activeTerrain = Gaia.TerrainHelper.GetActiveTerrain();

            if (activeTerrain == null)
            {
                return;
            }
            if (activeTerrain.terrainData == null)
            {
                return;
            }

            SpawnRule sr = m_spawner.m_settings.m_spawnerRules[spawnRuleID];
            switch (sr.m_resourceType)
            {
                case SpawnerResourceType.TerrainTexture:
                    int texturePrototypeID = -1;

                    //look up prototype ID based on the old texture, if available it means the user wants to switch out textures
                    int localTerrainTextureIdx = 0;

                    foreach (TerrainLayer proto in activeTerrain.terrainData.terrainLayers)
                    {
                        if (oldTexture != null)
                        {
                            if (PWCommon2.Utils.IsSameTexture(oldTexture, proto.diffuseTexture, false))
                            {
                                texturePrototypeID = localTerrainTextureIdx;
                                break;
                            }
                        }
                        else
                        {
                            texturePrototypeID = m_spawner.m_settings.m_resources.PrototypeIdxInTerrain(SpawnerResourceType.TerrainTexture, sr.m_resourceIdx);
                        }

                        localTerrainTextureIdx++;
                    }

                    if (texturePrototypeID != -1)
                    {
                        foreach (Terrain t in Terrain.activeTerrains)
                        {
                            ResourceProtoTexture resourceProtoTexture = m_spawner.m_settings.m_resources.m_texturePrototypes[sr.m_resourceIdx];
                            //reference the exisiting prototypes, then assign them - otherwise the terrain details won't update properly
                            TerrainLayer[] exisitingLayers = t.terrainData.terrainLayers;
#if SUBSTANCE_PLUGIN_ENABLED
                            if (resourceProtoTexture.m_substanceMaterial != null)
                            {
                                List<Substance.Game.SubstanceGraph> substanceGraphs = resourceProtoTexture.m_substanceMaterial.graphs;
                                if (substanceGraphs != null)
                                {
                                    if (resourceProtoTexture.substanceSourceIndex == 0)
                                    {
                                        resourceProtoTexture.substanceSourceIndex = 1;
                                    }
                                    List<Texture2D> substanceTextures = substanceGraphs[resourceProtoTexture.substanceSourceIndex - 1].GetGeneratedTextures();
                                    if (substanceTextures.Count > 0)
                                    {
                                        foreach (Texture2D texture in substanceTextures)
                                        {
                                            if (texture.name.EndsWith("baseColor"))
                                            {
                                                exisitingLayers[texturePrototypeID].diffuseTexture = texture;
                                            }
                                            else if (texture.name.EndsWith("normal"))
                                            {
                                                if (texture.format == TextureFormat.BC5)
                                                {
                                                    Debug.LogWarning("Normal compress format is BC5 please select the substance material and select DXT5");
                                                }
                                                exisitingLayers[texturePrototypeID].normalMapTexture = texture;
                                            }
                                            else if (texture.name.EndsWith("mask"))
                                            {
                                                exisitingLayers[texturePrototypeID].maskMapTexture = texture;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogError("No substance textures found at graph index 0. Please make sure you've generated your substance material");
                                    }
                                }
                            }
                            else
                            {
                                exisitingLayers[texturePrototypeID].diffuseTexture = resourceProtoTexture.m_texture;
                                exisitingLayers[texturePrototypeID].normalMapTexture = resourceProtoTexture.m_normal;
                                exisitingLayers[texturePrototypeID].maskMapTexture = resourceProtoTexture.m_maskmap;
                            }
#else
                            exisitingLayers[texturePrototypeID].diffuseTexture = resourceProtoTexture.m_texture;
                            exisitingLayers[texturePrototypeID].normalMapTexture = resourceProtoTexture.m_normal;
                            exisitingLayers[texturePrototypeID].maskMapTexture = resourceProtoTexture.m_maskmap;
#endif
                            exisitingLayers[texturePrototypeID].tileSize = new Vector2(resourceProtoTexture.m_sizeX, resourceProtoTexture.m_sizeY);
                            exisitingLayers[texturePrototypeID].tileOffset = new Vector2(resourceProtoTexture.m_offsetX, resourceProtoTexture.m_offsetY);
                            exisitingLayers[texturePrototypeID].normalScale = resourceProtoTexture.m_normalScale;
                            exisitingLayers[texturePrototypeID].metallic = resourceProtoTexture.m_metallic;
                            exisitingLayers[texturePrototypeID].smoothness = resourceProtoTexture.m_smoothness;
                            t.terrainData.terrainLayers = exisitingLayers;
                        }
                    }
                    else
                    {
                        if (isUserRefresh)
                        {
                            //Prototype was not found
                            if (EditorUtility.DisplayDialog("Texture not found on Terrain?", "It looks like this texture is not part of a terrain layer on the current terrain because it has not been spawned on this terrain yet. Do you want to add this texture to all currently active terrains in the scene now?", "Add the texture", "Cancel"))
                            {
                                m_spawner.m_settings.m_resources.AddPrototypeToTerrain(sr.m_resourceType, sr.m_resourceIdx, Terrain.activeTerrains);
                            }
                        }
                    }
                    break;
                case SpawnerResourceType.TerrainDetail:

                    int detailPrototypeID = -1;

                    //look up prototype ID based on the old texture, if available it means the user wants to switch out textures
                    if (oldTexture != null || oldGameObject != null)
                    {
                        int localTerrainIdx = 0;
                        foreach (DetailPrototype proto in activeTerrain.terrainData.detailPrototypes)
                        {
                            if (m_spawner.m_settings.m_resources.m_detailPrototypes[sr.m_resourceIdx].m_renderMode == DetailRenderMode.VertexLit || (m_spawner.m_settings.m_resources.m_detailPrototypes[sr.m_resourceIdx].m_renderMode == DetailRenderMode.Grass && oldGameObject != null))
                            {
                                if (oldGameObject == proto.prototype)
                                {
                                    detailPrototypeID = localTerrainIdx;
                                    break;
                                }
                            }
                            else
                            {
                                if (PWCommon2.Utils.IsSameTexture(oldTexture, proto.prototypeTexture, false) == true)
                                {
                                    detailPrototypeID = localTerrainIdx;
                                    break;
                                }
                            }
                            localTerrainIdx++;
                        }
                    }
                    else
                    {
                        detailPrototypeID = m_spawner.m_settings.m_resources.PrototypeIdxInTerrain(SpawnerResourceType.TerrainDetail, sr.m_resourceIdx);
                    }
                    if (detailPrototypeID != -1)
                    {
                        foreach (Terrain t in Terrain.activeTerrains)
                        {
                            ResourceProtoDetail resourceProtoDetail = m_spawner.m_settings.m_resources.m_detailPrototypes[sr.m_resourceIdx];
                            //reference the exisiting prototypes, then assign them - otherwise the terrain details won't update properly
                            DetailPrototype[] exisitingPrototypes = t.terrainData.detailPrototypes;
                            exisitingPrototypes[detailPrototypeID].bendFactor = resourceProtoDetail.m_bendFactor;
                            exisitingPrototypes[detailPrototypeID].dryColor = resourceProtoDetail.m_dryColour;
                            exisitingPrototypes[detailPrototypeID].healthyColor = resourceProtoDetail.m_healthyColour;
                            exisitingPrototypes[detailPrototypeID].maxHeight = resourceProtoDetail.m_maxHeight;
                            exisitingPrototypes[detailPrototypeID].minHeight = resourceProtoDetail.m_minHeight;
                            exisitingPrototypes[detailPrototypeID].minWidth = resourceProtoDetail.m_maxWidth;
                            exisitingPrototypes[detailPrototypeID].maxWidth = resourceProtoDetail.m_minWidth;
                            exisitingPrototypes[detailPrototypeID].noiseSpread = resourceProtoDetail.m_noiseSpread;
                            exisitingPrototypes[detailPrototypeID].prototype = resourceProtoDetail.m_detailProtoype;
                            exisitingPrototypes[detailPrototypeID].prototypeTexture = resourceProtoDetail.m_detailTexture;
                            exisitingPrototypes[detailPrototypeID].renderMode = resourceProtoDetail.m_renderMode;
                            if (resourceProtoDetail.m_detailProtoype != null)
                            {
                                exisitingPrototypes[detailPrototypeID].usePrototypeMesh = true;
                            }
                            else
                            {
                                exisitingPrototypes[detailPrototypeID].usePrototypeMesh = false;
                            }
                            t.terrainData.detailPrototypes = exisitingPrototypes;
                            //t.terrainData.detailPrototypes[prototypeID].usePrototypeMesh = resourceProtoDetail.pr;
                        }
                    }
                    else
                    {
                        if (isUserRefresh)
                        {
                            //Prototype was not found
                            if (EditorUtility.DisplayDialog("Detail Prototype not found on Terrain?", "It looks like this detail prototype is not part of the terrain details on the current terrain because it has not been spawned on this terrain yet. Do you want to add this detail prototype to all currently active terrains in the scene now?", "Add the prototype", "Cancel"))
                            {
                                m_spawner.m_settings.m_resources.AddPrototypeToTerrain(sr.m_resourceType, sr.m_resourceIdx, Terrain.activeTerrains);
                            }
                        }
                    }
                    break;

                case SpawnerResourceType.TerrainTree:

                    int treePrototypeID = -1;

                    //look up prototype ID based on the old texture, if available it means the user wants to switch out textures
                    if (oldGameObject != null)
                    {
                        int localTerrainIdx = 0;
                        foreach (TreePrototype proto in activeTerrain.terrainData.treePrototypes)
                        {
                            if (oldGameObject == proto.prefab)
                            {
                                treePrototypeID = localTerrainIdx;
                                break;
                            }
                            localTerrainIdx++;
                        }
                    }
                    else
                    {
                        treePrototypeID = m_spawner.m_settings.m_resources.PrototypeIdxInTerrain(SpawnerResourceType.TerrainDetail, sr.m_resourceIdx);
                    }
                    if (treePrototypeID != -1)
                    {
                        foreach (Terrain t in Terrain.activeTerrains)
                        {
                            ResourceProtoTree resourceProtoTree = m_spawner.m_settings.m_resources.m_treePrototypes[sr.m_resourceIdx];
                            //reference the exisiting prototypes, then assign them - otherwise the terrain details won't update properly
                            TreePrototype[] exisitingPrototypes = t.terrainData.treePrototypes;
                            exisitingPrototypes[treePrototypeID].bendFactor = resourceProtoTree.m_bendFactor;
                            exisitingPrototypes[treePrototypeID].prefab = resourceProtoTree.m_desktopPrefab;
                            t.terrainData.treePrototypes = exisitingPrototypes;
                        }
                    }
                    else
                    {
                        if (isUserRefresh)
                        {
                            //Prototype was not found
                            if (EditorUtility.DisplayDialog("Tree not found on Terrain?", "It looks like this tree is not part of the tree prototypes on the current terrain because it has not been spawned on this terrain yet. Do you want to add this tree prototype to all currently active terrains in the scene now?", "Add the prototype", "Cancel"))
                            {
                                m_spawner.m_settings.m_resources.AddPrototypeToTerrain(sr.m_resourceType, sr.m_resourceIdx, Terrain.activeTerrains);
                            }
                        }
                    }
                    break;
            }
        }

#if SUBSTANCE_PLUGIN_ENABLED
        private Texture2D GetSubstanceTexture(ResourceProtoTexture resource, string textureSearch)
        {
            Texture2D returningTexture = null;
            List<Substance.Game.SubstanceGraph> substanceGraphs = resource.m_substanceMaterial.graphs;
            if (substanceGraphs != null)
            {
                List<Texture2D> substanceTextures = substanceGraphs[resource.substanceSourceIndex - 1].GetGeneratedTextures();
                if (substanceTextures.Count > 0)
                {
                    foreach (Texture2D texture in substanceTextures)
                    {
                        if (texture.name.EndsWith(textureSearch))
                        {
                            returningTexture = texture;
                            break;
                        }
                    }
                }
                else
                {
                    Debug.LogError("No substance textures found at graph index " + resource.substanceSourceIndex + "." + "Please make sure you've generated your substance material");
                }
            }

            return returningTexture;
        }
#endif

        private void DrawTexturePrototype(bool showHelp)
        {
            GaiaResourceEditor.DrawTexturePrototype(m_textureResourcePrototypeBeingDrawn, m_editorUtils, showHelp);
            if (m_editorUtils.Button("ResourceProtoRefreshButton"))
            {
                RefreshTerrainPrototype(m_spawnRuleIndexBeingDrawn, null, null, 0, true);
            }
        }

        private void DrawTreePrototype(bool showHelp)
        {
            GaiaResourceEditor.DrawTreePrototype(m_treeResourcePrototypeBeingDrawn, m_editorUtils, showHelp);
            if (m_editorUtils.Button("ResourceProtoRefreshButton"))
            {
                RefreshTerrainPrototype(m_spawnRuleIndexBeingDrawn, null, null, 0, true);
            }
        }

        private void DrawTerrainDetailPrototype(bool showHelp)
        {
            GaiaResourceEditor.DrawTerrainDetailPrototype(m_terrainDetailPrototypeBeingDrawn, m_editorUtils, showHelp);
            if (m_editorUtils.Button("ResourceProtoRefreshButton"))
            {
                RefreshTerrainPrototype(m_spawnRuleIndexBeingDrawn, null, null, 0, true);
            }
        }


        private void DrawGameObjectPrototype(bool showHelp)
        {
            GaiaResourceEditor.DrawGameObjectPrototype(m_gameObjectResourcePrototypeBeingDrawn, m_editorUtils, showHelp);
        }

        private void DrawSpawnerExtensionPrototype(bool showHelp)
        {
            GaiaResourceEditor.DrawSpawnExtensionPrototype(m_spawnExtensionPrototypeBeingDrawn, m_editorUtils, showHelp);
        }

        private void DrawStampDistributionPrototype(bool showHelp)
        {
            GaiaResourceEditor.DrawStampDistributionPrototype(m_stampDistributionPrototypeBeingDrawn, m_editorUtils, m_stampCategoryNames, m_stampCategoryIDArray, showHelp);
        }

        private void DrawWorldBiomeMaskPrototype(bool showHelp)
        {
            GaiaResourceEditor.DrawWorldBiomeMaskPrototype(m_worldBiomeMaskPrototypeBeingDrawn, m_editorUtils, showHelp);
        }

        private void DrawSaveAndLoad(bool obj)
        {
            GUI.backgroundColor = m_normalBGColor;
            if (m_spawner.m_createdfromBiomePreset)
            {
                m_SaveAndLoadMessage = m_editorUtils.GetTextValue("CreatedFromBiomePresetMessage");
                m_SaveAndLoadMessageType = MessageType.Warning;
            }

            if (m_spawner.m_createdFromGaiaManager)
            {
                m_SaveAndLoadMessage = m_editorUtils.GetTextValue("CreatedFromGaiaManagerMessage");
                m_SaveAndLoadMessageType = MessageType.Warning;
            }

            if (m_SaveAndLoadMessage != "")
                EditorGUILayout.HelpBox(m_SaveAndLoadMessage, m_SaveAndLoadMessageType, true);

            EditorGUILayout.BeginHorizontal();
            if (m_editorUtils.Button("LoadButton"))
            {
                //Dismiss Tutorial messages at this point
                m_spawner.m_createdfromBiomePreset = false;
                m_spawner.m_createdFromGaiaManager = false;

                string path = m_spawner.m_settings.lastSavePath;
                if (string.IsNullOrEmpty(path))
                {
                    path = GaiaDirectories.GetSettingsDirectory();
                }
                path = path.Remove(path.LastIndexOf(Path.AltDirectorySeparatorChar));

                string openFilePath = EditorUtility.OpenFilePanel("Load Spawner settings..", path, "asset");

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
                    SpawnerSettings settingsToLoad = (SpawnerSettings)AssetDatabase.LoadAssetAtPath(openFilePath, typeof(SpawnerSettings));

                    if (settingsToLoad != null)
                    {
                        //Load in the resource file that was last used first

                        //settingsToLoad.m_resourcesPath = AssetDatabase.GUIDToAssetPath(settingsToLoad.m_resourcesGUID);

                        m_spawner.LoadSettings(settingsToLoad);

                        //m_spawner.m_settings.m_resources = (GaiaResource)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(settingsToLoad.m_resourcesGUID), typeof(GaiaResource));

                        CreateMaskLists();
                        //Reset Preview Images state
                        m_previewImageDisplayedDuringLayout = new bool[m_spawner.m_settings.m_spawnerRules.Count];
                        //Update the internal editor position / scale values after loading
                        //x = m_stamper.m_settings.m_x;
                        //y = m_stamper.m_settings.m_y;
                        //z = m_stamper.m_settings.m_z;
                        //rotation = m_stamper.m_settings.m_rotation;
                        //width = m_stamper.m_settings.m_width;
                        //height = m_stamper.m_settings.m_height;
                        //mark stamper as dirty so it will be redrawn
                        m_spawner.m_spawnPreviewDirty = true;
                        m_spawner.SetWorldBiomeMasksDirty();
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
                //Dismiss Tutorial messages at this point
                m_spawner.m_createdfromBiomePreset = false;
                m_spawner.m_createdFromGaiaManager = false;

                string dialogPath = m_spawner.m_settings.lastSavePath;
                string filename = m_spawner.transform.name;
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
                    //Update with newest GUID of resource file
                    //m_spawner.m_settings.m_resourcesGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_spawner.m_settings.m_resources));


                    // Check if there is already an asset in this path
                    //SpawnerSettings settingsToLoad = (SpawnerSettings)AssetDatabase.LoadAssetAtPath(saveFilePath, typeof(SpawnerSettings));

                    //if (settingsToLoad == null)
                    //{
                    //    //asset does not exist yet => we need to create a new one.

                    //}
                    //else
                    //{
                    //    //asset does exist already => we need to overwrite the settings in there without creating a new file, otherwise other files such as 
                    //    //Biome spawners might lose references to this spawner settings file.
                    //    settingsToLoad = Instantiate(m_spawner.m_settings);
                    //    EditorUtility.SetDirty(settingsToLoad);
                    //    AssetDatabase.SaveAssets();
                    //}
                    m_spawner.m_settings.lastSavePath = saveFilePath;
                    AssetDatabase.CreateAsset(m_spawner.m_settings, saveFilePath);
                    AssetDatabase.ImportAsset(saveFilePath);

                    //Check if save was successful
                    SpawnerSettings settingsToLoad = (SpawnerSettings)AssetDatabase.LoadAssetAtPath(saveFilePath, typeof(SpawnerSettings));
                    if (settingsToLoad != null)
                    {
                        m_SaveAndLoadMessage = m_editorUtils.GetContent("SaveSuccessful").text;
                        m_changesMadeSinceLastSave = false;
                        m_SaveAndLoadMessageType = MessageType.Info;
                        //dissociate the current stamper settings from the file we just saved, otherwise the user will continue editing the file afterwards
                        //We do this by just loading the file in again we just created
                        m_spawner.LoadSettings(settingsToLoad);
                        CreateMaskLists();
                        m_spawner.m_spawnPreviewDirty = true;
                        m_spawner.SetWorldBiomeMasksDirty();
                        //update the gaia manager window (if exists)

                        foreach (GaiaManagerEditor gme in Resources.FindObjectsOfTypeAll<GaiaManagerEditor>())
                        {
                            gme.UpdateAllSpawnersList();
                        }


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




        public void Spawn(bool allTerrains)
        {
            bool cancel = true;

            //Check that they have at least one single selected terrain when doing a local spawn
            if (!allTerrains && Gaia.TerrainHelper.GetActiveTerrainCount() < 1)
            {
                EditorUtility.DisplayDialog("OOPS!", "You must have at least one terrain visible in order to do a local spawn. Please place your spawner over a terraon or add a new terrain from the Gaia Manager Window.", "OK");
                return;
            }
            else
            {
                cancel = false;
            }

            //Check that the resources are in the terrain
            //if (!cancel)
            //{
            //    //Check not required for world maps
            //    if (!m_spawner.m_settings.m_isWorldmapSpawner)
            //    {
            //        cancel = m_spawner.CheckForMissingResources(allTerrains);
            //        if (cancel)
            //        {
            //            Debug.Log("Spawner " + m_spawner.name + " is missing resources on the terrain, spawn was cancelled. Please deactivate the rule that contains the missing resources, or let the Spawner add the missing resources to the terrain.");
            //        }
            //    }


            //}

            if (!cancel)
            {
                //deactivate preview - so that we can see the result

                if (m_gaiaSettings.m_spawnerAutoHidePreviewMilliseconds > 0)
                {
                    m_activatePreviewRequested = true;
                    m_activatePreviewTimeStamp = GaiaUtils.GetUnixTimestamp();
                }
                m_spawner.m_drawPreview = false;

                //int totalNumberOfSpawnRules = m_spawner.m_settings.m_spawnerRules.Where(x => x.m_isActive == true).Count();
                //if (allTerrains)
                //{
                //    totalNumberOfSpawnRules *= m_spawner.GetWorldSpawnSteps();
                //} 

                m_spawner.Spawn(allTerrains);
                //GaiaUtils.ClearProgressBarNoEditor();
            }


        }




        /// <summary>
        /// Delete any old rules left over from previous resources / changes to resources
        /// </summary>
        void CleanUpRules()
        {
            //Drop out if no spawner or resources
            if (m_spawner == null || m_spawner.m_settings.m_resources == null)
            {
                return;
            }

            //Drop out if spawner doesnt have resources
            int idx = 0;
            SpawnRule rule;
            bool dirty = false;
            while (idx < m_spawner.m_settings.m_spawnerRules.Count)
            {
                rule = m_spawner.m_settings.m_spawnerRules[idx];

                switch (rule.m_resourceType)
                {
                    case GaiaConstants.SpawnerResourceType.TerrainTexture:
                        {
                            if (rule.m_resourceIdx >= m_spawner.m_settings.m_resources.m_texturePrototypes.Length)
                            {
                                m_spawner.m_settings.m_spawnerRules.RemoveAt(idx);
                                dirty = true;
                            }
                            else
                            {
                                if (rule.m_name != m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_name)
                                {
                                    rule.m_name = m_spawner.m_settings.m_resources.m_texturePrototypes[rule.m_resourceIdx].m_name;
                                    dirty = true;
                                }
                                idx++;
                            }
                            break;
                        }
                    case GaiaConstants.SpawnerResourceType.TerrainDetail:
                        {
                            if (rule.m_resourceIdx >= m_spawner.m_settings.m_resources.m_detailPrototypes.Length)
                            {
                                m_spawner.m_settings.m_spawnerRules.RemoveAt(idx);
                                dirty = true;
                            }
                            else
                            {
                                if (rule.m_name != m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_name)
                                {
                                    rule.m_name = m_spawner.m_settings.m_resources.m_detailPrototypes[rule.m_resourceIdx].m_name;
                                    dirty = true;
                                }
                                idx++;
                            }
                            break;
                        }
                    case GaiaConstants.SpawnerResourceType.TerrainTree:
                        {
                            if (rule.m_resourceIdx >= m_spawner.m_settings.m_resources.m_treePrototypes.Length)
                            {
                                m_spawner.m_settings.m_spawnerRules.RemoveAt(idx);
                                dirty = true;
                            }
                            else
                            {
                                if (rule.m_name != m_spawner.m_settings.m_resources.m_treePrototypes[rule.m_resourceIdx].m_name)
                                {
                                    rule.m_name = m_spawner.m_settings.m_resources.m_treePrototypes[rule.m_resourceIdx].m_name;
                                    dirty = true;
                                }
                                idx++;
                            }
                            break;
                        }
                    case GaiaConstants.SpawnerResourceType.GameObject:
                        {
                            if (rule.m_resourceIdx >= m_spawner.m_settings.m_resources.m_gameObjectPrototypes.Length)
                            {
                                m_spawner.m_settings.m_spawnerRules.RemoveAt(idx);
                                dirty = true;
                            }
                            else
                            {
                                if (rule.m_name != m_spawner.m_settings.m_resources.m_gameObjectPrototypes[rule.m_resourceIdx].m_name)
                                {
                                    rule.m_name = m_spawner.m_settings.m_resources.m_gameObjectPrototypes[rule.m_resourceIdx].m_name;
                                    dirty = true;
                                }
                                idx++;
                            }
                            break;
                        }
                    case GaiaConstants.SpawnerResourceType.SpawnExtension:
                        {
                            if (rule.m_resourceIdx >= m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes.Length)
                            {
                                m_spawner.m_settings.m_spawnerRules.RemoveAt(idx);
                                dirty = true;
                            }
                            else
                            {
                                if (rule.m_name != m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes[rule.m_resourceIdx].m_name)
                                {
                                    rule.m_name = m_spawner.m_settings.m_resources.m_spawnExtensionPrototypes[rule.m_resourceIdx].m_name;
                                    dirty = true;
                                }
                                idx++;
                            }
                            break;
                        }

                    default:
                        idx++;
                        break;

                }
            }
            //Mark it as dirty if we deleted something
            if (dirty)
            {
                Debug.LogWarning(string.Format("{0} : There was a mismatch between your spawner settings and your resources file. Spawner settings have been updated to match resources.", m_spawner.name));
                EditorUtility.SetDirty(m_spawner);
            }
        }

        ///// <summary>
        ///// Draw a progress bar
        ///// </summary>
        ///// <param name="label"></param>
        ///// <param name="value"></param>

        //void ProgressBar(string label, float value)
        //{
        //    // Get a rect for the progress bar using the same margins as a textfield:
        //    Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
        //    EditorGUI.ProgressBar(rect, value, label);
        //    EditorGUILayout.Space();
        //}

        /// <summary>
        /// Handy layer mask interface
        /// </summary>
        /// <param name="label"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        static LayerMask LayerMaskField(GUIContent label, LayerMask layerMask)
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

        /// <summary>
        /// Get a content label - look the tooltip up if possible
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        GUIContent GetLabel(string name)
        {
            string tooltip = "";
            if (m_showTooltips && m_tooltips.TryGetValue(name, out tooltip))
            {
                return new GUIContent(name, tooltip);
            }
            else
            {
                return new GUIContent(name);
            }
        }

        /// <summary>
        /// The tooltips
        /// </summary>
        static Dictionary<string, string> m_tooltips = new Dictionary<string, string>
        {
            { "Resources", "The object that contains the resources that these rules will apply to." },
            { "Execution Mode", "The way this spawner runs. Design time : At design time only. Runtime Interval : At run time on a timed interval. Runtime Triggered Interval : At run time on a timed interval, and only when the tagged game object is closer than the trigger range from the center of the spawner." },
            { "Shape", "The shape of the spawn area. The spawner will only spawn within this area." },
            { "Range","Distance in meters from the centre of the spawner that the spawner can spawn in. Shown as a red box or sphere in the gizmos." },
            { "Spawn Interval", "The time in seconds between spawn iterations." },
            { "Trigger Range","Distance in meters from the centre of the spawner that the trigger will activate." },
            { "Trigger Tags","The tags of the game objects that will set the spawner off. Multiple tags can be separated by commas eg Player,Minion etc." },
            { "Rule Selector", "The way a rule is selected to be spawned. \nAll : All rules are selected. \nFittest : Only the rule with the fittest spawn criteria is selected. If multiple rules have the same fitness then one will be randomly selected.\nWeighted Fittest : The chance of a rule being selected is directly proportional to its fitness. Fitter rules have more chance of selection. Use this to create more natural blends between objects.\nRandom : Rule selection is random." },
            { "Spawn Mdoe", "This setting controls whether the spawned instances will replace, will be added to, or will be removed from the existing instances on the terrain." },
            { "Collision Layers", "Controls which layers are checked for collisions when spawning. Must at least include the layer the terrain is on. Add additional layers if other collisions need to be detected as well. Influences terrain detection, tree detection and game object detection." },
            { "Location Selector", "How the spawner selects locations to spawn in. \nEvery Location: The spawner will attempt to spawn at every location. \nEvery Location Jittered: The spawner will attempt to spawn at every location, but will offset the location by a random jitter factor. Use this to break up lines.\nRandom Location: The spawner will attempt to spawn at random locations.\nRandom Location Clustered: The spawner will attempt to spawn clusters at random locations." },
            { "Location Increment", "The distance from the last location that every new location will be incremented in meters." },
            { "Max Jitter Percent", "Every new location will be offset by a random distance up to a maximum of the jitter percentage multiplied by the location increment." },
            { "Locations Per Spawn", "The number of locations that will be checked every Spawn interval. This does not guarantee that something will be spawned at that location, because lack of fitness may preclude that location from being used." },
            { "Max Cluster Size", "The maximum individuals in a cluster before a new cluster is started." },

            { "X", "Delete all rules."},
            { "I", "Inavtivate all rules."},
            { "A", "Activate all rules."},
            { "+", "Add a rule."},
            { "-", "Delete the rule."},
            { "Visualise", "Visualise this rule in the visualiser."},

            { "Distance Mask", "Mask fitness over distance. Left hand side of curve represents the centre of the spawner. Use this to alter spawn success away from centre e.g. peter out towards edges."},
            { "Area Mask", "Mask fitness over area. None - Don't apply image filter. Grey Scale - apply image filter using greys scale. R - Apply filter from red channel. G - Apply filter from green channel. B - Apply filter from blue channel. A - Apply filter from alpha channel. Terrain Texture Slot - apply mask from texture painted on terrain."},
            { "Image Mask", "The texure to use as the source of the area mask."},
            { "Smooth Mask", "Smooth the mask before applying it. This is a nice way to clean noise up in the mask, or to soften the edges of the mask."},
            { "Normalise Mask", "Normalise the mask before applying it. Ensures that the full dynamic range of the mask is used."},
            { "Invert Mask", "Invert the mask before applying it."},
            { "Flip Mask", "Flip the mask on its x and y axis mask before applying it. Useful sometimes to match the unity terrain as this is flipped internally."},
            { "Seed", "The unique seed for this spawner. If the environment, resources or rules dont change, then hitting Reset and respawning will always regenerate the same result." },

            { "Noise Mask", "Mask fitness with a noise value."},
            { "Noise Seed", "The seed value for the noise function - the same seed will always generate the same noise for a given set of parameters."},
            { "Octaves", "The amount of detail in the noise - more octaves mean more detail and longer calculation time."},
            { "Persistence", "The roughness of the noise. Controls how quickly amplitudes diminish for successive octaves. 0..1."},
            { "Frequency", "The frequency of the first octave."},
            { "Lacunarity", "The frequency multiplier between successive octaves. Experiment between 1.5 - 3.5."},
            { "Zoom", "The zoom level of the noise. Larger zooms display the noise over larger areas."},
            { "Invert", "Invert the noise."},


            { "Name", "Rule name - purely for convenience" },
            { "Resource Type", "The type of resource this rule will apply to." },
            { "Selected Resource", "The resource this rule applies to. To modify how the resource interprets terrain fitness change its spawn criteria." },
            { "Min Viable Fitness", "The minimum fitness needed to be considered viable to spawn." },
            { "Failure Rate", "The amount of the time that the rule will fail even if fit enough. 0 means never fail, and 1 means always fail. Use this to thin things out." },
            { "Max Instances", "The maximum number of resource instances this rule can spawn. Use this to stop over population." },
            { "Ignore Max Instances", "Ignores the max instances criteria. Useful for texturing very large terrains." },
            { "Active", "Whether this rule is active or not. Use this to disable the rule."},
            { "Curr Inst Count", "The number of instances of this rule that have been spawned."},
            { "Instances Spawned", "The number of times this resource has been spawned." },
            { "Inactive Inst Count", "The number of inactive instances that have been spawned, but are now inactive and in the pool for re-use. Only relevant when game objects have been spawned" },

            { "Active Rules", "The number of active rules being managed by the spawner."},
            { "Inactive Rules", "The number of inactive rules being managed by the spawner."},
            { "TOTAL Rules", "The total number of rules being managed by the spawner."},
            { "MAX INSTANCES", "The maximum number of instances that can be managed by the spawner."},
            { "Active Instances", "The number of active instances being managed by the spawner."},
            { "Inactive Instances", "The number inactive instances being managed by the spawner."},
            { "TOTAL Instances", "The total number of active and inactive instances being managed by the spawner."},

            { "Min Direction", "Minimum rotation in degrees for this game object spawner." },
            { "Max Direction", "Maximum rotation in degrees fpr this game object spawner." },

            { "Ground Level", "Ground level for this feature, used to make positioning easier." },
            { "Show Ground Level", "Show ground level." },
            { "Stick To Ground", "Stick to ground level." },
            { "Show Gizmos", "Show the spawners gizmos." },
            { "Show Rulers", "Show rulers." },
            { "Show Statistics", "Show spawner statistics." },
            { "Flatten", "Flatten the entire terrain - use with care!" },
            { "Smooth", "Smooth the entire terrain - removes jaggies and increases frame rate - run multiple times to increase effect - use with care!" },
            { "Clear Trees", "Clear trees from entire terrain - use with care!" },
            { "Clear Details", "Clear details / grass from entire terrain - use with care!" },
            { "Ground", "Position the spawner at ground level on the terrain." },
            { "Fit To Terrain", "Fits and aligns the spawner to the terrain." },
            { "Reset", "Resets the spawner, deletes any spawned game objects, and resets the random number generator." },
            { "Spawn Local", "Run a single spawn iteration at the exact spawner position and range. You can run as many spawn iterations as you like." },
            { "Spawn World", "Run a single spawn iteration on every terrain in the scene. You can run as many spawn iterations as you like." },
        };
    }
}
