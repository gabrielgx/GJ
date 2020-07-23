using Gaia.Internal;
using PWCommon2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using Gaia.Pipeline.LWRP;
using Gaia.Pipeline.HDRP;
using Gaia.Pipeline.URP;
using Gaia.Pipeline;
using UnityStandardAssets.Characters.FirstPerson;
using UnityStandardAssets.Characters.ThirdPerson;
#if UNITY_2018_3_OR_NEWER
using UnityEngine.Networking;
#endif

namespace Gaia
{
    /// <summary>
    /// Handy helper for all things Gaia
    /// </summary>
    public class GaiaManagerEditor : EditorWindow, IPWEditor
    {
        #region Variables, Properties
        private GUIStyle m_boxStyle;
        private GUIStyle m_wrapStyle;
        private GUIStyle m_titleStyle;
        private GUIStyle m_headingStyle;
        private GUIStyle m_bodyStyle;
        private GUIStyle m_linkStyle;
        private GaiaSettings m_settings;
        private UnityPipelineProfile m_gaiaPipelineSettings;
        private IEnumerator m_updateCoroutine;
        private EditorUtils m_editorUtils;

        private TabSet m_mainTabs;
        private TabSet m_extensionsTabs;

        //Extension manager
        bool m_needsScan = true;
        GaiaExtensionManager m_extensionMgr = new GaiaExtensionManager();
        //private bool m_foldoutSession = false;
        //private bool m_foldoutTerrain = false;
        //private bool m_foldoutSpawners = false;
        //private bool m_foldoutCharacters = false;
        //private bool m_foldoutUtils = false;
        private GaiaConstants.EnvironmentSize m_oldTargetSize;
        private GaiaConstants.EnvironmentTarget m_oldTargetEnv;
        private bool m_foldoutTerrainResolutionSettings = false;

        // Icon tests
        private Texture2D m_stdIcon;
        private Texture2D m_advIcon;
        private Texture2D m_gxIcon;
        private Texture2D m_moreIcon;

        //Bool system checks
        private bool m_shadersNotImported;
        public bool m_showSetupPanel;
        private bool m_enableGUI;
        private Color m_defaultPanelColor;

        //Water Profiles
        private string m_unityVersion;
        private List<string> m_profileList = new List<string>();
        private List<Material> m_allMaterials = new List<Material>();
        private int newProfileListIndex = 0;

        //Terrain resolution settings
        private GaiaConstants.HeightmapResolution m_heightmapResolution;
        private GaiaConstants.TerrainTextureResolution m_controlTextureResolution;
        private GaiaConstants.TerrainTextureResolution m_basemapResolution;
        private int m_detailResolutionPerPatch;
        private int m_detailResolution;
        private int m_biomePresetSelection = int.MinValue;

        //Biomes and Spawners
        private List<BiomePresetDropdownEntry> m_allBiomePresets = new List<BiomePresetDropdownEntry>();
        private List<BiomeSpawnerListEntry> m_BiomeSpawnersToCreate = new List<BiomeSpawnerListEntry>();
        private List<BiomeSpawnerListEntry> m_advancedTabAllSpawners = new List<BiomeSpawnerListEntry>();
        private List<AdvancedTabBiomeListEntry> m_advancedTabAllBiomes = new List<AdvancedTabBiomeListEntry>();
        private UnityEditorInternal.ReorderableList m_biomeSpawnersList;
        private UnityEditorInternal.ReorderableList m_advancedTabBiomesList;
        private UnityEditorInternal.ReorderableList m_advancedTabSpawnersList;

        //Misc
        private bool m_foldoutSpawnerSettings;
        private bool m_foldOutWorldSizeSettings;
        private GUIStyle m_helpStyle;
        private bool m_foldoutExtrasSettings;
        private bool m_advancedTabFoldoutSpawners;
        private bool m_advancedTabFoldoutBiomes;


        private GaiaSessionManager m_sessionManager;
        private bool m_initResSettings;

        private bool m_statusCheckPerformed;
        private bool m_showAutoStreamSettingsBox;
        private bool m_terrainCreationRunning;

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


        public bool PositionChecked { get; set; }
        #endregion

        #region Gaia Menu Items
        /// <summary>
        /// Show Gaia Manager editor window
        /// </summary>
        [MenuItem("Window/" + PWConst.COMMON_MENU + "/Gaia/Show Gaia Manager... %g", false, 40)]
        public static void ShowGaiaManager()
        {
            try
            {
                var manager = EditorWindow.GetWindow<Gaia.GaiaManagerEditor>(false, "Gaia Manager");
                //Manager can be null if the dependency package installation is started upon opening the manager window.
                if (manager != null)
                {
                    Vector2 initialSize = new Vector2(650f, 450f);
                    manager.position = new Rect(new Vector2(Screen.currentResolution.width / 2f - initialSize.x / 2f, Screen.currentResolution.height / 2f - initialSize.y / 2f), initialSize);
                    manager.Show();
                }
            }
            catch (Exception ex)
            {
                //not catching anything specific here, but the maintenance and shader installation tasks can trigger a null reference on that "GetWindow" above

                //get rid off the warning for unused "ex"
                if (ex.Message == "")
                { }
            };
        }

        ///// <summary>
        ///// Show the forum
        ///// </summary>
        //[MenuItem("Window/Gaia/Show Forum...", false, 60)]
        //public static void ShowForum()
        //{
        //    Application.OpenURL(
        //        "http://www.procedural-worlds.com/forum/gaia/");
        //}

        /// <summary>
        /// Show documentation
        /// </summary>
        [MenuItem("Window/" + PWConst.COMMON_MENU + "/Gaia/Show Extensions...", false, 65)]
        public static void ShowExtensions()
        {
            Application.OpenURL("http://www.procedural-worlds.com/gaia/?section=gaia-extensions");
        }
        #endregion

        #region Constructors destructors and related delegates

        /// <summary>
        /// Setup on destroy
        /// </summary>
        private void OnDestroy()
        {
            if (m_editorUtils != null)
            {
                m_editorUtils.Dispose();
            }
        }

        /// <summary>
        /// See if we can preload the manager with existing settings
        /// </summary>
        public void OnEnable()
        {
            m_defaultPanelColor = GUI.backgroundColor;

            if (EditorGUIUtility.isProSkin)
            {
                if (m_stdIcon == null)
                {
                    m_stdIcon = Resources.Load("gstdIco_p") as Texture2D;
                }
                if (m_advIcon == null)
                {
                    m_advIcon = Resources.Load("gadvIco_p") as Texture2D;
                }
                if (m_gxIcon == null)
                {
                    m_gxIcon = Resources.Load("ggxIco_p") as Texture2D;
                }
                if (m_moreIcon == null)
                {
                    m_moreIcon = Resources.Load("gmoreIco_p") as Texture2D;
                }
            }
            else
            {
                if (m_stdIcon == null)
                {
                    m_stdIcon = Resources.Load("gstdIco") as Texture2D;
                }
                if (m_advIcon == null)
                {
                    m_advIcon = Resources.Load("gadvIco") as Texture2D;
                }
                if (m_gxIcon == null)
                {
                    m_gxIcon = Resources.Load("ggxIco") as Texture2D;
                }
                if (m_moreIcon == null)
                {
                    m_moreIcon = Resources.Load("gmoreIco") as Texture2D;
                }
            }

            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            var mainTabs = new Tab[] {
                new Tab ("Standard", m_stdIcon, StandardTab),
                new Tab ("Advanced", m_advIcon, AdvancedTab),
                new Tab ("GX", m_gxIcon, ExtensionsTab),
                new Tab ("More...", m_moreIcon, TutorialsAndSupportTab),
            };

            var gxTabs = new Tab[] {
                new Tab ("PanelExtensions", InstalledExtensionsTab),
                new Tab ("Partners & Extensions", MoreOnProceduralWorldsTab),
            };

            m_mainTabs = new TabSet(m_editorUtils, mainTabs);
            m_extensionsTabs = new TabSet(m_editorUtils, gxTabs);

            //Signal we need a scan
            m_needsScan = true;

            //Set the Gaia directories up
            GaiaUtils.CreateGaiaStampDirectories();

            //Get or create existing settings object
            if (m_settings == null)
            {
                m_settings = (GaiaSettings)PWCommon2.AssetUtils.GetAssetScriptableObject("GaiaSettings");
                if (m_settings == null)
                {
                    m_settings = CreateSettingsAsset();
                }
            }

            m_settings = GaiaUtils.GetGaiaSettings();
            if (m_settings == null)
            {
                Debug.Log("Gaia Settings are missing from our project, please make sure Gaia settings is in your project.");
                return;
            }

            m_gaiaPipelineSettings = m_settings.m_pipelineProfile;

            //Sets up the render to the correct pipeline
            if (GraphicsSettings.renderPipelineAsset == null)
            {
                m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.BuiltIn;
                m_settings.m_pipelineProfile.m_activePipelineInstalled = GaiaConstants.EnvironmentRenderer.BuiltIn;
            }
            else if (GraphicsSettings.renderPipelineAsset.GetType().ToString().Contains("HDRenderPipelineAsset"))
            {
                m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.HighDefinition;
            }
            else if (GraphicsSettings.renderPipelineAsset.GetType().ToString().Contains("UniversalRenderPipelineAsset"))
            {
                m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.Universal;
            }
            else
            {
                m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.Lightweight;
            }

            //Set water profile
            newProfileListIndex = m_settings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex;
            if (newProfileListIndex > m_settings.m_gaiaWaterProfile.m_waterProfiles.Count + 1)
            {
                newProfileListIndex = 0;
                m_settings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex = 0;
            }

            //Make sure we have defaults
            if (m_settings.m_currentDefaults == null)
            {
                m_settings.m_currentDefaults = (GaiaDefaults)PWCommon2.AssetUtils.GetAssetScriptableObject("GaiaDefaults");
                EditorUtility.SetDirty(m_settings);
            }

            //Initialize editor resolution settings with defaults
            if (m_settings.m_currentDefaults != null)
            {
                m_oldTargetSize = (GaiaConstants.EnvironmentSize)m_settings.m_currentSize;
                m_oldTargetEnv = (GaiaConstants.EnvironmentTarget)m_settings.m_currentEnvironment;
                m_heightmapResolution = (GaiaConstants.HeightmapResolution)m_settings.m_currentDefaults.m_heightmapResolution;
                m_controlTextureResolution = (GaiaConstants.TerrainTextureResolution)m_settings.m_currentDefaults.m_controlTextureResolution;
                m_basemapResolution = (GaiaConstants.TerrainTextureResolution)m_settings.m_currentDefaults.m_baseMapSize;
                m_detailResolutionPerPatch = m_settings.m_currentDefaults.m_detailResolutionPerPatch;
                m_detailResolution = m_settings.m_currentDefaults.m_detailResolution;
            }


            //Not required anymore with new spawner system
            //Grab first resource we can find
            //if (m_settings.m_currentResources == null)
            //{
            //    m_settings.m_currentResources = (GaiaResource)PWCommon1.AssetUtils.GetAssetScriptableObject("GaiaResources");
            //    EditorUtility.SetDirty(m_settings);
            //}

            ////Grab first game object resource we can find
            //if (m_settings.m_currentGameObjectResources == null)
            //{
            //    m_settings.m_currentGameObjectResources = m_settings.m_currentResources;
            //    EditorUtility.SetDirty(m_settings);
            //}

            if (!Application.isPlaying)
            {
                StartEditorUpdates();
                m_updateCoroutine = GetNewsUpdate();
            }



            string[] allSpawnerPresetGUIDs = AssetDatabase.FindAssets("t:BiomePreset");

            for (int i = 0; i < allSpawnerPresetGUIDs.Length; i++)
            {
                BiomePreset sp = (BiomePreset)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(allSpawnerPresetGUIDs[i]), typeof(BiomePreset));
                if (sp != null)
                {
                    m_allBiomePresets.Add(new BiomePresetDropdownEntry { ID = i, name = sp.name, biomePreset = sp });
                }
            }
            m_allBiomePresets.Sort();
            //Add the artifical "Custom" option
            m_allBiomePresets.Add(new BiomePresetDropdownEntry { ID = -999, name = "Custom", biomePreset = null });

            if (m_allBiomePresets.Count > 0)
            {
                m_biomePresetSelection = m_allBiomePresets[0].ID;
            }

            if (m_biomePresetSelection != int.MinValue)
            {
                //Fill in initial content
                AddBiomeSpawnersForSelectedPreset();
                CreateBiomePresetList();
            }

            CreateAdvancedTabBiomesList();

            CreateAdvancedTabSpawnersList();

            m_initResSettings = true;
        }

        private void AddBiomeSpawnersForSelectedPreset()
        {
            m_BiomeSpawnersToCreate.Clear();

            BiomePresetDropdownEntry entry = m_allBiomePresets.Find(x => x.ID == m_biomePresetSelection);
            if (entry.biomePreset != null)
            {

                //Need to create a deep copy of the preset list, otherwise the users will overwrite it when they add custom spawners
                foreach (BiomeSpawnerListEntry spawnerListEntry in entry.biomePreset.m_spawnerPresetList)
                {
                    if (spawnerListEntry.m_spawnerSettings != null)
                    {
                        m_BiomeSpawnersToCreate.Add(spawnerListEntry);
                    }
                }
            }
        }

        private void CreateAdvancedTabBiomesList()
        {
            m_advancedTabAllBiomes.Clear();
            string[] allBiomeGUIDS = AssetDatabase.FindAssets("t:BiomePreset");

            for (int i = 0; i < allBiomeGUIDS.Length; i++)
            {
                BiomePreset biomePreset = (BiomePreset)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(allBiomeGUIDS[i]), typeof(BiomePreset));
                if (biomePreset != null)
                {
                    m_advancedTabAllBiomes.Add(new AdvancedTabBiomeListEntry { m_autoAssignPrototypes = true, m_biomePreset = biomePreset });
                }
            }
            m_advancedTabAllBiomes.Sort();

            m_advancedTabBiomesList = new UnityEditorInternal.ReorderableList(m_advancedTabAllBiomes, typeof(AdvancedTabBiomeListEntry), false, true, false, false);
            m_advancedTabBiomesList.drawElementCallback = DrawAdvancedTabBiomeListElement;
            m_advancedTabBiomesList.drawHeaderCallback = DrawAdvancedTabBiomeListHeader;
            m_advancedTabBiomesList.elementHeightCallback = OnElementHeightSpawnerPresetListEntry;


        }

        private void DrawAdvancedTabBiomeListHeader(Rect rect)
        {
            BiomeListEditor.DrawListHeader(rect, true, m_advancedTabAllBiomes, m_editorUtils, "AdvancedTabBiomeListHeader");
        }

        private void DrawAdvancedTabBiomeListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            BiomeListEditor.DrawListElement_AdvancedTab(rect, m_advancedTabAllBiomes[index], m_editorUtils);
        }

        private void CreateAdvancedTabSpawnersList()
        {
            m_advancedTabAllSpawners.Clear();
            string[] allSpawnerGUIDs = AssetDatabase.FindAssets("t:SpawnerSettings l:GaiaManagerSpawner");

            for (int i = 0; i < allSpawnerGUIDs.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(allSpawnerGUIDs[i]);
                SpawnerSettings spawnerSettings = (SpawnerSettings)AssetDatabase.LoadAssetAtPath(assetPath, typeof(SpawnerSettings));
                if (spawnerSettings != null)
                {
                    m_advancedTabAllSpawners.Add(new BiomeSpawnerListEntry { m_autoAssignPrototypes = true, m_spawnerSettings = spawnerSettings });
                }
            }
            m_advancedTabAllSpawners.Sort();


            m_advancedTabSpawnersList = new UnityEditorInternal.ReorderableList(m_advancedTabAllSpawners, typeof(BiomeSpawnerListEntry), false, true, false, false);
            m_advancedTabSpawnersList.elementHeightCallback = OnElementHeightSpawnerPresetListEntry;
            m_advancedTabSpawnersList.drawElementCallback = DrawAdvancedTabSpawnerListElement;
            m_advancedTabSpawnersList.drawHeaderCallback = DrawAdvancedTabSpawnerPresetListHeader;
            m_advancedTabSpawnersList.onAddCallback = OnAddSpawnerPresetListEntry;
            m_advancedTabSpawnersList.onRemoveCallback = OnRemoveSpawnerPresetListEntry;
            m_advancedTabSpawnersList.onReorderCallback = OnReorderSpawnerPresetList;
        }

        /// <summary>
        /// Settings up settings on disable
        /// </summary>
        void OnDisable()
        {
            StopEditorUpdates();
        }

        #region Spawner Preset List

        void CreateBiomePresetList()
        {
            m_biomeSpawnersList = new UnityEditorInternal.ReorderableList(m_BiomeSpawnersToCreate, typeof(BiomeSpawnerListEntry), true, true, true, true);
            m_biomeSpawnersList.elementHeightCallback = OnElementHeightSpawnerPresetListEntry;
            m_biomeSpawnersList.drawElementCallback = DrawSpawnerPresetListElement;
            m_biomeSpawnersList.drawHeaderCallback = DrawSpawnerPresetListHeader;
            m_biomeSpawnersList.onAddCallback = OnAddSpawnerPresetListEntry;
            m_biomeSpawnersList.onRemoveCallback = OnRemoveSpawnerPresetListEntry;
            m_biomeSpawnersList.onReorderCallback = OnReorderSpawnerPresetList;
        }

        private void OnReorderSpawnerPresetList(ReorderableList list)
        {
            //Do nothing, changing the order does not immediately affect anything in this window
        }

        private void OnRemoveSpawnerPresetListEntry(ReorderableList list)
        {
            m_BiomeSpawnersToCreate = SpawnerPresetListEditor.OnRemoveListEntry(m_BiomeSpawnersToCreate, m_biomeSpawnersList.index);
            list.list = m_BiomeSpawnersToCreate;
            m_biomePresetSelection = -999;
        }

        private void OnAddSpawnerPresetListEntry(ReorderableList list)
        {
            m_BiomeSpawnersToCreate = SpawnerPresetListEditor.OnAddListEntry(m_BiomeSpawnersToCreate);
            list.list = m_BiomeSpawnersToCreate;
            m_biomePresetSelection = -999;
        }

        private void DrawSpawnerPresetListHeader(Rect rect)
        {
            SpawnerPresetListEditor.DrawListHeader(rect, true, m_BiomeSpawnersToCreate, m_editorUtils, "SpawnerAdded");
        }

        private void DrawSpawnerPresetListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SpawnerPresetListEditor.DrawListElement(rect, m_BiomeSpawnersToCreate[index], m_editorUtils, this);
        }

        private void DrawAdvancedTabSpawnerPresetListHeader(Rect rect)
        {
            SpawnerPresetListEditor.DrawListHeader(rect, true, m_BiomeSpawnersToCreate, m_editorUtils, "AdvancedTabSpawnerListHeader");
        }

        private void DrawAdvancedTabSpawnerListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SpawnerPresetListEditor.DrawListElement_AdvancedTab(rect, m_advancedTabAllSpawners[index], m_editorUtils);
        }

        private float OnElementHeightSpawnerPresetListEntry(int index)
        {
            return SpawnerPresetListEditor.OnElementHeight();
        }



        #endregion

        /// <summary>
        /// Creates a new Gaia settings asset
        /// </summary>
        /// <returns>New gaia settings asset</returns>
        public static GaiaSettings CreateSettingsAsset()
        {
            GaiaSettings settings = ScriptableObject.CreateInstance<Gaia.GaiaSettings>();
            AssetDatabase.CreateAsset(settings, GaiaDirectories.GetSettingsDirectory() + "/GaiaSettings.asset");
            AssetDatabase.SaveAssets();
            return settings;
        }

        #endregion

        #region Tabs
        /// <summary>
        /// Draw the brief editor
        /// </summary>
        void StandardTab()
        {
#if !UNITY_2019_3_OR_NEWER
            EditorGUILayout.HelpBox(Application.unityVersion + " is not supported by Gaia, please use 2019.3+.", MessageType.Error);
            GUI.enabled = false;
#endif
            if (!m_enableGUI)
            {
                GUI.backgroundColor = new Color(1f, 0.7311321f, 0.7311321f, 1f);
            }

            GUILayout.Space(5f);

            //Show the Setup panel settings
            m_editorUtils.Panel("PanelSetup", SetupSettingsEnabled, m_showSetupPanel);

            GUI.backgroundColor = m_defaultPanelColor;

            GUI.enabled = m_enableGUI && !m_terrainCreationRunning;

            m_editorUtils.Panel("NewWorldSettings", NewWorldSettings, m_enableGUI);

            EditorGUI.indentLevel++;

            GUILayout.Space(5f);

            //Add in a check for linear deferred lighting
            if (m_settings.m_currentEnvironment == GaiaConstants.EnvironmentTarget.Desktop ||
              m_settings.m_currentEnvironment == GaiaConstants.EnvironmentTarget.PowerfulDesktop ||
              m_settings.m_currentEnvironment == GaiaConstants.EnvironmentTarget.Custom)
            {
                var tier1 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier1);
                var tier2 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier2);
                var tier3 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier3);
                if (PlayerSettings.colorSpace != ColorSpace.Linear || tier1.renderingPath != RenderingPath.DeferredShading)
                {
                    if (m_editorUtils.ButtonAutoIndent("0. Set Linear Deferred"))
                    {
                        var manager = GetWindow<GaiaManagerEditor>();

                        if (EditorUtility.DisplayDialog(
                        m_editorUtils.GetTextValue("SettingLinearDeferred"),
                        m_editorUtils.GetTextValue("SetLinearDeferred"),
                        m_editorUtils.GetTextValue("Yes"), m_editorUtils.GetTextValue("Cancel")))
                        {
                            manager.Close();

                            PlayerSettings.colorSpace = ColorSpace.Linear;

                            tier1.renderingPath = RenderingPath.DeferredShading;
                            EditorGraphicsSettings.SetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier1, tier1);

                            tier2.renderingPath = RenderingPath.DeferredShading;
                            EditorGraphicsSettings.SetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier2, tier2);

                            tier3.renderingPath = RenderingPath.DeferredShading;
                            EditorGraphicsSettings.SetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier3, tier3);

#if UNITY_2018_1_OR_NEWER && !UNITY_2019_1_OR_NEWER
                            LightmapEditorSettings.lightmapper = LightmapEditorSettings.Lightmapper.ProgressiveCPU;
#elif UNITY_2019_1_OR_NEWER
                            LightmapEditorSettings.lightmapper = LightmapEditorSettings.Lightmapper.ProgressiveGPU;
#endif

#if UNITY_2018_1_OR_NEWER
                            Lightmapping.realtimeGI = true;
                            Lightmapping.bakedGI = true;
                            LightmapEditorSettings.realtimeResolution = 2f;
                            LightmapEditorSettings.bakeResolution = 40f;
                            Lightmapping.indirectOutputScale = 2f;
                            RenderSettings.defaultReflectionResolution = 256;
                            if (QualitySettings.shadowDistance < 350f)
                            {
                                QualitySettings.shadowDistance = 350f;
                            }
#else
                            if (QualitySettings.shadowDistance < 250f)
                            {
                                QualitySettings.shadowDistance = 250f;
                            }
#endif
                            if (Lightmapping.giWorkflowMode == Lightmapping.GIWorkflowMode.Iterative)
                            {
                                Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
                            }

                            if (GameObject.Find("Directional light") != null)
                            {
                                RenderSettings.sun = GameObject.Find("Directional light").GetComponent<Light>();
                            }
                            else if (GameObject.Find("Directional Light") != null)
                            {
                                RenderSettings.sun = GameObject.Find("Directional Light").GetComponent<Light>();
                            }
                        }
                    }
                }
            }

            EditorGUILayout.BeginHorizontal();

            if (ButtonLeftAligned("StandardTabButtonCreateTerrain", GUILayout.ExpandWidth(true)))
            {
                int actualTerrainCount = Gaia.TerrainHelper.GetActiveTerrainCount();
                if (actualTerrainCount != 0)
                {

                    if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("AlreadyFoundTerrainHeader"), string.Format(m_editorUtils.GetTextValue("AlreadyFoundTerrain"), actualTerrainCount, 0), m_editorUtils.GetTextValue("OK"), m_editorUtils.GetTextValue("Cancel")))
                    {
                        BiomePresetDropdownEntry selectedPresetEntry = m_allBiomePresets.Find(x => x.ID == m_biomePresetSelection);
                        CreateBiome(selectedPresetEntry);
                        //prepare resource prototype arrays once, so the same prototypes can be added to all the tiles.
                        TerrainLayer[] terrainLayers = new TerrainLayer[0];
                        DetailPrototype[] terrainDetails = new DetailPrototype[0];
                        TreePrototype[] terrainTrees = new TreePrototype[0];

                        GaiaDefaults.GetPrototypes(m_BiomeSpawnersToCreate, ref terrainLayers, ref terrainDetails, ref terrainTrees, Terrain.activeTerrain);

                        foreach (Terrain t in Terrain.activeTerrains)
                        {
                            GaiaDefaults.ApplyPrototypesToTerrain(t, terrainLayers, terrainDetails, terrainTrees);
                        }

                    }
                }
                else
                {

                    //Switch off pro only features if activated for some reason
#if !GAIA_PRO_PRESENT
                    m_settings.m_createTerrainScenes = false;
                    m_settings.m_unloadTerrainScenes = false;
                    m_settings.m_floatingPointFix = false;
#endif
                    //No terrain yet, create everything as usual
                    //Check lighting first
#if HDPipeline
                    GaiaHDRPPipelineUtils.SetDefaultHDRPLighting(m_settings.m_pipelineProfile, false);
#else
                    GaiaLighting.SetDefaultAmbientLight(m_settings.m_gaiaLightingProfile);
#endif


                    bool cancel = false;

                    //Abort if exporting to scenes is active and the current scene has not been saved yet - we need a valid scene filename to create subfolders for the scene files, etc.
                    if (m_settings.m_createTerrainScenes && string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
                    {
                        bool scenesSaved = false;

                        if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("SceneNotSavedYetTitle"), m_editorUtils.GetTextValue("SceneNotSavedYetText"), m_editorUtils.GetTextValue("SaveNow"), m_editorUtils.GetTextValue("Cancel")))
                        {
                            scenesSaved = EditorSceneManager.SaveOpenScenes();
                        }
                        else
                        {
                            //Canceled out
                            cancel = true;
                        }

                        //Did the user actually save the scene after the prompt?
                        if (!scenesSaved)
                        {
                            cancel = true;
                        }
                    }

                    if (!cancel)
                    {

                        float totalSteps = 3;
                        float currentStep = 0f;
                        EditorUtility.DisplayProgressBar("Creating Terrain", "Creating Terrain", ++currentStep / totalSteps);
                        GaiaSessionManager.OnWorldCreated -= CreateToolsAfterWorld;
                        GaiaSessionManager.OnWorldCreated += CreateToolsAfterWorld;
                        m_terrainCreationRunning = true;
                        CreateTerrain(true);

                    }
                }
            }

            //EditorGUILayout.LabelField(" - OR - ", GUILayout.Width(68f));

            if (ButtonLeftAligned("StandardTabButtonWorldDesigner", GUILayout.ExpandWidth(true)))
            {
                GaiaUtils.GetOrCreateWorldDesigner();
                //Important: Subscribe to this event AFTER the call to GetOrCreateWorldDesigner for correct order of operation
                GaiaSessionManager.OnWorldCreated -= AddDefaultWorldBiomeToWorldDesigner;
                GaiaSessionManager.OnWorldCreated += AddDefaultWorldBiomeToWorldDesigner;
                m_terrainCreationRunning = true;

#if GAIA_PRO_PRESENT
                if (SessionManager != null)
                {
                    WorldOriginEditor.m_sessionManagerExits = true;
                }
#else
                if (SessionManager != null)
                {
                    Gaia2TopPanel.m_sessionManagerExits = true;
                }
#endif
            }

            EditorGUILayout.EndHorizontal();

            string buttonLabel;

            buttonLabel = m_editorUtils.GetTextValue("CreateExtrasButtonStart");
            bool first = true;

            if (m_settings.m_currentController != GaiaConstants.EnvironmentControllerType.None)
            {
                first = false;
                buttonLabel += m_editorUtils.GetTextValue("CreateExtrasButtonPlayer");
            }

            GaiaLightingProfile lightingProfile = m_settings.m_gaiaLightingProfile;

            if (lightingProfile.m_selectedLightingProfileValuesIndex != -99)
            {
                if (!first)
                    buttonLabel += ", ";
                first = false;
                buttonLabel += m_editorUtils.GetTextValue("CreateExtrasButtonSkies");
            }

            if (m_settings.m_currentWaterPro != GaiaConstants.GaiaWaterProfileType.None)
            {
                if (!first)
                    buttonLabel += ", ";
                first = false;
                buttonLabel += m_editorUtils.GetTextValue("CreateExtrasButtonWater");
            }

            if (m_settings.m_createWind)
            {
                if (!first)
                    buttonLabel += ", ";
                first = false;
                buttonLabel += m_editorUtils.GetTextValue("CreateExtrasButtonWind");
            }
            if (m_settings.m_createScreenShotter)
            {
                if (!first)
                    buttonLabel += ", ";
                first = false;
                buttonLabel += m_editorUtils.GetTextValue("CreateExtrasButtonScreenShotter");
            }

            //only display the button if there is actually anything being to be added to the scene
            if (!first)
            {
                //if (m_editorUtils.ButtonAutoIndent(new GUIContent(buttonLabel, m_editorUtils.GetTooltip("CreateExtrasButtonStart"))))
                if (ButtonLeftAligned(new GUIContent(buttonLabel, m_editorUtils.GetTooltip("CreateExtrasButtonStart"))))
                {
                    //Only do this if we have 1 terrain
                    if (DisplayErrorIfNotMinimumTerrainCount(1))
                    {
                        return;
                    }

                    if (lightingProfile.m_selectedLightingProfileValuesIndex != -99)
                    {
                        if (!EditorUtility.DisplayDialog("Adding Ambient Skies Samples", "You're about to add a sky with lighting and post processing setup to your scene that might overwrite existing lighting and post processing settings. Continue?", "Yes", "No"))
                        {
                            return;
                        }
                    }

                    if (m_settings.m_currentController != GaiaConstants.EnvironmentControllerType.None)
                    {
                        CreatePlayer();
                    }
                    if (lightingProfile.m_selectedLightingProfileValuesIndex != -99)
                    {
                        //m_settings.m_gaiaLightingProfile.m_lightingProfile = m_settings.m_currentSkies;
                        CreateSky();
                    }
                    else
                    {
                        GaiaLighting.RemoveSystems();
                    }
                    if (m_settings.m_currentWater != GaiaConstants.Water.None || m_settings.m_currentWaterPro != GaiaConstants.GaiaWaterProfileType.None)
                    {
                        CreateWater();
                    }
                    else
                    {

                        GaiaWater.RemoveSystems();
                    }
                    if (m_settings.m_createScreenShotter)
                    {
                        CreateScreenShotter();
                    }
                    if (m_settings.m_createWind)
                    {
                        CreateWindZone();
                    }
                    else
                    {
#if GAIA_PRO_PRESENT
                        ProceduralWorldsGlobalWeather.RemoveGlobalWindShader();
#endif
                    }

                    if (m_settings.m_enableLocationManager)
                    {
                        LocationSystemEditor.AddLocationSystem();
                    }
                    else
                    {
                        LocationSystemEditor.RemoveLocationSystem();
                    }

                    if (m_settings.m_gaiaWaterProfile.m_multiSceneLightingSupport)
                    {
                        if (GaiaGlobal.Instance != null)
                        {
                            GaiaGlobal.Instance.ApplySetting(m_settings.m_gaiaWaterProfile.m_waterProfiles[m_settings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex], m_settings.m_gaiaWaterProfile);
                        }
                    }

                    if (m_settings.m_gaiaLightingProfile.m_multiSceneLightingSupport)
                    {
                        if (GaiaGlobal.Instance != null)
                        {
                            GaiaGlobal.Instance.ApplySetting(m_settings.m_gaiaLightingProfile.m_lightingProfiles[m_settings.m_gaiaLightingProfile.m_selectedLightingProfileValuesIndex], m_settings.m_gaiaLightingProfile);
                        }
                    }
                }
            }

            string buttonCount = "3. ";
            if (first)
            {
                buttonCount = "2. ";
            }

            if (Lightmapping.isRunning)
            {
                if (ButtonLeftAligned(new GUIContent(buttonCount + m_editorUtils.GetTextValue("Cancel Bake"), m_editorUtils.GetTooltip("Cancel Bake"))))
                {
                    GaiaLighting.CancelLightmapBaking();
                }
            }
            else
            {
                if (Lightmapping.giWorkflowMode == Lightmapping.GIWorkflowMode.OnDemand)
                {
                    if (ButtonLeftAligned(new GUIContent(buttonCount + m_editorUtils.GetTextValue("QuickBakeLighting"), m_editorUtils.GetTooltip("QuickBakeLighting"))))
                    {
                        GaiaLighting.QuickBakeLighting();
                    }
                }
                else
                {
                    if (ButtonLeftAligned(new GUIContent(buttonCount + m_editorUtils.GetTextValue("Bake Lighting"), m_editorUtils.GetTooltip("Bake Lighting"))))
                    {
                        if (EditorUtility.DisplayDialog(
                            m_editorUtils.GetTextValue("BakingLightmaps!"),
                            m_editorUtils.GetTextValue("BakingLightmapsInfo"),
                            m_editorUtils.GetTextValue("Bake"), m_editorUtils.GetTextValue("Cancel")))
                        {
                            GaiaLighting.BakeLighting(m_settings.m_gaiaLightingProfile);
                        }
                    }
                }
            }

            EditorGUILayout.Space();
            m_editorUtils.Label("FollowTheWorkFlow");
            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("TutorialLink"), m_linkStyle))
            {
                Application.OpenURL("http://www.procedural-worlds.com/gaia/?section=tutorials-gaia-pro");
            }
            EditorGUI.indentLevel--;

            GUI.enabled = true;
        }

        private void AddDefaultWorldBiomeToWorldDesigner()
        {
            try
            {
                GaiaTerrainLoaderManager.Instance.SwitchToWorldMap();
                GameObject worldMapObj = GameObject.Find(GaiaConstants.worldDesignerObject);
                if (worldMapObj != null)
                {
                    Selection.activeObject = worldMapObj;
                    worldMapObj.GetComponent<WorldMap>().LookAtWorldMap();

                    BiomePresetDropdownEntry selectedPresetEntry = m_allBiomePresets.Find(x => x.ID == m_biomePresetSelection);
                    if (selectedPresetEntry.name != "Custom")
                    {
                        Spawner spawner = worldMapObj.GetComponent<Spawner>();
                        SpawnRule rule = new SpawnRule();
                        rule.m_resourceType = GaiaConstants.SpawnerResourceType.WorldBiomeMask;
                        spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes = GaiaUtils.AddElementToArray(spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes, new ResourceProtoWorldBiomeMask() { m_name = selectedPresetEntry.name, m_biomePreset = selectedPresetEntry.biomePreset });
                        rule.m_name = selectedPresetEntry.name;
                        rule.m_resourceIdx = 0;
                        //Fold out by default
                        rule.m_isFoldedOut = true;
                        spawner.m_settings.m_spawnerRules.Add(rule);
                    }
                }

                //Adjust the scene view so you can see the world designer
                if (SceneView.lastActiveSceneView != null)
                {
                    if (m_settings != null)
                    {
                        SceneView.lastActiveSceneView.LookAtDirect(new Vector3(0f, 300f, -1f * (m_settings.m_currentDefaults.m_terrainSize * m_settings.m_tilesX / 2f)), Quaternion.Euler(30f, 0f, 0f));
                        Repaint();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error while adding the default biome after world map creation. Message: " + e.Message + " Stack Trace: " + e.StackTrace);
            }
            finally
            {
                m_terrainCreationRunning = false;
                GaiaSessionManager.OnWorldCreated -= AddDefaultWorldBiomeToWorldDesigner;
            }
        }


        //This method is subscribed to the OnWorldCreatedEvent in the Session Manager and creates the tools after the world creation in a coroutine has been finished.
        void CreateToolsAfterWorld()
        {
            try
            {
                //Create the spawners
                //Check if there already exist a fitting biome Game Object to group our spawners under
                BiomePresetDropdownEntry selectedPresetEntry = m_allBiomePresets.Find(x => x.ID == m_biomePresetSelection);

                ProgressBar.Show(ProgressBarPriority.CreateSceneTools, "Creating Tools", "Creating Biome", 0, 2);
                List<Spawner> createdSpawners = CreateBiome(selectedPresetEntry);
                ProgressBar.Show(ProgressBarPriority.CreateSceneTools, "Creating Tools", "Creating Stamper", 1, 2);
                GameObject stamperObj = ShowStamper(createdSpawners);
                Stamper stamper = stamperObj.GetComponent<Stamper>();
                for (int i = 0; i < stamper.m_autoSpawners.Count(); i++)
                {
                    stamper.m_autoSpawners[i].isActive = m_BiomeSpawnersToCreate[i].m_isActiveInStamper;
                }

                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            catch (Exception e)
            {
                Debug.LogError("Error while creating tools after terrain creation. Message: " + e.Message + " Stack Trace: " + e.StackTrace);
            }
            finally
            {
                ProgressBar.Clear(ProgressBarPriority.CreateSceneTools);
                m_terrainCreationRunning = false;
                GaiaSessionManager.OnWorldCreated -= CreateToolsAfterWorld;
            }
        }

        /// <summary>
        /// Draw the detailed editor
        /// </summary>
        void AdvancedTab()
        {
#if !UNITY_2019_3_OR_NEWER
            EditorGUILayout.HelpBox(Application.unityVersion + " is not supported in Gaia, please use 2019.3+.", MessageType.Error);
            GUI.enabled = false;
#endif

            EditorGUI.indentLevel++;

            GUI.enabled = m_enableGUI;

            GUILayout.Space(5f);
            m_editorUtils.Panel("SystemInfoSettings", SystemInfoSettingsEnabled, false);
            m_editorUtils.Panel("PanelTerrain", AdvancedPanelTerrain, false);
            m_editorUtils.Panel("PanelBiomesAndSpawners", AdvancedPanelBiomesAndSpawners, false);
            m_editorUtils.Panel("PanelExtras", AdvancedPanelExtras, false);
            m_editorUtils.Panel("PanelUtilities", AdvancedPanelUtilities, false);


            EditorGUILayout.Space();
            m_editorUtils.Label("AdvancedTabIntro");

            EditorGUI.indentLevel++;
        }

        private void AdvancedPanelUtilities(bool helpEnabled)
        {
            //EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("ComingSoonInfo"), MessageType.Info);

            // if (m_editorUtils.ButtonAutoIndent("FinalizeScene"))
            // {
            //     if (EditorUtility.DisplayDialog("Finalizing Scene",
            //         "You are about to finalize your scene this will remove the stamper and spawners from your scene. This will create a finalized version of the scene so you can return to the dev version at any time. Are you sure you want to proceed?",
            //         "Yes", "No"))
            //     {
            //         GaiaUtils.FinalizeScene();
            //     }
            // }

            if (m_editorUtils.ButtonAutoIndent("Show Stamp Converter"))
            {
                ShowGaiaStampConverter();
            }

            if (m_editorUtils.ButtonAutoIndent("Terrain Mesh Export"))
            {
                ShowTerrainObjExporter();
            }

            if (m_editorUtils.ButtonAutoIndent("Scanner"))
            {
                Selection.activeGameObject = Scanner.CreateScanner();
                if (Selection.activeGameObject != null)
                {
                    SceneView.lastActiveSceneView.LookAt(Selection.activeGameObject.transform.position);
                }
            }

            if (m_editorUtils.ButtonAutoIndent("Resource Helper"))
            {
                var resourceHelper = EditorWindow.GetWindow<GaiaResourceHelper>(false, m_editorUtils.GetTextValue("Resource Helper"));
                resourceHelper.Show();
                resourceHelper.position = new Rect(position.position + new Vector2(50, 50), new Vector2(300, 200));
                resourceHelper.minSize = new Vector2(300, 200);
            }

            if (m_editorUtils.ButtonAutoIndent("Show Location Manager"))
            {
                LocationSystemEditor.ShowLocationManager();
            }

            GUI.enabled = true;

#if !GAIA_PRO_PRESENT
            bool currentGUIState = GUI.enabled;
            GUI.enabled = false;
#endif
            if (m_editorUtils.ButtonAutoIndent("Mask Map Exporter"))
            {
                ShowMaskMapExporter();
            }
#if !GAIA_PRO_PRESENT
            GUI.enabled = currentGUIState;
#endif
        }

        private void AdvancedPanelExtras(bool helpEnabled)
        {
            if (m_editorUtils.ButtonAutoIndent("Add Character"))
            {

                Selection.activeGameObject = CreatePlayer();

                //#if GAIA_PRESENT
                //                    GameObject underwaterFX = GameObject.Find("Directional Light");
                //                    GaiaReflectionProbeUpdate theProbeUpdater = FindObjectOfType<GaiaReflectionProbeUpdate>();
                //                    GaiaUnderWaterEffects effectsSettings = underwaterFX.GetComponent<GaiaUnderWaterEffects>();
                //                    if (theProbeUpdater != null && effectsSettings != null)
                //                    {
                //#if UNITY_EDITOR
                //                        effectsSettings.player = effectsSettings.GetThePlayer();
                //#endif
                //                    }
                //#endif
            }
            if (m_editorUtils.ButtonAutoIndent("Add Wind Zone"))
            {
                Selection.activeGameObject = CreateWindZone();
            }
            if (m_editorUtils.ButtonAutoIndent("Add Water"))
            {
                Selection.activeGameObject = CreateWater();
            }
            if (m_editorUtils.ButtonAutoIndent("Add Screen Shotter"))
            {
                Selection.activeGameObject = CreateScreenShotter();
            }
            if (m_editorUtils.ButtonAutoIndent("UpdateSceneSettingsFromProfile"))
            {
                LoadUpSceneSettings.UpdateSceneSettingsFromProfile();
            }
        }

        private void AdvancedPanelBiomesAndSpawners(bool helpEnabled)
        {
            Rect biomesFoldOutRect = EditorGUILayout.GetControlRect();
            m_advancedTabFoldoutBiomes = EditorGUI.Foldout(biomesFoldOutRect, m_advancedTabFoldoutBiomes, m_editorUtils.GetContent("AdvancedFoldoutAddBiomes"));
            if (m_advancedTabFoldoutBiomes)
            {
                if (m_allBiomePresets.Exists(x => x.biomePreset == null && x.ID != -999))
                {
                    CreateAdvancedTabBiomesList();
                }

                //the hardcoded 15 are for some indent below the foldout label
                biomesFoldOutRect.x += 15;
                biomesFoldOutRect.width -= 15;
                biomesFoldOutRect.y += EditorGUIUtility.singleLineHeight * 1.5f;
                m_advancedTabBiomesList.DoList(biomesFoldOutRect);

                biomesFoldOutRect.y += m_advancedTabBiomesList.GetHeight() - 5;
                biomesFoldOutRect.x += biomesFoldOutRect.width * 0.65f;
                biomesFoldOutRect.width = biomesFoldOutRect.width * 0.35f;

                if (GUI.Button(biomesFoldOutRect, m_editorUtils.GetContent("CreateNewBiomeButton")))
                {
                    BiomePreset newPreset = ScriptableObject.CreateInstance<BiomePreset>();
                    //The term "Biome" will automatically be added
                    newPreset.name = "Custom";
                    BiomeController biomeController = newPreset.CreateBiome(false);
                    Selection.activeObject = biomeController;
                    EditorGUIUtility.PingObject(Selection.activeObject);
                }

                GUILayout.Space(m_advancedTabBiomesList.GetHeight() + EditorGUIUtility.singleLineHeight * 2f);
            }


            Rect spawnerFoldOutRect = EditorGUILayout.GetControlRect();
            m_advancedTabFoldoutSpawners = EditorGUI.Foldout(spawnerFoldOutRect, m_advancedTabFoldoutSpawners, m_editorUtils.GetContent("AdvancedFoldoutAddSpawners"));
            if (m_advancedTabFoldoutSpawners)
            {
                if (m_advancedTabAllSpawners.Exists(x => x.m_spawnerSettings == null))
                {
                    CreateAdvancedTabSpawnersList();
                }

                //the hardcoded 15 are for some indent below the foldout label
                spawnerFoldOutRect.x += 15;
                spawnerFoldOutRect.width -= 15;
                spawnerFoldOutRect.y += EditorGUIUtility.singleLineHeight * 1.5f;
                m_advancedTabSpawnersList.DoList(spawnerFoldOutRect);
                spawnerFoldOutRect.y += m_advancedTabSpawnersList.GetHeight() - 5;
                spawnerFoldOutRect.x += spawnerFoldOutRect.width * 0.65f;
                spawnerFoldOutRect.width = spawnerFoldOutRect.width * 0.35f;

                if (GUI.Button(spawnerFoldOutRect, m_editorUtils.GetContent("CreateNewSpawnerButton")))
                {
                    GameObject spawnerObj = new GameObject("New Spawner");
                    Spawner spawner = spawnerObj.AddComponent<Spawner>();
                    spawner.m_createdFromGaiaManager = true;
                    spawner.FitToAllTerrains();
                    Selection.activeGameObject = spawnerObj;
                }

                GUILayout.Space(m_advancedTabSpawnersList.GetHeight() + EditorGUIUtility.singleLineHeight * 2f);
            }
        }

        private void AdvancedPanelTerrain(bool helpEnabled)
        {
            if (m_editorUtils.ButtonAutoIndent("Show Session Manager"))
            {
                ShowSessionManager();
            }
            if (m_editorUtils.ButtonAutoIndent("Create Terrain"))
            {
                CreateTerrain(false);
            }
            if (m_editorUtils.ButtonAutoIndent("Create World Map Editor"))
            {
                GameObject worldMapObj = GaiaUtils.GetOrCreateWorldDesigner();
                Selection.activeObject = worldMapObj;
                worldMapObj.GetComponent<WorldMap>().LookAtWorldMap();
            }
            if (m_editorUtils.ButtonAutoIndent("Show Stamper"))
            {
                ShowStamper();
            }
        }


        /// <summary>
        /// Draw the extension editor
        /// </summary>
        void ExtensionsTab()
        {
#if !UNITY_2019_1_OR_NEWER

            EditorGUILayout.HelpBox(Application.unityVersion + " is not supported by Gaia, please use 2019.3+.", MessageType.Error);
            GUI.enabled = false;
#endif
            //GUILayout.Space(5f);

            m_editorUtils.Tabs(m_extensionsTabs);

            /*
            EditorGUI.indentLevel++;
            m_editorUtils.Panel("PanelExtensions", InstalledExtensionsTab, true);
            EditorGUI.indentLevel--;

            GUILayout.Space(5f);
            m_editorUtils.Label("GaiaExtensionsIntro1");
            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("GaiaExtensionsLink"), m_linkStyle))
            {
                Application.OpenURL("http://www.procedural-worlds.com/gaia/?section=gaia-extensions");
            }
            */
        }

        private void InstalledExtensionsTab()
        {
            GUILayout.Space(5f);

            //And scan if something has changed
            if (m_needsScan)
            {
                m_extensionMgr.ScanForExtensions();
                if (m_extensionMgr.GetInstalledExtensionCount() != 0)
                {
                    m_needsScan = false;
                }
            }

            int methodIdx = 0;
            string cmdName;
            string currFoldoutName = "";
            string prevFoldoutName = "";
            MethodInfo command;
            string[] cmdBreakOut = new string[0];
            List<GaiaCompatiblePackage> packages;
            List<GaiaCompatiblePublisher> publishers = m_extensionMgr.GetPublishers();

            foreach (GaiaCompatiblePublisher publisher in publishers)
            {
                if (publisher.InstalledPackages() > 0)
                {
                    if (publisher.m_installedFoldedOut = m_editorUtils.Foldout(publisher.m_installedFoldedOut, new GUIContent(publisher.m_publisherName)))
                    {
                        EditorGUI.indentLevel++;

                        packages = publisher.GetPackages();
                        foreach (GaiaCompatiblePackage package in packages)
                        {
                            if (package.m_isInstalled)
                            {
                                if (package.m_installedFoldedOut = m_editorUtils.Foldout(package.m_installedFoldedOut, new GUIContent(package.m_packageName)))
                                {
                                    EditorGUI.indentLevel++;
                                    methodIdx = 0;
                                    //Now loop thru and process
                                    while (methodIdx < package.m_methods.Count)
                                    {
                                        command = package.m_methods[methodIdx];
                                        cmdBreakOut = command.Name.Split('_');

                                        //Ignore if we are not a valid thing
                                        if ((cmdBreakOut.GetLength(0) != 2 && cmdBreakOut.GetLength(0) != 3) || cmdBreakOut[0] != "GX")
                                        {
                                            methodIdx++;
                                            continue;
                                        }

                                        //Get foldout and command name
                                        if (cmdBreakOut.GetLength(0) == 2)
                                        {
                                            currFoldoutName = "";
                                        }
                                        else
                                        {
                                            currFoldoutName = Regex.Replace(cmdBreakOut[1], "(\\B[A-Z])", " $1");
                                        }
                                        cmdName = Regex.Replace(cmdBreakOut[cmdBreakOut.GetLength(0) - 1], "(\\B[A-Z])", " $1");

                                        if (currFoldoutName == "")
                                        {
                                            methodIdx++;
                                            if (m_editorUtils.ButtonAutoIndent(new GUIContent(cmdName)))
                                            {
                                                command.Invoke(null, null);
                                            }
                                        }
                                        else
                                        {
                                            prevFoldoutName = currFoldoutName;

                                            //Make sure we have it in our dictionary
                                            if (!package.m_methodGroupFoldouts.ContainsKey(currFoldoutName))
                                            {
                                                package.m_methodGroupFoldouts.Add(currFoldoutName, false);
                                            }

                                            if (package.m_methodGroupFoldouts[currFoldoutName] = m_editorUtils.Foldout(package.m_methodGroupFoldouts[currFoldoutName], new GUIContent(currFoldoutName)))
                                            {
                                                EditorGUI.indentLevel++;

                                                while (methodIdx < package.m_methods.Count && currFoldoutName == prevFoldoutName)
                                                {
                                                    command = package.m_methods[methodIdx];
                                                    cmdBreakOut = command.Name.Split('_');

                                                    //Drop out if we are not a valid thing
                                                    if ((cmdBreakOut.GetLength(0) != 2 && cmdBreakOut.GetLength(0) != 3) || cmdBreakOut[0] != "GX")
                                                    {
                                                        methodIdx++;
                                                        continue;
                                                    }

                                                    //Get foldout and command name
                                                    if (cmdBreakOut.GetLength(0) == 2)
                                                    {
                                                        currFoldoutName = "";
                                                    }
                                                    else
                                                    {
                                                        currFoldoutName = Regex.Replace(cmdBreakOut[1], "(\\B[A-Z])", " $1");
                                                    }
                                                    cmdName = Regex.Replace(cmdBreakOut[cmdBreakOut.GetLength(0) - 1], "(\\B[A-Z])", " $1");

                                                    if (currFoldoutName != prevFoldoutName)
                                                    {
                                                        continue;
                                                    }

                                                    if (m_editorUtils.ButtonAutoIndent(new GUIContent(cmdName)))
                                                    {
                                                        command.Invoke(null, null);
                                                    }

                                                    methodIdx++;
                                                }

                                                EditorGUI.indentLevel--;
                                            }
                                            else
                                            {
                                                while (methodIdx < package.m_methods.Count && currFoldoutName == prevFoldoutName)
                                                {
                                                    command = package.m_methods[methodIdx];
                                                    cmdBreakOut = command.Name.Split('_');

                                                    //Drop out if we are not a valid thing
                                                    if ((cmdBreakOut.GetLength(0) != 2 && cmdBreakOut.GetLength(0) != 3) || cmdBreakOut[0] != "GX")
                                                    {
                                                        methodIdx++;
                                                        continue;
                                                    }

                                                    //Get foldout and command name
                                                    if (cmdBreakOut.GetLength(0) == 2)
                                                    {
                                                        currFoldoutName = "";
                                                    }
                                                    else
                                                    {
                                                        currFoldoutName = Regex.Replace(cmdBreakOut[1], "(\\B[A-Z])", " $1");
                                                    }
                                                    cmdName = Regex.Replace(cmdBreakOut[cmdBreakOut.GetLength(0) - 1], "(\\B[A-Z])", " $1");

                                                    if (currFoldoutName != prevFoldoutName)
                                                    {
                                                        continue;
                                                    }

                                                    methodIdx++;
                                                }
                                            }
                                        }
                                    }

                                    /*
                                    foreach (MethodInfo command in package.m_methods)
                                    {
                                        cmdBreakOut = command.Name.Split('_');

                                        if ((cmdBreakOut.GetLength(0) == 2 || cmdBreakOut.GetLength(0) == 3) && cmdBreakOut[0] == "GX")
                                        {
                                            if (cmdBreakOut.GetLength(0) == 2)
                                            {
                                                currFoldoutName = "";
                                            }
                                            else
                                            {
                                                currFoldoutName = cmdBreakOut[1];
                                                Debug.Log(currFoldoutName);
                                            }

                                            cmdName = Regex.Replace(cmdBreakOut[cmdBreakOut.GetLength(0) - 1], "(\\B[A-Z])", " $1");
                                            if (m_editorUtils.ButtonAutoIndent(new GUIContent(cmdName)))
                                            {
                                                command.Invoke(null, null);
                                            }
                                        }
                                    }
                                        */

                                    EditorGUI.indentLevel--;
                                }
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        void TutorialsAndSupportTab()
        {
            GUILayout.Space(5f);

            EditorGUI.indentLevel++;
            m_editorUtils.Text("Review the QuickStart guide and other product documentation in the Gaia / Documentation directory.");
            GUILayout.Space(5f);

            if (m_settings.m_hideHeroMessage)
            {
                if (m_editorUtils.ClickableHeadingNonLocalized(m_settings.m_latestNewsTitle))
                {
                    Application.OpenURL(m_settings.m_latestNewsUrl);
                }

                m_editorUtils.TextNonLocalized(m_settings.m_latestNewsBody);
                GUILayout.Space(5f);
            }

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Tutorials"), m_linkStyle))
            {
                Application.OpenURL("http://www.procedural-worlds.com/gaia/?section=tutorials-gaia-pro");
            }
            m_editorUtils.Text("Check our growing selection of video tutorials and support articles to become a Gaia Pro expert.");
            GUILayout.Space(5f);

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Join Our Community"), m_linkStyle))
            {
                Application.OpenURL("https://discord.gg/rtKn8rw");
            }
            m_editorUtils.Text("Whether you need an answer now or feel like a chat our friendly discord community is a great place to learn!");
            GUILayout.Space(5f);

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Ticketed Support"), m_linkStyle))
            {
                Application.OpenURL("https://proceduralworlds.freshdesk.com/support/home");
            }
            m_editorUtils.Text("Don't let your question get lost in the noise. All ticketed requests are answered, and usually within 48 hours.");
            GUILayout.Space(5f);

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Help us Grow - Rate & Review!"), m_linkStyle))
            {
                Application.OpenURL("https://assetstore.unity.com/publishers/15277");
            }
            m_editorUtils.Text("Quality products are a huge investment to create & support. Please take a moment to show your appreciation by leaving a rating & review.");
            GUILayout.Space(5f);

            if (m_settings.m_hideHeroMessage)
            {
                if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Show Hero Message"), m_linkStyle))
                {
                    m_settings.m_hideHeroMessage = false;
                    EditorUtility.SetDirty(m_settings);
                }
                m_editorUtils.Text("Show latest news and hero messages in Gaia.");
                GUILayout.Space(5f);
            }
            EditorGUI.indentLevel--;
        }

        void MoreOnProceduralWorldsTab()
        {
            GUILayout.Space(5f);

            EditorGUI.indentLevel++;
            m_editorUtils.Text("Super charge your development with our amazing partners & extensions.");
            GUILayout.Space(5f);

            if (m_settings.m_hideHeroMessage)
            {
                if (ClickableHeaderCustomStyle(m_editorUtils.GetContent(m_settings.m_latestNewsTitle), m_linkStyle))
                {
                    Application.OpenURL(m_settings.m_latestNewsUrl);
                }

                m_editorUtils.TextNonLocalized(m_settings.m_latestNewsBody);
                GUILayout.Space(5f);
            }

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Our Partners"), m_linkStyle))
            {
                Application.OpenURL("http://www.procedural-worlds.com/partners/");
            }
            m_editorUtils.Text("The content included with Gaia is an awesome starting point for your game, but that's just the tip of the iceberg. Learn more about how these talented publishers can help you to create amazing environments in Unity.");
            GUILayout.Space(5f);

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Gaia eXtensions (GX)"), m_linkStyle))
            {
                Application.OpenURL("http://www.procedural-worlds.com/gaia/?section=gaia-extensions");
            }
            m_editorUtils.Text("Gaia eXtensions accelerate and simplify your development by automating asset setup in your scene. Check out the quality assets we have integrated for you!");
            GUILayout.Space(5f);

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Help Us to Grow - Spread The Word!"), m_linkStyle))
            {
                Application.OpenURL("https://www.facebook.com/proceduralworlds/");
            }
            m_editorUtils.Text("Get regular news updates and help us to grow by liking and sharing our Facebook page!");
            GUILayout.Space(5f);

            if (m_settings.m_hideHeroMessage)
            {
                if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Show Hero Message"), m_linkStyle))
                {
                    m_settings.m_hideHeroMessage = false;
                    EditorUtility.SetDirty(m_settings);
                }
                m_editorUtils.Text("Show latest news and hero messages in Gaia.");
                GUILayout.Space(5f);
            }
            EditorGUI.indentLevel--;
        }
        #endregion


        void OnInspectorUpdate()
        {
            if (!m_statusCheckPerformed)
            {
                GaiaManagerStatusCheck();
            }
        }


        #region On GUI
        void OnGUI()
        {
            m_editorUtils.Initialize(); // Do not remove this!

            //Set up the box style
            if (m_boxStyle == null)
            {
                m_boxStyle = new GUIStyle(GUI.skin.box);
                m_boxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                m_boxStyle.fontStyle = FontStyle.Bold;
                m_boxStyle.alignment = TextAnchor.UpperLeft;
            }

            //Setup the wrap style
            if (m_wrapStyle == null)
            {
                m_wrapStyle = new GUIStyle(GUI.skin.label);
                m_wrapStyle.fontStyle = FontStyle.Normal;
                m_wrapStyle.wordWrap = true;
            }

            if (m_bodyStyle == null)
            {
                m_bodyStyle = new GUIStyle(GUI.skin.label);
                m_bodyStyle.fontStyle = FontStyle.Normal;
                m_bodyStyle.wordWrap = true;
            }

            if (m_titleStyle == null)
            {
                m_titleStyle = new GUIStyle(m_bodyStyle);
                m_titleStyle.fontStyle = FontStyle.Bold;
                m_titleStyle.fontSize = 20;
            }

            if (m_headingStyle == null)
            {
                m_headingStyle = new GUIStyle(m_bodyStyle);
                m_headingStyle.fontStyle = FontStyle.Bold;
            }

            if (m_linkStyle == null)
            {
                m_linkStyle = new GUIStyle(m_bodyStyle);
                m_linkStyle.wordWrap = false;
                m_linkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
                m_linkStyle.stretchWidth = false;
            }

            //Check if we are currently creating new terrains in a coroutine - need to lock the GUI then
            if (m_terrainCreationRunning)
            {
                GUI.enabled = false;
            }

            //Check for state of compiler
            if (EditorApplication.isCompiling)
            {
                m_needsScan = true;
            }


            m_editorUtils.GUIHeader();

            GUILayout.Space(4);

            m_editorUtils.TabsNoBorder(m_mainTabs);


            if (m_settings.m_pipelineProfile.m_pipelineSwitchUpdates)
            {
                EditorApplication.update -= EditorPipelineUpdate;
                EditorApplication.update += EditorPipelineUpdate;
            }
            else
            {
                EditorApplication.update -= EditorPipelineUpdate;
            }

        }

        private void NewWorldSettings(bool helpEnabled)
        {
            m_editorUtils.InlineHelp("World Size", helpEnabled);
            Rect rect = EditorGUILayout.GetControlRect();

            float lineHeight = EditorGUIUtility.singleLineHeight + 3;
            Rect labelRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, EditorGUIUtility.labelWidth, lineHeight);
            Rect fieldRect = new Rect(labelRect.x + labelRect.width, rect.y, (rect.width - EditorGUIUtility.labelWidth - labelRect.width), lineHeight);

            //World size settings

            EditorGUI.LabelField(new Rect(labelRect.x - EditorGUIUtility.labelWidth, labelRect.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("World Size").text, m_editorUtils.Styles.heading);
            m_oldTargetSize = m_settings.m_currentSize;
            GaiaConstants.EnvironmentSizePreset oldTargetSizePreset = m_settings.m_targeSizePreset;
            m_settings.m_targeSizePreset = (GaiaConstants.EnvironmentSizePreset)EditorGUI.EnumPopup(new Rect(labelRect.x, labelRect.y, labelRect.width + fieldRect.width - 23f, labelRect.height), m_settings.m_targeSizePreset);

            if (m_settings.m_targeSizePreset == GaiaConstants.EnvironmentSizePreset.Custom && oldTargetSizePreset != GaiaConstants.EnvironmentSizePreset.Custom)
            {
                //User just switched to custom -> unfold the extra options
                m_foldOutWorldSizeSettings = true;
            }

            GUIContent btnState = new GUIContent("+");
            if (m_foldOutWorldSizeSettings)
            {
                btnState = new GUIContent("-");
            }
            if (GUI.Button(new Rect(labelRect.x + labelRect.width + fieldRect.width - 20f, labelRect.y, 20f, labelRect.height - 3f), btnState))
            {
                m_foldOutWorldSizeSettings = !m_foldOutWorldSizeSettings;
            }

            switch (m_settings.m_targeSizePreset)
            {
                case GaiaConstants.EnvironmentSizePreset.Tiny:
                    m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is256MetersSq;
                    m_settings.m_currentDefaults.m_terrainSize = GaiaUtils.EnvironmentSizeToInt(m_settings.m_currentSize);
                    break;
                case GaiaConstants.EnvironmentSizePreset.Small:
                    m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is512MetersSq;
                    m_settings.m_currentDefaults.m_terrainSize = GaiaUtils.EnvironmentSizeToInt(m_settings.m_currentSize);
                    break;
                case GaiaConstants.EnvironmentSizePreset.Medium:
                    m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is1024MetersSq;
                    m_settings.m_currentDefaults.m_terrainSize = GaiaUtils.EnvironmentSizeToInt(m_settings.m_currentSize);
                    break;
                case GaiaConstants.EnvironmentSizePreset.Large:
                    m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is2048MetersSq;
                    m_settings.m_currentDefaults.m_terrainSize = GaiaUtils.EnvironmentSizeToInt(m_settings.m_currentSize);
                    break;
                case GaiaConstants.EnvironmentSizePreset.XLarge:
                    m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is4096MetersSq;
                    m_settings.m_currentDefaults.m_terrainSize = GaiaUtils.EnvironmentSizeToInt(m_settings.m_currentSize);
                    break;
            }

            if (m_settings.m_targeSizePreset != GaiaConstants.EnvironmentSizePreset.Custom)
            {
                m_settings.m_createTerrainScenes = false;
                m_settings.m_unloadTerrainScenes = false;
                m_settings.m_floatingPointFix = false;
                m_showAutoStreamSettingsBox = false;
                m_settings.m_tilesX = 1;
                m_settings.m_tilesZ = 1;
            }

            Rect foldOutWorldSizeRect = EditorGUILayout.GetControlRect();
            //m_foldOutWorldSizeSettings = EditorGUI.Foldout(new Rect(foldOutWorldSizeRect.x + EditorGUIUtility.labelWidth, foldOutWorldSizeRect.y, foldOutWorldSizeRect.width, foldOutWorldSizeRect.height), m_foldOutWorldSizeSettings, m_editorUtils.GetContent("AdvancedWorldSize"));
            if (m_foldOutWorldSizeSettings)
            {
                //Label
                EditorGUI.LabelField(new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y + lineHeight, rect.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("AdvancedWorldSize"), m_editorUtils.Styles.heading);

                EditorGUI.indentLevel++;

                //X Label
                Rect numFieldRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y + lineHeight * 2, (rect.width - EditorGUIUtility.labelWidth) * 0.2f, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(numFieldRect, m_editorUtils.GetContent("X Tiles"));
                // X Entry Field
                numFieldRect = new Rect(numFieldRect.x + numFieldRect.width, numFieldRect.y, numFieldRect.width, EditorGUIUtility.singleLineHeight);
                int oldTilesX = m_settings.m_tilesX;
                int tilesX = EditorGUI.IntField(numFieldRect, m_settings.m_tilesX);
                //Empty Label Field for Spacing
                numFieldRect = new Rect(numFieldRect.x + numFieldRect.width, numFieldRect.y, numFieldRect.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(numFieldRect, " ");
                //Z Label
                numFieldRect = new Rect(numFieldRect.x + numFieldRect.width, numFieldRect.y, numFieldRect.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(numFieldRect, m_editorUtils.GetContent("Z Tiles"));
                // Z Entry Field
                numFieldRect = new Rect(numFieldRect.x + numFieldRect.width, numFieldRect.y, numFieldRect.width, EditorGUIUtility.singleLineHeight);
                int oldTilesZ = m_settings.m_tilesZ;
                int tilesZ = EditorGUI.IntField(numFieldRect, m_settings.m_tilesZ);
                //Empty Label Field for Spacing

                labelRect.y = numFieldRect.y + lineHeight;
                fieldRect.y = labelRect.y;
                GUILayout.Space(lineHeight * 3);

                EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Terrain Size"));
                m_settings.m_currentSize = (GaiaConstants.EnvironmentSize)EditorGUI.EnumPopup(fieldRect, m_settings.m_currentSize);


                labelRect.y += lineHeight;
                fieldRect.y += lineHeight;

#if !GAIA_PRO_PRESENT
                bool currentGUIState = GUI.enabled;
                GUI.enabled = false;
                Rect proOnlyRect = new Rect(fieldRect);
                proOnlyRect.x += 30;
                proOnlyRect.width = 100;

#endif


#if GAIA_PRO_PRESENT
                //automatic activation of create terrain scenes / unload terrain scenes at a certain world size
                if (oldTilesX < 3 && tilesX >= 3 && tilesZ < 3 || oldTilesZ < 3 && tilesZ >= 3 && tilesX < 3)
                {
                    m_settings.m_createTerrainScenes = true;
                    m_settings.m_unloadTerrainScenes = true;
                    m_settings.m_floatingPointFix = true;
                    m_showAutoStreamSettingsBox = true;
                }
#endif

                if (m_showAutoStreamSettingsBox)
                {
                    Rect helpBoxRect = new Rect(fieldRect);
                    helpBoxRect.x += 50;
                    helpBoxRect.y += 4;
                    helpBoxRect.width -= 50;
                    helpBoxRect.height = 40;
                    EditorGUI.HelpBox(helpBoxRect, m_editorUtils.GetTextValue("AutoStreamSettings"), MessageType.Info);
                }

                GUILayout.Space(lineHeight);
                EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("CreateTerrainScenes"));
                m_settings.m_createTerrainScenes = EditorGUI.Toggle(fieldRect, m_settings.m_createTerrainScenes);
#if !GAIA_PRO_PRESENT
                EditorGUI.LabelField(proOnlyRect, m_editorUtils.GetContent("GaiaProOnly"));
#endif

                labelRect.y += lineHeight;
                fieldRect.y += lineHeight;

                GUILayout.Space(lineHeight);

                EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("UnloadTerrainScenes"));
                m_settings.m_unloadTerrainScenes = EditorGUI.Toggle(fieldRect, m_settings.m_unloadTerrainScenes);
#if !GAIA_PRO_PRESENT
                proOnlyRect.y += lineHeight;
                EditorGUI.LabelField(proOnlyRect, m_editorUtils.GetContent("GaiaProOnly"));
#endif

                labelRect.y += lineHeight;
                fieldRect.y += lineHeight;
                GUILayout.Space(lineHeight);

                EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("FloatingPointFix"));
                m_settings.m_floatingPointFix = EditorGUI.Toggle(fieldRect, m_settings.m_floatingPointFix);
#if !GAIA_PRO_PRESENT
                proOnlyRect.y += lineHeight;
                EditorGUI.LabelField(proOnlyRect, m_editorUtils.GetContent("GaiaProOnly"));
#endif
                if (m_settings.m_createTerrainScenes || m_settings.m_unloadTerrainScenes || m_settings.m_floatingPointFix)
                {
                    m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Custom;
                }

#if !GAIA_PRO_PRESENT
                GUI.enabled = currentGUIState;
#endif

                labelRect.y += lineHeight;
                fieldRect.y += lineHeight;
                GUILayout.Space(lineHeight);

                int world_xDimension = m_settings.m_tilesX * m_settings.m_currentDefaults.m_terrainSize;
                int world_zDimension = m_settings.m_tilesZ * m_settings.m_currentDefaults.m_terrainSize;
                int numberOfTerrains = m_settings.m_tilesX * m_settings.m_tilesZ;

                string worldXText = String.Format("{0:0} m", world_xDimension);
                string worldZText = String.Format("{0:0} m", world_zDimension);
                if (world_xDimension > 1000 || world_zDimension > 1000)
                {
                    worldXText = String.Format("{0:0.00} km", world_xDimension / 1000f);
                    worldZText = String.Format("{0:0.00} km", world_zDimension / 1000f);
                }

                GUIContent worldSizeInfo = new GUIContent(m_editorUtils.GetContent("TotalWorldSize").text + String.Format(": {0} x {1}, " + m_editorUtils.GetContent("Terrains").text + ": {2}", worldXText, worldZText, numberOfTerrains));
                EditorGUI.LabelField(new Rect(labelRect.x, labelRect.y, labelRect.width + fieldRect.width, labelRect.height), worldSizeInfo, m_editorUtils.Styles.heading);

                if (tilesX != m_settings.m_tilesX || tilesZ != m_settings.m_tilesZ || m_oldTargetSize != m_settings.m_currentSize)
                {

                    m_settings.m_tilesX = tilesX;
                    m_settings.m_tilesZ = tilesZ;

                    if (m_settings.m_tilesX > 1 ||
                        m_settings.m_tilesZ > 1 ||
                        m_settings.m_currentSize == GaiaConstants.EnvironmentSize.Is8192MetersSq ||
                        m_settings.m_currentSize == GaiaConstants.EnvironmentSize.Is16384MetersSq
                        )
                    {
                        m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Custom;
                    }
                    else
                    {
                        switch (m_settings.m_currentSize)
                        {
                            case GaiaConstants.EnvironmentSize.Is256MetersSq:
                                m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Tiny;
                                break;
                            case GaiaConstants.EnvironmentSize.Is512MetersSq:
                                m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Small;
                                break;
                            case GaiaConstants.EnvironmentSize.Is1024MetersSq:
                                m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Medium;
                                break;
                            case GaiaConstants.EnvironmentSize.Is2048MetersSq:
                                m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Large;
                                break;
                            case GaiaConstants.EnvironmentSize.Is4096MetersSq:
                                m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.XLarge;
                                break;
                        }

                    }

                    EditorUtility.SetDirty(m_settings);
                }
                EditorGUI.indentLevel--;
            } //end of foldout

            labelRect.y += lineHeight * 1.3f;
            fieldRect.y += lineHeight * 1.3f;

            //Quality

            if (helpEnabled)
            {
                //GUILayout.Space(lineHeight * 0.5f);
                m_editorUtils.InlineHelp("Quality Header", helpEnabled);
                labelRect.y += GUILayoutUtility.GetLastRect().height;
                fieldRect.y += GUILayoutUtility.GetLastRect().height;
                GUILayout.Space(lineHeight * 1f);
                labelRect.y += lineHeight * 0.75f;
                fieldRect.y += lineHeight * 0.75f;
            }



            EditorGUI.LabelField(new Rect(labelRect.x - EditorGUIUtility.labelWidth, labelRect.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("Quality Header").text, m_editorUtils.Styles.heading);
            m_oldTargetEnv = m_settings.m_currentEnvironment;
            m_settings.m_currentEnvironment = (GaiaConstants.EnvironmentTarget)EditorGUI.EnumPopup(new Rect(labelRect.x, labelRect.y, labelRect.width + fieldRect.width - 23f, labelRect.height), m_settings.m_currentEnvironment);


            if (m_settings.m_currentEnvironment == GaiaConstants.EnvironmentTarget.Custom && m_oldTargetEnv != GaiaConstants.EnvironmentTarget.Custom)
            {
                //User just switched to custom -> unfold the extra options
                m_foldoutTerrainResolutionSettings = true;
            }


            btnState = new GUIContent("+");
            if (m_foldoutTerrainResolutionSettings)
            {
                btnState = new GUIContent("-");
                GUILayout.Space(lineHeight * 5f);
            }
            if (GUI.Button(new Rect(labelRect.x + labelRect.width + fieldRect.width - 20f, labelRect.y, 20f, labelRect.height - 3f), btnState))
            {
                m_foldoutTerrainResolutionSettings = !m_foldoutTerrainResolutionSettings;
            }


            labelRect.y += lineHeight;
            fieldRect.y += lineHeight;

            bool resSettingsChangeCheck = false;

            Rect resSettingsRect = EditorGUILayout.GetControlRect();
            resSettingsRect.y = labelRect.y;
            //m_foldoutTerrainResolutionSettings = EditorGUI.Foldout(new Rect(resSettingsRect.x + EditorGUIUtility.labelWidth, resSettingsRect.y, resSettingsRect.width, resSettingsRect.height), m_foldoutTerrainResolutionSettings, m_editorUtils.GetContent("AdvancedQuality"));
            if (m_foldoutTerrainResolutionSettings)
            {
                //Label
                EditorGUI.LabelField(new Rect(resSettingsRect.x + EditorGUIUtility.labelWidth, resSettingsRect.y, resSettingsRect.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("AdvancedQuality"), m_editorUtils.Styles.heading);

                EditorGUI.indentLevel++;
                resSettingsChangeCheck = TerrainResolutionSettingsEnabled(resSettingsRect, false);
                EditorGUI.indentLevel--;
                labelRect.y += EditorGUIUtility.singleLineHeight * 6;
                fieldRect.y += EditorGUIUtility.singleLineHeight * 6;
            }

            labelRect.y += lineHeight * 0.3f;
            fieldRect.y += lineHeight * 0.3f;

            //Default biome

            if (helpEnabled)
            {
                m_editorUtils.InlineHelp("BiomePreset", helpEnabled);
                labelRect.y += GUILayoutUtility.GetLastRect().height;
                fieldRect.y += GUILayoutUtility.GetLastRect().height;
                labelRect.y += lineHeight * 0.75f;
                fieldRect.y += lineHeight * 0.75f;
            }

            EditorGUI.LabelField(new Rect(labelRect.x - EditorGUIUtility.labelWidth, labelRect.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("BiomePreset").text, m_editorUtils.Styles.heading);
            int lastBiomePresetSelection = m_biomePresetSelection;
            if (m_biomePresetSelection == int.MinValue)
            {
                m_biomePresetSelection = 0;
            }

            m_biomePresetSelection = EditorGUI.IntPopup(new Rect(labelRect.x, labelRect.y, labelRect.width + fieldRect.width - 23f, labelRect.height), m_biomePresetSelection, m_allBiomePresets.Select(x => x.name).ToArray(), m_allBiomePresets.Select(x => x.ID).ToArray());

            if (lastBiomePresetSelection != m_biomePresetSelection)
            {
                AddBiomeSpawnersForSelectedPreset();
                //re-create the reorderable list with the new contents
                CreateBiomePresetList();
            }

            if (m_biomePresetSelection == -999 && lastBiomePresetSelection != -999)
            {
                //user just switched to "Custom", foldout the extended options
                m_foldoutSpawnerSettings = true;
            }

            btnState = new GUIContent("+");
            if (m_foldoutSpawnerSettings)
            {
                btnState = new GUIContent("-");
            }
            if (GUI.Button(new Rect(labelRect.x + labelRect.width + fieldRect.width - 20f, labelRect.y, 20f, labelRect.height - 3f), btnState))
            {
                m_foldoutSpawnerSettings = !m_foldoutSpawnerSettings;
            }

            labelRect.y += lineHeight;
            fieldRect.y += lineHeight;
            GUILayout.Space(lineHeight);

            Rect spawnerFoldOutRect = EditorGUILayout.GetControlRect();
            spawnerFoldOutRect.y = labelRect.y;

            //m_foldoutSpawnerSettings = EditorGUI.Foldout(new Rect(spawnerFoldOutRect.x + EditorGUIUtility.labelWidth, spawnerFoldOutRect.y, spawnerFoldOutRect.width, spawnerFoldOutRect.height), m_foldoutSpawnerSettings, m_editorUtils.GetContent("AdvancedSpawners"));

            if (m_foldoutSpawnerSettings)
            {
                //Label
                EditorGUI.LabelField(new Rect(spawnerFoldOutRect.x + EditorGUIUtility.labelWidth, spawnerFoldOutRect.y, spawnerFoldOutRect.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("DefaultBiomeSettings"), m_editorUtils.Styles.heading);

                //the hardcoded 15 are for some indent below the foldout label
                Rect listRect = new Rect(spawnerFoldOutRect.x + EditorGUIUtility.labelWidth + 15, spawnerFoldOutRect.y + EditorGUIUtility.singleLineHeight + (EditorGUIUtility.singleLineHeight * 0.25f), spawnerFoldOutRect.width - EditorGUIUtility.labelWidth - 15, m_biomeSpawnersList.GetHeight()); //EditorGUILayout.GetControlRect(true, m_spawnerPresetList.GetHeight());
                m_biomeSpawnersList.DoList(listRect);
                GUILayout.Space(m_biomeSpawnersList.GetHeight());
                labelRect.y += m_biomeSpawnersList.GetHeight();
                fieldRect.y += m_biomeSpawnersList.GetHeight();
            }

            //Scene settings
            if (helpEnabled)
            {
                GUILayout.Space(lineHeight * 0.5f);
                m_editorUtils.InlineHelp("Extras", helpEnabled);
                labelRect.y += GUILayoutUtility.GetLastRect().height;
                fieldRect.y += GUILayoutUtility.GetLastRect().height;
                labelRect.y += lineHeight * 1.5f;
                fieldRect.y += lineHeight * 1.5f;
                GUILayout.Space(GUILayoutUtility.GetLastRect().height);

            }
            else
            {
                labelRect.y += lineHeight * 0.5f;
                fieldRect.y += lineHeight * 0.5f;
                //GUILayout.Space(lineHeight * 2f);
            }

            EditorGUI.LabelField(new Rect(labelRect.x - EditorGUIUtility.labelWidth, labelRect.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("Extras").text, m_editorUtils.Styles.heading);

            Rect extrasFoldOut = EditorGUILayout.GetControlRect();
            extrasFoldOut.y = labelRect.y;
            m_foldoutExtrasSettings = EditorGUI.Foldout(new Rect(extrasFoldOut.x + EditorGUIUtility.labelWidth, extrasFoldOut.y, extrasFoldOut.width, extrasFoldOut.height), m_foldoutExtrasSettings, m_editorUtils.GetContent("AdvancedExtras"));
            if (m_foldoutExtrasSettings)
            {
                EditorGUI.indentLevel++;
                ExtrasSettingsEnabled(extrasFoldOut, helpEnabled);
                EditorGUI.indentLevel--;
                labelRect.y += EditorGUIUtility.singleLineHeight * 4;
                fieldRect.y += EditorGUIUtility.singleLineHeight * 4;
                if (m_settings.m_pipelineProfile.m_activePipelineInstalled != m_settings.m_currentRenderer)
                {
                    //Need more space when the change pipeline button is drawn
                    GUILayout.Space(EditorGUIUtility.singleLineHeight * 12);
                }
                else
                {
                    if (helpEnabled)
                    {
                        GUILayout.Space(EditorGUIUtility.singleLineHeight * 11);
                    }
                    else
                    {
                        GUILayout.Space(EditorGUIUtility.singleLineHeight * 10);
                    }
                }

                GaiaLightingProfile lightingProfile = m_settings.m_gaiaLightingProfile;
#if !GAIA_PRO_PRESENT
                //need extra space when the Gaia Pro info is displayed
                if (lightingProfile.m_selectedLightingProfileValuesIndex > 0 && lightingProfile.m_selectedLightingProfileValuesIndex < lightingProfile.m_lightingProfiles.Count)
                {
                    if (lightingProfile.m_lightingProfiles[lightingProfile.m_selectedLightingProfileValuesIndex].m_typeOfLighting == "Procedural Worlds Sky")
                    {
                        GUILayout.Space(EditorGUIUtility.singleLineHeight * 4.5f);
                    }
                }
#endif
            }

            //Evaluate the resolution settings etc. according to what the user choses in the Manager
            //we only want to execute this on initially opening the window, or when settings have changed
            if (m_initResSettings || resSettingsChangeCheck || m_oldTargetEnv != m_settings.m_currentEnvironment || m_oldTargetSize != m_settings.m_currentSize)
            {
                if (m_oldTargetEnv != m_settings.m_currentEnvironment)
                {
                    switch (m_settings.m_currentEnvironment)
                    {
                        case GaiaConstants.EnvironmentTarget.UltraLight:
                            m_settings.m_currentDefaults = m_settings.m_ultraLightDefaults;
                            //m_settings.m_currentResources = m_settings.m_ultraLightResources;
                            //m_settings.m_currentGameObjectResources = m_settings.m_ultraLightGameObjectResources;
                            m_settings.m_currentWaterPrefabName = m_settings.m_waterMobilePrefabName;
                            //m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is512MetersSq;
                            break;
                        case GaiaConstants.EnvironmentTarget.MobileAndVR:
                            m_settings.m_currentDefaults = m_settings.m_mobileDefaults;
                            //m_settings.m_currentResources = m_settings.m_mobileResources;
                            //m_settings.m_currentGameObjectResources = m_settings.m_mobileGameObjectResources;
                            m_settings.m_currentWaterPrefabName = m_settings.m_waterMobilePrefabName;
                            //m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is1024MetersSq;
                            break;
                        case GaiaConstants.EnvironmentTarget.Desktop:
                            m_settings.m_currentDefaults = m_settings.m_desktopDefaults;
                            //m_settings.m_currentResources = m_settings.m_desktopResources;
                            //m_settings.m_currentGameObjectResources = m_settings.m_desktopGameObjectResources;
                            m_settings.m_currentWaterPrefabName = m_settings.m_waterPrefabName;
                            //m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is2048MetersSq;
                            break;
                        case GaiaConstants.EnvironmentTarget.PowerfulDesktop:
                            m_settings.m_currentDefaults = m_settings.m_powerDesktopDefaults;
                            //m_settings.m_currentResources = m_settings.m_powerDesktopResources;
                            //m_settings.m_currentGameObjectResources = m_settings.m_powerDesktopGameObjectResources;
                            m_settings.m_currentWaterPrefabName = m_settings.m_waterPrefabName;
                            //m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is2048MetersSq;
                            break;
                    }
                }

                m_settings.m_currentDefaults.m_terrainSize = GaiaUtils.EnvironmentSizeToInt(m_settings.m_currentSize);

                GaiaUtils.SetSettingsForEnvironment(m_settings, m_settings.m_currentEnvironment);

                if (m_settings.m_currentEnvironment != GaiaConstants.EnvironmentTarget.Custom)
                {
                    m_heightmapResolution = (GaiaConstants.HeightmapResolution)m_settings.m_currentDefaults.m_heightmapResolution;
                    m_controlTextureResolution = (GaiaConstants.TerrainTextureResolution)m_settings.m_currentDefaults.m_controlTextureResolution;
                    m_basemapResolution = (GaiaConstants.TerrainTextureResolution)m_settings.m_currentDefaults.m_baseMapSize;
                    m_detailResolutionPerPatch = m_settings.m_currentDefaults.m_detailResolutionPerPatch;
                    m_detailResolution = m_settings.m_currentDefaults.m_detailResolution;
                }
                m_settings.m_currentDefaults.m_heightmapResolution = (int)m_heightmapResolution;
                m_settings.m_currentDefaults.m_controlTextureResolution = (int)m_controlTextureResolution;
                m_settings.m_currentDefaults.m_baseMapSize = (int)m_basemapResolution;
                m_detailResolutionPerPatch = Mathf.RoundToInt(Mathf.Clamp(m_detailResolutionPerPatch, 8, 128));
                m_detailResolution = Mathf.RoundToInt(Mathf.Clamp(m_detailResolution, 0, 4096));
                m_settings.m_currentDefaults.m_detailResolutionPerPatch = m_detailResolutionPerPatch;
                m_settings.m_currentDefaults.m_detailResolution = m_detailResolution;
                m_initResSettings = false;
                EditorUtility.SetDirty(m_settings);
                EditorUtility.SetDirty(m_settings.m_currentDefaults);
            }



        }

        internal void UpdateAllSpawnersList()
        {
            CreateAdvancedTabSpawnersList();
        }

        /// <summary>
        /// Terrain resolution settings foldout
        /// </summary>
        /// <param name="helpEnabled"></param>
        private bool TerrainResolutionSettingsEnabled(Rect rect, bool helpEnabled)
        {
            EditorGUI.BeginChangeCheck();



            Rect labelRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, EditorGUIUtility.labelWidth, rect.height);
            Rect fieldRect = new Rect(labelRect.x + EditorGUIUtility.labelWidth, rect.y, rect.width - labelRect.width - EditorGUIUtility.labelWidth, rect.height);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            ////Display notice that these fields cannot be edited if the custom setting is not chosen
            //if (m_targetEnv != GaiaConstants.EnvironmentTarget.Custom)
            //{
            //    EditorGUI.LabelField(fieldRect, m_editorUtils.GetContent("QualityCustomNotice"));
            //    labelRect.y += EditorGUIUtility.singleLineHeight;
            //    fieldRect.y += EditorGUIUtility.singleLineHeight;
            //    GUI.enabled = false;
            //}

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Heightmap Resolution"));
            m_heightmapResolution = (GaiaConstants.HeightmapResolution)EditorGUI.EnumPopup(fieldRect, m_heightmapResolution);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Control Texture Resolution"));
            m_controlTextureResolution = (GaiaConstants.TerrainTextureResolution)EditorGUI.EnumPopup(fieldRect, m_controlTextureResolution);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Basemap Resolution"));
            m_basemapResolution = (GaiaConstants.TerrainTextureResolution)EditorGUI.EnumPopup(fieldRect, m_basemapResolution);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Detail Resolution Per Patch"));
            m_detailResolutionPerPatch = EditorGUI.IntField(fieldRect, m_detailResolutionPerPatch);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Detail Resolution"));
            m_detailResolution = EditorGUI.IntField(fieldRect, m_detailResolution);



            //m_heightmapResolution = (GaiaConstants.HeightmapResolution)m_editorUtils.EnumPopup("Heightmap Resolution", m_heightmapResolution, helpEnabled);
            //m_controlTextureResolution = (GaiaConstants.TerrainTextureResolution)m_editorUtils.EnumPopup("Control Texture Resolution", m_controlTextureResolution, helpEnabled);
            //m_basemapResolution = (GaiaConstants.TerrainTextureResolution)m_editorUtils.EnumPopup("Basemap Resolution", m_basemapResolution, helpEnabled);
            //m_detailResolutionPerPatch = m_editorUtils.IntField("Detail Resolution Per Patch", m_detailResolutionPerPatch, helpEnabled);
            //m_detailResolution = m_editorUtils.IntField("Detail Resolution", m_detailResolution, helpEnabled);

            bool changeCheckTriggered = false;

            if (EditorGUI.EndChangeCheck())
            {
                m_settings.m_currentEnvironment = GaiaConstants.EnvironmentTarget.Custom;
                changeCheckTriggered = true;
            }

            return changeCheckTriggered;


        }

        private bool ExtrasSettingsEnabled(Rect rect, bool helpEnabled)
        {
            EditorGUI.BeginChangeCheck();

            Rect labelRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, EditorGUIUtility.labelWidth, rect.height);
            Rect fieldRect = new Rect(labelRect.x + EditorGUIUtility.labelWidth, rect.y, rect.width - labelRect.width - EditorGUIUtility.labelWidth, rect.height);
            Rect buttonRect = labelRect;

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;


            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Controller"));
            m_settings.m_currentController = (GaiaConstants.EnvironmentControllerType)EditorGUI.EnumPopup(fieldRect, m_settings.m_currentController);
            switch (m_settings.m_currentController)
            {
                case GaiaConstants.EnvironmentControllerType.FirstPerson:
                    m_settings.m_currentPlayerPrefabName = "FPSController";
                    break;
                case GaiaConstants.EnvironmentControllerType.ThirdPerson:
                    m_settings.m_currentPlayerPrefabName = "ThirdPersonController";
                    break;
                case GaiaConstants.EnvironmentControllerType.FlyingCamera:
                    m_settings.m_currentPlayerPrefabName = "FlyCam";
                    break;
            }

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            //Skies

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Skies"));

            GaiaLightingProfile lightingProfile = m_settings.m_gaiaLightingProfile;

            //Building up a value array of incrementing ints of the size of the lighting profile values array, this array will then match the displayed string selection in the popup
            int[] lightingProfileValuesIndices = Enumerable
                                .Repeat(0, (int)((lightingProfile.m_lightingProfiles.Count() - 1) / 1) + 1)
                                .Select((tr, ti) => tr + (1 * ti))
                                .ToArray();
            string[] profileNames = lightingProfile.m_lightingProfiles.Select(x => x.m_typeOfLighting).ToArray();

            //Injecting the "None" option
            lightingProfileValuesIndices = GaiaUtils.AddElementToArray(lightingProfileValuesIndices, -99);
            profileNames = GaiaUtils.AddElementToArray(profileNames, "None");

            lightingProfile.m_selectedLightingProfileValuesIndex = EditorGUI.IntPopup(fieldRect, lightingProfile.m_selectedLightingProfileValuesIndex, profileNames, lightingProfileValuesIndices);
#if !GAIA_PRO_PRESENT
            if (lightingProfile.m_selectedLightingProfileValuesIndex > 0 && lightingProfile.m_selectedLightingProfileValuesIndex < lightingProfile.m_lightingProfiles.Count)
            {
                if (lightingProfile.m_lightingProfiles[lightingProfile.m_selectedLightingProfileValuesIndex].m_profileType == GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                {
                    labelRect.y += EditorGUIUtility.singleLineHeight * 1.5f;
                    fieldRect.y += EditorGUIUtility.singleLineHeight * 1.5f;
                    EditorGUI.HelpBox(new Rect(labelRect.position, new Vector2(labelRect.width + fieldRect.width, EditorGUIUtility.singleLineHeight * 3f)), m_editorUtils.GetTextValue("GaiaProLightingProfileInfo"), MessageType.Info);
                    labelRect.y += EditorGUIUtility.singleLineHeight * 2.5f;
                    fieldRect.y += EditorGUIUtility.singleLineHeight * 2.5f;
                }
            }
#endif

            if (lightingProfile.m_selectedLightingProfileValuesIndex != -99)
            {
#if UNITY_POST_PROCESSING_STACK_V2
                labelRect.y += EditorGUIUtility.singleLineHeight;
                fieldRect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("PostProcessing"));
                m_settings.m_enablePostProcessing = EditorGUI.Toggle(fieldRect, m_settings.m_enablePostProcessing);
#endif
            }

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            //Water
            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Water"));
            if (newProfileListIndex > m_settings.m_gaiaWaterProfile.m_waterProfiles.Count)
            {
                newProfileListIndex = 0;
            }
            m_profileList.Clear();
            if (m_settings.m_gaiaWaterProfile.m_waterProfiles.Count > 0)
            {
                foreach (GaiaWaterProfileValues profile in m_settings.m_gaiaWaterProfile.m_waterProfiles)
                {
                    m_profileList.Add(profile.m_typeOfWater);
                }
            }
            m_profileList.Add("None");
            newProfileListIndex = EditorGUI.Popup(fieldRect, newProfileListIndex, m_profileList.ToArray());

            if (m_settings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex != newProfileListIndex)
            {
                m_settings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex = newProfileListIndex;
            }
            if (m_profileList[newProfileListIndex] != "None")
            {
                labelRect.y += EditorGUIUtility.singleLineHeight;
                fieldRect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("UnderwaterEffects"));
                m_settings.m_enableUnderwaterEffects = EditorGUI.Toggle(fieldRect, m_settings.m_enableUnderwaterEffects);
            }

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            //Wind

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Wind"));
            m_settings.m_createWind = EditorGUI.Toggle(fieldRect, m_settings.m_createWind);
            if (m_settings.m_createWind)
            {
                labelRect.y += EditorGUIUtility.singleLineHeight;
                fieldRect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("WindType"));
                m_settings.m_windType = (GaiaConstants.GaiaGlobalWindType)EditorGUI.EnumPopup(fieldRect, m_settings.m_windType);
            }

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("AmbientAudio"));
            m_settings.m_enableAmbientAudio = EditorGUI.Toggle(fieldRect, m_settings.m_enableAmbientAudio);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Screenshotter"));
            m_settings.m_createScreenShotter = EditorGUI.Toggle(fieldRect, m_settings.m_createScreenShotter);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("LocationManager"));
            m_settings.m_enableLocationManager = EditorGUI.Toggle(fieldRect, m_settings.m_enableLocationManager);

            //m_heightmapResolution = (GaiaConstants.HeightmapResolution)m_editorUtils.EnumPopup("Heightmap Resolution", m_heightmapResolution, helpEnabled);
            //m_controlTextureResolution = (GaiaConstants.TerrainTextureResolution)m_editorUtils.EnumPopup("Control Texture Resolution", m_controlTextureResolution, helpEnabled);
            //m_basemapResolution = (GaiaConstants.TerrainTextureResolution)m_editorUtils.EnumPopup("Basemap Resolution", m_basemapResolution, helpEnabled);
            //m_detailResolutionPerPatch = m_editorUtils.IntField("Detail Resolution Per Patch", m_detailResolutionPerPatch, helpEnabled);
            //m_detailResolution = m_editorUtils.IntField("Detail Resolution", m_detailResolution, helpEnabled);

            bool changeCheckTriggered = false;

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(m_settings.m_gaiaLightingProfile);
                m_settings.m_gaiaLightingProfile.m_enableAmbientAudio = m_settings.m_enableAmbientAudio;
                m_settings.m_gaiaLightingProfile.m_enablePostProcessing = m_settings.m_enablePostProcessing;

                EditorUtility.SetDirty(m_settings.m_gaiaWaterProfile);
                m_settings.m_gaiaWaterProfile.m_supportUnderwaterEffects = m_settings.m_enableUnderwaterEffects;

                changeCheckTriggered = true;
            }

            return changeCheckTriggered;


        }

        /// <summary>
        /// Editor Update
        /// </summary>
        public void EditorPipelineUpdate()
        {
            if (m_settings == null)
            {
                m_settings = GaiaUtils.GetGaiaSettings();
            }
            if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.Lightweight)
            {
                GaiaLWRPPipelineUtils.StartLWRPSetup(m_settings.m_pipelineProfile).MoveNext();
            }
            else if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                GaiaHDRPPipelineUtils.StartHDRPSetup(m_settings.m_pipelineProfile).MoveNext();
            }
            else if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.Universal)
            {
                GaiaURPPipelineUtils.StartURPSetup(m_settings.m_pipelineProfile).MoveNext();
            }
        }

        #endregion

        #region Gaia Main Function Calls
        /// <summary>
        /// Create and returns a defaults asset
        /// </summary>
        /// <returns>New defaults asset</returns>
        public static GaiaDefaults CreateDefaultsAsset()
        {
            GaiaDefaults defaults = ScriptableObject.CreateInstance<Gaia.GaiaDefaults>();
            AssetDatabase.CreateAsset(defaults, string.Format(GaiaDirectories.GetSettingsDirectory() + "/GD-{0:yyyyMMdd-HHmmss}.asset", DateTime.Now));
            AssetDatabase.SaveAssets();
            return defaults;
        }

        /// <summary>
        /// Create and returns a resources asset
        /// </summary>
        /// <returns>New resources asset</returns>
        //public static GaiaResource CreateResourcesAsset()
        //{
        //    GaiaResource resources = ScriptableObject.CreateInstance<Gaia.GaiaResource>();
        //    AssetDatabase.CreateAsset(resources, string.Format(GaiaDirectories.GetDataDirectory() + "/GR-{0:yyyyMMdd-HHmmss}.asset", DateTime.Now));
        //    AssetDatabase.SaveAssets();
        //    return resources;
        //}

        /// <summary>
        /// Set up the Gaia Present defines
        /// </summary>
        public static void SetGaiaDefinesStatic()
        {
            string currBuildSettings = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            //Check for and inject GAIA_PRESENT
            if (!currBuildSettings.Contains("GAIA_PRESENT"))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, currBuildSettings + ";GAIA_PRESENT");
            }
        }

        /// <summary>
        /// Creates a biome from the selected biome preset entry.
        /// </summary>
        /// <param name="selectedPresetEntry">The selected biome preset entry from the Gaia Manager</param>
        /// <returns></returns>
        private List<Spawner> CreateBiome(BiomePresetDropdownEntry selectedPresetEntry)
        {
            int totalSteps = m_BiomeSpawnersToCreate.Where(x => x.m_spawnerSettings != null).Count();
            int currentStep = 0;
            List<Spawner> createdSpawners = new List<Spawner>();
            GameObject sessionManager = ShowSessionManager();
            Transform gaiaTransform = sessionManager.transform.parent;
            Transform target = gaiaTransform.Find(selectedPresetEntry.name);
            if (target == null)
            {
                GameObject newGO = new GameObject();
                newGO.name = selectedPresetEntry.name + " Biome";
                newGO.transform.parent = gaiaTransform;
                target = newGO.transform;
            }

            BiomeController biomeController = target.GetComponent<BiomeController>();
            if (biomeController == null)
            {
                biomeController = target.gameObject.AddComponent<BiomeController>();
            }
#if UNITY_POST_PROCESSING_STACK_V2
            if (selectedPresetEntry.biomePreset != null)
            {
                biomeController.m_postProcessProfile = selectedPresetEntry.biomePreset.postProcessProfile;
            }
#endif

            //Track created spawners 

            foreach (BiomeSpawnerListEntry spawnerListEntry in m_BiomeSpawnersToCreate.Where(x => x.m_spawnerSettings != null))
            {
                Spawner spawner = spawnerListEntry.m_spawnerSettings.CreateSpawner(false, biomeController.transform);
                ProgressBar.Show(ProgressBarPriority.CreateBiomeTools, "Creating Biome", "Creating Biome " + selectedPresetEntry.name, ++currentStep, totalSteps);
                biomeController.m_autoSpawners.Add(new AutoSpawner() { isActive = spawnerListEntry.m_isActiveInBiome, status = AutoSpawnerStatus.Initial, spawner = spawner });
                createdSpawners.Add(spawner);
            }
            if (createdSpawners.Count > 0)
            {
                biomeController.m_settings.m_range = createdSpawners[0].m_settings.m_spawnRange;
            }
            ProgressBar.Clear(ProgressBarPriority.CreateBiomeTools);
            return createdSpawners;

        }

        /// <summary>
        /// Create the terrain
        /// </summary>
        void CreateTerrain(bool createSpawners)
        {
            //Only do this if we have < 1 terrain
            int actualTerrainCount = Gaia.TerrainHelper.GetActiveTerrainCount();
            if (actualTerrainCount != 0)
            {
                EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("Already a terrain in the scene"), string.Format(m_editorUtils.GetTextValue("You currently have {0} active terrains in your scene, Gaia will not create a terrain for you, but will create the biome spawner you selected."), actualTerrainCount, 0), m_editorUtils.GetTextValue("OK"));
            }
            else
            {
                //Collect the new world settings for world creation
                WorldCreationSettings worldCreationSettings = ScriptableObject.CreateInstance<WorldCreationSettings>();
                worldCreationSettings.m_xTiles = m_settings.m_tilesX;
                worldCreationSettings.m_zTiles = m_settings.m_tilesZ;
                worldCreationSettings.m_tileSize = m_settings.m_currentDefaults.m_terrainSize;
                //increase the possible height according to terrain size - when dealing with large world scenes, a much higher 
                //height space is required to allow for adequate height changes across the world.
                worldCreationSettings.m_tileHeight = m_settings.m_tilesX * worldCreationSettings.m_tileSize;
#if GAIA_PRO_PRESENT
                worldCreationSettings.m_createInScene = m_settings.m_createTerrainScenes;
                worldCreationSettings.m_autoUnloadScenes = m_settings.m_unloadTerrainScenes;
                worldCreationSettings.m_applyFloatingPointFix = m_settings.m_floatingPointFix;
#else
                worldCreationSettings.m_createInScene = false;
                worldCreationSettings.m_autoUnloadScenes = false;
                worldCreationSettings.m_applyFloatingPointFix = false;
#endif

                //Check if we need to add resources from spawners as well
                if (createSpawners)
                {
                    worldCreationSettings.m_spawnerPresetList = m_BiomeSpawnersToCreate;
                }

                GaiaSessionManager.CreateWorld(worldCreationSettings);
#if GAIA_PRO_PRESENT
                if (SessionManager != null)
                {
                    WorldOriginEditor.m_sessionManagerExits = true;
                }
#else
                if (SessionManager != null)
                {
                    Gaia2TopPanel.m_sessionManagerExits = true;
                }
#endif


                //Adjust the scene view so you can see the terrain
                if (SceneView.lastActiveSceneView != null)
                {
                    if (m_settings != null)
                    {
                        SceneView.lastActiveSceneView.LookAtDirect(new Vector3(0f, 300f, -1f * (m_settings.m_currentDefaults.m_terrainSize / 2f)), Quaternion.Euler(30f, 0f, 0f));
                        Repaint();
                    }
                }
            }
        }

        /// <summary>
        /// Create / show the session manager
        /// </summary>
        GameObject ShowSessionManager(bool pickupExistingTerrain = false)
        {
            GameObject mgrObj = GaiaSessionManager.GetSessionManager(pickupExistingTerrain).gameObject;
#if GAIA_PRO_PRESENT
            if (mgrObj != null)
            {
                WorldOriginEditor.m_sessionManagerExits = true;
            }
#else
            if (SessionManager != null)
            {
                Gaia2TopPanel.m_sessionManagerExits = true;
            }
#endif
            Selection.activeGameObject = mgrObj;
            return mgrObj;
        }


        GameObject ShowMaskMapExporter()
        {
#if GAIA_PRO_PRESENT
            GameObject maskMapExporterObj = GameObject.Find("Mask Map Exporter");
            if (maskMapExporterObj == null)
            {
                GameObject gaiaObj = GaiaUtils.GetGaiaGameObject();
                maskMapExporterObj = new GameObject("Mask Map Exporter");
                maskMapExporterObj.transform.parent = gaiaObj.transform;
                MaskMapExport export = maskMapExporterObj.AddComponent<MaskMapExport>();
                export.FitToAllTerrains();
            }
            Selection.activeGameObject = maskMapExporterObj;
            return maskMapExporterObj;
#else
            return null;
#endif
        }

        /// <summary>
        /// Select or create a stamper
        /// </summary>
        GameObject ShowStamper(List<Spawner> autoSpawnerCandidates = null)
        {
            ////Only do this if we have 1 terrain
            //if (DisplayErrorIfNotMinimumTerrainCount(1))
            //{
            //    return;
            //}

            //Make sure we have a session manager
            //m_sessionManager = m_resources.CreateOrFindSessionManager().GetComponent<GaiaSessionManager>();

            //Make sure we have gaia object
            GameObject gaiaObj = GaiaUtils.GetGaiaGameObject();

            //Create or find the stamper
            GameObject stamperObj = GameObject.Find("Stamper");
            if (stamperObj == null)
            {
                stamperObj = new GameObject("Stamper");
                stamperObj.transform.parent = gaiaObj.transform;
                Stamper stamper = stamperObj.AddComponent<Stamper>();
                stamper.m_settings = ScriptableObject.CreateInstance<StamperSettings>();
                //stamper.m_resources = m_settings.m_currentResources;

                //Add an image mask as start configuration
                stamper.m_settings.m_imageMasks = new ImageMask[1];
                stamper.m_settings.m_imageMasks[0] = new ImageMask();

                if (m_settings != null && m_settings.m_defaultStamp != null)
                {
                    stamper.m_settings.m_imageMasks[0].m_imageMaskTexture = m_settings.m_defaultStamp;
                    stamper.m_settings.m_mixHeightImage = m_settings.m_defaultMixHeightStamp;
                }
                ImageMaskListEditor.OpenStampBrowser(stamper.m_settings.m_imageMasks[0]);
                stamper.UpdateStamp();
                stamperObj.transform.position = new Vector3Double(stamper.m_settings.m_x, stamper.m_settings.m_y, stamper.m_settings.m_z);
                stamper.UpdateMinMaxHeight();
                stamper.m_seaLevel = m_settings.m_currentDefaults.m_seaLevel;

                //if spawners are supplied, set them up for automatic spawning
                if (autoSpawnerCandidates != null && autoSpawnerCandidates.Count > 0)
                {
                    foreach (Spawner autoSpawnerCandidate in autoSpawnerCandidates)
                    {
                        AutoSpawner newAutoSpawner = new AutoSpawner()
                        {
                            isActive = true,
                            status = AutoSpawnerStatus.Initial,
                            spawner = autoSpawnerCandidate
                        };
                        stamper.m_autoSpawners.Add(newAutoSpawner);
                    }
                }
#if GAIA_PRO_PRESENT
                if (GaiaUtils.HasDynamicLoadedTerrains())
                {
                    stamper.m_loadTerrainMode = LoadMode.EditorSelected;
                    //make sure the stamper does not load in the complete world right off the bat
                    stamper.FitToAllTerrains(true);
                }
                else
                {
#endif
                stamper.FitToAllTerrains();
#if GAIA_PRO_PRESENT
                }
#endif
                ImageMaskListEditor.OpenStampBrowser(stamper.m_settings.m_imageMasks[0]);
            }

            Selection.activeGameObject = stamperObj;

            //activate Gizmos in scene view - too many users confused by missing stamper preview when Gizmos are turned off
            foreach (SceneView sv in SceneView.sceneViews)
            {
                sv.drawGizmos = true;
            }

            return stamperObj;
        }

        /// <summary>
        /// Create or select the existing visualiser
        /// </summary>
        /// <returns>New or exsiting visualiser - or null if no terrain</returns>
        //GameObject ShowVisualiser()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    GameObject gaiaObj = GaiaUtils.GetGaiaGameObject();
        //    GameObject visualiserObj = GameObject.Find("Visualiser");
        //    if (visualiserObj == null)
        //    {
        //        visualiserObj = new GameObject("Visualiser");
        //        visualiserObj.AddComponent<ResourceVisualiser>();
        //        visualiserObj.transform.parent = gaiaObj.transform;

        //        //Center it on the terrain
        //        visualiserObj.transform.position = Gaia.TerrainHelper.GetActiveTerrainCenter();
        //    }
        //    ResourceVisualiser visualiser = visualiserObj.GetComponent<ResourceVisualiser>();
        //    visualiser.m_resources = m_settings.m_currentResources;
        //    return visualiserObj;
        //}

        /// <summary>
        /// Show a normal exporter
        /// </summary>
        void ShowNormalMaskExporter()
        {
            //Only do this if we have 1 terrain
            if (DisplayErrorIfNotMinimumTerrainCount(1))
            {
                return;
            }

            var export = EditorWindow.GetWindow<GaiaNormalExporterEditor>(false, m_editorUtils.GetTextValue("Normalmap Exporter"));
            export.Show();
        }

        /// <summary>
        /// Show the terrain height adjuster
        /// </summary>
        void ShowTerrainHeightAdjuster()
        {
            var export = EditorWindow.GetWindow<GaiaTerrainHeightAdjuster>(false, m_editorUtils.GetTextValue("Height Adjuster"));
            export.Show();
        }

        /// <summary>
        /// Show the terrain explorer helper
        /// </summary>
        void ShowTerrainUtilties()
        {
            var export = EditorWindow.GetWindow<GaiaTerrainExplorerEditor>(false, m_editorUtils.GetTextValue("Terrain Utilities"));
            export.Show();
        }

        /// <summary>
        /// Show a texture mask exporter
        /// </summary>
        void ShowTexureMaskExporter()
        {
            //Only do this if we have 1 terrain
            if (DisplayErrorIfNotMinimumTerrainCount(1))
            {
                return;
            }

            var export = EditorWindow.GetWindow<GaiaMaskExporterEditor>(false, m_editorUtils.GetTextValue("Splatmap Exporter"));
            export.Show();
        }

        /// <summary>
        /// Show a grass mask exporter
        /// </summary>
        void ShowGrassMaskExporter()
        {
            //Only do this if we have 1 terrain
            if (DisplayErrorIfNotMinimumTerrainCount(1))
            {
                return;
            }

            var export = EditorWindow.GetWindow<GaiaGrassMaskExporterEditor>(false, m_editorUtils.GetTextValue("Grassmask Exporter"));
            export.Show();
        }

        /// <summary>
        /// Show flowmap exporter
        /// </summary>
        void ShowFlowMapMaskExporter()
        {
            //Only do this if we have 1 terrain
            if (DisplayErrorIfNotMinimumTerrainCount(1))
            {
                return;
            }

            var export = EditorWindow.GetWindow<GaiaWaterflowMapEditor>(false, m_editorUtils.GetTextValue("Flowmap Exporter"));
            export.Show();
        }

        /// <summary>
        /// Show a terrain obj exporter
        /// </summary>
        void ShowTerrainObjExporter()
        {
            //if (DisplayErrorIfNotMinimumTerrainCount(1))
            //{
            //    return;
            //}

            var export = EditorWindow.GetWindow<ExportTerrain>(false, m_editorUtils.GetTextValue("Export Terrain"));
            export.Show();
        }

        /// <summary>
        /// Export the world as a PNG heightmap
        /// </summary>
        void ExportWorldAsHeightmapPNG()
        {
            if (DisplayErrorIfNotMinimumTerrainCount(1))
            {
                return;
            }

            GaiaWorldManager mgr = new GaiaWorldManager(Terrain.activeTerrains);
            if (mgr.TileCount > 0)
            {
                string path = GaiaDirectories.GetExportDirectory();
                path = Path.Combine(path, PWCommon2.Utils.FixFileName(string.Format("Terrain-Heightmap-{0:yyyyMMdd-HHmmss}", DateTime.Now)));
                mgr.ExportWorldAsPng(path);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog(
                    m_editorUtils.GetTextValue("Export complete"),
                    m_editorUtils.GetTextValue(" Your heightmap has been saved to : ") + path,
                    m_editorUtils.GetTextValue("OK"));
            }
        }

        /// <summary>
        /// Export the shore mask as a png file
        /// </summary>
        void ExportShoremaskAsPNG()
        {
            //Only do this if we have 1 terrain
            if (DisplayErrorIfNotMinimumTerrainCount(1))
            {
                return;
            }

            var export = EditorWindow.GetWindow<ShorelineMaskerEditor>(false, m_editorUtils.GetTextValue("Export Shore"));
            export.m_seaLevel = GaiaSessionManager.GetSessionManager(false).GetSeaLevel();
            export.Show();
        }

        /// <summary>
        /// Show the Gaia Stamp converter
        /// </summary>
        void ShowGaiaStampConverter()
        {
            var convert = EditorWindow.GetWindow<ConvertStamps>(false, m_editorUtils.GetTextValue("Convert Stamps"));
            convert.Show();
        }


        /// <summary>
        /// Show the extension exporter
        /// </summary>
        void ShowExtensionExporterEditor()
        {
            var export = EditorWindow.GetWindow<GaiaExtensionExporterEditor>(false, m_editorUtils.GetTextValue("Export GX"));
            export.Show();
        }

        /// <summary>
        /// Display an error if there is not exactly one terrain
        /// </summary>
        /// <param name="requiredTerrainCount">The amount required</param>
        /// <param name="feature">The feature name</param>
        /// <returns>True if an error, false otherwise</returns>
        private bool DisplayErrorIfInvalidTerrainCount(int requiredTerrainCount, string feature = "")
        {
            int actualTerrainCount = Gaia.TerrainHelper.GetActiveTerrainCount();
            if (actualTerrainCount != requiredTerrainCount)
            {
                if (string.IsNullOrEmpty(feature))
                {
                    if (actualTerrainCount < requiredTerrainCount)
                    {
                        EditorUtility.DisplayDialog(
                            m_editorUtils.GetTextValue("OOPS!"),
                            string.Format(m_editorUtils.GetTextValue("You currently have {0} active terrains in your scene, but to " +
                            "use this feature you need {1}. Please load in unloaded terrains or create a terrain!"), actualTerrainCount, requiredTerrainCount),
                            m_editorUtils.GetTextValue("OK"));
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(
                            m_editorUtils.GetTextValue("OOPS!"),
                            string.Format(m_editorUtils.GetTextValue("You currently have {0} active terrains in your scene, but to " +
                            "use this feature you need {1}. Please remove terrain!"), actualTerrainCount, requiredTerrainCount),
                            m_editorUtils.GetTextValue("OK"));
                    }
                }
                else
                {
                    if (actualTerrainCount < requiredTerrainCount)
                    {
                        EditorUtility.DisplayDialog(
                            m_editorUtils.GetTextValue("OOPS!"),
                            string.Format(m_editorUtils.GetTextValue("You currently have {0} active terrains in your scene, but to " +
                            "use {2} you need {1}. Please load in unloaded terrains or create a terrain!"), actualTerrainCount, requiredTerrainCount, feature),
                            m_editorUtils.GetTextValue("OK"));
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(
                            m_editorUtils.GetTextValue("OOPS!"),
                            string.Format(m_editorUtils.GetTextValue("You currently have {0} active terrains in your scene, but to " +
                            "use {2} you need {1}. Please remove terrain!"), actualTerrainCount, requiredTerrainCount, feature),
                            m_editorUtils.GetTextValue("OK"));
                    }
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Display an error if there is not exactly one terrain
        /// </summary>
        /// <param name="requiredTerrainCount">The amount required</param>
        /// <param name="feature">The feature name</param>
        /// <returns>True if an error, false otherwise</returns>
        private bool DisplayErrorIfNotMinimumTerrainCount(int requiredTerrainCount, string feature = "")
        {
            int actualTerrainCount = Gaia.TerrainHelper.GetActiveTerrainCount();
            if (actualTerrainCount < requiredTerrainCount)
            {
                if (string.IsNullOrEmpty(feature))
                {
                    if (actualTerrainCount < requiredTerrainCount)
                    {
                        EditorUtility.DisplayDialog(
                            m_editorUtils.GetTextValue("OOPS!"),
                            string.Format(m_editorUtils.GetTextValue("You currently have {0} active terrains in your scene, but to " +
                            "use this feature you need at least {1}. Please load in unloaded terrains or create a terrain!"), actualTerrainCount, requiredTerrainCount),
                            m_editorUtils.GetTextValue("OK"));
                    }
                }
                else
                {
                    if (actualTerrainCount < requiredTerrainCount)
                    {
                        EditorUtility.DisplayDialog(
                            m_editorUtils.GetTextValue("OOPS!"),
                            string.Format(m_editorUtils.GetTextValue("You currently have {0} active terrains in your scene, but to " +
                            "use {2} you need at least {1}.Please load in unloaded terrains or create a terrain!"), actualTerrainCount, requiredTerrainCount, feature),
                            m_editorUtils.GetTextValue("OK"));
                    }
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Get the range from the terrain
        /// </summary>
        /// <returns></returns>
        private float GetRangeFromTerrain()
        {
            float range = (m_settings.m_currentDefaults.m_terrainSize / 2) * m_settings.m_tilesX;
            Terrain t = Gaia.TerrainHelper.GetActiveTerrain();
            if (t != null)
            {
                range = (Mathf.Max(t.terrainData.size.x, t.terrainData.size.z) / 2f) * m_settings.m_tilesX;
            }
            return range;
        }

        ///// <summary>
        ///// Create a texture spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateTextureSpawner()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentResources.CreateCoverageTextureSpawner(GetRangeFromTerrain(), Mathf.Clamp(m_settings.m_currentDefaults.m_terrainSize / (float)m_settings.m_currentDefaults.m_controlTextureResolution, 0.2f, 100f));
        //}

        ///// <summary>
        ///// Create a detail spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateDetailSpawner()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentResources.CreateCoverageDetailSpawner(GetRangeFromTerrain(), Mathf.Clamp(m_settings.m_currentDefaults.m_terrainSize / (float)m_settings.m_currentDefaults.m_detailResolution, 0.2f, 100f));
        //}

        ///// <summary>
        ///// Create a clustered detail spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateClusteredDetailSpawner()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentResources.CreateClusteredDetailSpawner(GetRangeFromTerrain(), Mathf.Clamp(m_settings.m_currentDefaults.m_terrainSize / (float)m_settings.m_currentDefaults.m_detailResolution, 0.2f, 100f));
        //}

        ///// <summary>
        ///// Create a tree spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateClusteredTreeSpawnerFromTerrainTrees()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentResources.CreateClusteredTreeSpawner(GetRangeFromTerrain());
        //}

        ///// <summary>
        ///// Create a tree spawner from game objecxts
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateClusteredTreeSpawnerFromGameObjects()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentGameObjectResources.CreateClusteredGameObjectSpawnerForTrees(GetRangeFromTerrain());
        //}

        ///// <summary>
        ///// Create a tree spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateCoverageTreeSpawner()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentResources.CreateCoverageTreeSpawner(GetRangeFromTerrain());
        //}

        ///// <summary>
        ///// Create a tree spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateCoverageTreeSpawnerFromGameObjects()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentGameObjectResources.CreateCoverageGameObjectSpawnerForTrees(GetRangeFromTerrain());
        //}

        ///// <summary>
        ///// Create a game object spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateCoverageGameObjectSpawner()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentGameObjectResources.CreateCoverageGameObjectSpawner(GetRangeFromTerrain());
        //}

        ///// <summary>
        ///// Create a game object spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateClusteredGameObjectSpawner()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentGameObjectResources.CreateClusteredGameObjectSpawner(GetRangeFromTerrain());
        //}
        #endregion

        #region Create Step 2 (Player, water, sky etc)
        /// <summary>
        /// Create a player
        /// </summary>
        GameObject CreatePlayer()
        {
            //Gaia Settings to check pipeline selected
            if (m_settings == null)
            {
                Debug.LogWarning("Gaia Settings are missing from your project, please make sure Gaia settings is in your project.");
                return null;
            }

            //Only do this if we have 1 terrain
            if (DisplayErrorIfNotMinimumTerrainCount(1))
            {
                return null;
            }

            GameObject playerObj = null;

            bool dynamicLoadedTerrains = GaiaUtils.HasDynamicLoadedTerrains();

            //If nothing selected then make the default the fly cam
            string playerPrefabName = m_settings.m_currentPlayerPrefabName;
            if (string.IsNullOrEmpty(playerPrefabName))
            {
                playerPrefabName = "FlyCam";
            }

            GameObject mainCam = GameObject.Find("Main Camera");
            if (mainCam == null)
            {
                mainCam = GameObject.Find("Camera");
            }
            GameObject firstPersonController = GameObject.Find("FPSController");
            GameObject thirdPersonController = GameObject.Find("ThirdPersonController");
            GameObject flyCamController = GameObject.Find("FlyCam");
            GameObject flyCamControllerUI = GameObject.Find("FlyCamera UI");

            if (mainCam != null)
            {
                DestroyImmediate(mainCam);
            }
            if (firstPersonController != null)
            {
                DestroyImmediate(firstPersonController);
            }
            if (thirdPersonController != null)
            {
                DestroyImmediate(thirdPersonController);
            }
            if (flyCamController != null)
            {
                DestroyImmediate(flyCamController);
            }
            if (flyCamControllerUI != null)
            {
                DestroyImmediate(flyCamControllerUI);
            }

            //Get the centre of world at game height plus a bit
            Vector3 location = Gaia.TerrainHelper.GetWorldCenter(true);// GetActiveTerrainCenter(true);
            if (location == Vector3.zero)
            {
                Debug.LogWarning("There was no active terrain in the scene to place the player on. The player will be created at the scene origin (X=0 Y=0 Z=0)");
            }

            //Get the suggested camera far distance based on terrain scale
            Terrain terrain = GetActiveTerrain();
            //float cameraDistance = Mathf.Clamp(terrain.terrainData.size.x, 250f, 2048) + 200f;
            //fixed Distance of 2000 for the sky dome to be visible always.
            float cameraDistance = 2000;

            GaiaSceneInfo sceneinfo = GaiaSceneInfo.GetSceneInfo();

            GameObject parentObject = GaiaUtils.GetGlobalSceneObject();

            //Create the player
            if (playerPrefabName == "FlyCam")
            {
                playerObj = new GameObject();
                playerObj.name = GaiaConstants.playerFlyCamName;
                playerObj.tag = "MainCamera";
                playerObj.AddComponent<FlareLayer>();
#if !UNITY_2017_1_OR_NEWER
                playerObj.AddComponent<GUILayer>();
#endif
                playerObj.AddComponent<AudioListener>();
                playerObj.AddComponent<FreeCamera>();

                Camera cameraComponent = playerObj.GetComponent<Camera>();
                cameraComponent.farClipPlane = cameraDistance;
                cameraComponent.depthTextureMode = DepthTextureMode.Depth;
                if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.Lightweight)
                {
                    cameraComponent.allowHDR = false;
                    cameraComponent.allowMSAA = true;
                }
                else
                {
                    cameraComponent.allowHDR = true;

                    var tier1 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier1);
                    var tier2 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier2);
                    var tier3 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier3);
                    if (tier1.renderingPath == RenderingPath.DeferredShading || tier2.renderingPath == RenderingPath.DeferredShading || tier3.renderingPath == RenderingPath.DeferredShading)
                    {
                        cameraComponent.allowMSAA = false;
                    }
                    else
                    {
                        cameraComponent.allowMSAA = true;
                    }
                }

                //Adds character controller to allow triggers to be used
                CharacterController characterController = playerObj.GetComponent<CharacterController>();
                if (characterController == null)
                {
                    characterController = playerObj.AddComponent<CharacterController>();
                    characterController.radius = 0.5f;
                    characterController.height = 0.5f;
                }

#if GAIA_PRO_PRESENT
                //Add the simple terrain culling script, useful in any case
                SimpleTerrainCulling culling = playerObj.GetComponent<SimpleTerrainCulling>();
                if (culling == null)
                {
                    playerObj.AddComponent<SimpleTerrainCulling>();
                }
#endif

                CameraLayerCulling layerCulling = playerObj.GetComponent<CameraLayerCulling>();
                if (layerCulling == null)
                {
                    layerCulling = playerObj.AddComponent<CameraLayerCulling>();
                    layerCulling.UpdateDefaults();
                }

                layerCulling.m_mainCamera = cameraComponent;

                //Lift it to about eye height above terrain
                location.y += 1.8f;
                if (sceneinfo != null)
                {
                    if (location.y < sceneinfo.m_seaLevel)
                    {
                        location.y = sceneinfo.m_seaLevel + 1.8f;
                    }
                }
                playerObj.transform.position = location;

                //Set up UI
                if (m_settings.m_flyCamUI == null)
                {
                    Debug.LogError("[CreatePlayer()] Fly cam UI has not been assigned in the gaia settings. Assign it then try again");
                }
                else
                {
                    flyCamControllerUI = PrefabUtility.InstantiatePrefab(m_settings.m_flyCamUI) as GameObject;
                    flyCamControllerUI.name = "FlyCamera UI";
                    flyCamControllerUI.transform.SetParent(GaiaUtils.GetPlayerObject().transform);

                }
            }
            else if (playerPrefabName == "FPSController")
            {
                GameObject playerPrefab = PWCommon2.AssetUtils.GetAssetPrefab(playerPrefabName);
                if (playerPrefab != null)
                {
                    location.y += 1f;
                    playerObj = Instantiate(playerPrefab, location, Quaternion.identity) as GameObject;
                    playerObj.name = GaiaConstants.playerFirstPersonName;
                    playerObj.tag = "Player";
                    playerObj.transform.position = location;
                    if (playerObj.GetComponent<AudioSource>() != null)
                    {
                        AudioSource theAudioSource = playerObj.GetComponent<AudioSource>();
                        theAudioSource.volume = 0.125f;
                    }
                    Camera cameraComponent = playerObj.GetComponentInChildren<Camera>();
                    if (cameraComponent != null)
                    {
                        cameraComponent.farClipPlane = cameraDistance;
                        if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.Lightweight)
                        {
                            cameraComponent.allowHDR = false;
                            cameraComponent.allowMSAA = true;
                        }
                        else
                        {
                            cameraComponent.allowHDR = true;

                            var tier1 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier1);
                            var tier2 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier2);
                            var tier3 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier3);
                            if (tier1.renderingPath == RenderingPath.DeferredShading || tier2.renderingPath == RenderingPath.DeferredShading || tier3.renderingPath == RenderingPath.DeferredShading)
                            {
                                cameraComponent.allowMSAA = false;
                            }
                            else
                            {
                                cameraComponent.allowMSAA = true;
                            }
                        }
                    }
#if GAIA_PRO_PRESENT
                    //Add the simple terrain culling script, useful in any case
                    SimpleTerrainCulling culling = cameraComponent.gameObject.GetComponent<SimpleTerrainCulling>();
                    if (culling == null)
                    {
                        cameraComponent.gameObject.AddComponent<SimpleTerrainCulling>();
                    }
                    
                    //Add the "Wait for terrain loading" script, otherwise character might fall through the terrain
                    if (dynamicLoadedTerrains)
                    {
                        RigidbodyWaitForTerrainLoad waitForLoad = playerObj.GetComponent<RigidbodyWaitForTerrainLoad>();
                        if (waitForLoad == null)
                        {
                            waitForLoad = playerObj.AddComponent<RigidbodyWaitForTerrainLoad>();
                        }

                        FirstPersonController fpsc = playerObj.GetComponent<FirstPersonController>();
                        if (fpsc != null)
                        {
                            fpsc.enabled = false;
                            if (!waitForLoad.m_componentsToActivate.Contains(fpsc))
                            {
                                waitForLoad.m_componentsToActivate.Add(fpsc);
                            }
                        }

                    }
#endif

                }
            }
            else if (playerPrefabName == "ThirdPersonController")
            {
                GameObject playerPrefab = PWCommon2.AssetUtils.GetAssetPrefab(playerPrefabName);
                if (playerPrefab != null)
                {
                    if (dynamicLoadedTerrains)
                    {
                        location.y += 0.5f;
                    }
                    else
                    {
                        location.y += 0.05f;
                    }

                    playerObj = Instantiate(playerPrefab, location, Quaternion.identity) as GameObject;
                    playerObj.name = GaiaConstants.playerThirdPersonName;
                    playerObj.tag = "Player";
                    playerObj.transform.position = location;
                }

                mainCam = new GameObject("Main Camera");
                location.y += 1.5f;
                location.z -= 5f;
                mainCam.transform.position = location;
                Camera cameraComponent = mainCam.AddComponent<Camera>();
                cameraComponent.farClipPlane = cameraDistance;
                if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.Lightweight)
                {
                    cameraComponent.allowHDR = false;
                    cameraComponent.allowMSAA = true;
                }
                else
                {
                    cameraComponent.allowHDR = true;

                    var tier1 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier1);
                    var tier2 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier2);
                    var tier3 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier3);
                    if (tier1.renderingPath == RenderingPath.DeferredShading || tier2.renderingPath == RenderingPath.DeferredShading || tier3.renderingPath == RenderingPath.DeferredShading)
                    {
                        cameraComponent.allowMSAA = false;
                    }
                    else
                    {
                        cameraComponent.allowMSAA = true;
                    }
                }

                CharacterController characterController = mainCam.GetComponent<CharacterController>();
                if (characterController == null)
                {
                    characterController = mainCam.AddComponent<CharacterController>();
                    characterController.height = 0.5f;
                }
#if GAIA_PRO_PRESENT
                //Add the simple terrain culling script, useful in any case
                SimpleTerrainCulling culling = mainCam.GetComponent<SimpleTerrainCulling>();
                if (culling == null)
                {
                    mainCam.AddComponent<SimpleTerrainCulling>();
                }
                //Add the "Wait for terrain loading" script, otherwise character might fall through the terrain
                if (dynamicLoadedTerrains)
                {
                    RigidbodyWaitForTerrainLoad waitForLoad = playerObj.GetComponent<RigidbodyWaitForTerrainLoad>();
                    if (waitForLoad == null)
                    {
                        waitForLoad = playerObj.AddComponent<RigidbodyWaitForTerrainLoad>();
                    }
                    ThirdPersonCharacter tpc = playerObj.GetComponent<ThirdPersonCharacter>();
                    if (tpc != null)
                    {
                        tpc.enabled = false;
                        if (!waitForLoad.m_componentsToActivate.Contains(tpc))
                        {
                            waitForLoad.m_componentsToActivate.Add(tpc);
                        }
                    }
                    ThirdPersonUserControl tpuc = playerObj.GetComponent<ThirdPersonUserControl>();
                    if (tpuc != null)
                    {
                        tpuc.enabled = false;
                        if (!waitForLoad.m_componentsToActivate.Contains(tpuc))
                        {
                            waitForLoad.m_componentsToActivate.Add(tpuc);
                        }
                    }

                }
#endif


#if !UNITY_2017_1_OR_NEWER
                mainCam.AddComponent<GUILayer>();
#endif
                mainCam.AddComponent<FlareLayer>();
                mainCam.AddComponent<AudioListener>();
                mainCam.tag = "MainCamera";

                CameraController cameraController = mainCam.AddComponent<CameraController>();
                cameraController.target = playerObj;
                cameraController.targetHeight = 1.8f;
                cameraController.distance = 5f;
                cameraController.maxDistance = 20f;
                cameraController.minDistance = 2.5f;
            }

            if (playerObj != null)
            {
                //Adjust the scene view to see the camera
                if (SceneView.lastActiveSceneView != null)
                {
                    if (m_settings.m_focusPlayerOnSetup)
                    {
                        SceneView.lastActiveSceneView.LookAtDirect(playerObj.transform.position, playerObj.transform.rotation);
                        Repaint();
                    }
                }
            }

            playerObj.transform.SetParent(GaiaUtils.GetPlayerObject().transform);
#if GAIA_PRO_PRESENT
            if (dynamicLoadedTerrains)
            {
                GaiaTerrainLoader loader = playerObj.GetComponent<GaiaTerrainLoader>();
                if (loader == null)
                {
                    loader = playerObj.AddComponent<GaiaTerrainLoader>();
                }
                loader.LoadMode = LoadMode.RuntimeAlways;
                float size = terrain.terrainData.size.x * 1.25f * 2f;
                loader.m_loadingBounds = new BoundsDouble(playerObj.transform.position, new Vector3(size, size, size));
            }

            //Do we require the floating point fix component? 
            if (GaiaUtils.UsesFloatingPointFix())
            {
                FloatingPointFix fix = playerObj.GetComponent<FloatingPointFix>();
                if (fix == null)
                {
                    fix = playerObj.AddComponent<FloatingPointFix>();
                }
                fix.threshold = m_settings.m_FPFDefaultThreshold;
            }
#endif

            return playerObj;
        }

        /// <summary>
        /// Create a scene exporter object
        /// </summary>
        /*
        GameObject ShowSceneExporter()
        {
            GameObject exporterObj = GameObject.Find("Exporter");
            if (exporterObj == null)
            {
                exporterObj = new GameObject("Exporter");
                exporterObj.transform.position = Gaia.TerrainHelper.GetActiveTerrainCenter(false);
                GaiaExporter exporter = exporterObj.AddComponent<GaiaExporter>();
                GameObject gaiaObj = GameObject.Find("Gaia");
                if (gaiaObj != null)
                {
                    exporterObj.transform.parent = gaiaObj.transform;
                    exporter.m_rootObject = gaiaObj;
                }
                exporter.m_defaults = m_defaults;
                exporter.m_resources = m_resources;
                exporter.IngestGaiaSetup();
            }
            return exporterObj;
                     */

        /// <summary>
        /// Create a wind zone
        /// </summary>
        private GameObject CreateWindZone()
        {
            WindZone globalWind = FindObjectOfType<WindZone>();
            if (globalWind == null)
            {
                GameObject windZoneObj = new GameObject("Wind Zone");
                windZoneObj.transform.Rotate(new Vector3(25f, 0f, 0f));
                globalWind = windZoneObj.AddComponent<WindZone>();
                switch (m_settings.m_windType)
                {
                    case GaiaConstants.GaiaGlobalWindType.Calm:
                        globalWind.windMain = 0.2f;
                        globalWind.windTurbulence = 0.2f;
                        globalWind.windPulseMagnitude = 0.2f;
                        globalWind.windPulseFrequency = 0.05f;
                        break;
                    case GaiaConstants.GaiaGlobalWindType.Moderate:
                        globalWind.windMain = 0.45f;
                        globalWind.windTurbulence = 0.35f;
                        globalWind.windPulseMagnitude = 0.2f;
                        globalWind.windPulseFrequency = 0.1f;
                        break;
                    case GaiaConstants.GaiaGlobalWindType.Strong:
                        globalWind.windMain = 0.65f;
                        globalWind.windTurbulence = 0.3f;
                        globalWind.windPulseMagnitude = 0.2f;
                        globalWind.windPulseFrequency = 0.25f;
                        break;
                    case GaiaConstants.GaiaGlobalWindType.None:
                        globalWind.windMain = 0f;
                        globalWind.windTurbulence = 0f;
                        globalWind.windPulseMagnitude = 0f;
                        globalWind.windPulseFrequency = 0f;
                        break;
                }

                GameObject gaiaObj = GaiaUtils.GetGlobalSceneObject();
                windZoneObj.transform.SetParent(gaiaObj.transform);
            }
            else
            {
                switch (m_settings.m_windType)
                {
                    case GaiaConstants.GaiaGlobalWindType.Calm:
                        globalWind.windMain = 0.2f;
                        globalWind.windTurbulence = 0.2f;
                        globalWind.windPulseMagnitude = 0.2f;
                        globalWind.windPulseFrequency = 0.05f;
                        break;
                    case GaiaConstants.GaiaGlobalWindType.Moderate:
                        globalWind.windMain = 0.45f;
                        globalWind.windTurbulence = 0.35f;
                        globalWind.windPulseMagnitude = 0.2f;
                        globalWind.windPulseFrequency = 0.1f;
                        break;
                    case GaiaConstants.GaiaGlobalWindType.Strong:
                        globalWind.windMain = 0.65f;
                        globalWind.windTurbulence = 0.3f;
                        globalWind.windPulseMagnitude = 0.2f;
                        globalWind.windPulseFrequency = 0.25f;
                        break;
                    case GaiaConstants.GaiaGlobalWindType.None:
                        globalWind.windMain = 0f;
                        globalWind.windTurbulence = 0f;
                        globalWind.windPulseMagnitude = 0f;
                        globalWind.windPulseFrequency = 0f;
                        break;
                }
            }

            GameObject returingObject = globalWind.gameObject;

            return returingObject;
        }

        /// <summary>
        /// Create water
        /// </summary>
        GameObject CreateWater()
        {
            //Only do this if we have 1 terrain
            if (DisplayErrorIfNotMinimumTerrainCount(1))
            {
                return null;
            }
#if GAIA_2_PRESENT && !AMBIENT_WATER
            if (m_settings.m_currentWaterPro == GaiaConstants.GaiaWaterProfileType.None)
            {
                GaiaWater.RemoveSystems();
            }
            else
            {
                Material waterMat = GaiaWater.GetGaiaOceanMaterial();
                GaiaWater.GetProfile(m_settings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex, waterMat, m_settings.m_gaiaWaterProfile, m_settings.m_pipelineProfile.m_activePipelineInstalled, true, false);
            }
#endif
            GameObject waterGO = GameObject.Find(GaiaConstants.waterSurfaceObject);
            return waterGO;
        }

        /// <summary>
        /// Create the sky
        /// </summary>
        void CreateSky()
        {
#if GAIA_2_PRESENT
            GaiaLighting.GetProfile(m_settings.m_gaiaLightingProfile, m_settings.m_pipelineProfile, m_settings.m_pipelineProfile.m_activePipelineInstalled);
#else
            Debug.Log("Lighting could not be created because Ambient Skies exists in this project! Please use Ambient Skies to set up lighting in your scene!");
#endif
        }

        /// <summary>
        /// Create and return a screen shotter object
        /// </summary>
        /// <returns></returns>
        GameObject CreateScreenShotter()
        {
            GameObject shotterObj = GameObject.Find("Screen Shotter");
            if (shotterObj == null)
            {
                if (m_settings == null)
                {
                    m_settings = GaiaUtils.GetGaiaSettings();
                }
                shotterObj = new GameObject("Screen Shotter");
                Gaia.ScreenShotter shotter = shotterObj.AddComponent<Gaia.ScreenShotter>();
                shotter.m_targetDirectory = m_settings.m_screenshotsDirectory.Replace("Assets/", "");
                shotter.m_watermark = PWCommon2.AssetUtils.GetAsset("Made With Gaia Watermark.png", typeof(Texture2D)) as Texture2D;



                GameObject gaiaObj = GaiaUtils.GetGlobalSceneObject();
                shotterObj.transform.parent = gaiaObj.transform;
                shotterObj.transform.position = Gaia.TerrainHelper.GetActiveTerrainCenter(false);
            }

            return shotterObj;
        }

        #endregion

        #region Setup Tab Functions

        /// <summary>
        /// Setup panel settings
        /// </summary>
        /// <param name="helpEnabled"></param>
        private void SetupSettingsEnabled(bool helpEnabled)
        {
            bool currentGUIState = GUI.enabled;

            if (!m_shadersNotImported)
            {
                EditorGUILayout.HelpBox("Shader library folder is missing. Please reimport Gaia and insure that 'PW Shader Library' is imported.", MessageType.Error);
            }
            else
            {
                bool enablePackageButton = false;

#if !UNITY_POST_PROCESSING_STACK_V2
                if (m_settings.m_currentRenderer != GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    m_editorUtils.InlineHelp("PostProcessingSetupHelp", helpEnabled);
                    m_editorUtils.Heading("PostProcessingSettings");
                    EditorGUILayout.HelpBox("Install Post Processing from the package manager. Use Help to learn how to install Post Processing From Package Manager", MessageType.Error);
                    GUILayout.Space(5f);
                }
#endif

                if (m_editorUtils.ClickableText("InstallPipelineHelp"))
                {
                    Application.OpenURL("https://docs.unity3d.com/Manual/ScriptableRenderPipeline.html");
                }

                m_editorUtils.InlineHelp("InstallPipelineHelp", helpEnabled);

                EditorGUILayout.Space();

                m_editorUtils.Heading("RenderPipelineSettings");
                if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.BuiltIn)
                {
                    EditorGUILayout.BeginHorizontal();
                    m_editorUtils.LabelField("RenderPipeline");

                    string[] displayedOptions;
                    int[] optionValues;

#if UNITY_2019_3_OR_NEWER
                    displayedOptions = new string[3] { "BuiltIn", "Universal", "HighDefinition" };
                    optionValues = new int[3] { 0, 2, 3 };

#else
                    displayedOptions = new string[3] { "BuiltIn", "Lightweight", "HighDefinition" };
                    optionValues = new int[3] { 0, 1, 3 };
#endif
                    int selectedRenderer = EditorGUILayout.IntPopup((int)m_settings.m_currentRenderer, displayedOptions, optionValues);
                    m_settings.m_currentRenderer = (GaiaConstants.EnvironmentRenderer)selectedRenderer;
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    m_editorUtils.LabelField("RenderPipeline");
                    EditorGUILayout.LabelField("Installed " + m_settings.m_pipelineProfile.m_activePipelineInstalled.ToString());
                    EditorGUILayout.EndHorizontal();
                }

                m_editorUtils.InlineHelp("RenderPipelineSetupHelp", helpEnabled);

                //Revert back to built-in renderer
                if (m_settings.m_pipelineProfile.m_activePipelineInstalled != m_settings.m_currentRenderer)
                {
                    if (m_settings.m_pipelineProfile.m_activePipelineInstalled != GaiaConstants.EnvironmentRenderer.BuiltIn && m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.BuiltIn)
                    {
                        if (m_editorUtils.Button("RevertGaiaBackToBuiltIn"))
                        {
                            if (EditorUtility.DisplayDialog("Upgrading Gaia Pipeline!", "You're about to revert gaia back to use Built-In render pipeline. Are you sure you want to proceed?", "Yes", "No"))
                            {
                                m_enableGUI = true;
                                if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.Lightweight)
                                {
                                    m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.BuiltIn;
                                    GaiaLWRPPipelineUtils.CleanUpLWRP(m_settings.m_pipelineProfile, m_settings);
                                }
                                else if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.HighDefinition)
                                {
                                    m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.BuiltIn;
                                    GaiaHDRPPipelineUtils.CleanUpHDRP(m_settings.m_pipelineProfile, m_settings);
                                }
                                else if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.Universal)
                                {
                                    GaiaURPPipelineUtils.CleanUpURP(m_settings.m_pipelineProfile, m_settings);
                                }
                            }
                        }
                    }
                    else
                    {
                        //Disable Install button
                        enablePackageButton = true;
                        //LWRP Version Limit
                        if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.Lightweight)
                        {
                            if (Application.unityVersion.Contains("2019.1"))
                            {
                                EditorGUILayout.BeginHorizontal();
                                m_editorUtils.LabelField("MinLWRP");
                                EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_min2019_1LWVersion);
                                EditorGUILayout.EndHorizontal();
                                //EditorGUILayout.BeginHorizontal();
                                //m_editorUtils.LabelField("MaxLWRP");
                                //EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_max2019_1LWVersion);
                                //EditorGUILayout.EndHorizontal();
                            }
                            else if (Application.unityVersion.Contains("2019.2"))
                            {
                                EditorGUILayout.BeginHorizontal();
                                m_editorUtils.LabelField("MinLWRP");
                                EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_min2019_2LWVersion);
                                EditorGUILayout.EndHorizontal();
                                //EditorGUILayout.BeginHorizontal();
                                //m_editorUtils.LabelField("MaxLWRP");
                                //EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_max2019_2LWVersion);
                                //EditorGUILayout.EndHorizontal();
                            }
                            else
                            {
                                EditorGUILayout.BeginHorizontal();
                                m_editorUtils.LabelField("MinLWRP");
                                EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_min2019_3LWVersion);
                                EditorGUILayout.EndHorizontal();
                                //EditorGUILayout.BeginHorizontal();
                                //m_editorUtils.LabelField("MaxLWRP");
                                //EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_max2019_3LWVersion);
                                //EditorGUILayout.EndHorizontal();
                            }
                        }
                        //HDRP Version Limit
                        else if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.HighDefinition)
                        {
                            if (Application.unityVersion.Contains("2019.1"))
                            {
                                EditorGUILayout.BeginHorizontal();
                                m_editorUtils.LabelField("MinHDRP");
                                EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_min2019_1HDVersion);
                                EditorGUILayout.EndHorizontal();
                                //EditorGUILayout.BeginHorizontal();
                                //m_editorUtils.LabelField("MaxHDRP");
                                //EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_max2019_1HDVersion);
                                //EditorGUILayout.EndHorizontal();
                            }
                            else if (Application.unityVersion.Contains("2019.2"))
                            {
                                EditorGUILayout.BeginHorizontal();
                                m_editorUtils.LabelField("MinHDRP");
                                EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_min2019_2HDVersion);
                                EditorGUILayout.EndHorizontal();
                                //EditorGUILayout.BeginHorizontal();
                                //m_editorUtils.LabelField("MaxHDRP");
                                //EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_max2019_2HDVersion);
                                //EditorGUILayout.EndHorizontal();
                            }
                            else if (Application.unityVersion.Contains("2019.3"))
                            {
                                EditorGUILayout.BeginHorizontal();
                                m_editorUtils.LabelField("MinHDRP");
                                EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_min2019_3HDVersion);
                                EditorGUILayout.EndHorizontal();
                                //EditorGUILayout.BeginHorizontal();
                                //m_editorUtils.LabelField("MaxHDRP");
                                //EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_max2019_3HDVersion);
                                //EditorGUILayout.EndHorizontal();
                            }
                            else
                            {
                                EditorGUILayout.BeginHorizontal();
                                m_editorUtils.LabelField("MinHDRP");
                                EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_min2019_4HDVersion);
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        //URP Version Limit
                        else if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.Universal)
                        {
                            if (Application.unityVersion.Contains("2019.3"))
                            {
                                EditorGUILayout.BeginHorizontal();
                                m_editorUtils.LabelField("MinUPRP");
                                EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_min2019_3UPVersion);
                                EditorGUILayout.EndHorizontal();
                                //EditorGUILayout.BeginHorizontal();
                                //m_editorUtils.LabelField("MaxUPRP");
                                //EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_max2019_3UPVersion);
                                //EditorGUILayout.EndHorizontal();
                            }
                            else if (Application.unityVersion.Contains("2019.4"))
                            {
                                EditorGUILayout.BeginHorizontal();
                                m_editorUtils.LabelField("MinUPRP");
                                EditorGUILayout.LabelField(m_settings.m_pipelineProfile.m_min2019_4UPVersion);
                                EditorGUILayout.EndHorizontal();
                            }
                        }

                        //Upgrade to LWRP/HDRP
                        if (m_editorUtils.Button("UpgradeGaiaTo" + m_settings.m_currentRenderer.ToString()))
                        {
                            if (EditorUtility.DisplayDialog("Upgrading Gaia Pipeline", "You're about to change Gaia to use " + m_settings.m_currentRenderer.ToString() + " render pipeline. Are you sure you want to proceed?", "Yes", "No"))
                            {
                                m_enableGUI = true;
                                if (EditorUtility.DisplayDialog("Save Scene", "Would you like to save your scene before switching render pipeline?", "Yes", "No"))
                                {
                                    if (EditorSceneManager.GetActiveScene().isDirty)
                                    {
                                        EditorSceneManager.SaveOpenScenes();
                                    }

                                    AssetDatabase.SaveAssets();
                                }

                                if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.HighDefinition)
                                {
                                    EditorUtility.DisplayProgressBar("Installing " + m_settings.m_currentRenderer.ToString(), "Preparing To Install " + m_settings.m_currentRenderer.ToString(), 0f);
                                    GaiaHDRPPipelineUtils.m_waitTimer1 = 1f;
                                    GaiaHDRPPipelineUtils.m_waitTimer2 = 3f;
                                    GaiaHDRPPipelineUtils.SetPipelineAsset(m_settings.m_pipelineProfile);
                                }
                                else if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.Universal)
                                {
                                    EditorUtility.DisplayProgressBar("Installing " + m_settings.m_currentRenderer.ToString(), "Preparing To Install " + m_settings.m_currentRenderer.ToString(), 0f);
                                    GaiaURPPipelineUtils.m_waitTimer1 = 1f;
                                    GaiaURPPipelineUtils.m_waitTimer2 = 3f;
                                    GaiaURPPipelineUtils.SetPipelineAsset(m_settings.m_pipelineProfile);
                                }
                            }
                        }
                    }
                }
                else
                {
                    //Revert back to built-in renderer
                    if (m_settings.m_pipelineProfile.m_activePipelineInstalled != GaiaConstants.EnvironmentRenderer.BuiltIn)
                    {
                        if (m_editorUtils.Button("RevertGaiaBackToBuiltIn"))
                        {
                            if (EditorUtility.DisplayDialog("Upgrading Gaia Pipeline", "You're about to revert Gaia back to use Built-In render pipeline. Are you sure you want to proceed?", "Yes", "No"))
                            {
                                m_enableGUI = true;
                                if (EditorUtility.DisplayDialog("Save Scene", "Would you like to save your scene before switching render pipeline?", "Yes", "No"))
                                {
                                    if (EditorSceneManager.GetActiveScene().isDirty)
                                    {
                                        EditorSceneManager.SaveOpenScenes();
                                    }

                                    AssetDatabase.SaveAssets();
                                }


                                if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.Lightweight)
                                {
                                    m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.BuiltIn;
                                    GaiaLWRPPipelineUtils.CleanUpLWRP(m_settings.m_pipelineProfile, m_settings);
                                }
                                else if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.HighDefinition)
                                {
                                    m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.BuiltIn;
                                    GaiaHDRPPipelineUtils.CleanUpHDRP(m_settings.m_pipelineProfile, m_settings);
                                }
                                else if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.Universal)
                                {
                                    m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.BuiltIn;
                                    GaiaURPPipelineUtils.CleanUpURP(m_settings.m_pipelineProfile, m_settings);
                                }
                            }
                        }
                    }
                }

                if (!m_enableGUI)
                {
                    MaterialLibraryEntry[] materialLibrary = null;
                    GaiaPackageVersion unityVersion = GaiaPackageVersion.Unity2019_1;

                    //Installation setup
                    if (Application.unityVersion.Contains("2019.2"))
                    {
                        unityVersion = GaiaPackageVersion.Unity2019_2;
                    }
                    else if (Application.unityVersion.Contains("2019.3"))
                    {
                        unityVersion = GaiaPackageVersion.Unity2019_3;
                    }

                    GetPackages(unityVersion, out materialLibrary, out enablePackageButton);

                    GUILayout.Space(5f);

                    m_editorUtils.Heading("PackagesThatWillBeInstalled");

                    if (!enablePackageButton)
                    {
                        EditorGUILayout.HelpBox("Shader Installation is not yet supported on this version of Unity.", MessageType.Info);
                        GUI.enabled = false;
                    }

                    GUI.backgroundColor = Color.red;

                    if (EditorApplication.isCompiling)
                    {
                        GUI.enabled = false;
                    }

                    if (m_editorUtils.Button("InstallPackages"))
                    {
                        ProcedualWorlds.Gaia.PackageSystem.PackageInstallerUtils.m_installShaders = true;
                        ProcedualWorlds.Gaia.PackageSystem.PackageInstallerUtils.m_timer = 7f;
                        ProcedualWorlds.Gaia.PackageSystem.PackageInstallerUtils.StartInstallation(Application.unityVersion, m_settings.m_currentRenderer, materialLibrary, m_settings.m_pipelineProfile);
                    }

                    m_editorUtils.InlineHelp("PackageInstallSetupHelp", helpEnabled);

                    GUI.enabled = currentGUIState;
                }

                GUI.backgroundColor = m_defaultPanelColor;
            }
        }

        /// <summary>
        /// System info settings
        /// </summary>
        /// <param name="helpEnabled"></param>
        private void SystemInfoSettingsEnabled(bool helpEnabled)
        {
            m_editorUtils.Heading("UnityInfo");
            EditorGUILayout.LabelField("Unity Version: " + Application.unityVersion);
            EditorGUILayout.LabelField("Company Name: " + Application.companyName);
            EditorGUILayout.LabelField("Product Name: " + Application.productName);
            EditorGUILayout.LabelField("Project Version: " + Application.version);
            EditorGUILayout.LabelField("Project Data Path: " + Application.dataPath);

            EditorGUILayout.Space();
            m_editorUtils.Heading("SystemInfo");
            EditorGUILayout.LabelField("Graphics Card Name: " + SystemInfo.graphicsDeviceName);
            EditorGUILayout.LabelField("Graphics Card Version: " + SystemInfo.graphicsDeviceVersion);
            EditorGUILayout.LabelField("Graphics Card Memory: " + SystemInfo.graphicsMemorySize + " MB");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Processor Name: " + SystemInfo.processorType);
            EditorGUILayout.LabelField("Processor Core Count: " + SystemInfo.processorCount);
            EditorGUILayout.LabelField("Processor Speed: " + SystemInfo.processorFrequency + " GHz");
        }

        /// <summary>
        /// Check if shaders need to be installed
        /// </summary>
        /// <returns></returns>
        private bool MissingShaders()
        {
            //Currently not needed, need to see if we want to re-enable this for a different directory

            //bool exist = false;
            //m_enableGUI = false;

            //string[] folders = Directory.GetDirectories(Application.dataPath, ".", SearchOption.AllDirectories);
            //foreach (string folder in folders)
            //{
            //    if (folder.Contains("PW Shader Library"))
            //    {
            //        m_enableGUI = true;
            //        exist = true;
            //    }
            //}

            //return exist;

            return true;
        }

        /// <summary>
        /// Check the project for files and check if needs to be installed
        /// </summary>
        /// <returns></returns>
        private bool AreShadersInstalledCorrectly(GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            if (m_settings != null)
            {
                foreach (MaterialLibraryEntry entry in m_settings.m_pipelineProfile.m_materialLibrary)
                {
                    string targetShaderName = entry.m_builtInShaderName;
                    switch (renderPipeline)
                    {
                        case GaiaConstants.EnvironmentRenderer.BuiltIn:
                            targetShaderName = entry.m_builtInShaderName;
                            break;
                        case GaiaConstants.EnvironmentRenderer.Lightweight:
                            //not supported anymore                           
                            break;
                        case GaiaConstants.EnvironmentRenderer.Universal:
                            targetShaderName = entry.m_URPReplacementShaderName;
                            break;
                        case GaiaConstants.EnvironmentRenderer.HighDefinition:
                            targetShaderName = entry.m_HDRPReplacementShaderName;
                            break;
                    }

                    Material[] entryMaterials = entry.m_materials;

                    Shader targetShader = Shader.Find(targetShaderName);

                    if (targetShader == null)
                    {
                        Debug.LogError("Target shader for material library entry " + entry.m_name + " not found!");
                        continue;
                    }

                    foreach (Material material in entryMaterials)
                    {
                        if (material != null)
                        {
                            string path = AssetDatabase.GetAssetPath(material);
                            if (material.shader.name.StartsWith("Hidden/InternalErrorShader") || material.shader != targetShader)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the unity packages shaders/setup
        /// </summary>
        /// <param name="unityVersion"></param>
        /// <param name="shaderLibrary"></param>
        private void GetPackages(GaiaPackageVersion unityVersion, out MaterialLibraryEntry[] materialLibrary, out bool isSupported)
        {
            isSupported = false;

            materialLibrary = null;

            if (m_settings == null)
            {
                m_settings = GaiaUtils.GetGaiaSettings();
            }

            if (m_gaiaPipelineSettings == null)
            {
                m_gaiaPipelineSettings = m_settings.m_pipelineProfile;
            }

            if (m_gaiaPipelineSettings == null)
            {
                Debug.LogError("Gaia Pipeline Profile is empty, check the Gaia Settings to insure the profile is defined.");
                return;
            }

            foreach (GaiaPackageSettings packageSettings in m_gaiaPipelineSettings.m_packageSetupSettings)
            {
                if (packageSettings.m_unityVersion == unityVersion)
                {
                    materialLibrary = m_gaiaPipelineSettings.m_materialLibrary;

                    if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.BuiltIn)
                    {
                        isSupported = packageSettings.m_isSupported;
                    }
                    else if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.Lightweight)
                    {
                        isSupported = packageSettings.m_isSupported;
                    }
                    else
                    {
                        isSupported = packageSettings.m_isSupported;
                    }
                }
            }

        }

        /// <summary>
        /// Checks the gaia manager and updates the bool checks
        /// </summary>
        public void GaiaManagerStatusCheck(bool force = false)
        {
            if (!m_statusCheckPerformed || force)
            {
                //Do Maintenance Tasks (Delete unneccessary files, etc.)
                if (!Application.isPlaying)
                {
                    DoMaintenance();
                }

                //Check if shaders are missing
                m_shadersNotImported = MissingShaders();

                //disabled for now
                m_enableGUI = true;
                m_enableGUI = AreShadersInstalledCorrectly(m_settings.m_pipelineProfile.m_activePipelineInstalled);

                if (m_enableGUI)
                {
                    m_showSetupPanel = false;
                }
                else
                {
                    m_showSetupPanel = true;
                }

#if !UNITY_2019_1_OR_NEWER

                            m_enableGUI = false;

#endif

                this.Repaint();
                m_statusCheckPerformed = true;

            }
        }

        /// <summary>
        /// Performs maintenance tasks such as deleting unwanted files from earlier Gaia installations
        /// </summary>
        private void DoMaintenance()
        {
            //Only Perform maintenance tasks if token exists
            if (File.Exists(GaiaDirectories.GetSettingsDirectory() + "\\" + GaiaConstants.maintenanceTokenFilename))
            {
                //Do not ask this every time in dev env
                if (!System.IO.Directory.Exists(GaiaUtils.GetAssetPath("Dev Utilities")))
                {
                    EditorUtility.DisplayDialog("Update detected", "It looks like you are opening the Gaia Manager the first time after an update or the initial installation. It will now close down again to perform a few maintenance tasks first.", "OK");
                }
                //Get Maintenance Profile
                GaiaMaintenanceProfile maintenanceProfile = (GaiaMaintenanceProfile)PWCommon2.AssetUtils.GetAssetScriptableObject("Gaia Maintenance Profile");

                if (maintenanceProfile == null)
                {
                    Debug.LogWarning("Could not find Gaia Maintenance Profile, maintenance tasks will be skipped.");
                    return;
                }

                //Do not perform this every time in dev env
                if (!System.IO.Directory.Exists(GaiaUtils.GetAssetPath("Dev Utilities")))
                {

                    //Look up mesh colliders in our prefabs and set the cooking options to the defaults. Otherwise this can be fatal for spawning and entering playmode performance!
                    List<string> paths = new List<string>();
                    for (int i = 0; i < maintenanceProfile.meshColliderPrefabPaths.Length; i++)
                    {
                        string path = GaiaDirectories.GetGaiaDirectory() + maintenanceProfile.meshColliderPrefabPaths[i];
                        if (Directory.Exists(path))
                        {
                            paths.Add(path);
                        }
                    }

                    string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab", paths.ToArray());
                    //Keep a list of all prefabs we come along, we need them later to fix the layers on them as well
                    List<GameObject> allPrefabs = new List<GameObject>();
                    List<string> allPrefabPaths = new List<string>();

                    int currentGUID = 0;
                    foreach (string prefabGUID in allPrefabGuids)
                    {
                        EditorUtility.DisplayProgressBar(m_editorUtils.GetTextValue("MaintenanceProgressMeshTitle"), String.Format(m_editorUtils.GetTextValue("MaintenanceProgressMeshText"), currentGUID, allPrefabGuids.Length), (float)currentGUID / (float)allPrefabGuids.Length);
                        string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
                        allPrefabPaths.Add(prefabPath);

                        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);

                        MeshCollider[] allMeshColliders = prefab.GetComponentsInChildren<MeshCollider>();

                        bool madeChanges = false;

                        for (int i = allMeshColliders.Length - 1; i >= 0; i--)
                        {
                            MeshCollider collider = allMeshColliders[i];
                            Mesh mesh = collider.sharedMesh;
                            GameObject gameObject = collider.gameObject;
                            DestroyImmediate(collider);
                            MeshCollider newCollider = gameObject.AddComponent<MeshCollider>();
                            newCollider.sharedMesh = mesh;
                            madeChanges = true;
                        }
                        if (madeChanges)
                        {
                            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                        }
                        PrefabUtility.UnloadPrefabContents(prefab);
                        allPrefabs.Add(prefab);
                        currentGUID++;
                    }

                    EditorUtility.ClearProgressBar();

                    //Perform layer fix
                    GaiaUtils.FixPrefabLayers(allPrefabPaths);
                }

                List<string> directories = new List<string>(Directory.EnumerateDirectories(Application.dataPath, ".", SearchOption.AllDirectories));
                bool changesMade = false;
                foreach (string directory in directories)
                {
                    //Deletion Tasks
                    foreach (DeletionTask deletionTask in maintenanceProfile.m_deletionTasks)
                    {
                        if (PerformDeletionTask(directory, deletionTask))
                        {
                            changesMade = true;
                        }
                    }
                }

                //Rename Tasks
                foreach (RenameTask renameTask in maintenanceProfile.m_renameTasks)
                {
                    if (PerformRenameTask(renameTask))
                    {
                        changesMade = true;
                    }
                }

                if (changesMade)
                {
                    AssetDatabase.Refresh();
                }
            }

            //Maintenance done, remove token
            if (!System.IO.Directory.Exists(GaiaUtils.GetAssetPath("Dev Utilities")))
            {
                FileUtil.DeleteFileOrDirectory(GaiaDirectories.GetSettingsDirectory() + "\\" + GaiaConstants.maintenanceTokenFilename);
                FileUtil.DeleteFileOrDirectory(GaiaDirectories.GetSettingsDirectory() + "\\" + GaiaConstants.maintenanceTokenFilename + ".meta");
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Performs a single maintenance deletion task
        /// </summary>
        /// <param name="deletionTask"></param>
        private bool PerformDeletionTask(string directory, DeletionTask deletionTask)
        {
            if (deletionTask.m_pathContains == null || deletionTask.m_pathContains == "")
            {
                Debug.LogWarning("Empty Path for Gaia Maintenance Deletion Task is not supported.");
                return false;
            }

            if ((deletionTask.m_Name == null || deletionTask.m_Name == "") && (deletionTask.m_fileExtension == null || deletionTask.m_fileExtension == ""))
            {
                Debug.LogWarning("Deletion Task must at least contain either a name, or a file extension to check against.");
                return false;
            }

            bool changesMade = false;

            //filter out folders where the search string appears ONLY BEFORE the asset directory, 
            //e.g. because the user has their entire project placed in a folder containing the search string - we would not want to touch those
            if (directory.Contains(deletionTask.m_pathContains) && directory.LastIndexOf("Assets") < directory.LastIndexOf(deletionTask.m_pathContains))
            {
                //Check if we should perform this for subfolders as well, otherwise we will do the deletion only when the path ends with the search string
                if (deletionTask.m_includeSubDirectories || directory.EndsWith(deletionTask.m_pathContains))
                {
                    switch (deletionTask.m_taskType)
                    {
                        case DeletionTaskType.Directory:
                            if (CheckDeletionTaskCondition(deletionTask, directory, ""))
                            {
                                FileUtil.DeleteFileOrDirectory(directory);
                                changesMade = true;
                            }

                            break;
                        case DeletionTaskType.File:

                            DirectoryInfo dirInfo = new DirectoryInfo(directory);
                            var files = dirInfo.GetFiles();
                            foreach (FileInfo fileInfo in files)
                            {
                                if (CheckDeletionTaskCondition(deletionTask, fileInfo.Name.Replace(fileInfo.Extension, ""), fileInfo.Extension))
                                {
                                    FileUtil.DeleteFileOrDirectory(fileInfo.FullName);
                                    changesMade = true;
                                }
                            }

                            break;
                    }
                }
            }

            return changesMade;
        }

        /// <summary>
        /// Performs a single maintenance rename task
        /// </summary>
        /// <param name="renameTask"></param>
        private bool PerformRenameTask(RenameTask renameTask)
        {
            if (String.IsNullOrEmpty(renameTask.m_newPath) || String.IsNullOrEmpty(renameTask.m_oldPath))
            {
                Debug.LogWarning("Empty Paths for Gaia Maintenance Rename Task is not supported.");
                return false;
            }

            bool changesMade = false;

            string pathOld = GaiaDirectories.GetGaiaDirectory() + renameTask.m_oldPath;
            if (Directory.Exists(pathOld))
            {
                string pathNew = GaiaDirectories.GetGaiaDirectory() + renameTask.m_newPath;
                AssetDatabase.MoveAsset(pathOld, pathNew);
                changesMade = true;
            }

            return changesMade;
        }

        private bool CheckDeletionTaskCondition(DeletionTask deletionTask, string nameString, string extensionString)
        {
            switch (deletionTask.m_taskType)
            {
                case DeletionTaskType.Directory:
                    return (deletionTask.m_checkType == MaintenanceCheckType.Contains && nameString.Contains(deletionTask.m_Name)) ||
                               (deletionTask.m_checkType == MaintenanceCheckType.Equals && nameString.Equals(deletionTask.m_Name));
                case DeletionTaskType.File:
                    bool nameApplies = false;
                    bool extensionApplies = false;

                    //Breaking down the check into multiple if-statements, easier to read that way

                    if (deletionTask.m_Name == null || deletionTask.m_Name == "")
                    {
                        nameApplies = true;
                    }

                    if (deletionTask.m_checkType == MaintenanceCheckType.Contains && nameString.Contains(deletionTask.m_Name))
                    {
                        nameApplies = true;
                    }

                    if (deletionTask.m_checkType == MaintenanceCheckType.Equals && nameString.Equals(deletionTask.m_Name))
                    {
                        nameApplies = true;
                    }

                    if (deletionTask.m_fileExtension == null || deletionTask.m_fileExtension == "")
                    {
                        extensionApplies = true;
                    }

                    if (deletionTask.m_checkType == MaintenanceCheckType.Contains && extensionString.Contains(deletionTask.m_fileExtension))
                    {
                        extensionApplies = true;
                    }

                    if (deletionTask.m_checkType == MaintenanceCheckType.Equals && extensionString.Equals(deletionTask.m_fileExtension))
                    {
                        extensionApplies = true;
                    }

                    return nameApplies && extensionApplies;

            }

            return false;
        }

        /// <summary>
        /// Setup the material name list
        /// </summary>
        //private bool SetupMaterials(GaiaConstants.EnvironmentRenderer renderPipeline, GaiaSettings gaiaSettings, int profileIndex)
        //{
        //    bool successful = false;

        //    string[] folderPaths = Directory.GetDirectories(Application.dataPath + m_materialLocation, ".", SearchOption.AllDirectories);
        //    m_unityVersion = Application.unityVersion;
        //    m_unityVersion = m_unityVersion.Remove(m_unityVersion.LastIndexOf(".")).Replace(".", "_0");
        //    string keyWordToSearch = "";

        //    if (renderPipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
        //    {
        //        keyWordToSearch = PackageInstallerUtils.m_builtInKeyWord;
        //    }
        //    else if (renderPipeline == GaiaConstants.EnvironmentRenderer.Lightweight)
        //    {
        //        keyWordToSearch = PackageInstallerUtils.m_lightweightKeyWord;
        //    }
        //    else if (renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
        //    {
        //        keyWordToSearch = PackageInstallerUtils.m_universalKeyWord;
        //    }
        //    else
        //    {
        //        keyWordToSearch = PackageInstallerUtils.m_highDefinitionKeyWord;
        //    }

        //    string mainFolder = "";
        //    foreach (string folderPath in folderPaths)
        //    {
        //        string finalFolderName = folderPath.Substring(folderPath.LastIndexOf("\\"));

        //        if (finalFolderName.Contains(keyWordToSearch + " " + m_unityVersion))
        //        {
        //            mainFolder = finalFolderName;
        //            break;
        //        }
        //    }

        //    m_profileList.Clear();

        //    List<Material> allMaterials = GetMaterials(mainFolder);
        //    if (allMaterials != null)
        //    {
        //        foreach (Material mat in allMaterials)
        //        {
        //            m_profileList.Add(mat.name);
        //        }
        //    }
        //    //Always add the "None" option for water
        //    m_profileList.Add("None");

        //    if (allMaterials.Count > 0)
        //    {
        //        successful = true;
        //    }
        //    if (m_allMaterials[profileIndex] != null)
        //    {
        //        gaiaSettings.m_gaiaWaterProfile.m_activeWaterMaterial = m_allMaterials[profileIndex];
        //    }
        //    return successful;
        //}

        /// <summary>
        /// Removes Suffix in file formats required
        /// </summary>
        /// <param name="path"></param>
        private List<Material> GetMaterials(string path)
        {
            List<Material> materials = new List<Material>();

            DirectoryInfo dirInfo = new DirectoryInfo(path);
            var files = dirInfo.GetFiles();
            foreach (FileInfo file in files)
            {
                if (file.Extension.EndsWith("mat"))
                {
                    materials.Add(AssetDatabase.LoadAssetAtPath<Material>(GaiaUtils.GetAssetPath(file.Name)));
                }
            }

            m_allMaterials = materials;

            return materials;
        }

        #endregion

        #region System Helpers

        /// <summary>
        /// Display a button that takes editor indentation into account
        /// </summary>
        /// <param name="content">Text, image and tooltip for this button</param>
        /// <returns>True is clicked</returns>
        ///
        public bool ButtonLeftAligned(string key, params GUILayoutOption[] options)
        {
            TextAnchor oldalignment = GUI.skin.button.alignment;
            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            bool result = m_editorUtils.Button(key, options);
            GUI.skin.button.alignment = oldalignment;
            return result;
        }

        /// <summary>
        /// Display a button that takes editor indentation into account
        /// </summary>
        /// <param name="content">Text, image and tooltip for this button</param>
        /// <returns>True is clicked</returns>
        public bool ButtonLeftAligned(GUIContent content)
        {
            TextAnchor oldalignment = GUI.skin.button.alignment;
            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            bool result = m_editorUtils.Button(content);
            GUI.skin.button.alignment = oldalignment;
            return result;
        }


        /// <summary>
        /// Get a clamped size value
        /// </summary>
        /// <param name="newSize"></param>
        /// <returns></returns>
        float GetClampedSize(float newSize)
        {
            return Mathf.Clamp(newSize, 32f, m_settings.m_currentDefaults.m_size);
        }

        #region Helper methods

        /// <summary>
        /// Get the asset path of the first thing that matches the name
        /// </summary>
        /// <param name="name">Name to search for</param>
        /// <returns></returns>
        private static string GetAssetPath(string name)
        {
#if UNITY_EDITOR
            string[] assets = AssetDatabase.FindAssets(name, null);
            if (assets.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(assets[0]);
            }
#endif
            return null;
        }

        /// <summary>
        /// Get the currently active terrain - or any terrain
        /// </summary>
        /// <returns>A terrain if there is one</returns>
        public static Terrain GetActiveTerrain()
        {
            //Grab active terrain if we can
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null && terrain.isActiveAndEnabled)
            {
                return terrain;
            }

            //Then check rest of terrains
            for (int idx = 0; idx < Terrain.activeTerrains.Length; idx++)
            {
                terrain = Terrain.activeTerrains[idx];
                if (terrain != null && terrain.isActiveAndEnabled)
                {
                    return terrain;
                }
            }
            return null;
        }

        #endregion

        bool ClickableHeaderCustomStyle(GUIContent content, GUIStyle style, GUILayoutOption[] options = null)
        {
            var position = GUILayoutUtility.GetRect(content, style, options);
            Handles.BeginGUI();
            Color oldColor = Handles.color;
            Handles.color = style.normal.textColor;
            Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
            Handles.color = oldColor;
            Handles.EndGUI();
            EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);
            return GUI.Button(position, content, style);
        }

        /// <summary>
        /// Get the latest news from the web site at most once every 24 hours
        /// </summary>
        /// <returns></returns>
        IEnumerator GetNewsUpdate()
        {
            TimeSpan elapsed = new TimeSpan(DateTime.Now.Ticks - m_settings.m_lastWebUpdate);
            if (elapsed.TotalHours < 24.0)
            {
                StopEditorUpdates();
            }
            else
            {
                if (PWApp.CONF != null)
                {
#if UNITY_2018_3_OR_NEWER
                    using (UnityWebRequest www = new UnityWebRequest("http://www.procedural-worlds.com/gaiajson.php?gv=gaia-" + PWApp.CONF.Version))
                    {
                        while (!www.isDone)
                        {
                            yield return www;
                        }

                        if (!string.IsNullOrEmpty(www.error))
                        {
                            //Debug.Log(www.error);
                        }
                        else
                        {
                            try
                            {
                                string result = www.url;
                                int first = result.IndexOf("####");
                                if (first > 0)
                                {
                                    result = result.Substring(first + 10);
                                    first = result.IndexOf("####");
                                    if (first > 0)
                                    {
                                        result = result.Substring(0, first);
                                        result = result.Replace("<br />", "");
                                        result = result.Replace("&#8221;", "\"");
                                        result = result.Replace("&#8220;", "\"");
                                        var message = JsonUtility.FromJson<GaiaMessages>(result);
                                        m_settings.m_latestNewsTitle = message.title;
                                        m_settings.m_latestNewsBody = message.bodyContent;
                                        m_settings.m_latestNewsUrl = message.url;
                                        m_settings.m_lastWebUpdate = DateTime.Now.Ticks;
                                        EditorUtility.SetDirty(m_settings);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                //Debug.Log(e.Message);
                            }
                        }
                    }
#else
                    using (WWW www = new WWW("http://www.procedural-worlds.com/gaiajson.php?gv=gaia-" + PWApp.CONF.Version))
                    {
                        while (!www.isDone)
                        {
                            yield return www;
                        }

                        if (!string.IsNullOrEmpty(www.error))
                        {
                            //Debug.Log(www.error);
                        }
                        else
                        {
                            try
                            {
                                string result = www.text;
                                int first = result.IndexOf("####");
                                if (first > 0)
                                {
                                    result = result.Substring(first + 10);
                                    first = result.IndexOf("####");
                                    if (first > 0)
                                    {
                                        result = result.Substring(0, first);
                                        result = result.Replace("<br />", "");
                                        result = result.Replace("&#8221;", "\"");
                                        result = result.Replace("&#8220;", "\"");
                                        var message = JsonUtility.FromJson<GaiaMessages>(result);
                                        m_settings.m_latestNewsTitle = message.title;
                                        m_settings.m_latestNewsBody = message.bodyContent;
                                        m_settings.m_latestNewsUrl = message.url;
                                        m_settings.m_lastWebUpdate = DateTime.Now.Ticks;
                                        EditorUtility.SetDirty(m_settings);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                //Debug.Log(e.Message);
                            }
                        }
                    }
                
#endif
                }
            }
            StopEditorUpdates();
        }

        /// <summary>
        /// Import Package
        /// </summary>
        /// <param name="packageName"></param>
        public static void ImportPackage(string packageName)
        {
            string packageGaia = AssetUtils.GetAssetPath(packageName + ".unitypackage");
            Debug.Log(packageGaia);
            if (!string.IsNullOrEmpty(packageGaia))
            {
                AssetDatabase.ImportPackage(packageGaia, true);
            }
            else
                Debug.Log("Unable to find Gaia Dependencies.unitypackage");
        }

        /// <summary>
        /// Start editor updates
        /// </summary>
        public void StartEditorUpdates()
        {
            EditorApplication.update += EditorUpdate;
        }

        //Stop editor updates
        public void StopEditorUpdates()
        {
            EditorApplication.update -= EditorUpdate;
        }

        /// <summary>
        /// This is executed only in the editor - using it to simulate co-routine execution and update execution
        /// </summary>
        void EditorUpdate()
        {
            if (m_updateCoroutine == null)
            {
                StopEditorUpdates();
            }
            else
            {
                m_updateCoroutine.MoveNext();
            }
        }
        #endregion

        #region GAIA eXtensions GX
        public static List<Type> GetTypesInNamespace(string nameSpace)
        {
            List<Type> gaiaTypes = new List<Type>();

            int assyIdx, typeIdx;
            System.Type[] types;
            System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            for (assyIdx = 0; assyIdx < assemblies.Length; assyIdx++)
            {
                if (assemblies[assyIdx].FullName.StartsWith("Assembly"))
                {
                    types = assemblies[assyIdx].GetTypes();
                    for (typeIdx = 0; typeIdx < types.Length; typeIdx++)
                    {
                        if (!string.IsNullOrEmpty(types[typeIdx].Namespace))
                        {
                            if (types[typeIdx].Namespace.StartsWith(nameSpace))
                            {
                                gaiaTypes.Add(types[typeIdx]);
                            }
                        }
                    }
                }
            }
            return gaiaTypes;
        }

        /// <summary>
        /// Return true if image FX have been included
        /// </summary>
        /// <returns></returns>
        public static bool GotImageFX()
        {
            List<Type> types = GetTypesInNamespace("UnityStandardAssets.ImageEffects");
            if (types.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Commented out tooltips
        ///// <summary>
        ///// The tooltips
        ///// </summary>
        //static Dictionary<string, string> m_tooltips = new Dictionary<string, string>
        //{
        //    { "Execution Mode", "The way this spawner runs. Design time : At design time only. Runtime Interval : At run time on a timed interval. Runtime Triggered Interval : At run time on a timed interval, and only when the tagged game object is closer than the trigger range from the center of the spawner." },
        //    { "Controller", "The type of control method that will be set up. " },
        //    { "Environment", "The type of environment that will be set up. This pre-configures your terrain settings to be better suited for the environment you are targeting. You can modify these setting by modifying the relevant terrain default settings." },
        //    { "Renderer", "The terrain renderer you are targeting. The 2018x renderers are only relevent when using Unity 2018 and above." },
        //    { "Terrain Size", "The size of the terrain you are setting up. Please be aware that larger terrain sizes are harder for Unity to render, and will result in slow frame rates. You also need to consider your target environment as well. A mobile or VR device will have problems with large terrains." },
        //    { "Terrain Defaults", "The default settings that will be used when creating new terrains." },
        //    { "Terrain Resources", "The texture, detail and tree resources that will be used when creating new terrains." },
        //    { "GameObject Resources", "The game object resources that will be passed to your GameObject spawners when creating new spawners." },
        //    { "1. Create Terrain & Show Stamper", "Creates your terrain based on the setting in the panel above. You use the stamper to terraform your terrain." },
        //    { "2. Create Spawners", "Creates the spawners based on your resources in the panel above. You use spawners to inject these resources into your scene." },
        //    { "3. Create Player, Water and Screenshotter", "Creates the things you most commonly need in your scene to make it playable." },
        //    { "3. Create Player, Wind, Water and Screenshotter", "Creates the things you most commonly need in your scene to make it playable." },
        //    { "Show Session Manager", "The session manager records stamping and spawning operations so that you can recreate your terrain later." },
        //    { "Create Terrain", "Creates a terrain based on your settings." },
        //    { "Create Coverage Texture Spawner", "Creates a texture spawner so you can paint your terrain." },
        //    { "Create Coverage Grass Spawner", "Creates a grass (terrain details) spawner so you can cover your terrain with grass." },
        //    { "Create Clustered Grass Spawner", "Creates a grass (terrain details) spawner so you can cover your terrain with patches with grass." },
        //    { "Create Coverage Terrain Tree Spawner", "Creates a terrain tree spawner so you can cover your terrain with trees." },
        //    { "Create Clustered Terrain Tree Spawner", "Creates a terrain tree spawner so you can cover your terrain with clusters with trees." },
        //    { "Create Coverage Prefab Tree Spawner", "Creates a tree spawner from prefabs so you can cover your terrain with trees." },
        //    { "Create Clustered Prefab Tree Spawner", "Creates a tree spawner from prefabs so you can cover your terrain with clusters with trees." },
        //    { "Create Coverage Prefab Spawner", "Creates a spawner from prefabs so you can cover your terrain with instantiations of those prefabs." },
        //    { "Create Clustered Prefab Spawner", "Creates a spawner from prefabs so you can cover your terrain with clusters of those prefabs." },
        //    { "Show Stamper", "Shows a stamper. Use the stamper to terraform your terrain." },
        //    { "Show Scanner", "Shows the scanner. Use the scanner to create new stamps from textures, world machine .r16 files, IBM 16 bit RAW file, MAC 16 bit RAW files, Terrains, and Meshes (with mesh colliders)." },
        //    { "Show Visualiser", "Shows the visualiser. Use the visualiser to visualise and configure fitness values for your resources." },
        //    { "Show Terrain Utilities", "Shows terrain utilities. These are a great way to add additional interest to your terrains." },
        //    { "Show Splatmap Exporter", "Shows splatmap exporter. Exports your texture splatmaps." },
        //    { "Show Grass Exporter", "Shows grass exporter. Exports your grass control maps." },
        //    { "Show Mesh Exporter", "Shows mesh exporter. Exports your terrain as a low poly mesh. Use in conjunction with Base Map Exporter and Normal Map Exporter in Terrain Utilties to create cool mesh features to use in the distance." },
        //    { "Show Shore Exporter", "Shows shore exporter. Exports a mask of your terrain shoreline." },
        //    { "Show Extension Exporter", "Shows extension exporter. Use extensions to save resource and spawner configurations for later use via the GX tab." },
        //};
        #endregion
    }
}
