#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
#if HDPipeline
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Gaia
{
    [System.Serializable]
    public class GaiaTimeOfDay
    {
        public GaiaConstants.TimeOfDayStartingMode m_todStartingType;
        public bool m_todEnabled;
        public float m_todDayTimeScale;
        public AnimationCurve m_intensityCurves;
        public int m_todHour;
        public float m_todMinutes;
    }

    [System.Serializable]
    public class GaiaWeather
    {
        public float m_season;
        public float m_windDirection;
    }

    [ExecuteAlways]
    public class GaiaGlobal : MonoBehaviour
    {
        public static GaiaGlobal Instance
        {
            get { return m_instance; }
            set
            {
                if (m_instance != value)
                {
                    m_instance = value;
                }
            }
        }
        [SerializeField]
        private static GaiaGlobal m_instance;

        public Camera m_mainCamera;
        public Material WaterMaterial;

        #region Variables Setting Saving

        public bool m_enableSettingSaving = true;
        public GaiaLightingProfileValues m_lightingSavedProfileValues;
        public GaiaWaterProfileValues m_waterSavedProfileValues;

        [Header("Global Settings")]
        public string m_typeOfLighting = "Morning";
        //public GaiaConstants.GaiaLightingProfileType m_profileType = GaiaConstants.GaiaLightingProfileType.Morning;
        public int m_lightingProfileIndex = 0;
        public int m_waterProfileIndex = 0;
        [Header("Post Processing Settings")]
        public string m_postProcessingProfile = "Ambient Sample Default Evening Post Processing";
        public bool m_directToCamera = true;
        [Header("HDRP Post Processing Settings")]
        public string m_hDPostProcessingProfile = "Ambient Sample Default Evening Post Processing";
        [Header("Ambient Audio Settings")]
        [HideInInspector]
        public AudioClip m_ambientAudio;
        [Range(0f, 1f)]
        public float m_ambientVolume = 0.55f;
        [Header("Sun Settings")]
        [Range(0f, 360f)]
        public float m_sunRotation = 0f;
        [Range(0f, 360f)]
        public float m_sunPitch = 65f;
        public Color m_sunColor = Color.white;
        public float m_sunIntensity = 1f;
        [Header("LWRP Sun Settings")]
        public Color m_lWSunColor = Color.white;
        public float m_lWSunIntensity = 1f;
        [Header("HDRP Sun Settings")]
        public Color m_hDSunColor = Color.white;
        public float m_hDSunIntensity = 1f;
        [Header("Sun Shadow Settings")]
        public LightShadows m_shadowCastingMode = LightShadows.Soft;
        [Range(0f, 1f)]
        public float m_shadowStrength = 1f;
        public LightShadowResolution m_sunShadowResolution = LightShadowResolution.FromQualitySettings;
        [Header("HDRP Shadow Settings")]
        public float m_hDShadowDistance = 700f;
        public GaiaConstants.HDShadowResolution m_hDShadowResolution = GaiaConstants.HDShadowResolution.Resolution1024;
        public bool m_hDContactShadows = true;
        public GaiaConstants.ContactShadowsQuality m_hDContactShadowQuality = GaiaConstants.ContactShadowsQuality.Medium;
        public int m_hDContactShadowCustomQuality = 10;
        public float m_hDContactShadowsDistance = 150f;
        [Range(0f, 1f)]
        public float m_hDContactShadowOpacity = 1f;
        public bool m_hDMicroShadows = true;
        [Range(0f, 1f)]
        public float m_hDMicroShadowOpacity = 1f;
        [Header("Skybox Settings")]
        [HideInInspector]
        public Cubemap m_skyboxHDRI;
        public Color m_skyboxTint = new Color(0.5f, 0.5f, 0.5f, 1f);
        [Range(0f, 8f)]
        public float m_skyboxExposure = 1.6f;
        [Space(15)]
        [Range(0f, 1f)]
        public float m_sunSize = 0.04f;
        [Range(0.01f, 10f)]
        public float m_sunConvergence = 10f;
        [Range(0f, 5f)]
        public float m_atmosphereThickness = 1f;
        public Color m_groundColor = Color.gray;
        [Header("HDRP Skybox Settings")]
        public GaiaConstants.HDSkyType m_hDSkyType = GaiaConstants.HDSkyType.HDRI;
        public GaiaConstants.HDSkyUpdateMode m_hDSkyUpdateMode = GaiaConstants.HDSkyUpdateMode.OnChanged;
        [Space(10)]
        //HDRI
        [HideInInspector]
        public Cubemap m_hDHDRISkybox;
        public float m_hDHDRIExposure = 0.75f;
        public float m_hDHDRIMultiplier = 1f;
        [Space(10)]
        //Gradient
        public Color m_hDGradientTopColor = Color.blue;
        public Color m_hDGradientMiddleColor = Color.cyan;
        public Color m_hDGradientBottomColor = Color.white;
        public float m_hDGradientDiffusion = 1f;
        public float m_hDGradientExposure = 0f;
        public float m_hDGradientMultiplier = 1f;
        [Space(10)]
        //Procedural
        public bool m_hDProceduralEnableSunDisk = true;
        public bool m_hDProceduralIncludeSunInBaking = true;
        public float m_hDProceduralSunSize = 0.015f;
        public float m_hDProceduralSunSizeConvergence = 9.5f;
        public float m_hDProceduralAtmosphereThickness = 1f;
        public Color32 m_hDProceduralSkyTint = new Color32(128, 128, 128, 128);
        public Color32 m_hDProceduralGroundColor = new Color32(148, 161, 176, 255);
        public float m_hDProceduralExposure = 1f;
        public float m_hDProceduralMultiplier = 2.5f;
        //Physically Based Sky
        //Planet
        public bool m_hDPBSEarthPreset = true;
        public float m_hDPBSPlanetaryRadius = 6378.759f;
        public bool m_hDPBSSphericalMode = true;
        public float m_hDPBSSeaLevel = 50f;
        public Vector3 m_hDPBSPlantetCenterPosition = new Vector3(0f, -6378.759f, 0f);
        public Vector3 m_hDPBSPlanetRotation = new Vector3(0f, 0f, 0f);
        [HideInInspector]
        public Cubemap m_hDPBSGroundAlbedoTexture;
        public Color m_hDPBSGroundTint = new Color(0.5803922f, 0.6313726f, 0.6901961f);
        [HideInInspector]
        public Cubemap m_hDPBSGroundEmissionTexture;
        public float m_hDPBSGroundEmissionMultiplier = 1f;
        //Space
        public Vector3 m_hDPBSSpaceRotation = new Vector3(0f, 0f, 0f);
        [HideInInspector]
        public Cubemap m_hDPBSSpaceEmissionTexture;
        public float m_hDPBSSpaceEmissionMultiplier = 1f;
        //Air
        public float m_hDPBSAirMaximumAltitude = 70f;
        public Color m_hDPBSAirOpacity = Color.white;
        public Color m_hDPBSAirAlbedo = Color.white;
        public float m_hDPBSAirDensityBlue = 0.232f;
        public Color m_hDPBSAirTint = new Color(0.172f, 0.074f, 0.030f);
        //Aerosols
        public float m_hDPBSAerosolMaximumAltitude = 8.3f;
        public float m_hDPBSAerosolDensity = 0.5f;
        public Color m_hDPBSAerosolAlbedo = Color.white;
        public float m_hDPBSAerosolAnisotropy = 0f;
        //Artistic Overrides
        public float m_hDPBSColorSaturation = 1f;
        public float m_hDPBSAlphaSaturation = 1f;
        public float m_hDPBSAlphaMultiplier = 1f;
        public Color m_hDPBSHorizonTint = Color.white;
        public float m_hDPBSHorizonZenithShift = 0f;
        public Color m_hDPBSZenithTint = Color.white;
        //Miscellaneous
        public int m_hDPBSNumberOfBounces = 8;
        public GaiaConstants.HDIntensityMode m_hDPBSIntensityMode = GaiaConstants.HDIntensityMode.Exposure;
        public float m_hDPBSMultiplier = 1f;
        public float m_hDPBSExposure = 1f;
        public bool m_hDPBSIncludeSunInBaking = true;

        [Header("Ambient Light Settings")]
        public AmbientMode m_ambientMode = AmbientMode.Trilight;
        [Range(0f, 10f)]
        public float m_ambientIntensity = 1f;
        public Color m_skyAmbient = Color.white;
        public Color m_equatorAmbient = Color.gray;
        public Color m_groundAmbient = Color.gray;
        [Header("HDRP Ambient Light Settings")]
        public GaiaConstants.HDAmbientMode m_hDAmbientMode = GaiaConstants.HDAmbientMode.Static;
        public float m_hDAmbientDiffuseIntensity = 1f;
        public float m_hDAmbientSpecularIntensity = 1f;
        [Header("Fog Settings")]
        public FogMode m_fogMode = FogMode.Linear;
        public Color m_fogColor = Color.white;
        [Range(0f, 1f)]
        public float m_fogDensity = 0.01f;
        public float m_fogStartDistance = 15f;
        public float m_fogEndDistance = 800f;
        [Header("HDRP Fog Settings")]
        public GaiaConstants.HDFogType m_hDFogType = GaiaConstants.HDFogType.Volumetric;
        public GaiaConstants.HDFogType2019_3 m_hDFogType2019_3 = GaiaConstants.HDFogType2019_3.Volumetric;
        [Space(10)]
        //Exponential
        [Range(0f, 1f)]
        public float m_hDExponentialFogDensity = 1f;
        public float m_hDExponentialFogDistance = 200f;
        public float m_hDExponentialFogBaseHeight = 0f;
        [Range(0f, 1f)]
        public float m_hDExponentialFogHeightAttenuation = 0.2f;
        public float m_hDExponentialFogMaxDistance = 5000f;
        [Space(10)]
        //Linear
        [Range(0f, 1f)]
        public float m_hDLinearFogDensity = 1f;
        public float m_hDLinearFogStart = 5f;
        public float m_hDLinearFogEnd = 1200f;
        public float m_hDLinearFogHeightStart = 100f;
        public float m_hDLinearFogHeightEnd = 800f;
        public float m_hDLinearFogMaxDistance = 5000f;
        [Space(10)]
        //Volumetric
        public Color m_hDVolumetricFogScatterColor = Color.white;
        public float m_hDVolumetricFogDistance = 1000f;
        public float m_hDVolumetricFogBaseHeight = 100f;
        public float m_hDVolumetricFogMeanHeight = 200f;
        [Range(0f, 1f)]
        public float m_hDVolumetricFogAnisotropy = 0.75f;
        [Range(0f, 1f)]
        public float m_hDVolumetricFogProbeDimmer = 0.8f;
        public float m_hDVolumetricFogMaxDistance = 5000f;
        public float m_hDVolumetricFogDepthExtent = 50f;
        [Range(0f, 1f)]
        public float m_hDVolumetricFogSliceDistribution = 0f;
#if UNITY_EDITOR
        [Header("Lightmapping Settings")]
        public LightmapEditorSettings.Lightmapper m_lightmappingMode = LightmapEditorSettings.Lightmapper.ProgressiveGPU;
#endif

        //Main Global
        public bool m_hasBeenSaved = false;
        [HideInInspector]
        public GaiaLightingProfile m_lightingProfile;
        [HideInInspector]
        public GaiaWaterProfile m_waterProfile;
        private GaiaSettings m_gaiaSettings;
        [HideInInspector]
        public Material m_masterSkyboxMaterial;
        [HideInInspector]
        public bool m_parentObjects = true;
        [HideInInspector]
        public bool m_hideProcessVolume = true;
        [HideInInspector]
        public bool m_enablePostProcessing = true;
        [HideInInspector]
        public bool m_enableAmbientAudio = true;
        [HideInInspector]
        public bool m_enableFog = true;
        [HideInInspector]
        public GaiaConstants.GaiaProAntiAliasingMode m_antiAliasingMode = GaiaConstants.GaiaProAntiAliasingMode.TAA;
        [HideInInspector]
        public float m_antiAliasingTAAStrength = 0.7f;
        [HideInInspector]
        public bool m_cameraDithering = true;
        [HideInInspector]
        public float m_cameraAperture = 16f;
        [HideInInspector]
        public bool m_usePhysicalCamera = false;
        [HideInInspector]
        public Vector2 m_cameraSensorSize = new Vector2(70.41f, 52.63f);
        [HideInInspector]
        public bool m_globalReflectionProbe = true;

        #endregion

        #region Variables Time Of Day

        public GaiaTimeOfDay GaiaTimeOfDayValue
        {
            get { return m_gaiaTimeOfDay; }
            set
            {
                m_gaiaTimeOfDay = value;
                UpdateGaiaTimeOfDay(false);
            }
        }
        [SerializeField]
        private GaiaTimeOfDay m_gaiaTimeOfDay = new GaiaTimeOfDay();

        #endregion

        #region Variables Weather

        public GaiaWeather GaiaWeather
        {
            get { return m_gaiaWeather; }
            set
            {
                m_gaiaWeather = value;
                UpdateGaiaWeather();
            }
        }
        [SerializeField]
        private GaiaWeather m_gaiaWeather = new GaiaWeather();

        #endregion

        #region Private Stored Values

        [SerializeField]
        private Light m_sunLight;

        [SerializeField]
        private Light m_moonLight;

        [SerializeField]
        public bool WeatherPresent = false;

#if HDPipeline
        [SerializeField]
        private HDAdditionalLightData SunHDLightData;

        [SerializeField]
        private HDAdditionalLightData MoonHDLightData;
#endif

        #endregion

        #region Public Stored Values

        public const string m_shaderLightDirection = "_PW_MainLightDir";
        public const string m_shaderLightColor = "_PW_MainLightColor";
        public const string m_shaderSpecLightColor = "_PW_MainLightSpecular";
        public const string m_shaderWind = "_WaveDirection";
        public const string m_shaderReflectionTexture = "_ReflectionTex";
        public const string m_shaderAmbientColor = "_AmbientColor";

        #endregion

        [SerializeField]
        private bool m_sunLightExists;
#if GAIA_PRO_PRESENT
        [SerializeField]
        private bool m_moonLightExists;
#endif

        #region Unity Functions

        private void Awake()
        {
            if (m_mainCamera == null)
            {
                m_mainCamera = GaiaUtils.GetCamera();
            }
        }

        private void Start()
        {
            m_instance = this;
            WeatherPresent = CheckWeatherPresent();
            UpdateGaiaTimeOfDay(false);

            if (m_sunLight == null)
            {
                m_sunLight = GaiaUtils.GetMainDirectionalLight();
            }
            if (m_sunLight != null)
            {
                m_sunLightExists = true;
            }

            if (m_moonLight == null)
            {
                GameObject moonObject = GameObject.Find("Moon Light");
                if (moonObject != null)
                {
                    m_moonLight = moonObject.GetComponent<Light>();
                }
            }
#if GAIA_PRO_PRESENT
            if (m_moonLight != null)
            {
                m_moonLightExists = true;
            }
#endif

        }

        public void OnEnable()
        {
            m_instance = this;
            WeatherPresent = CheckWeatherPresent();

            if (!Application.isPlaying)
            {
                if (m_lightingProfile == null)
                {
                    return;
                }

                if (m_hasBeenSaved)
                {
                    LoadSettings(m_lightingProfile, true);
                }

                if (m_waterProfile == null)
                {
                    return;
                }

                if (m_hasBeenSaved)
                {
                    LoadSettings(m_waterProfile, true);
                }

                if (m_gaiaSettings == null)
                {
                    m_gaiaSettings = GaiaUtils.GetGaiaSettings();
                    if (m_gaiaSettings == null)
                    {
                        return;
                    }
                }
            }

            if (m_mainCamera == null)
            {
                m_mainCamera = GaiaUtils.GetCamera();
            }

            if (m_sunLight == null)
            {
                m_sunLight = GaiaUtils.GetMainDirectionalLight();
            }

            if (m_moonLight == null)
            {
                GameObject moonObject = GameObject.Find("Moon Light");
                if (moonObject != null)
                {
                    m_moonLight = moonObject.GetComponent<Light>();
                }
            }

            UpdateGaiaTimeOfDay(false);
        }

        private void Update()
        {
#if GAIA_PRO_PRESENT
            if (WeatherPresent)
            {
                if (ProceduralWorldsGlobalWeather.Instance.CheckIsNight())
                {
                    Shader.SetGlobalVector(GaiaWeatherShaderID.m_globalLightDirection, -ProceduralWorldsGlobalWeather.Instance.m_moonLight.transform.forward);
                    Shader.SetGlobalVector(GaiaWeatherShaderID.m_globalLightColor, ProceduralWorldsGlobalWeather.Instance.m_moonLight.color * ProceduralWorldsGlobalWeather.Instance.m_moonLight.intensity);
                }
                else
                {
                    Shader.SetGlobalVector(GaiaWeatherShaderID.m_globalLightDirection, -ProceduralWorldsGlobalWeather.Instance.m_sunLight.transform.forward);
                    Shader.SetGlobalVector(GaiaWeatherShaderID.m_globalLightColor, ProceduralWorldsGlobalWeather.Instance.m_sunLight.color * ProceduralWorldsGlobalWeather.Instance.m_sunLight.intensity);
                }
            }
            else
            {
                if (m_sunLightExists)
                {
                    if (m_sunLight != null)
                    {
                        Shader.SetGlobalVector(GaiaWeatherShaderID.m_globalLightDirection, -m_sunLight.transform.forward);
                        Shader.SetGlobalVector(GaiaWeatherShaderID.m_globalLightColor, m_sunLight.color * m_sunLight.intensity);
                    }
                }
            }
#else
            if (m_sunLightExists)
            {
                Shader.SetGlobalVector(m_shaderLightDirection, -m_sunLight.transform.forward);
                Shader.SetGlobalVector(m_shaderLightColor, m_sunLight.color * m_sunLight.intensity);
            }
#endif

            if (WeatherPresent)
            {
                if (Application.isPlaying)
                {
                    if (GaiaTimeOfDayValue.m_todEnabled)
                    {
                        GaiaTimeOfDayValue.m_todMinutes += Time.deltaTime * GaiaTimeOfDayValue.m_todDayTimeScale;
                    }
                }
                else
                {
#if GAIA_PRO_PRESENT
                    if (ProceduralWorldsGlobalWeather.Instance.RunInEditor)
                    {
                        GaiaTimeOfDayValue.m_todMinutes += Time.deltaTime * GaiaTimeOfDayValue.m_todDayTimeScale;
                    }
                    else
                    {
                        return;
                    }
#endif
                }

                if (GaiaTimeOfDayValue.m_todMinutes > 59.1f)
                {
                    GaiaTimeOfDayValue.m_todMinutes = 0f;
                    GaiaTimeOfDayValue.m_todHour++;
                }

                if (GaiaTimeOfDayValue.m_todHour > 23)
                {
                    GaiaTimeOfDayValue.m_todHour = 0;
                }

                UpdateGaiaTimeOfDay(false);
            }
        }

        #endregion

        #region Setting Saving Functions

        /// <summary>
        /// Applies all the settings
        /// </summary>
        public void ApplySetting(GaiaLightingProfileValues profile, GaiaLightingProfile mainProfile)
        {
            if (!m_enableSettingSaving)
            {
                return;
            }

            if (profile == null || mainProfile == null)
            {
                return;
            }

            m_lightingSavedProfileValues = profile;

            //Main Global
            m_lightingProfile = mainProfile;
            //m_profileType = mainProfile.m_lightingProfile;
            m_lightingProfileIndex = mainProfile.m_selectedLightingProfileValuesIndex;
            m_masterSkyboxMaterial = mainProfile.m_masterSkyboxMaterial;
            m_parentObjects = mainProfile.m_parentObjects;
            m_hideProcessVolume = mainProfile.m_hideProcessVolume;
            m_enableAmbientAudio = mainProfile.m_enableAmbientAudio;
            m_enableFog = mainProfile.m_enableFog;
            m_antiAliasingMode = mainProfile.m_antiAliasingMode;
            m_antiAliasingTAAStrength = mainProfile.m_antiAliasingTAAStrength;
            m_cameraDithering = mainProfile.m_cameraDithering;
            m_cameraAperture = mainProfile.m_cameraAperture;
            m_usePhysicalCamera = mainProfile.m_usePhysicalCamera;
            m_cameraSensorSize = mainProfile.m_cameraSensorSize;
            m_globalReflectionProbe = mainProfile.m_globalReflectionProbe;

            m_hasBeenSaved = true;
        }

        
        /// <summary>
        /// Applies all the settings
        /// </summary>
        public void ApplySetting(GaiaWaterProfileValues profile, GaiaWaterProfile mainProfile)
        {
            if (!m_enableSettingSaving)
            {
                return;
            }

            if (profile == null || mainProfile == null)
            {
                return;
            }

            m_waterSavedProfileValues = profile;

            //Main Global
            m_waterProfile = mainProfile;
            //m_profileType = mainProfile.m_lightingProfile;
            m_waterProfileIndex = mainProfile.m_selectedWaterProfileValuesIndex;

            m_hasBeenSaved = true;
        }

        /// <summary>
        /// Loads all the settings to profile
        /// </summary>
        /// <param name="profile"></param>
        public GaiaLightingProfileValues LoadSettings(GaiaLightingProfile mainProfile, bool loadType)
        {
            if (!m_enableSettingSaving)
            {
                return null;
            }

            if (mainProfile == null)
            {
                return null;
            }

            if (m_lightingProfileIndex > mainProfile.m_lightingProfiles.Count - 1)
            {
                m_lightingProfileIndex = 1;
            }

            //Main Global
            if (loadType)
            {
                mainProfile.m_selectedLightingProfileValuesIndex = m_lightingProfileIndex;
            }

            mainProfile.m_lightingProfiles[m_lightingProfileIndex] = m_lightingSavedProfileValues;
            mainProfile.m_masterSkyboxMaterial = m_masterSkyboxMaterial;
            mainProfile.m_parentObjects = m_parentObjects;
            mainProfile.m_hideProcessVolume = m_hideProcessVolume;
            mainProfile.m_enableAmbientAudio = m_enableAmbientAudio;
            mainProfile.m_enableFog = m_enableFog;
            mainProfile.m_antiAliasingMode = m_antiAliasingMode;
            mainProfile.m_antiAliasingTAAStrength = m_antiAliasingTAAStrength;
            mainProfile.m_cameraDithering = m_cameraDithering;
            mainProfile.m_cameraAperture = m_cameraAperture;
            mainProfile.m_usePhysicalCamera = m_usePhysicalCamera;
            mainProfile.m_cameraSensorSize = m_cameraSensorSize;
            mainProfile.m_globalReflectionProbe = m_globalReflectionProbe;

            return m_lightingSavedProfileValues;
        }

        public GaiaWaterProfileValues LoadSettings(GaiaWaterProfile mainProfile, bool loadType)
        {
            if (!m_enableSettingSaving)
            {
                return null;
            }

            if (mainProfile == null)
            {
                return null;
            }

            if (m_waterProfileIndex > mainProfile.m_waterProfiles.Count - 1)
            {
                m_lightingProfileIndex = 1;
            }

            //Main Global
            if (loadType)
            {
                mainProfile.m_selectedWaterProfileValuesIndex = m_waterProfileIndex;
            }

            mainProfile.m_waterProfiles[m_waterProfileIndex] = m_waterSavedProfileValues;
            return m_waterSavedProfileValues;
        }

        /// <summary>
        /// Loads the profile type only
        /// </summary>
        /// <param name="mainProfile"></param>
        public void LoadProfileType(GaiaLightingProfile mainProfile)
        {
            mainProfile.m_selectedLightingProfileValuesIndex = m_lightingProfileIndex;
        }

        /// <summary>
        /// Loads the profile type only
        /// </summary>
        /// <param name="mainProfile"></param>
        public void LoadProfileType(GaiaWaterProfile mainProfile)
        {
            mainProfile.m_selectedWaterProfileValuesIndex = m_waterProfileIndex;
        }

#endregion

        #region Gaia Time Of Day Functions

        public void UpdateGaiaTimeOfDay(bool revertDefault)
        {
#if GAIA_PRO_PRESENT
            if (WeatherPresent)
            {
                bool applicationUpdate = !ProceduralWorldsGlobalWeather.Instance.IsRaining;
                if (ProceduralWorldsGlobalWeather.Instance.IsSnowing)
                {
                    applicationUpdate = false;
                }

                if (GaiaTimeOfDayValue.m_todHour != m_gaiaTimeOfDay.m_todHour || GaiaTimeOfDayValue.m_todMinutes != m_gaiaTimeOfDay.m_todMinutes)
                {
                    if (Application.isPlaying && applicationUpdate)
                    {
                        PW_VFX_Atmosphere.Instance.UpdateSystem();
                    }
                }

                UpdateNightMode();
            }

            m_gaiaTimeOfDay = GaiaTimeOfDayValue;
#endif
        }

        /// <summary>
        /// Sets the starting time of day mode
        /// </summary>
        /// <param name="mode"></param>
        public void UpdateTimeOfDayMode(GaiaConstants.TimeOfDayStartingMode mode, bool revertDefault)
        {
            switch (mode)
            {
                case GaiaConstants.TimeOfDayStartingMode.Morning:
                    GaiaTimeOfDayValue.m_todHour = 6;
                    m_gaiaTimeOfDay.m_todHour = 6;
                    GaiaTimeOfDayValue.m_todMinutes = 30f;
                    m_gaiaTimeOfDay.m_todMinutes = 30;
                    break;
                case GaiaConstants.TimeOfDayStartingMode.Day:
                    GaiaTimeOfDayValue.m_todHour = 15;
                    m_gaiaTimeOfDay.m_todHour = 15;
                    GaiaTimeOfDayValue.m_todMinutes = 0f;
                    m_gaiaTimeOfDay.m_todMinutes = 0;
                    break;
                case GaiaConstants.TimeOfDayStartingMode.Evening:
                    GaiaTimeOfDayValue.m_todHour = 17;
                    m_gaiaTimeOfDay.m_todHour = 17;
                    GaiaTimeOfDayValue.m_todMinutes = 30f;
                    m_gaiaTimeOfDay.m_todMinutes = 30;
                    break;
                case GaiaConstants.TimeOfDayStartingMode.Night:
                    GaiaTimeOfDayValue.m_todHour = 1;
                    m_gaiaTimeOfDay.m_todHour = 1;
                    GaiaTimeOfDayValue.m_todMinutes = 0f;
                    m_gaiaTimeOfDay.m_todMinutes = 0;
                    break;
            }

            UpdateGaiaTimeOfDay(revertDefault);
        }

        /// <summary>
        /// Update the night mode stuff
        /// </summary>
        public void UpdateNightMode()
        {
#if GAIA_PRO_PRESENT
            if (WeatherPresent)
            {
                if (m_sunLight == null)
                {
                    m_sunLight = GaiaUtils.GetMainDirectionalLight();
                    if (m_sunLight != null)
                    {
                        m_sunLightExists = true;
                    }
                }

                if (m_moonLight == null)
                {
                    GameObject moonObject = GameObject.Find("Moon Light");
                    if (moonObject != null)
                    {
                        m_moonLight = moonObject.GetComponent<Light>();
                    }

                    if (m_moonLight != null)
                    {
                        m_moonLightExists = true;
                    }
                }

                if (ProceduralWorldsGlobalWeather.Instance.CheckIsNight())
                {
                    if (m_moonLightExists)
                    {
                        RenderSettings.sun = m_moonLight;
                    }

                    if (m_sunLightExists)
                    {
#if HDPipeline
                        if (SunHDLightData == null)
                        {
                            SunHDLightData = m_sunLight.GetComponent<HDAdditionalLightData>();
                            if (SunHDLightData == null)
                            {
                                SunHDLightData = m_sunLight.gameObject.AddComponent<HDAdditionalLightData>();
                            }
                        }

                        SunHDLightData.intensity = 0;
                        SunHDLightData.lightUnit = LightUnit.Lux;
#endif
                        m_sunLight.intensity = 0f;
                    }
                }
                else
                {
                    if (m_moonLightExists)
                    {
#if HDPipeline
                        if (MoonHDLightData == null)
                        {
                            MoonHDLightData = m_moonLight.GetComponent<HDAdditionalLightData>();
                            if (MoonHDLightData == null)
                            {
                                MoonHDLightData = m_moonLight.gameObject.AddComponent<HDAdditionalLightData>();
                            }
                        }

                        MoonHDLightData.intensity = 0;
                        MoonHDLightData.lightUnit = LightUnit.Lux;
#endif

                        m_moonLight.intensity = 0f;
                    }

                    if (m_sunLightExists)
                    {
                        RenderSettings.sun = m_sunLight;
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Checks if the weather system is present in the scene
        /// </summary>
        /// <returns></returns>
        public static bool CheckWeatherPresent()
        {
#if GAIA_PRO_PRESENT
            bool weatherPresent = ProceduralWorldsGlobalWeather.Instance != null;
            return weatherPresent;
#else
            return false;
#endif
        }

        /// <summary>
        /// Checks if the weather system is present in the scene
        /// </summary>
        /// <returns></returns>
        public static void CheckWeatherPresent(bool setStatus)
        {
#if GAIA_PRO_PRESENT
            if (Instance != null)
            {
                if (setStatus)
                {
                    Instance.WeatherPresent = ProceduralWorldsGlobalWeather.Instance != null;
                }
            }
#endif
        }

        #endregion

        #region Gaia Weather Functions

        public void UpdateGaiaWeather()
        {
#if GAIA_PRO_PRESENT
            if (WeatherPresent)
            {
                ProceduralWorldsGlobalWeather.Instance.Season = GaiaWeather.m_season;
                ProceduralWorldsGlobalWeather.Instance.WindDirection = GaiaWeather.m_windDirection;
            }
#endif
        }

        #endregion

        #region Public Static Functions

        /// <summary>
        /// This function is used to return the current time of day value from 0-1.
        /// This is used for evaluation animation cruves and color gradients in the weather or any other systems required.
        /// </summary>
        /// <param name="gaiaGlobal"></param>
        /// <returns></returns>
        public static float GetTimeOfDayMainValue()
        {
            float value = 0;
            value = ((Instance.GaiaTimeOfDayValue.m_todHour * 60f) + Instance.GaiaTimeOfDayValue.m_todMinutes) / 1440f;
            return value;
        }

        /// <summary>
        /// Gets the current Hour
        /// </summary>
        /// <returns></returns>
        public static int GetTimeOfDayHour()
        {
            int value = 0;
            value = Instance.GaiaTimeOfDayValue.m_todHour;
            return value;
        }

        /// <summary>
        /// Gets the current Minute
        /// </summary>
        /// <returns></returns>
        public static float GetTimeOfDayMinute()
        {
            float value = 0;
            value = Instance.GaiaTimeOfDayValue.m_todMinutes;
            return value;
        }

        //Network Get
        public static void GaiaGlobalNetworkSyncGetAll(out int timeHour, out float timeMinute, out bool isRaining, out bool isSnowing, out bool isTODEnabled, out float timeScale)
        {
            timeHour = 15;
            timeMinute = 0f;
            isRaining = false;
            isSnowing = false;
            isTODEnabled = false;
            timeScale = 0;

            if (Instance != null)
            {
                //Set Time
                timeHour = Instance.GaiaTimeOfDayValue.m_todHour;
                timeMinute = Instance.GaiaTimeOfDayValue.m_todMinutes;
                isTODEnabled = Instance.GaiaTimeOfDayValue.m_todEnabled;
                timeScale = Instance.GaiaTimeOfDayValue.m_todDayTimeScale;
            }

#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                //Set Weather
                isRaining = ProceduralWorldsGlobalWeather.Instance.IsRaining;
                isSnowing = ProceduralWorldsGlobalWeather.Instance.IsSnowing;
            }
#endif
        }
        public static void GaiaGlobalNetworkSyncGetTimeAndWeather(out int timeHour, out float timeMinute, out bool isRaining, out bool isSnowing)
        {
            timeHour = 15;
            timeMinute = 0f;
            isRaining = false;
            isSnowing = false;

            if (Instance != null)
            {
                //Set Time
                timeHour = Instance.GaiaTimeOfDayValue.m_todHour;
                timeMinute = Instance.GaiaTimeOfDayValue.m_todMinutes;
            }

#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                //Set Weather
                isRaining = ProceduralWorldsGlobalWeather.Instance.IsRaining;
                isSnowing = ProceduralWorldsGlobalWeather.Instance.IsSnowing;
            }
#endif
        }
        public static void GaiaGlobalNetworkSyncGetTime(out int timeHour, out float timeMinute)
        {
            timeHour = 15;
            timeMinute = 0f;

            if (Instance != null)
            {
                //Set Time
                timeHour = Instance.GaiaTimeOfDayValue.m_todHour;
                timeMinute = Instance.GaiaTimeOfDayValue.m_todMinutes;
            }
        }
        public static void GaiaGlobalNetworkSyncGetWeather(out bool isRaining, out bool isSnowing)
        {
            isRaining = false;
            isSnowing = false;

#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                //Set Weather
                isRaining = ProceduralWorldsGlobalWeather.Instance.IsRaining;
                isSnowing = ProceduralWorldsGlobalWeather.Instance.IsSnowing;
            }
#endif
        }
        public static void GaiaGlobalNetworkSyncGetTimeStatus(out bool isTODEnabled, out float timeScale)
        {
            isTODEnabled = false;
            timeScale = 0;

#if GAIA_PRO_PRESENT
            if (Instance != null)
            {
                isTODEnabled = Instance.GaiaTimeOfDayValue.m_todEnabled;
                timeScale = Instance.GaiaTimeOfDayValue.m_todDayTimeScale;
            }
#endif
        }
        //Network Set
        public static void GaiaGlobalNetworkSyncSetAll(int timeHour, float timeMinute, bool isRaining, bool isSnowing, bool isTODEnabled, float timeScale)
        {
            if (Instance != null)
            {
                //Set Time
                Instance.GaiaTimeOfDayValue.m_todHour = timeHour;
                Instance.GaiaTimeOfDayValue.m_todMinutes = timeMinute;
                Instance.GaiaTimeOfDayValue.m_todEnabled = isTODEnabled;
                Instance.GaiaTimeOfDayValue.m_todDayTimeScale = timeScale;
                Instance.UpdateGaiaTimeOfDay(false);
            }

#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                //Set Weather
                if (!ProceduralWorldsGlobalWeather.Instance.IsRaining && isRaining)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsRaining = isRaining;
                    ProceduralWorldsGlobalWeather.Instance.PlayRain();
                }
                else if (ProceduralWorldsGlobalWeather.Instance.IsRaining && !isRaining)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsRaining = isRaining;
                    ProceduralWorldsGlobalWeather.Instance.StopRain();
                }
                if (!ProceduralWorldsGlobalWeather.Instance.IsSnowing && isSnowing)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsSnowing = isSnowing;
                    ProceduralWorldsGlobalWeather.Instance.PlaySnow();
                }
                else if (ProceduralWorldsGlobalWeather.Instance.IsSnowing && !isSnowing)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsSnowing = isSnowing;
                    ProceduralWorldsGlobalWeather.Instance.StopSnow();
                }
                ProceduralWorldsGlobalWeather.Instance.UpdateAllSystems(false);
                ProceduralWorldsGlobalWeather.Instance.ForceUpdateSkyShaders();
            }
#endif
        }
        public static void GaiaGlobalNetworkSyncSetTimeAndWeather(int timeHour, float timeMinute, bool isRaining, bool isSnowing)
        {
            if (Instance != null)
            {
                //Set Time
                Instance.GaiaTimeOfDayValue.m_todHour = timeHour;
                Instance.GaiaTimeOfDayValue.m_todMinutes = timeMinute;
                Instance.UpdateGaiaTimeOfDay(false);
            }

#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                //Set Weather
                if (!ProceduralWorldsGlobalWeather.Instance.IsRaining && isRaining)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsRaining = isRaining;
                    ProceduralWorldsGlobalWeather.Instance.PlayRain();
                }
                else if (ProceduralWorldsGlobalWeather.Instance.IsRaining && !isRaining)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsRaining = isRaining;
                    ProceduralWorldsGlobalWeather.Instance.StopRain();
                }
                if (!ProceduralWorldsGlobalWeather.Instance.IsSnowing && isSnowing)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsSnowing = isSnowing;
                    ProceduralWorldsGlobalWeather.Instance.PlaySnow();
                }
                else if (ProceduralWorldsGlobalWeather.Instance.IsSnowing && !isSnowing)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsSnowing = isSnowing;
                    ProceduralWorldsGlobalWeather.Instance.StopSnow();
                }
                ProceduralWorldsGlobalWeather.Instance.UpdateAllSystems(false);
                ProceduralWorldsGlobalWeather.Instance.ForceUpdateSkyShaders();
            }
#endif
        }
        public static void GaiaGlobalNetworkSyncSetTime(int timeHour, float timeMinute)
        {
            if (Instance != null)
            {
                //Set Time
                Instance.GaiaTimeOfDayValue.m_todHour = timeHour;
                Instance.GaiaTimeOfDayValue.m_todMinutes = timeMinute;
                Instance.UpdateGaiaTimeOfDay(false);
            }
        }
        public static void GaiaGlobalNetworkSyncSetWeather(bool isRaining, bool isSnowing)
        {     
#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                //Set Weather
                if (!ProceduralWorldsGlobalWeather.Instance.IsRaining && isRaining)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsRaining = isRaining;
                    ProceduralWorldsGlobalWeather.Instance.PlayRain();
                }
                else if (ProceduralWorldsGlobalWeather.Instance.IsRaining && !isRaining)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsRaining = isRaining;
                    ProceduralWorldsGlobalWeather.Instance.StopRain();
                }
                if (!ProceduralWorldsGlobalWeather.Instance.IsSnowing && isSnowing)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsSnowing = isSnowing;
                    ProceduralWorldsGlobalWeather.Instance.PlaySnow();
                }
                else if (ProceduralWorldsGlobalWeather.Instance.IsSnowing && !isSnowing)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsSnowing = isSnowing;
                    ProceduralWorldsGlobalWeather.Instance.StopSnow();
                }
                ProceduralWorldsGlobalWeather.Instance.UpdateAllSystems(false);
                ProceduralWorldsGlobalWeather.Instance.ForceUpdateSkyShaders();
            }
#endif
        }
        public static void GaiaGlobalNetworkSyncSetTimeStatus(bool isTODEnabled, float timeScale)
        {
#if GAIA_PRO_PRESENT
            if (Instance != null)
            {
                Instance.GaiaTimeOfDayValue.m_todEnabled = isTODEnabled;
                Instance.GaiaTimeOfDayValue.m_todDayTimeScale = timeScale;
            }
#endif
        }

        /// <summary>
        /// Used to set the player transform for weather VFX to follow
        /// </summary>
        /// <param name="player"></param>
        public static void SetNetworkedPlayerTransform(Transform player)
        {
#if GAIA_PRO_PRESENT
            if (player != null)
            {
                if (ProceduralWorldsGlobalWeather.Instance != null)
                {
                    ProceduralWorldsGlobalWeather.Instance.m_player = player;
                }
            }
            else
            {
                Debug.LogError("No player transform that was set is valid. You sure the value isn't null?");
            }
#endif
        }

        #endregion
    }
}