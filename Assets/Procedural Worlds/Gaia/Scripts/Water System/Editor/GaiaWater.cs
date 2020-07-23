using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ProcedualWorlds.WaterSystem.MeshGeneration;
using ProcedualWorlds.WaterSystem;
using System.IO;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace Gaia
{
    public static class GaiaWater
    {
        #region Variables

        //Water profiles
        public static List<GaiaWaterProfileValues> m_waterProfiles;
        public static GaiaWaterProfile m_waterProfile;

        //Water shader that is found
        private static string m_unityVersion;
        public static string m_waterShader = "PWS/SP/Water/Ocean vP2.1 2019_01_14";
        public static List<string> m_profileList = new List<string>();
        public static List<Material> m_allMaterials = new List<Material>();

        private const string m_builtInKeyWord = "SP";
        private const string m_lightweightKeyWord = "LW";
        private const string m_universalKeyWord = "UP";
        private const string m_highDefinitionKeyWord = "HD";

        //Parent Object Values
        public static GameObject m_parentObject;

        //Stores the gaia settings
        private static GaiaSettings m_gaiaSettings;

        //Stores the camera
        private static Camera m_camera;

        #endregion

        #region Setup

        public static void GetProfile(int selectedProfile, Material masterMaterial, GaiaWaterProfile waterProfile, GaiaConstants.EnvironmentRenderer renderPipeline, bool spawnWater, bool updateSettingsOnly)
        {
            m_waterProfile = waterProfile;
            if (m_waterProfile == null)
            {
                m_waterProfile = AssetDatabase.LoadAssetAtPath<GaiaWaterProfile>(GetAssetPath("Gaia Water System Profile"));
            }

            if (m_waterProfile == null)
            {
                Debug.LogError("[AmbientSkiesSamples.GetProfile()] Asset 'Gaia Water System Profile' could not be found please make sure it exists within your project or that the name has not been changed. Due to this error the method will now exit.");
                return;
            }
            else
            {
                if (selectedProfile == m_waterProfile.m_waterProfiles.Count)
                {
                    return;
                }

                UpdateGlobalWater(masterMaterial, waterProfile.m_waterProfiles[selectedProfile], renderPipeline, waterProfile, spawnWater, updateSettingsOnly);
            }
        }

        #endregion

        #region Apply Settings

        /// <summary>
        /// Updates the global lighting settings in your scene
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="renderPipeline"></param>
        private static void UpdateGlobalWater(Material masterMaterial, GaiaWaterProfileValues profile, GaiaConstants.EnvironmentRenderer renderPipeline, GaiaWaterProfile waterProfile, bool spawnWater, bool updateSettingsOnly)
        {
            GaiaUtils.GetGlobalSceneObject();

            //Spawns the water prefab in the scene
            if (spawnWater)
            {
                SpawnWater(m_waterProfile.m_waterPrefab);
            }

            if (updateSettingsOnly)
            {
                //Update the settings on the material
                SetWaterMainSettings(profile, masterMaterial, renderPipeline);

                //Sets the underwater effects in the scene
                SetUnderwaterEffects(m_waterProfile, profile);

            }
            else
            {
                //Water mesh generation
                UpdateWaterMeshQuality(m_waterProfile, m_waterProfile.m_waterPrefab, spawnWater);

                //Update the settings on the material
                SetWaterMainSettings(profile, masterMaterial, renderPipeline);

                //Sets the underwater effects in the scene
                SetUnderwaterEffects(m_waterProfile, profile);
            }

            //Sets the waters reflection settings
            SetupWaterReflectionSettings(m_waterProfile, profile, true);

            Material underwaterMaterial = GaiaUtils.GetWaterMaterial(GaiaConstants.waterSurfaceObject, true);
            UpdateWaterMaterialInstances(masterMaterial, underwaterMaterial);

            //Normal Camera Setup
            SetCameraNormal();

            //Mark water as dirty
            MarkWaterMaterialDirty(m_waterProfile.m_activeWaterMaterial);

            if (GaiaGlobal.Instance != null)
            {
                if (m_waterProfile.m_multiSceneLightingSupport)
                {
                    GaiaGlobal.Instance.ApplySetting(m_waterProfile.m_waterProfiles[m_waterProfile.m_selectedWaterProfileValuesIndex], m_waterProfile);
                    GaiaGlobal.Instance.m_enableSettingSaving = true;
                    GaiaGlobal.Instance.OnEnable();
                }
                else
                {
                    GaiaGlobal.Instance.m_enableSettingSaving = false;
                    GaiaGlobal.Instance.OnEnable();
                }
            }

            //Destroys the parent object if it contains no partent childs
            DestroyParent("Ambient Skies Samples Environment");
        }

        /// <summary>
        /// Sets the waters main settings
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="waterMaterial"></param>
        private static void SetWaterMainSettings(GaiaWaterProfileValues profile, Material waterMaterial, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            //Tiling
            waterMaterial.SetFloat("_FoamTexTile", profile.m_foamTiling);
            waterMaterial.SetFloat("_NormalTile", profile.m_waterTiling);
            //Color
            waterMaterial.SetTexture("_WaterDepthRamp", profile.m_colorDepthRamp);
            waterMaterial.SetFloat("_TransparentDepth", profile.m_transparentDistance);
            //Normal
            waterMaterial.SetTexture("_NormalLayer0", profile.m_normalLayer0);
            waterMaterial.SetFloat("_NormalLayer0Scale", profile.m_normalStrength0);
            waterMaterial.SetTexture("_NormalLayer1", profile.m_normalLayer1);
            waterMaterial.SetFloat("_NormalLayer1Scale", profile.m_normalStrength1);
            waterMaterial.SetTexture("_NormalLayer2", profile.m_fadeNormal);
            waterMaterial.SetFloat("_NormalLayer2Scale", profile.m_fadeNormalStrength);
            //Foam
            waterMaterial.SetTexture("_FoamTex", profile.m_foamTexture);
            waterMaterial.SetTexture("_FoamRampAlpha", profile.m_foamAlphaRamp);
            waterMaterial.SetFloat("_FoamDepth", profile.m_foamDistance);
            //Reflection
            waterMaterial.SetFloat("_ReflectionDistortion", profile.m_reflectionDistortion);
            waterMaterial.SetFloat("_ReflectionStrength", profile.m_reflectionStrength);
            //Wave
            waterMaterial.SetFloat("_WaveShoreMove", profile.m_shorelineMovement);
            waterMaterial.SetFloat("WaveLength", profile.m_waveCount);
            waterMaterial.SetFloat("_WaveSpeed", profile.m_waveSpeed);
            waterMaterial.SetFloat("_WaveSteepness", profile.m_waveSize);
            //Lighting
            waterMaterial.SetColor("_MainLightSpecular", profile.m_specularColor);
            waterMaterial.SetFloat("_Metallic", profile.m_metallic);
            waterMaterial.SetFloat("_Smoothness", profile.m_smoothness);
            //Fade
            waterMaterial.SetFloat("_NormalFadeStart", profile.m_fadeStart);
            waterMaterial.SetFloat("_NormalFadeDistance", profile.m_fadeDistance);
        }

        /// <summary>
        /// Sets up and adds the camera normal pass
        /// </summary>
        private static void SetCameraNormal()
        {
            if (m_camera == null)
            {
                m_camera = GaiaUtils.GetCamera();
            }

            if (m_camera != null)
            {
                /*
                CameraNormals normals = camera.gameObject.GetComponent<CameraNormals>();
                if (normals == null)
                {
                    normals = camera.gameObject.AddComponent<CameraNormals>();
                }
                normals.normalsShader = Shader.Find("Unlit/PW_Water_CameraNormals");
                */

                PW_Builtin_Refraction camera_Refraction = m_camera.gameObject.GetComponent<PW_Builtin_Refraction>();
                if (camera_Refraction == null)
                {
                    camera_Refraction = m_camera.gameObject.AddComponent<PW_Builtin_Refraction>();
                    camera_Refraction.renderSize = PW_Builtin_Refraction.PW_RENDER_SIZE.HALF;
                    camera_Refraction.PreRender(m_camera);
                }
            }
        }

        /// <summary>
        /// Sets the underwater effects
        /// </summary>
        /// <param name="profile"></param>
        private static void SetUnderwaterEffects(GaiaWaterProfile profile, GaiaWaterProfileValues profileValues)
        {
            if (Application.isPlaying)
            {
                return;
            }
            if (profile.m_supportUnderwaterEffects)
            {
                float seaLevel = 0f;
                GaiaSceneInfo sceneInfo = GaiaSceneInfo.GetSceneInfo();
                if (sceneInfo != null)
                {
                    seaLevel = sceneInfo.m_seaLevel;
                }
                if (m_parentObject == null)
                {
                    m_parentObject = GetOrCreateParentObject(GaiaConstants.gaiaWaterObject, true);
                }
                GameObject underwaterEffectsObject = GameObject.Find("Underwater Effects");
                if (underwaterEffectsObject == null)
                {
                    underwaterEffectsObject = new GameObject("Underwater Effects");
                }

                GaiaUnderwaterEffects underwaterEffects = underwaterEffectsObject.GetComponent<GaiaUnderwaterEffects>();
                if (underwaterEffects == null)
                {
                    underwaterEffects = underwaterEffectsObject.AddComponent<GaiaUnderwaterEffects>();
                    underwaterEffects.LoadUnderwaterSystemAssets();

                    GameObject underwaterParticles = GameObject.Find(profile.m_underwaterParticles.name);
                    FollowPlayerSystem followPlayer = null;
                    if (profile.m_supportUnderwaterParticles)
                    {
                        if (underwaterParticles == null)
                        {
                            underwaterParticles = PrefabUtility.InstantiatePrefab(profile.m_underwaterParticles) as GameObject;
                            followPlayer = underwaterParticles.GetComponent<FollowPlayerSystem>();
                            if (followPlayer == null)
                            {
                                followPlayer = underwaterParticles.AddComponent<FollowPlayerSystem>();
                            }
                            followPlayer.m_player = underwaterEffects.m_playerCamera;
                            followPlayer.m_particleObjects.Add(underwaterParticles);
                            underwaterEffects.m_underwaterParticles = underwaterParticles;
                            underwaterParticles.transform.position = underwaterEffects.m_playerCamera.transform.position;
                        }
                        else
                        {
                            underwaterParticles.transform.position = underwaterEffects.m_playerCamera.transform.position;
                        }

                        followPlayer = underwaterParticles.GetComponent<FollowPlayerSystem>();
                        if (followPlayer != null)
                        {
                            followPlayer.m_player = underwaterEffects.m_playerCamera;
                        }

                        underwaterParticles.transform.SetParent(underwaterEffectsObject.transform);
                    }
                    else
                    {
                        if (underwaterParticles != null)
                        {
                            Object.DestroyImmediate(underwaterParticles);
                        }
                    }

                    underwaterEffectsObject.transform.SetParent(m_parentObject.transform);
                }

                if (underwaterEffects != null)
                {
                    underwaterEffects.LoadUnderwaterSystemAssets();
                    underwaterEffects.m_framesPerSecond = profile.m_causticFramePerSecond;
                    underwaterEffects.m_causticSize = profile.m_causticSize;
                    underwaterEffects.m_useCaustics = profile.m_useCastics;
                    underwaterEffects.m_mainLight = profile.m_mainCausticLight;
                    underwaterEffects.m_seaLevel = seaLevel;
                    underwaterEffects.m_fogColorGradient = profileValues.m_underwaterFogGradient;
                    underwaterEffects.m_fogDistance = profileValues.m_underwaterFogDistance;
                    underwaterEffects.m_fogDensity = profileValues.m_underwaterFogDensity;
                    underwaterEffects.m_nearFogDistance = profileValues.m_underwaterNearFogDistance;
                    underwaterEffects.m_constUnderwaterPostExposure = profileValues.m_constUnderwaterPostExposure;
                    underwaterEffects.m_constUnderwaterColorFilter = profileValues.m_constUnderwaterColorFilter;
                    underwaterEffects.m_gradientUnderwaterPostExposure = profileValues.m_gradientUnderwaterPostExposure;
                    underwaterEffects.m_gradientUnderwaterColorFilter = profileValues.m_gradientUnderwaterColorFilter;
                    if (underwaterEffects.m_waterReflections == null)
                    {
                        underwaterEffects.m_waterReflections = Object.FindObjectOfType<PWS_WaterSystem>();
                    }

                    FollowPlayerSystem followPlayer = null;
                    GameObject underwaterParticles = GameObject.Find(profile.m_underwaterParticles.name);
                    if (profile.m_supportUnderwaterParticles)
                    {
                        if (underwaterParticles == null)
                        {
                            underwaterParticles = PrefabUtility.InstantiatePrefab(profile.m_underwaterParticles) as GameObject;
                            followPlayer = underwaterParticles.GetComponent<FollowPlayerSystem>();
                            if (followPlayer == null)
                            {
                                followPlayer = underwaterParticles.AddComponent<FollowPlayerSystem>();
                            }
                            followPlayer.m_player = underwaterEffects.m_playerCamera;
                            underwaterParticles.transform.position = underwaterEffects.m_playerCamera.transform.position;
                        }
                        else
                        {
                            followPlayer = underwaterParticles.GetComponent<FollowPlayerSystem>();
                            if (followPlayer != null)
                            {
                                followPlayer.m_player = underwaterEffects.m_playerCamera;
                            }

                            underwaterParticles.transform.position = underwaterEffects.m_playerCamera.transform.position;
                        }

                        underwaterParticles.transform.SetParent(underwaterEffectsObject.transform);
                    }
                    else
                    {
                        if (underwaterParticles != null)
                        {
                            Object.DestroyImmediate(underwaterParticles);
                        }
                    }

                    underwaterEffectsObject.transform.SetParent(m_parentObject.transform);
                }

                if (profile.m_supportUnderwaterFog)
                {
                    GameObject underwaterHorizon = GameObject.Find(profile.m_underwaterHorizonPrefab.name);
                    if (underwaterHorizon == null)
                    {
                        underwaterHorizon = PrefabUtility.InstantiatePrefab(profile.m_underwaterHorizonPrefab) as GameObject;
                        underwaterEffects.m_horizonObject = underwaterHorizon;
                        if (underwaterHorizon != null)
                        {
                            FollowPlayerSystem followPlayer = underwaterHorizon.GetComponent<FollowPlayerSystem>();
                            if (followPlayer == null)
                            {
                                followPlayer = underwaterHorizon.AddComponent<FollowPlayerSystem>();
                            }

                            followPlayer.m_followPlayer = true;
                            followPlayer.m_isWaterObject = true;
                            followPlayer.m_particleObjects.Add(underwaterHorizon);
                            followPlayer.m_player = underwaterEffects.m_playerCamera;
                            followPlayer.m_useOffset = true;
                            followPlayer.m_xoffset = 250f;
                            followPlayer.m_zoffset = 0f;
                            followPlayer.m_yOffset = 580f;
                            underwaterHorizon.transform.SetParent(underwaterEffectsObject.transform);
                            underwaterHorizon.transform.position = new Vector3(0f, -4000f, 0f);
                        }
                    }
                }
                else
                {
                    GameObject underwaterHorizon = GameObject.Find(profile.m_underwaterHorizonPrefab.name);
                    if (underwaterHorizon != null)
                    {
                        Object.DestroyImmediate(underwaterHorizon);
                    }
                }

                #if UNITY_POST_PROCESSING_STACK_V2
                if (profile.m_supportUnderwaterPostProcessing)
                {
                    GameObject postProcessObject = underwaterEffects.m_underwaterPostFX;
                    if (underwaterEffects.m_underwaterPostFX != null)
                    {
                        underwaterEffects.m_underwaterPostFX.SetActive(true);
                    }
                    if (postProcessObject == null)
                    {
                        postProcessObject = new GameObject("Underwater Post Processing");
                        postProcessObject.transform.SetParent(underwaterEffectsObject.transform);
                        postProcessObject.transform.position = new Vector3(0f, -3500f + seaLevel, 0f);
                        postProcessObject.layer = LayerMask.NameToLayer("TransparentFX");

                        PostProcessVolume postProcessVolume = postProcessObject.AddComponent<PostProcessVolume>();
                        postProcessVolume.sharedProfile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(GetAssetPath(profileValues.m_postProcessingProfile));
                        postProcessVolume.priority = 3f;

                        BoxCollider boxCollider = postProcessObject.AddComponent<BoxCollider>();
                        boxCollider.isTrigger = true;
                        boxCollider.size = new Vector3(10000f, 7000f, 10000f);
                    }
                    else
                    {
                        postProcessObject.transform.SetParent(underwaterEffectsObject.transform);
                        postProcessObject.transform.position = new Vector3(0f, -3500f + seaLevel, 0f);
                        postProcessObject.layer = LayerMask.NameToLayer("TransparentFX");

                        PostProcessVolume postProcessVolume = postProcessObject.GetComponent<PostProcessVolume>();
                        if (postProcessVolume != null)
                        {
                            postProcessVolume.sharedProfile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(GetAssetPath(profileValues.m_postProcessingProfile));
                            postProcessVolume.priority = 3f;
                        }

                        BoxCollider boxCollider = postProcessObject.GetComponent<BoxCollider>();
                        if (boxCollider != null)
                        {
                            boxCollider.isTrigger = true;
                            boxCollider.size = new Vector3(10000f, 7000f, 10000f);
                        }
                    }

                    underwaterEffects.m_underwaterPostFX = postProcessObject;
                    if (underwaterEffects.m_underwaterPostFX != null)
                    {
                        underwaterEffects.m_underwaterPostFX.SetActive(false);
                    }
                }
                else
                {
                    GameObject postProcessObject = GameObject.Find("Underwater Post Processing");
                    if (postProcessObject != null)
                    {
                        Object.DestroyImmediate(postProcessObject);
                    }
                }
                #endif
            }
            else
            {
                GameObject underwaterEffects = GameObject.Find("Underwater Effects");
                if (underwaterEffects != null)
                {
                    Object.DestroyImmediate(underwaterEffects);
                }
            }
        }

        /// <summary>
        /// Sets up the auto wind setup on the water
        /// </summary>
        /// <param name="profile"></param>
        private static void SetAutoWind(GaiaWaterProfile profile)
        {
            GameObject waterObject = GameObject.Find(GaiaConstants.waterSurfaceObject);
            if (waterObject != null)
            {
                /*
                GaiaWaterWind waterWind = waterObject.GetComponent<GaiaWaterWind>();
                if (profile.m_autoWindControlOnWater)
                {
                    if (waterWind == null)
                    {
                        waterWind = waterObject.AddComponent<GaiaWaterWind>();
                        if (waterWind.m_waterMaterial == null)
                        {
                            waterWind.m_waterMaterial = waterObject.GetComponent<Renderer>().sharedMaterial;
                        }
                        if (waterWind.m_windZone == null)
                        {
                            waterWind.m_windZone = Object.FindObjectOfType<WindZone>();
                        }
                    }
                }
                else
                {
                    if (waterWind != null)
                    {
                        Object.DestroyImmediate(waterWind);
                    }
                }
                */
            }
        }

        /// <summary>
        /// Sets the water mesh quality
        /// </summary>
        /// <param name="profile"></param>
        public static void UpdateWaterMeshQuality(GaiaWaterProfile profile, GameObject waterObject, bool isNewSpawn = false)
        {
            if (profile.m_enableWaterMeshQuality)
            {
                GameObject waterGameObject = GameObject.Find(waterObject.name);
                if (waterGameObject == null)
                {
                    Debug.LogWarning("Water has not been added to the scene. Please add it to the scene then try configure the water mesh quality.");
                }
                else
                {
                    bool regenerate = isNewSpawn;
                    PWS_MeshGenerationPro waterGeneration = waterGameObject.GetComponent<PWS_MeshGenerationPro>();
                    if (waterGeneration == null)
                    {
                        waterGeneration = waterGameObject.AddComponent<PWS_MeshGenerationPro>();
                        waterGeneration.m_MeshType = profile.m_meshType;
                        if (waterGeneration.m_Size.x != profile.m_xSize || waterGeneration.m_Size.z != profile.m_zSize)
                        {
                            regenerate = true;
                        }

                        if (waterGeneration.m_meshDensity.x != profile.m_customMeshQuality || waterGeneration.m_meshDensity.y != profile.m_customMeshQuality)
                        {
                            regenerate = true;
                        }
                        
                        waterGeneration.m_Size.x = profile.m_xSize;
                        waterGeneration.m_Size.z = profile.m_zSize;

                        switch (profile.m_waterMeshQuality)
                        {
                            case GaiaConstants.WaterMeshQuality.VeryLow:
                                waterGeneration.m_meshDensity.x = 2;
                                waterGeneration.m_meshDensity.y = 2;
                                break;
                            case GaiaConstants.WaterMeshQuality.Low:
                                waterGeneration.m_meshDensity.x = 4;
                                waterGeneration.m_meshDensity.y = 4;
                                break;
                            case GaiaConstants.WaterMeshQuality.Medium:
                                waterGeneration.m_meshDensity.x = 6;
                                waterGeneration.m_meshDensity.y = 6;
                                break;
                            case GaiaConstants.WaterMeshQuality.High:
                                waterGeneration.m_meshDensity.x = 8;
                                waterGeneration.m_meshDensity.y = 8;
                                break;
                            case GaiaConstants.WaterMeshQuality.VeryHigh:
                                waterGeneration.m_meshDensity.x = 10;
                                waterGeneration.m_meshDensity.y = 10;

                                break;
                            case GaiaConstants.WaterMeshQuality.Ultra:
                                waterGeneration.m_meshDensity.x = 12;
                                waterGeneration.m_meshDensity.y = 12;

                                break;
                            case GaiaConstants.WaterMeshQuality.Cinematic:
                                waterGeneration.m_meshDensity.x = 14;
                                waterGeneration.m_meshDensity.y = 14;

                                break;
                            case GaiaConstants.WaterMeshQuality.Custom:
                                waterGeneration.m_meshDensity.x = profile.m_customMeshQuality;
                                waterGeneration.m_meshDensity.y = profile.m_customMeshQuality;
                                break;
                        }
                    }
                    else
                    {
                        waterGeneration.m_MeshType = profile.m_meshType;
                        waterGeneration.m_Size.x = profile.m_xSize;
                        waterGeneration.m_Size.z = profile.m_zSize;

                        switch (profile.m_waterMeshQuality)
                        {
                            case GaiaConstants.WaterMeshQuality.VeryLow:
                                waterGeneration.m_meshDensity.x = 2;
                                waterGeneration.m_meshDensity.y = 2;
                                break;
                            case GaiaConstants.WaterMeshQuality.Low:
                                waterGeneration.m_meshDensity.x = 4;
                                waterGeneration.m_meshDensity.y = 4;
                                break;
                            case GaiaConstants.WaterMeshQuality.Medium:
                                waterGeneration.m_meshDensity.x = 6;
                                waterGeneration.m_meshDensity.y = 6;
                                break;
                            case GaiaConstants.WaterMeshQuality.High:
                                waterGeneration.m_meshDensity.x = 8;
                                waterGeneration.m_meshDensity.y = 8;
                                break;
                            case GaiaConstants.WaterMeshQuality.VeryHigh:
                                waterGeneration.m_meshDensity.x = 10;
                                waterGeneration.m_meshDensity.y = 10;

                                break;
                            case GaiaConstants.WaterMeshQuality.Ultra:
                                waterGeneration.m_meshDensity.x = 12;
                                waterGeneration.m_meshDensity.y = 12;

                                break;
                            case GaiaConstants.WaterMeshQuality.Cinematic:
                                waterGeneration.m_meshDensity.x = 14;
                                waterGeneration.m_meshDensity.y = 14;

                                break;
                            case GaiaConstants.WaterMeshQuality.Custom:
                                waterGeneration.m_meshDensity.x = profile.m_customMeshQuality;
                                waterGeneration.m_meshDensity.y = profile.m_customMeshQuality;
                                break;
                        }
                    }

                    if (regenerate)
                    {
                        waterGeneration.ProceduralMeshGeneration();
                    }
                }
            }
            else
            {
                GameObject waterGameObject = GameObject.Find(profile.m_waterPrefab.name);
                if (waterGameObject == null)
                {
                    Debug.LogWarning("Water has not been added to the scene. Please add it to the scene then try configure the water mesh quality.");
                }
                else
                {
                    PWS_MeshGenerationPro waterGeneration = waterGameObject.GetComponent<PWS_MeshGenerationPro>();
                    if (waterGeneration != null)
                    {
                        Object.DestroyImmediate(waterGeneration);
                    }

                    MeshFilter meshFilter = waterGameObject.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        if (meshFilter.sharedMesh.name != profile.m_waterPrefab.name)
                        {
                            meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(GetAssetPath(profile.m_waterPrefab.name));
                        }
                    }
                }
            }
        }

        #endregion

        #region Utils

        /// <summary>
        /// Sets water reflections up in the scene
        /// </summary>
        /// <param name="reflectionOn"></param>
        public static void SetWaterReflectionsType(bool reflectionOn, GaiaConstants.EnvironmentRenderer renderPipeline, GaiaWaterProfile profile, GaiaWaterProfileValues waterProfileValues)
        {
            if (m_waterProfile == null)
            {
                m_waterProfile = AssetDatabase.LoadAssetAtPath<GaiaWaterProfile>(GetAssetPath("Gaia Water System Profile"));
            }

            GameObject waterObject = GameObject.Find(profile.m_waterPrefab.name);

            if (renderPipeline != GaiaConstants.EnvironmentRenderer.BuiltIn)
            {
                if (waterObject != null)
                {
                    PWS_WaterSystem reflection = waterObject.GetComponent<PWS_WaterSystem>();
                    if (reflection != null)
                    {
                        Object.DestroyImmediate(reflection);
                    }
                }

                if (renderPipeline == GaiaConstants.EnvironmentRenderer.Lightweight)
                {
                    LWRPWaterProbe(true);
                }
                else
                {
                    HDRPPlanarReflections(profile);
                }

                return;
            }

            if (CheckWaterMaterialAndShader(m_waterProfile.m_activeWaterMaterial))
            {
                m_waterProfile.m_enableReflections = reflectionOn;

                if (reflectionOn)
                {
                    if (waterObject != null)
                    {
                        PWS_WaterSystem reflection = waterObject.GetComponent<PWS_WaterSystem>();
                        if (reflection == null)
                        {
                            reflection = waterObject.AddComponent<PWS_WaterSystem>();
                        }
                    }
                }
                else
                {
                    if (waterObject != null)
                    {
                        PWS_WaterSystem reflection = waterObject.GetComponent<PWS_WaterSystem>();
                        if (reflection == null)
                        {
                            reflection = waterObject.AddComponent<PWS_WaterSystem>();
                            
                        }
                    }
                }

                SetupWaterReflectionSettings(m_waterProfile, waterProfileValues, false);
            }
            else
            {
                Debug.LogError("[GaiaProWater.SetWaterReflections()] Shader of the material does not = " + m_waterShader + " Or master water material in the water profile is empty");
            }
        }

        /// <summary>
        /// Get the asset path of the first thing that matches the name
        /// </summary>
        /// <param name="name">Name to search for</param>
        /// <returns>The path or null</returns>
        private static string GetAssetPath(string name)
        {
            string[] assets = AssetDatabase.FindAssets(name, null);
            if (assets.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(assets[0]);
            }
            return null;
        }

        /// <summary>
        /// Reflections for LWRP
        /// </summary>
        /// <param name="enabled"></param>
        private static void LWRPWaterProbe(bool enabled)
        {
            GameObject reflectionProbe = GameObject.Find("LWRP Water Reflection Probe");
            if (enabled)
            {
                if (m_parentObject == null)
                {
                    m_parentObject = GetOrCreateParentObject(GaiaConstants.gaiaWaterObject, true);
                }

                GaiaSceneInfo sceneInfo = GaiaSceneInfo.GetSceneInfo();
                if (reflectionProbe == null)
                {
                    reflectionProbe = new GameObject("LWRP Water Reflection Probe");
                }

                reflectionProbe.transform.SetParent(m_parentObject.transform);

                if (sceneInfo != null)
                {
                    reflectionProbe.transform.position = new Vector3(0f, sceneInfo.m_seaLevel + 0.5f, 0f);
                }
                else
                {
                    reflectionProbe.transform.position = new Vector3(0f, 0.5f, 0f);
                }

                ReflectionProbe probe = reflectionProbe.GetComponent<ReflectionProbe>();
                if (probe == null)
                {
                    probe = reflectionProbe.AddComponent<ReflectionProbe>();
                }

                probe.cullingMask = 0;
                probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
                probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
                probe.resolution = 512;
            }
            else
            {
                if (reflectionProbe != null)
                {
                    Object.DestroyImmediate(reflectionProbe);
                }
            }
        }

        /// <summary>
        /// Reflections for HDRP
        /// </summary>
        /// <param name="profile"></param>
        private static void HDRPPlanarReflections(GaiaWaterProfile profile)
        {
#if HDPipeline
            GameObject planarObject = GameObject.Find("HD Planar Water Reflections");
            if (profile.m_enableReflections)
            {
                if (m_parentObject == null)
                {
                    m_parentObject = GetOrCreateParentObject(GaiaConstants.gaiaWaterObject, true);
                }

                GaiaSceneInfo sceneInfo = GaiaSceneInfo.GetSceneInfo();
                if (planarObject == null)
                {
                    GameObject planarPrefab = profile.m_hdPlanarReflections;
                    if (planarPrefab != null)
                    {
                        planarObject = PrefabUtility.InstantiatePrefab(planarPrefab) as GameObject;
                    }
                    else
                    {
                        Debug.LogError("Missing 'HD Planar Water Reflections' Prefab in your project please make sure it exists");
                    }
                }

                Vector3 objectLocation = planarObject.transform.position;
                if (sceneInfo != null)
                {
                    objectLocation.y = sceneInfo.m_seaLevel + 1f;
                }

                planarObject.transform.position = objectLocation;
                planarObject.transform.SetParent(m_parentObject.transform);

                UnityEngine.Rendering.HighDefinition.PlanarReflectionProbe reflections = planarObject.GetComponent<UnityEngine.Rendering.HighDefinition.PlanarReflectionProbe>();
                if (reflections == null)
                {
                    reflections = planarObject.AddComponent<UnityEngine.Rendering.HighDefinition.PlanarReflectionProbe>();
                }

                reflections.RequestRenderNextUpdate();
            }
            else
            {
                if (planarObject != null)
                {
                    Object.DestroyImmediate(planarObject);
                }
            }
#endif
        }

        /// <summary>
        /// Get or create a parent object
        /// </summary>
        /// <param name="parentGameObject"></param>
        /// <param name="parentToGaia"></param>
        /// <returns>Parent Object</returns>
        private static GameObject GetOrCreateParentObject(string parentGameObject, bool parentToGaia)
        {
            //Get the parent object
            GameObject theParentGo = GameObject.Find(parentGameObject);

            if (theParentGo == null)
            {
                theParentGo = GameObject.Find(GaiaConstants.gaiaWaterObject);

                if (theParentGo == null)
                {
                    theParentGo = new GameObject(GaiaConstants.gaiaWaterObject);
                }
            }

            if (parentToGaia)
            {
                GameObject gaiaParent = GaiaUtils.GetGlobalSceneObject();
                if (gaiaParent != null)
                {
                    theParentGo.transform.SetParent(gaiaParent.transform);
                }
            }

            return theParentGo;
        }

        /// <summary>
        /// Checks if the water object exists
        /// </summary>
        /// <returns></returns>
        public static bool DoesWaterExist()
        {
            return GameObject.Find(GaiaConstants.gaiaWaterObject) != null;
        }

        /// <summary>
        /// Find parent object and destroys it if it's empty
        /// </summary>
        /// <param name="parentGameObject"></param>
        private static void DestroyParent(string parentGameObject)
        {
            //If string isn't empty
            if (!string.IsNullOrEmpty(parentGameObject))
            {
                //If string doesn't = Ambient Skies Environment
                if (parentGameObject != "Ambient Skies Samples Environment")
                {
                    //Sets the paramater to Ambient Skies Environment
                    parentGameObject = "Ambient Skies Samples Environment";
                }

                //Find parent object
                GameObject parentObject = GameObject.Find(parentGameObject);
                if (parentObject != null)
                {
                    //Find parents in parent object
                    Transform[] parentChilds = parentObject.GetComponentsInChildren<Transform>();
                    if (parentChilds.Length == 1)
                    {
                        //Destroy object if object is empty
                        Object.DestroyImmediate(parentObject);
                    }
                }
            }
        }

        /// <summary>
        /// Mark the water material as dirty to be saved
        /// </summary>
        /// <param name="waterMaterial"></param>
        private static void MarkWaterMaterialDirty(Material waterMaterial)
        {
            if (waterMaterial != null)
            {
                EditorUtility.SetDirty(waterMaterial);

                GaiaEditorUtils.MarkSceneDirty();
            }
        }

        #endregion

        #region Utils Pro

        /// <summary>
        /// Checks to see if the shader is good to begin applying settings
        /// </summary>
        /// <param name="waterMaterial"></param>
        /// <returns></returns>
        private static bool CheckWaterMaterialAndShader(Material waterMaterial)
        {
            bool shaderGood = false;
            if (waterMaterial == null)
            {
                shaderGood = false;
                return shaderGood;
            }
            if (waterMaterial.shader == Shader.Find(m_waterShader))
            {
                shaderGood = true;
            }

            shaderGood = true;

            return shaderGood;
        }

        /// <summary>
        /// Spawns the water prefab
        /// </summary>
        /// <param name="waterPrefab"></param>
        private static void SpawnWater(GameObject waterPrefab)
        {
            float seaLevel = 0f;
            GaiaSceneInfo sceneInfo = GaiaSceneInfo.GetSceneInfo();
            bool gaiaSeaLevelExists = false;
            if (sceneInfo != null)
            {
                seaLevel = sceneInfo.m_seaLevel;
                gaiaSeaLevelExists = true;
            }

            if (m_parentObject == null)
            {
                m_parentObject = GetOrCreateParentObject(GaiaConstants.gaiaWaterObject, true);
            }

            if (waterPrefab == null)
            {
                Debug.LogError("[GaiaProWater.SpawnWater()] Water prefab is empty please make sure a prefab is present that you want to spawn");
            }
            else
            {
                RemoveOldWater();

                float waterLocationXZ = 0f;

                GameObject waterObject = GameObject.Find(waterPrefab.name);
                if (waterObject == null)
                {
                    waterObject = PrefabUtility.InstantiatePrefab(waterPrefab) as GameObject;
                    waterObject.transform.SetParent(m_parentObject.transform);
                    waterObject.transform.position = new Vector3(waterLocationXZ, seaLevel, waterLocationXZ);
                }
                else
                {
                    if (!gaiaSeaLevelExists)
                    {
                        seaLevel = waterObject.transform.position.y;
                    }
                    waterObject.transform.SetParent(m_parentObject.transform);
                    waterObject.transform.position = new Vector3(waterLocationXZ, seaLevel, waterLocationXZ);
                }
            }

            foreach (Stamper stamper in Resources.FindObjectsOfTypeAll<Stamper>())
            {
                stamper.m_showSeaLevelPlane = false;
            }

            foreach (Spawner spawner in Resources.FindObjectsOfTypeAll<Spawner>())
            {
                spawner.m_showSeaLevelPlane = false;
            }
        }

        public static void UpdateWaterSeaLevel(GameObject waterPrefab, float seaLevel)
        {
            if (m_parentObject == null)
            {
                m_parentObject = GetOrCreateParentObject(GaiaConstants.gaiaWaterObject, true);
            }

            float waterLocationXZ = 0f;
            GameObject waterObject = GameObject.Find(waterPrefab.name);
            if (waterObject != null)
            {
                waterObject.transform.SetParent(m_parentObject.transform);
                waterObject.transform.position = new Vector3(waterLocationXZ, seaLevel, waterLocationXZ);
            }

            GameObject postProcessVolumeObject = GameObject.Find("Underwater Post Processing FX");
            if (postProcessVolumeObject != null)
            {
                postProcessVolumeObject.transform.SetParent(m_parentObject.transform);
                postProcessVolumeObject.transform.position = new Vector3(0f, seaLevel + 4.9f, 0f);
            }

            GaiaUnderwaterEffects underwaterEffects = GameObject.FindObjectOfType<GaiaUnderwaterEffects>();
            if (underwaterEffects != null)
            {
                underwaterEffects.m_seaLevel = seaLevel;
            }
        }

        /// <summary>
        /// Removes old water prefab from the scene
        /// </summary>
        private static void RemoveOldWater()
        {
            GameObject oldUnderwaterFX = GameObject.Find("Ambient Water Samples");
            if (oldUnderwaterFX != null)
            {
                Object.DestroyImmediate(oldUnderwaterFX);
            }
        }

        /// <summary>
        /// Removes systems from scene
        /// </summary>
        public static void RemoveSystems()
        {
            GameObject sampleContent = GameObject.Find(GaiaConstants.gaiaWaterObject);
            if (sampleContent != null)
            {
                Object.DestroyImmediate(sampleContent);
            }
        }

        /// <summary>
        /// Configures the water reflection settings
        /// </summary>
        /// <param name="profile"></param>
        public static void SetupWaterReflectionSettings(GaiaWaterProfile profile, GaiaWaterProfileValues waterProfileValues, bool forceUpdate)
        {
            if (m_camera == null)
            {
                m_camera = GaiaUtils.GetCamera();
            }

            PWS_WaterSystem[] reflections = Object.FindObjectsOfType<PWS_WaterSystem>();
            if (reflections != null)
            {
                foreach(PWS_WaterSystem reflection in reflections)
                {
                    reflection.m_disablePixelLights = profile.m_disablePixelLights;
                    reflection.m_clipPlaneOffset = profile.m_clipPlaneOffset;
                    reflection.m_reflectionLayers = profile.m_reflectedLayers;
                    reflection.m_HDR = profile.m_useHDR;
                    reflection.m_enableDisabeHeightFeature = profile.m_enableDisabeHeightFeature;
                    reflection.m_disableHeight = profile.m_disableHeight;
                    reflection.m_MSAA = profile.m_allowMSAA;
                    reflection.m_RenderUpdate = profile.m_waterRenderUpdateMode;
                    reflection.m_updateThreshold = profile.m_interval;
                    reflection.m_customReflectionDistance = profile.m_useCustomRenderDistance;
                    reflection.m_renderDistance = profile.m_customRenderDistance;
                    reflection.m_customReflectionDistances = profile.m_enableLayerDistances;
                    reflection.m_specColor = waterProfileValues.m_specularColor;
                    if (profile.m_enableLayerDistances)
                    {
                        reflection.m_distances = profile.m_customRenderDistances;
                    }
                    if (profile.m_enableReflections)
                    {
                        reflection.m_skyboxOnly = false;
                    }
                    else
                    {
                        reflection.m_skyboxOnly = true;
                    }

                    switch (profile.m_reflectionResolution)
                    {
                        case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8:
                            reflection.m_renderTextureSize = 8;
                            break;
                        case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution16:
                            reflection.m_renderTextureSize = 16;
                            break;
                        case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution32:
                            reflection.m_renderTextureSize = 32;
                            break;
                        case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution64:
                            reflection.m_renderTextureSize = 64;
                            break;
                        case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution128:
                            reflection.m_renderTextureSize = 128;
                            break;
                        case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution256:
                            reflection.m_renderTextureSize = 256;
                            break;
                        case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution512:
                            reflection.m_renderTextureSize = 512;
                            break;
                        case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution1024:
                            reflection.m_renderTextureSize = 1024;
                            break;
                        case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution2048:
                            reflection.m_renderTextureSize = 2048;
                            break;
                        case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution4096:
                            reflection.m_renderTextureSize = 4096;
                            break;
                        case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8192:
                            reflection.m_renderTextureSize = 8192;
                            break;
                    }
                    reflection.m_waterProfile = profile;
                    reflection.m_waterProfileValues = waterProfileValues;
                    if (forceUpdate)
                    {
                        reflection.m_ableToRender = true;
                        reflection.Generate();
                    }

                    reflection.UpdateShaderValues();
                    if (m_camera != null)
                    {
                        reflection.CalculateSmoothnessRay(m_camera.farClipPlane);
                    }
                    else
                    {
                        reflection.CalculateSmoothnessRay(1500f);
                    }

                    if (waterProfileValues.m_waterGradient != null && waterProfileValues.m_colorDepthRamp == null)
                    {
                        reflection.ForceBuildColorDepth();
                    }
                    else
                    {
                        reflection.m_waterTexture = waterProfileValues.m_colorDepthRamp;
                    }

                }
            }

            PW_Builtin_Refraction builtin_Refraction = GameObject.FindObjectOfType<PW_Builtin_Refraction>();
            if (builtin_Refraction != null)
            {
                builtin_Refraction.renderSize = waterProfileValues.m_refractionRenderResolution;
                builtin_Refraction.PreRender(m_camera);
            }
        }

        private static void UpdateWaterMaterialInstances(Material masterMaterial, Material underwaterMaterial)
        {
            List<Material> waterMaterials = new List<Material>();
            if (masterMaterial != null)
            {
                waterMaterials.Add(masterMaterial);
            }

            if (underwaterMaterial != null)
            {
                waterMaterials.Add(underwaterMaterial);
            }

            if (waterMaterials.Count == 2)
            {
                PWS_WaterSystem waterSystem = GameObject.FindObjectOfType<PWS_WaterSystem>();
                if (waterSystem != null)
                {
                    waterSystem.CreateWaterMaterialInstances(waterMaterials);
                }
            }
            else
            {
                Debug.LogWarning("Water Materials instances could not be created due to a missing material not being found.");
            }
        }

        /// <summary>
        /// Sets up underwater features
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="underwaterFog"></param>
        /// <param name="underwaterPostFX"></param>
        /// <param name="underwaterParticles"></param>
        private static void SetupUnderWaterFX(GaiaWaterProfileValues profile, bool underwaterFog, bool underwaterPostFX, bool underwaterParticles)
        {
            if (m_parentObject == null)
            {
                m_parentObject = GetOrCreateParentObject(GaiaConstants.gaiaWaterObject, true);
            }

            float seaLevel = 0f;
            GaiaSceneInfo sceneInfo = GaiaSceneInfo.GetSceneInfo();
            if (sceneInfo != null)
            {
                seaLevel -= sceneInfo.m_seaLevel + 4.9f;
            }

            #if UNITY_POST_PROCESSING_STACK_V2
            PostProcessProfile postProcess = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(GetAssetPath(profile.m_postProcessingProfile));
            if (underwaterPostFX)
            {
                if (postProcess == null)
                {
                    Debug.LogError("[GaiaProWater.SetupUnderWaterFX()] Post Processing profile could not be found. Please make sure the string defined is correct");
                }
                else
                {
                    GameObject postProcessVolumeObject = GameObject.Find("Underwater Post Processing FX");
                    if (postProcessVolumeObject == null)
                    {
                        postProcessVolumeObject = new GameObject("Underwater Post Processing FX");
                        postProcessVolumeObject.transform.SetParent(m_parentObject.transform);
                        postProcessVolumeObject.transform.position = new Vector3(0f, seaLevel, 0f);
                    }
                    else
                    {
                        postProcessVolumeObject.transform.SetParent(m_parentObject.transform);
                        postProcessVolumeObject.transform.position = new Vector3(0f, seaLevel, 0f);
                    }

                    BoxCollider collider = postProcessVolumeObject.GetComponent<BoxCollider>();
                    if (collider == null)
                    {
                        collider = postProcessVolumeObject.AddComponent<BoxCollider>();
                        collider.size = new Vector3(10000f, 5000f, 10000f);
                        collider.isTrigger = true;
                    }
                    else
                    {
                        collider.size = new Vector3(10000f, 5000f, 10000f);
                        collider.isTrigger = true;
                    }

                    PostProcessVolume processVolume = postProcessVolumeObject.GetComponent<PostProcessVolume>();
                    if (processVolume == null)
                    {
                        processVolume = postProcessVolumeObject.AddComponent<PostProcessVolume>();
                        processVolume.sharedProfile = postProcess;
                        processVolume.isGlobal = false;
                        processVolume.blendDistance = 5f;
                        processVolume.priority = 2f;
                    }
                    else
                    {
                        processVolume.sharedProfile = postProcess;
                        processVolume.isGlobal = false;
                        processVolume.blendDistance = 5f;
                        processVolume.priority = 2f;
                    }
                }
            }
            else
            {
                GameObject postProcessVolumeObject = GameObject.Find("Underwater Post Processing FX");
                if (postProcessVolumeObject != null)
                {
                    Object.DestroyImmediate(postProcessVolumeObject);
                }
            }
            #endif
        }

        /// <summary>
        /// Removes Suffix in file formats required
        /// </summary>
        /// <param name="path"></param>
        private static List<Material> GetMaterials(string path)
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

        /// <summary>
        /// Gets the gaia ocean material
        /// </summary>
        /// <returns></returns>
        public static Material GetGaiaOceanMaterial()
        {
            Material material = null;
            material = AssetDatabase.LoadAssetAtPath<Material>(GaiaUtils.GetAssetPath("Gaia Ocean.mat"));

            return material;
        }

        #endregion
    }
}