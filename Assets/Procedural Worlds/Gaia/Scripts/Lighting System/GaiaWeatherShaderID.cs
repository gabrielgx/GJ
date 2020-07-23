using UnityEngine;

namespace Gaia
{
    /// <summary>
    /// Contains all the shader ID's for all shaders needed for weather and water
    /// </summary>
    public static class GaiaWeatherShaderID
    {
        //PW Sky
        public static readonly int m_cloudFade;
        public static readonly int m_cloudBrightness;
        public static readonly int m_cloudAmbientColor;
        public static readonly int m_SunDirection;
        public static readonly int m_SunColor;
        public static readonly int m_cloudDomeBrightness;
        public static readonly int m_cloudSunDirection;
        public static readonly int m_skySunDirection;
        public static readonly int m_cloudOpacity;
        public static readonly int m_cloudDomeFogColor;
        public static readonly int m_cloudDomeSunColor;
        public static readonly int m_cloudDomeFinalCloudColor;
        public static readonly int m_cloudDomeFinalSkyColor;
        public static readonly int m_cloudHeightDensity;
        public static readonly int m_cloudHeightThickness;
        public static readonly int m_cloudSpeed;

        //PW VFX
        public static readonly int m_rainIntensity;
        public static readonly int m_weatherMainColor;
        public static readonly int m_weatherColor;

        //PW Water
        public static readonly int m_waterDepthRamp;
        public static readonly int m_waterSmoothness;
        public static readonly string m_waterGrabPass = "GrabPass";
        public static int m_underwaterColor;

        //PW Global
        public static readonly int m_globalLightDirection;
        public static readonly int m_globalLightColor;
        public static readonly int m_globalLightSpecColor;
        public static readonly int m_globalWind;
        public static readonly int m_globalReflectionTexture;
        public static readonly int m_globalAmbientColor;


        //Unity Skybox
        public static readonly string m_unitySkyboxShader = "Skybox/Procedural";
        public static readonly int m_unitySkyboxGroundColor;
        public static readonly int m_unitySkyboxAtmosphereThickness;
        public static readonly int m_unitySkyboxSunSize;
        public static readonly int m_unitySkyboxSunSizeConvergence;
        public static readonly int m_unitySkyboxTint;
        public static readonly int m_unitySkyboxExposure;

        //Check strings
        public static readonly string m_checkNameSpace = "Space";
        public static readonly string m_checkCloudHeight = "CloudsHight";

        static GaiaWeatherShaderID()
        {
            //PW Sky
            m_cloudFade = Shader.PropertyToID(("PW_Clouds_Fade"));
            m_cloudBrightness = Shader.PropertyToID(("PW_Cloud_Brightness"));
            m_cloudAmbientColor = Shader.PropertyToID(("PW_AmbientColor"));
            m_SunDirection = Shader.PropertyToID(("PW_SunDirection"));
            m_SunColor = Shader.PropertyToID(("PW_SunColor"));
            m_cloudDomeBrightness = Shader.PropertyToID(("PW_SkyDome_Brightness"));
            m_cloudSunDirection = Shader.PropertyToID(("PW_SunDirection_Clouds_HA"));
            m_skySunDirection = Shader.PropertyToID(("PW_SunDirection_Sky"));
            m_cloudOpacity = Shader.PropertyToID(("PW_Clouds_Opacity"));
            m_cloudDomeFogColor = Shader.PropertyToID(("PW_SkyDome_Fog_Color"));
            m_cloudDomeSunColor = Shader.PropertyToID(("PW_SkyDome_Sun_Color"));
            m_cloudDomeFinalCloudColor = Shader.PropertyToID(("PW_SkyDome_FinalClouds_Color"));
            m_cloudDomeFinalSkyColor = Shader.PropertyToID(("PW_SkyDome_FinalSky_Color"));
            m_cloudHeightDensity = Shader.PropertyToID(("PW_Clouds_Hight_Density"));
            m_cloudHeightThickness = Shader.PropertyToID(("PW_Clouds_Hight_Thickness"));
            m_cloudSpeed = Shader.PropertyToID(("PW_Clouds_Speed_HA"));

            //PW VFX
            m_rainIntensity = Shader.PropertyToID(("_PW_VFX_Weather_Intensity"));
            m_weatherMainColor = Shader.PropertyToID(("_MainColor"));
            m_weatherColor = Shader.PropertyToID(("_Color"));

            //PW Water
            m_waterDepthRamp = Shader.PropertyToID(("_WaterDepthRamp"));
            m_waterSmoothness = Shader.PropertyToID(("_Smoothness"));
            m_underwaterColor = Shader.PropertyToID("_UnderWaterColor");

            //PW Global
            m_globalLightDirection = Shader.PropertyToID(("_PW_MainLightDir"));
            m_globalLightColor = Shader.PropertyToID(("_PW_MainLightColor"));
            m_globalLightSpecColor = Shader.PropertyToID(("_PW_MainLightSpecular"));
            m_globalWind = Shader.PropertyToID(("_WaveDirection"));
            m_globalReflectionTexture = Shader.PropertyToID(("_ReflectionTex"));
            m_globalAmbientColor = Shader.PropertyToID(("_AmbientColor"));

            //Unity Skybox
            m_unitySkyboxGroundColor = Shader.PropertyToID(("_GroundColor"));
            m_unitySkyboxAtmosphereThickness = Shader.PropertyToID(("_AtmosphereThickness"));
            m_unitySkyboxSunSize = Shader.PropertyToID(("_SunSize"));
            m_unitySkyboxSunSizeConvergence = Shader.PropertyToID(("_SunSizeConvergence"));
            m_unitySkyboxTint = Shader.PropertyToID(("_SkyTint"));
            m_unitySkyboxExposure = Shader.PropertyToID(("_Exposure"));
        }
    }
}