using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gaia
{
    [System.Serializable]
    public class GaiaWaterProfileValues
    {
        [Header("Global Settings")]
        public string m_typeOfWater = "Deep Blue Ocean";
        public string m_profileRename;
        public bool m_userCustomProfile = false;

        [Header("Post Processing Settings")]
        public string m_postProcessingProfile = "Underwater Post Processing";

        [Header("Underwater Effects")]
        public Gradient m_underwaterFogGradient;
        public Color m_underwaterFogColor = Color.cyan;
        public float m_underwaterFogDepth = 100f;
        public float m_underwaterFogDistance = 45f;
        public float m_underwaterNearFogDistance = -4f;
        public float m_underwaterFogDensity = 0.045f;
        public float m_constUnderwaterPostExposure = 0f;
        public AnimationCurve m_gradientUnderwaterPostExposure = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));
        public Color m_constUnderwaterColorFilter = Color.white;
        public Gradient m_gradientUnderwaterColorFilter = new Gradient();

        [Header("Texture Settings")]
        public Texture2D m_colorDepthRamp;
        public Texture2D m_normalLayer0;
        public Texture2D m_normalLayer1;
        public Texture2D m_fadeNormal;
        public Texture2D m_foamTexture;
        public Texture2D m_foamAlphaRamp;
        public Texture2D m_renderTexture;

        [Header("Water Setup")]
        public PW_Builtin_Refraction.PW_RENDER_SIZE m_refractionRenderResolution = PW_Builtin_Refraction.PW_RENDER_SIZE.HALF;
        public Gradient m_waterGradient;
        public int m_gradientTextureResolution = 128;
        public int m_foamTiling = 128;
        public int m_waterTiling = 256;
        public float m_shorelineMovement = 0.1f;
        public float m_waveCount = 0.2f;
        public float m_waveSpeed = 3f;
        public float m_waveSize = 0.26f;
        public float m_transparentDistance = 10f;
        public float m_foamDistance = 8f;
        public float m_reflectionDistortion = 0.7f;
        public float m_reflectionStrength = 0.5f;
        public Color m_specularColor = Color.white;
        public float m_metallic = 0.25f;
        public float m_smoothness = 0.9f;
        public float m_normalStrength0 = 0.4f;
        public float m_normalStrength1 = 0.8f;
        public float m_fadeNormalStrength = 1f;
        public float m_fadeStart = 32f;
        public float m_fadeDistance = 128f;
    }
}