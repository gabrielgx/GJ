using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gaia
{
    [System.Serializable]
    public class GaiaLightingProfileValues
    {
        [Header("Global Settings")]
        public string m_typeOfLighting = "Morning";
        public GaiaConstants.GaiaLightingProfileType m_profileType = GaiaConstants.GaiaLightingProfileType.HDRI;
        public bool m_userCustomProfile = false;
        public string m_profileRename;
        [Header("Post Processing Settings")]
        public string m_postProcessingProfile = "Ambient Sample Default Evening Post Processing";
        public bool m_directToCamera = true;
        [Header("HDRP Post Processing Settings")]
        public string m_hDPostProcessingProfile = "Ambient Sample Default Evening Post Processing";
        [Header("Ambient Audio Settings")]
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
        public float m_shadowDistance = 200f;
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
        public Cubemap m_hDPBSGroundAlbedoTexture;
        public Color m_hDPBSGroundTint = new Color(0.5803922f, 0.6313726f, 0.6901961f);
        public Cubemap m_hDPBSGroundEmissionTexture;
        public float m_hDPBSGroundEmissionMultiplier = 1f;
        //Space
        public Vector3 m_hDPBSSpaceRotation = new Vector3(0f, 0f, 0f);
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

        /// <summary>
        /// Handy layer mask interface
        /// </summary>
        /// <param name="label"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        private LayerMask LayerMaskField(LayerMask layerMask)
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
                {
                    maskWithoutEmpty |= (1 << i);
                }
            }
            maskWithoutEmpty = EditorGUILayout.MaskField(maskWithoutEmpty, layers.ToArray());
            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                {
                    mask |= (1 << layerNumbers[i]);
                }
            }
            layerMask.value = mask;
            return layerMask;
        }
#endif
    }
}