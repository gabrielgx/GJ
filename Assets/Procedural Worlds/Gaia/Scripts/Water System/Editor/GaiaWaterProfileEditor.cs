using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ProcedualWorlds.WaterSystem.MeshGeneration;
using ProcedualWorlds.WaterSystem;
using Gaia.Internal;
using PWCommon2;
using System.IO;

namespace Gaia
{
    [CustomEditor(typeof(GaiaWaterProfile))]
    public class GaiaWaterProfileEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private string m_version;
        private string m_unityVersion;
        private GaiaSettings m_gaiaSettings;
        private GaiaWaterProfile m_profile;
        private GaiaWaterProfileValues m_profileValues;
        private PWS_MeshGenerationPro m_waterSystemPro;
        private GaiaConstants.EnvironmentRenderer m_renderPipeline;
        private List<string> m_profileList = new List<string>();
        //private List<Material> m_allMaterials = new List<Material>();
        [SerializeField]
        private Material m_gaiaOceanMat;
        //Where saved settings are kept
        private GaiaGlobal m_savedSettings;

        private int newProfileListIndex = 1;
        private bool updateSettingsOnly;
        private Texture2D m_waterTexture;
        private bool enableEditMode;

        public Gradient gradient;

        private void OnEnable()
        {
            //Get Gaia Lighting Profile object
            m_profile = (GaiaWaterProfile)target;

#if !GAIA_PRO_PRESENT
            if (m_profile != null)
            {
                for (int i = 0; i < m_profile.m_customRenderDistances.Length; i++)
                {
                    m_profile.m_customRenderDistances[i] = 0f;
                }

                m_profile.m_customRenderDistance = 0f;
            }
#endif
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = GaiaUtils.GetGaiaSettings();
            }

            GameObject multiObject = GameObject.Find("Gaia Global");
            if (multiObject != null)
            {
                if (m_savedSettings == null)
                {
                    m_savedSettings = multiObject.GetComponent<GaiaGlobal>();
                }
            }
            if (m_profile.m_multiSceneLightingSupport)
            {
                if (m_savedSettings != null)
                {
                    m_savedSettings.LoadProfileType(m_profile);
                    m_profileValues = m_profile.m_waterProfiles[m_profile.m_selectedWaterProfileValuesIndex];
                }
            }

            m_renderPipeline = m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled;
            m_version = PWApp.CONF.Version;

            m_waterSystemPro = Object.FindObjectOfType<PWS_MeshGenerationPro>();

            newProfileListIndex = m_gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex;
            if (newProfileListIndex < 0)
            {
                newProfileListIndex = 1;
            }

            m_gaiaOceanMat = GaiaWater.GetGaiaOceanMaterial();

            enableEditMode = System.IO.Directory.Exists(GaiaUtils.GetAssetPath("Dev Utilities"));
        }

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

        public override void OnInspectorGUI()
        {
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = GaiaUtils.GetGaiaSettings();
            }

            if (m_renderPipeline != m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled)
            {
                m_renderPipeline = m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled;
            }

            //Monitor for changes
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Profile Version: " + m_version);

            if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                EditorGUILayout.HelpBox("Water for High Definition Pipeline is not yet Supported in Gaia. This will be available in the near future.", MessageType.Info);
            }

            if (m_renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                if (enableEditMode)
                {
                    m_profile.m_editSettings = EditorGUILayout.ToggleLeft("Use Procedural Worlds Editor Settings", m_profile.m_editSettings);
                }
                else
                {
                    m_profile.m_editSettings = false;
                }

                m_editorUtils.Panel("UpdateSettings", RealtimeUpdateEnabled, false);
                m_editorUtils.Panel("GlobalSettings", GlobalSettingsEnabled, false);
                m_editorUtils.Panel("ReflectionSettings", ReflectionSettingsEnabled, false);
                m_editorUtils.Panel("MeshQualitySettings", WaterMeshQualityEnabled, false);
                m_editorUtils.Panel("UnderwaterSettings", UnderwaterSettingsEnabled, false);
                m_editorUtils.Panel("WaterProfileSettings", WaterProfileSettingsEnabled, false);

                if (m_profile.m_editSettings)
                {
                    DrawDefaultInspector();
                }
            }

            //Check for changes, make undo record, make changes and let editor know we are dirty
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_profile, "Made changes");
                EditorUtility.SetDirty(m_profile);

                if (m_profile.m_updateInRealtime)
                {
                    if (m_profile.m_selectedProfile != "None")
                    {
                        GaiaWater.GetProfile(newProfileListIndex, m_gaiaOceanMat, m_profile, m_renderPipeline, true, updateSettingsOnly);
                        updateSettingsOnly = false;
                    }
                }
            }
        }

        private void RealtimeUpdateEnabled(bool helpEnabled)
        {
            m_profile.m_updateInRealtime = m_editorUtils.ToggleLeft("UpdateInRealtime", m_profile.m_updateInRealtime);
            if (m_profile.m_updateInRealtime)
            {
                EditorGUILayout.HelpBox("Update In Realtime is enabled this will allow profiles to be endited inside the editor and automatically apply changes every frame. This feature can be expensive and should not be left enabled in testing and builds", MessageType.Warning);
            }
            else
            {
                if (m_editorUtils.Button("UpdateToScene"))
                {
                    if (m_profile.m_selectedProfile != "None")
                    {
                        GaiaWater.GetProfile(newProfileListIndex, m_gaiaOceanMat, m_profile, m_renderPipeline, true, true);
                    }
                }
            }
        }

        private void GlobalSettingsEnabled(bool helpEnabled)
        {
            /*
            m_profile.m_masterWaterMaterial = (Material)m_editorUtils.ObjectField("MasterWaterMaterial", m_profile.m_masterWaterMaterial, typeof(Material), false, GUILayout.Height(16f));
            if (m_profile.m_masterWaterMaterial == null)
            {
                EditorGUILayout.HelpBox("Missing a master water material, please add one.", MessageType.Error);
            }
            */
            m_profile.m_waterPrefab = (GameObject)m_editorUtils.ObjectField("WaterPrefab", m_profile.m_waterPrefab, typeof(GameObject), false, GUILayout.Height(16f));
            if (m_profile.m_waterPrefab == null)
            {
                EditorGUILayout.HelpBox("Missing a water prefab, please add one.", MessageType.Error);
            }
            m_profile.m_enableGPUInstancing = m_editorUtils.ToggleLeft("EnableGPUInstancing", m_profile.m_enableGPUInstancing);
            if (!m_profile.m_enableGPUInstancing)
            {
                EditorGUILayout.HelpBox("Enabling GPU instancing will help improve performance. We recommend you enable this setting.", MessageType.Info);
            }

            /*
            m_profile.m_enableOceanFoam = m_editorUtils.ToggleLeft("EnableOceanFoam", m_profile.m_enableOceanFoam);
            m_profile.m_enableBeachFoam = m_editorUtils.ToggleLeft("EnableBeachFoam", m_profile.m_enableBeachFoam);
            m_profile.m_autoWindControlOnWater = m_editorUtils.ToggleLeft("EnableAutoWindControl", m_profile.m_autoWindControlOnWater);
            */
        }

        private void ReflectionSettingsEnabled(bool helpEnabled)
        {
            m_profile.m_enableReflections = m_editorUtils.ToggleLeft("EnableReflections", m_profile.m_enableReflections);
            if (m_profile.m_enableReflections)
            {
                if (m_renderPipeline != GaiaConstants.EnvironmentRenderer.BuiltIn)
                {
                    if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.Lightweight)
                    {
                        EditorGUILayout.HelpBox("Realtime reflections is not yet supported in SRP", MessageType.Info);
                    }
                    else
                    {
                        m_profile.m_hdPlanarReflections = (GameObject)m_editorUtils.ObjectField("HDPlanarReflections", m_profile.m_hdPlanarReflections, typeof(GameObject), false, GUILayout.Height(16f));
                        if (m_profile.m_hdPlanarReflections == null)
                        {
                            EditorGUILayout.HelpBox("Missing HD Planar Reflections Prefab. Please add it to use this feature.", MessageType.Error);
                        }
                        else
                        {
                            if (m_editorUtils.Button("FocusPlanarReflections"))
                            {
                                GameObject reflectionObject = GameObject.Find(m_profile.m_hdPlanarReflections.name);
                                if (reflectionObject != null)
                                {
                                    Selection.activeObject = reflectionObject;
                                }
                                else
                                {
                                    Debug.Log("Object doesn't exist in the scene");
                                }
                            }

                            EditorGUILayout.HelpBox("In HDRP reflections are handled on the Planar Reflections. You can edit the settings from there. To changes the reflection resolution you can change this in the HD Render Pipeline asset. If you're reflections don't cover the field of view focus the planar reflections and update the field of view on the Planar Reflections object.", MessageType.Info);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Reflections render the object reflections on the water surface. This can be very expensive feature and could cause performance issue on low end machines.", MessageType.Info);

                    GUILayout.Space(15f);
                    m_editorUtils.Heading("ReflectionSupport");
                    m_profile.m_allowMSAA = m_editorUtils.ToggleLeft("AllowMSAA", m_profile.m_allowMSAA);
                    m_profile.m_useHDR = m_editorUtils.ToggleLeft("UseHDR", m_profile.m_useHDR);
                    m_profile.m_disablePixelLights = m_editorUtils.ToggleLeft("DisablePixelLights", m_profile.m_disablePixelLights);
                    m_profile.m_enableDisabeHeightFeature = m_editorUtils.ToggleLeft("EnableHeightFeatures", m_profile.m_enableDisabeHeightFeature, helpEnabled);
                    if (m_profile.m_enableDisabeHeightFeature)
                    {
                        EditorGUI.indentLevel++;
                        m_profile.m_disableHeight = m_editorUtils.FloatField("DisableHeight", m_profile.m_disableHeight, helpEnabled);
                        EditorGUI.indentLevel--;
                    }
                    m_profile.m_useCustomRenderDistance = m_editorUtils.ToggleLeft("UseCustomRenderDistance", m_profile.m_useCustomRenderDistance);
                    if (m_profile.m_useCustomRenderDistance)
                    {
                        EditorGUI.indentLevel++;
                        m_profile.m_enableLayerDistances = m_editorUtils.ToggleLeft("EnableMultiLayerDistance", m_profile.m_enableLayerDistances, helpEnabled);
                        if (m_profile.m_enableLayerDistances)
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
                                m_profile.m_customRenderDistances[i] = EditorGUILayout.FloatField(m_profile.m_customRenderDistances[i]);
                                EditorGUILayout.EndHorizontal();
                                EditorGUI.indentLevel--;
                            }
                        }
                        else
                        {
                            EditorGUI.indentLevel++;
                            m_profile.m_customRenderDistance = m_editorUtils.FloatField("CustomRenderDistance", m_profile.m_customRenderDistance);
                            EditorGUI.indentLevel--;
                        }
                        EditorGUI.indentLevel--;
                    }
                    GUILayout.Space(15f);
                    m_editorUtils.Heading("ReflectionSetup");
                    m_profile.m_clipPlaneOffset = m_editorUtils.Slider("ClipPlaneOffset", m_profile.m_clipPlaneOffset, -5f, 100f);
                    m_profile.m_waterRenderUpdateMode = (PWS_WaterSystem.RenderUpdateMode)EditorGUILayout.EnumPopup("Render Update Mode", m_profile.m_waterRenderUpdateMode);
                    if (m_profile.m_waterRenderUpdateMode == PWS_WaterSystem.RenderUpdateMode.Interval)
                    {
                        m_profile.m_interval = m_editorUtils.Slider("IntervalTime", m_profile.m_interval, 0f, 50f);
                    }
                    GUILayout.Space(15f);
                    m_editorUtils.Heading("ReflectionRendering");
                    m_profile.m_reflectionResolution = (GaiaConstants.GaiaProWaterReflectionsQuality)EditorGUILayout.EnumPopup("Reflection Resolution", m_profile.m_reflectionResolution);
                    m_profile.m_reflectedLayers = LayerMaskField("ReflectedLayers", m_editorUtils, m_profile.m_reflectedLayers);

                    if (!m_profile.m_updateInRealtime)
                    {
                        if (m_editorUtils.Button("UpdateReflectionSettings"))
                        {
                            GaiaWater.SetupWaterReflectionSettings(m_profile, m_profileValues, true);
                        }
                    }
                }
            }
        }

        private void WaterMeshQualityEnabled(bool helpEnabled)
        {
            //m_profile.m_enableWaterMeshQuality = EditorGUILayout.ToggleLeft("Enable Water Mesh Quality", m_profile.m_enableWaterMeshQuality);
            m_profile.m_enableWaterMeshQuality = true;
            if (m_profile.m_enableWaterMeshQuality)
            {
                m_profile.m_waterMeshQuality = (GaiaConstants.WaterMeshQuality)EditorGUILayout.EnumPopup("Water Mesh Quality", m_profile.m_waterMeshQuality);
                m_profile.m_meshType = (PWS_MeshGenerationPro.MeshType)EditorGUILayout.EnumPopup("Mesh Type", m_profile.m_meshType);
                if (m_profile.m_waterMeshQuality == GaiaConstants.WaterMeshQuality.Custom)
                {
                    if (m_profile.m_meshType == PWS_MeshGenerationPro.MeshType.Plane)
                    {
                        m_profile.m_customMeshQuality = m_editorUtils.IntSlider("CustomMeshQuality", m_profile.m_customMeshQuality, 1, 16);
                    }
                    else
                    {
                        m_profile.m_customMeshQuality = m_editorUtils.IntSlider("CustomMeshQuality", m_profile.m_customMeshQuality, 1, 16);
                        //m_profile.m_customMeshQuality = m_editorUtils.IntSlider("CustomMeshQuality", (int)m_profile.m_customMeshQuality * 2, 2, 256) / 2;
                    }

                }

                if (m_profile.m_meshType == PWS_MeshGenerationPro.MeshType.Plane)
                {
                    m_profile.m_xSize = m_editorUtils.IntField("WaterSize", m_profile.m_xSize);
                    m_profile.m_zSize = m_profile.m_xSize;
                }
                else
                {
                    m_profile.m_xSize = m_editorUtils.IntField("WaterSize", m_profile.m_xSize);
                    m_profile.m_zSize = m_profile.m_xSize;
                }

                EditorGUILayout.BeginHorizontal();
                m_editorUtils.Label("PolyCountGenerated");
                if (m_waterSystemPro == null)
                {
                    m_waterSystemPro = FindObjectOfType<PWS_MeshGenerationPro>();
                }
                else
                {
                    m_waterSystemPro.m_MeshType = m_profile.m_meshType;
                    m_waterSystemPro.m_Size.x = m_profile.m_xSize;
                    m_waterSystemPro.m_Size.z = m_profile.m_zSize;
                    m_waterSystemPro.m_meshDensity.x = m_profile.m_customMeshQuality;
                    m_waterSystemPro.m_meshDensity.y = m_profile.m_customMeshQuality;
                    EditorGUILayout.LabelField(m_waterSystemPro.CalculatePolysRequired().ToString());
                }
                EditorGUILayout.EndHorizontal();

                if (!m_profile.m_updateInRealtime)
                {
                    if (m_editorUtils.Button("UpdateWaterMeshQuality"))
                    {
                        GaiaWater.UpdateWaterMeshQuality(m_profile, m_profile.m_waterPrefab);
                    }
                }
            }
        }

        private void UnderwaterSettingsEnabled(bool helpEnabled)
        {
            if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                EditorGUILayout.HelpBox("Underwater effects are not yet supported in HDRP.", MessageType.Info);
            }
            else
            {
                //if (m_editorUtils.Button("EditUnderwaterSettings"))
                //{
                //    FocusUnderwaterSettings();
                //}

                m_editorUtils.Panel("CausticSettings", CausticSettingsEnabled, false);

                m_profile.m_underwaterParticles = (GameObject)m_editorUtils.ObjectField("UnderwaterParticlesPrefab", m_profile.m_underwaterParticles, typeof(GameObject), false, GUILayout.Height(16f));
                if (m_profile.m_underwaterParticles == null)
                {
                    EditorGUILayout.HelpBox("Missing underwater particles prefab, please add one.", MessageType.Error);
                }
                m_profile.m_underwaterHorizonPrefab = (GameObject)m_editorUtils.ObjectField("UnderwaterHorizonPrefab", m_profile.m_underwaterHorizonPrefab, typeof(GameObject), false, GUILayout.Height(16f));
                if (m_profile.m_underwaterHorizonPrefab == null)
                {
                    EditorGUILayout.HelpBox("Missing a underwater horizon prefab, please add one.", MessageType.Error);
                }
                m_profile.m_supportUnderwaterEffects = m_editorUtils.ToggleLeft("SupportUnderwaterEffects", m_profile.m_supportUnderwaterEffects);
                if (m_profile.m_supportUnderwaterEffects)
                {
                    m_profile.m_supportUnderwaterPostProcessing = m_editorUtils.ToggleLeft("SupportUnderwaterPostProcessing", m_profile.m_supportUnderwaterPostProcessing);

                    m_profile.m_supportUnderwaterFog = m_editorUtils.ToggleLeft("SupportUnderwaterFog", m_profile.m_supportUnderwaterFog);

                    m_profile.m_supportUnderwaterParticles = m_editorUtils.ToggleLeft("SupportUnderwaterParticles", m_profile.m_supportUnderwaterParticles);

                    if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.Lightweight)
                    {
                        EditorGUILayout.HelpBox("Underwater effects are in 'Experimental' These will change and be updated over time. LWRP does work but you may experience some artifacts.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Underwater effects are in 'Experimental' These will change and be updated over time.", MessageType.Info);
                    }
                }
            }
        }

        private void WaterProfileSettingsEnabled(bool helpEnabled)
        {
            EditorGUI.BeginChangeCheck();

            if (m_gaiaOceanMat != null)
            {
                //Setup Profiles
                m_profileList.Clear();
                if (m_profile.m_waterProfiles.Count > 0)
                {
                    foreach (GaiaWaterProfileValues profile in m_profile.m_waterProfiles)
                    {
                        m_profileList.Add(profile.m_typeOfWater);
                    }
                }
                m_profileList.Add("None");

                newProfileListIndex = m_profile.m_selectedWaterProfileValuesIndex;
                //Get Profile values
                if (newProfileListIndex > m_profileList.Count)
                {
                    newProfileListIndex = 0;
                }

                if (m_profileList[newProfileListIndex] == "None")
                {
                    m_profile.m_selectedWaterProfileValuesIndex = EditorGUILayout.Popup("Water Profile", m_profile.m_selectedWaterProfileValuesIndex, m_profileList.ToArray());
                    EditorGUILayout.HelpBox("No water profile selected", MessageType.Info);
                    return;
                }

                m_profileValues = m_profile.m_waterProfiles[newProfileListIndex];

                //Profile setup
                m_editorUtils.Heading("OceanSettings");

                EditorGUILayout.BeginHorizontal();
                if (m_profile.m_renamingProfile)
                {
                    m_profileValues.m_profileRename = m_editorUtils.TextField("NewProfileName", m_profileValues.m_profileRename);
                }
                else
                {
                    m_profile.m_selectedWaterProfileValuesIndex = EditorGUILayout.Popup("Water Profile", m_profile.m_selectedWaterProfileValuesIndex, m_profileList.ToArray());
                }
                if (m_profileValues.m_userCustomProfile)
                {
                    if (m_profile.m_renamingProfile)
                    {
                        if (m_editorUtils.Button("Save", GUILayout.MaxWidth(55f)))
                        {
                            m_profileValues.m_typeOfWater = m_profileValues.m_profileRename;
                            m_profile.m_selectedProfile = m_profileValues.m_profileRename;
                            m_profile.m_renamingProfile = false;
                        }
                    }
                    else
                    {
                        if (m_editorUtils.Button("Rename", GUILayout.MaxWidth(55f)))
                        {
                            m_profile.m_renamingProfile = true;
                            m_profileValues.m_profileRename = m_profileValues.m_typeOfWater;
                        }
                    }

                    if (m_editorUtils.Button("Remove", GUILayout.MaxWidth(30f)))
                    {
                        m_profile.m_renamingProfile = false;
                        m_profile.m_waterProfiles.RemoveAt(m_profile.m_selectedWaterProfileValuesIndex);
                        m_profile.m_selectedWaterProfileValuesIndex--;
                    }
                }
                if (m_editorUtils.Button("MakeCopy", GUILayout.MaxWidth(30f)))
                {
                    AddNewCustomProfile();
                }
                EditorGUILayout.EndHorizontal();

                if (m_profileList[newProfileListIndex] == "None")
                {
                    EditorGUILayout.HelpBox("No water profile selected", MessageType.Info);
                    return;
                }

                if (!m_profileValues.m_userCustomProfile)
                {
                    m_profile.m_renamingProfile = false;
                    EditorGUILayout.HelpBox("This is one of the default Gaia water profiles. To create your own please press the '+' button to create a copy from the current selected profile.", MessageType.Info);
                    GUI.enabled = false;
                }

                if (m_profile.m_editSettings)
                {
                    GUI.enabled = true;
                }

                m_editorUtils.Label("UnderwaterSettings");
                EditorGUI.indentLevel++;
                m_profileValues.m_postProcessingProfile = m_editorUtils.TextField("UnderwaterPostProcessingProfile", m_profileValues.m_postProcessingProfile, helpEnabled);
                if (m_profileValues.m_underwaterFogGradient == null)
                {
                    m_profileValues.m_underwaterFogGradient = new Gradient();
                }
                m_profileValues.m_underwaterFogGradient = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("UnderwaterFog"), m_editorUtils.GetTooltip("UnderwaterFog")), m_profileValues.m_underwaterFogGradient);
                m_editorUtils.InlineHelp("UnderwaterFog", helpEnabled);
                if (RenderSettings.fogMode == FogMode.Exponential || RenderSettings.fogMode == FogMode.ExponentialSquared)
                {
                    m_profileValues.m_underwaterFogDensity = m_editorUtils.Slider("UnderwaterFogDensity", m_profileValues.m_underwaterFogDensity, 0f, 1f, helpEnabled);
                }
                else
                {
                    m_profileValues.m_underwaterNearFogDistance = m_editorUtils.FloatField("UnderwaterFogStart", m_profileValues.m_underwaterNearFogDistance, helpEnabled);
                    m_profileValues.m_underwaterFogDistance = m_editorUtils.FloatField("UnderwaterFogEnd", m_profileValues.m_underwaterFogDistance, helpEnabled);
                }
                m_editorUtils.Heading("UnderwaterPostFX");
#if GAIA_PRO_PRESENT
                if (ProceduralWorldsGlobalWeather.Instance == null)
                {
                    m_profileValues.m_constUnderwaterPostExposure = m_editorUtils.Slider("UnderwaterPostExposureFX", m_profileValues.m_constUnderwaterPostExposure, -5f, 5f, helpEnabled);
                    m_profileValues.m_constUnderwaterColorFilter = m_editorUtils.ColorField("UnderwaterColorFilterFX", m_profileValues.m_constUnderwaterColorFilter, helpEnabled);
                }
                else
                {
                    m_profileValues.m_gradientUnderwaterPostExposure = m_editorUtils.CurveField("UnderwaterPostExposureFX", m_profileValues.m_gradientUnderwaterPostExposure, helpEnabled);
                    m_profileValues.m_gradientUnderwaterColorFilter = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("UnderwaterColorFilterFX"), m_editorUtils.GetTooltip("UnderwaterColorFilterFX")), m_profileValues.m_gradientUnderwaterColorFilter);
                    m_editorUtils.InlineHelp("UnderwaterColorFilterFX", helpEnabled);
                }
#else
                m_profileValues.m_gradientUnderwaterPostExposure = m_editorUtils.CurveField("UnderwaterPostExposureFX", m_profileValues.m_gradientUnderwaterPostExposure, helpEnabled);
                m_profileValues.m_gradientUnderwaterColorFilter = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("UnderwaterColorFilterFX"), m_editorUtils.GetTooltip("UnderwaterColorFilterFX")), m_profileValues.m_gradientUnderwaterColorFilter);
                m_editorUtils.InlineHelp("UnderwaterColorFilterFX", helpEnabled);
#endif
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                m_editorUtils.Label("LightingSettings");
                EditorGUI.indentLevel++;
                m_profileValues.m_metallic = m_editorUtils.Slider("Metallic", m_profileValues.m_metallic, 0f, 1f, helpEnabled);
                m_profileValues.m_smoothness = m_editorUtils.Slider("Smoothness", m_profileValues.m_smoothness, 0f, 1f, helpEnabled);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                m_editorUtils.Label("FadeSettings");
                EditorGUI.indentLevel++;
                m_profileValues.m_fadeStart = m_editorUtils.FloatField("FadeStart", m_profileValues.m_fadeStart, helpEnabled);
                m_profileValues.m_fadeDistance = m_editorUtils.FloatField("FadeDistance", m_profileValues.m_fadeDistance, helpEnabled);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                m_editorUtils.Label("TilingSettings");
                EditorGUI.indentLevel++;
                m_profileValues.m_foamTiling = m_editorUtils.IntField("FoamTiling", m_profileValues.m_foamTiling, helpEnabled);
                m_profileValues.m_waterTiling = m_editorUtils.IntField("WaterTiling", m_profileValues.m_waterTiling, helpEnabled);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                m_editorUtils.Label("ColorSettings");
                EditorGUI.indentLevel++;
                m_profileValues.m_refractionRenderResolution = (PW_Builtin_Refraction.PW_RENDER_SIZE)m_editorUtils.EnumPopup("RefractionResolution", m_profileValues.m_refractionRenderResolution, helpEnabled);
                EditorGUILayout.BeginHorizontal();
                m_profileValues.m_colorDepthRamp = (Texture2D)m_editorUtils.ObjectField("BakedColorDepthRamp", m_profileValues.m_colorDepthRamp, typeof(Texture2D), false, helpEnabled, GUILayout.Height(16f));
                if (m_editorUtils.Button("ClearBaked", GUILayout.Width(90f)))
                {
                    m_profileValues.m_colorDepthRamp = null;
                    PWS_WaterSystem pWS_WaterSystem = GameObject.FindObjectOfType<PWS_WaterSystem>();
                    if (pWS_WaterSystem != null)
                    {
                        pWS_WaterSystem.m_waterTexture = null;
                    }
                }
                EditorGUILayout.EndHorizontal();
                if (m_profileValues.m_colorDepthRamp == null)
                {
                    EditorGUILayout.BeginHorizontal();
                    m_profileValues.m_waterGradient = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("WaterGradientColor"), m_editorUtils.GetTooltip("WaterGradientColor")), m_profileValues.m_waterGradient);
                    if (m_editorUtils.Button("RevertGradient", GUILayout.Width(90f)))
                    {
                        m_profileValues.m_waterGradient = CreateNewWaterSurfaceGradient(newProfileListIndex);
                    }
                    EditorGUILayout.EndHorizontal();
                    m_editorUtils.InlineHelp("WaterGradientColor", helpEnabled);
                    EditorGUILayout.BeginHorizontal();
                    m_profileValues.m_gradientTextureResolution = Mathf.Clamp(EditorGUILayout.IntField(m_editorUtils.GetTextValue("TextureResolution"), m_profileValues.m_gradientTextureResolution), 1, 4096);
                    if (m_editorUtils.Button("GenerateTexture", GUILayout.Width(90f)))
                    {
                        string path = EditorUtility.SaveFilePanel("Save Water Depth Ramp as PNG", Application.dataPath, "New Water Depth Ramp.png", "png");
                        if (path.Length != 0)
                        {
                            GenerateGradientTexture(m_profileValues, path);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if (m_profileValues.m_waterGradient == null)
                    {
                        m_profileValues.m_waterGradient = CreateNewWaterSurfaceGradient(0);
                    }
                }
                m_profileValues.m_transparentDistance = m_editorUtils.Slider("TransparentDistance", m_profileValues.m_transparentDistance, 0f, 32f, helpEnabled);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                m_editorUtils.Label("NormalSettings");
                EditorGUI.indentLevel++;
                m_profileValues.m_normalLayer0 = (Texture2D)m_editorUtils.ObjectField("NormalLayer0", m_profileValues.m_normalLayer0, typeof(Texture2D), false, helpEnabled, GUILayout.Height(16f));
                m_profileValues.m_normalStrength0 = m_editorUtils.Slider("NormalStrength0", m_profileValues.m_normalStrength0, 0f, 3f, helpEnabled);
                m_profileValues.m_normalLayer1 = (Texture2D)m_editorUtils.ObjectField("NormalLayer1", m_profileValues.m_normalLayer1, typeof(Texture2D), false, helpEnabled, GUILayout.Height(16f));
                m_profileValues.m_normalStrength1 = m_editorUtils.Slider("NormalStrength1", m_profileValues.m_normalStrength1, 0f, 3f, helpEnabled);
                m_profileValues.m_fadeNormal = (Texture2D)m_editorUtils.ObjectField("FadeNormal", m_profileValues.m_fadeNormal, typeof(Texture2D), false, helpEnabled, GUILayout.Height(16f));
                m_profileValues.m_fadeNormalStrength = m_editorUtils.Slider("FadeNormalStrength", m_profileValues.m_fadeNormalStrength, 0f, 3f, helpEnabled);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                m_editorUtils.Label("FoamSettings");
                EditorGUI.indentLevel++;
                m_profileValues.m_foamAlphaRamp = (Texture2D)m_editorUtils.ObjectField("FoamAlphaRamp", m_profileValues.m_foamAlphaRamp, typeof(Texture2D), false, helpEnabled, GUILayout.Height(16f));
                m_profileValues.m_foamTexture = (Texture2D)m_editorUtils.ObjectField("FoamTexture", m_profileValues.m_foamTexture, typeof(Texture2D), false, helpEnabled, GUILayout.Height(16f));
                m_profileValues.m_foamDistance = m_editorUtils.Slider("FoamDistance", m_profileValues.m_foamDistance, 0f, 8f, helpEnabled);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                m_editorUtils.Label("ReflectionSettings");
                EditorGUI.indentLevel++;
                m_profileValues.m_specularColor = m_editorUtils.ColorField("SpecularColor", m_profileValues.m_specularColor, helpEnabled);
                m_profileValues.m_reflectionDistortion = m_editorUtils.Slider("ReflectionDistortion", m_profileValues.m_reflectionDistortion, 0f, 16f, helpEnabled);
                m_profileValues.m_reflectionStrength = m_editorUtils.Slider("ReflectionIntensity", m_profileValues.m_reflectionStrength, 0f, 1f, helpEnabled);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                m_editorUtils.Label("WaveSettings");
                EditorGUI.indentLevel++;
                m_profileValues.m_shorelineMovement = m_editorUtils.Slider("ShorelineMovement", m_profileValues.m_shorelineMovement, 0f, 1f, helpEnabled);
                m_profileValues.m_waveCount = m_editorUtils.FloatField("WaveCount", m_profileValues.m_waveCount, helpEnabled);
                m_profileValues.m_waveSpeed = m_editorUtils.FloatField("WaveSpeed", m_profileValues.m_waveSpeed, helpEnabled);
                m_profileValues.m_waveSize = m_editorUtils.FloatField("WaveSize", m_profileValues.m_waveSize, helpEnabled);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            else
            {
                m_gaiaOceanMat = GaiaWater.GetGaiaOceanMaterial();
                EditorGUILayout.HelpBox("Gaia Ocean Material was not found. The system will keep trying to search for 'Gaia Ocean.mat'", MessageType.Warning);
            }

            if (EditorGUI.EndChangeCheck())
            {
                updateSettingsOnly = true;
            }
        }

        private void CausticSettingsEnabled(bool helpEnabled)
        {
            m_profile.m_useCastics = m_editorUtils.ToggleLeft("UseCaustics", m_profile.m_useCastics, helpEnabled);
            if (m_profile.m_useCastics)
            {
                m_profile.m_mainCausticLight = (Light)m_editorUtils.ObjectField("MainCausticLight", m_profile.m_mainCausticLight, typeof(Light), true, helpEnabled, GUILayout.Height(16f));
                if (m_profile.m_mainCausticLight == null)
                {
                    m_profile.m_mainCausticLight = GaiaUtils.GetMainDirectionalLight();
                }
                m_profile.m_causticFramePerSecond = m_editorUtils.IntSlider("CausticFPS", m_profile.m_causticFramePerSecond, 15, 120, helpEnabled);
                m_profile.m_causticSize = m_editorUtils.Slider("CausticSize", m_profile.m_causticSize, 0.1f, 100f, helpEnabled);

                EditorGUILayout.HelpBox("Caustics setup is applied directly to the Directional Light using the cookie setup.", MessageType.Info);
            }
        }

        /// <summary>
        /// Adds a new user profile
        /// </summary>
        private void AddNewCustomProfile()
        {
            GaiaWaterProfileValues selectdValues = m_profileValues;
            GaiaWaterProfileValues newProfile = new GaiaWaterProfileValues();
            GaiaUtils.CopyFields(selectdValues, newProfile);
            int count = m_profile.m_waterProfiles.Count + 1;
            newProfile.m_typeOfWater = "New User Profile " + count;
            newProfile.m_userCustomProfile = true;
            m_profile.m_waterProfiles.Add(newProfile);

            m_profile.m_selectedWaterProfileValuesIndex = m_profile.m_waterProfiles.Count - 1;

            if (m_profile.m_selectedWaterProfileValuesIndex != -99)
            {
                GaiaWater.GetProfile(newProfileListIndex, m_gaiaOceanMat, m_profile, m_renderPipeline, true, true);
            }

            EditorUtility.SetDirty(m_profile);
        }

        /// <summary>
        /// Handy layer mask interface
        /// </summary>
        /// <param name="label"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        static LayerMask LayerMaskField(string label, EditorUtils editorUtils, LayerMask layerMask)
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

            label = editorUtils.GetContent(label).text;
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
        /// Gets and returns a directional light
        /// </summary>
        /// <returns></returns>
        private Light GetMainLight()
        {
            Light[] mainLight = FindObjectsOfType<Light>();
            if (mainLight != null)
            {
                foreach (Light light in mainLight)
                {
                    if (light.type == LightType.Directional)
                    {
                        return light;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets profile
        /// </summary>
        /// <returns></returns>
        private GaiaWaterProfileValues GetProfile(int index)
        {
            for (int i = 0; i < m_profile.m_waterProfiles.Count; i++)
            {
                if (i == index)
                {
                    return m_profile.m_waterProfiles[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Focuses the water material selected
        /// </summary>
        /// <param name="waterName"></param>
        private void FocusWaterMaterial(Object unityObject)
        {
            if (unityObject != null)
            {
                Selection.activeObject = unityObject;
            }
        }

        /// <summary>
        /// Focuses the underwater settings gameobject
        /// </summary>
        private void FocusUnderwaterSettings()
        {
            GameObject underwaterSettings = GameObject.Find("Underwater Effects");
            if (underwaterSettings != null)
            {
                Selection.activeObject = underwaterSettings;
            }
        }

        /// <summary>
        /// Generates the gradient texture then bakes it into a file
        /// </summary>
        /// <param name="m_waterProfileValues"></param>
        /// <param name="path"></param>
        private void GenerateGradientTexture(GaiaWaterProfileValues m_waterProfileValues, string path)
        {
            if (m_waterTexture == null || m_waterTexture.wrapMode != TextureWrapMode.Clamp)
            {
                m_waterTexture = new Texture2D(m_waterProfileValues.m_gradientTextureResolution, m_waterProfileValues.m_gradientTextureResolution);
                m_waterTexture.wrapMode = TextureWrapMode.Clamp;
            }
            else if (m_waterProfileValues.m_gradientTextureResolution != m_waterTexture.width)
            {
                m_waterTexture = new Texture2D(m_waterProfileValues.m_gradientTextureResolution, m_waterProfileValues.m_gradientTextureResolution);
                m_waterTexture.wrapMode = TextureWrapMode.Clamp;
            }
            if (m_waterTexture != null)
            {
                for (int x = 0; x < m_waterProfileValues.m_gradientTextureResolution; x++)
                {
                    for (int y = 0; y < m_waterProfileValues.m_gradientTextureResolution; y++)
                    {
                        Color color = m_waterProfileValues.m_waterGradient.Evaluate((float)x / (float)m_waterProfileValues.m_gradientTextureResolution);
                        m_waterTexture.SetPixel(x, y, color);
                    }
                }
                m_waterTexture.Apply();

                GenerateTexture(m_waterTexture, path);
            }
        }

        /// <summary>
        /// Generates a texture based of a Unity Gradient
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="path"></param>
        private void GenerateTexture(Texture2D texture, string path)
        {
            path = path.Replace(Application.dataPath, "Assets");

            var pngData = texture.EncodeToPNG();
            if (pngData != null)
            {
                File.WriteAllBytes(path, pngData);
            }

            EditorUtility.SetDirty(texture);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            m_profileValues.m_colorDepthRamp = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            m_profileValues.m_colorDepthRamp.wrapMode = TextureWrapMode.Clamp;

            m_waterTexture = null;
        }

        /// <summary>
        /// Creates a default gradient for water surface coloring
        /// </summary>
        /// <returns></returns>
        private Gradient CreateNewWaterSurfaceGradient(int profileIdx)
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[8];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

            if (profileIdx == 1)
            {
                colorKeys[0].color = GaiaUtils.GetColorFromHTML("E9E1D3");
                colorKeys[0].time = 0f;

                colorKeys[1].color = GaiaUtils.GetColorFromHTML("D9E0D0");
                colorKeys[1].time = 0.15f;

                colorKeys[2].color = GaiaUtils.GetColorFromHTML("B2DFD1");
                colorKeys[2].time = 0.3f;

                colorKeys[3].color = GaiaUtils.GetColorFromHTML("9ADFD4");
                colorKeys[3].time = 0.45f;

                colorKeys[4].color = GaiaUtils.GetColorFromHTML("7DBCB2");
                colorKeys[4].time = 0.6f;

                colorKeys[5].color = GaiaUtils.GetColorFromHTML("7BBCB1");
                colorKeys[5].time = 0.75f;

                colorKeys[6].color = GaiaUtils.GetColorFromHTML("629B91");
                colorKeys[6].time = 0.9f;

                colorKeys[7].color = GaiaUtils.GetColorFromHTML("5D988E");
                colorKeys[7].time = 1f;
            }
            else if (profileIdx == 2)
            {
                colorKeys[0].color = GaiaUtils.GetColorFromHTML("C9E9DA");
                colorKeys[0].time = 0f;

                colorKeys[1].color = GaiaUtils.GetColorFromHTML("D4D4D4");
                colorKeys[1].time = 0.15f;

                colorKeys[2].color = GaiaUtils.GetColorFromHTML("A4A4A4");
                colorKeys[2].time = 0.3f;

                colorKeys[3].color = GaiaUtils.GetColorFromHTML("909090");
                colorKeys[3].time = 0.45f;

                colorKeys[4].color = GaiaUtils.GetColorFromHTML("808080");
                colorKeys[4].time = 0.6f;

                colorKeys[5].color = GaiaUtils.GetColorFromHTML("636363");
                colorKeys[5].time = 0.75f;

                colorKeys[6].color = GaiaUtils.GetColorFromHTML("295571");
                colorKeys[6].time = 0.9f;

                colorKeys[7].color = GaiaUtils.GetColorFromHTML("142E48");
                colorKeys[7].time = 1f;
            }
            else if (profileIdx == 3)
            {
                colorKeys[0].color = GaiaUtils.GetColorFromHTML("C9E9DA");
                colorKeys[0].time = 0f;

                colorKeys[1].color = GaiaUtils.GetColorFromHTML("D4D4D4");
                colorKeys[1].time = 0.15f;

                colorKeys[2].color = GaiaUtils.GetColorFromHTML("A4A4A4");
                colorKeys[2].time = 0.3f;

                colorKeys[3].color = GaiaUtils.GetColorFromHTML("909090");
                colorKeys[3].time = 0.45f;

                colorKeys[4].color = GaiaUtils.GetColorFromHTML("808080");
                colorKeys[4].time = 0.6f;

                colorKeys[5].color = GaiaUtils.GetColorFromHTML("636363");
                colorKeys[5].time = 0.75f;

                colorKeys[6].color = GaiaUtils.GetColorFromHTML("296171");
                colorKeys[6].time = 0.9f;

                colorKeys[7].color = GaiaUtils.GetColorFromHTML("143F48");
                colorKeys[7].time = 1f;
            }
            else
            {
                colorKeys[0].color = GaiaUtils.GetColorFromHTML("FCFDFC");
                colorKeys[0].time = 0f;

                colorKeys[1].color = GaiaUtils.GetColorFromHTML("B5CAB9");
                colorKeys[1].time = 0.15f;

                colorKeys[2].color = GaiaUtils.GetColorFromHTML("739A7B");
                colorKeys[2].time = 0.3f;

                colorKeys[3].color = GaiaUtils.GetColorFromHTML("2D614A");
                colorKeys[3].time = 0.45f;

                colorKeys[4].color = GaiaUtils.GetColorFromHTML("24554A");
                colorKeys[4].time = 0.6f;

                colorKeys[5].color = GaiaUtils.GetColorFromHTML("1B434A");
                colorKeys[5].time = 0.75f;

                colorKeys[6].color = GaiaUtils.GetColorFromHTML("1A3B4A");
                colorKeys[6].time = 0.9f;

                colorKeys[7].color = GaiaUtils.GetColorFromHTML("17314A");
                colorKeys[7].time = 1f;
            }

            alphaKeys[0].alpha = 1f;
            alphaKeys[0].time = 0f;

            alphaKeys[1].alpha = 1f;
            alphaKeys[1].time = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }
    }
}