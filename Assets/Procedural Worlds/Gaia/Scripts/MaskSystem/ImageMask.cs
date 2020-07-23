using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using System.Linq;

namespace Gaia
{

    public enum ImageMaskOperation { CollisionMask = 5, DistanceMask = 1, HeightMask = 2, ImageMask = 0, NoiseMask = 4, SlopeMask = 3, Smooth = 8, StrengthTransform = 6, TerrainTexture=9, WorldBiomeMask=11, PROConcaveConvex = 10, PROHydraulicErosion = 7}
    public enum ImageMaskBlendMode { Multiply, GreaterThan, SmallerThan, Add, Subtract }
    public enum ImageMaskDistanceMaskAxes {[Description("XZ Circular")] XZ, [Description("X only")] X, [Description("Z Only")] Z }
    public enum ImageMaskInfluence { Local, Global }

    /// <summary>
    /// Toggle between two different ways of handling height masks
    /// Absolute will store the minimum and maximum value for the mask as absolute values relative to the sea levele
    /// Relative will store the minimum and maximum value for the mask as absolute values relative to the sea levele
    /// </summary>
    public enum HeightMaskType { Absolute, Relative }

    [System.Serializable]
    public class ImageMask
    {
        public bool m_active = true;
        public bool m_invert = false;
        public ImageMaskInfluence m_influence = ImageMaskInfluence.Local;
        public ImageMaskOperation m_operation;
        public ImageMaskBlendMode m_blendMode;
        public float m_strength = 1f;
        public float m_seaLevel = 0f;
        //The maximum terrain height, NOT the theoretical maximum height, but the highest measured physical point on the terrain 
        public float m_maxWorldHeight = 0f;


        //The minimum terrain height, NOT the theoretical minimum height, but the lowest measured physical point on the terrain 
        public float m_minWorldHeight = 0f;

        public float m_xOffSet = 0f;
        public float m_zOffSet = 0f;
        public float m_xOffSetScalar = 0f;
        public float m_zOffSetScalar = 0f;

        //The current multi-terrain op we are working on - used to get heightmap, normalmap etc. for the affected area
        [NonSerialized]
        public GaiaMultiTerrainOperation m_multiTerrainOperation;

        //Image Mask specific
        public Texture2D m_imageMaskTexture;
        public GaiaConstants.ImageMaskFilterMode m_imageMaskFilterMode;
        public Color m_imageMaskColorSelectionColor = Color.white;
        public float m_imageMaskColorSelectionAccuracy = 0.5f;

        //distance Mask specific
        public AnimationCurve m_distanceMaskCurve = new AnimationCurve(new Keyframe[2] { new Keyframe() { time = 0, value = 1, weightedMode = WeightedMode.None }, new Keyframe() { time = 1, value = 0, weightedMode = WeightedMode.None } });

        //height Mask specific
        public AnimationCurve m_heightMaskCurve = new AnimationCurve(new Keyframe[2] { new Keyframe() { time = 0, value = 0, weightedMode = WeightedMode.None }, new Keyframe() { time = 1, value = 1, weightedMode = WeightedMode.None } });

        //strength Transform specific
        public AnimationCurve m_strengthTransformCurve = NewAnimCurveStraightUpwards();

        public HeightMaskType m_heightMaskType = HeightMaskType.Relative;

        public float m_relativeHeightMin = 25f;
        public float m_relativeHeightMax = 75f;

        public HeightMaskType HeightMaskType
        {
            get
            {
                return m_heightMaskType;
            }
            set
            {
                if (value != m_heightMaskType)
                {
                    if (value == HeightMaskType.Relative)
                    {
                        m_relativeHeightMin = Mathf.InverseLerp(m_minWorldHeight, m_maxWorldHeight, m_absoluteHeightMin) * 100;
                        m_relativeHeightMax = Mathf.InverseLerp(m_minWorldHeight, m_maxWorldHeight, m_absoluteHeightMax) * 100;
                    }
                    else
                    {
                        m_absoluteHeightMin = Mathf.Lerp(m_minWorldHeight, m_maxWorldHeight, m_relativeHeightMin / 100f);
                        m_absoluteHeightMax = Mathf.Lerp(m_minWorldHeight, m_maxWorldHeight, m_relativeHeightMax / 100f);
                    }

                }
                m_heightMaskType = value;
            }
        }



        //The absolute minimum height for the heightmask selection, e.g. "the selection starts at 50 meters"
        public float m_absoluteHeightMin
        {
            get
            {
                //if(m_seaLevelRelativeHeightMin<=0)
                //    return m_seaLevel - m_seaLevelRelativeHeightMin;
                //else
                return m_seaLevel + m_seaLevelRelativeHeightMin;
            }
            set { m_seaLevelRelativeHeightMin = value - m_seaLevel; }
        }
        //The absolute maximum height for the heightmask selection, e.g. "the selection ends at 150 meters"
        public float m_absoluteHeightMax
        {
            get
            {
                //if (m_seaLevelRelativeHeightMax <= 0)
                //    return m_seaLevel - m_seaLevelRelativeHeightMax;
                //else
                return m_seaLevel + m_seaLevelRelativeHeightMax;
            }
            set { m_seaLevelRelativeHeightMax = value - m_seaLevel; }
        }
        //The absolute maximum height for the he
        //The minimum height expressed relative to the sea level, e.g. "the selection starts at 50 meters below the sea level"
        [SerializeField]
        private float m_seaLevelRelativeHeightMin = -10f;

        //The maximum height expressed relative to the sea level, e.g. "the selection ends at 100 meters above the sea level"
        [SerializeField]
        private float m_seaLevelRelativeHeightMax = 10f;
        //public float m_scalarMinHeight = 0;
        //public float m_scalarMaxHeight = 1f;


        public bool tree1active = false;
        public bool tree2active = false;


        public AnimationCurve m_slopeMaskCurve = new AnimationCurve(new Keyframe[2] { new Keyframe() { time = 0, value = 1, weightedMode = WeightedMode.None },
                                                                                      new Keyframe() { time = 1, value = 0, weightedMode = WeightedMode.None } });
        public float m_slopeMin = 0.0f;
        public float m_slopeMax = 0.1f;

        public ImageMaskDistanceMaskAxes m_distanceMaskAxes;

        public GaiaNoiseSettings m_gaiaNoiseSettings = new GaiaNoiseSettings();

#if UNITY_EDITOR
        public NoiseSettings m_noiseSettings;
        public NoiseToolSettings m_noiseToolSettings = new NoiseToolSettings();
        public NoiseSettingsGUI noiseSettingsGUI;
#endif


        public bool m_ShowNoiseTransformSettings = false;
        public bool m_ShowNoisePreviewTexture = true;
        public bool m_noisePreviewTextureLocked = false;
        public bool m_ShowNoiseTypeSettings = false;


        private Texture2D m_distanceMaskCurveTexture;
        private Texture2D distanceMaskCurveTexture
        {
            get
            {
                return ImageProcessing.CreateMaskCurveTexture(ref m_distanceMaskCurveTexture);
            }
        }

        private Texture2D m_heightMaskCurveTexture;
        private Texture2D heightMaskCurveTexture
        {
            get
            {
                return ImageProcessing.CreateMaskCurveTexture(ref m_heightMaskCurveTexture);
            }
        }

        private Texture2D m_slopeMaskCurveTexture;
        private Texture2D slopeMaskCurveTexture
        {
            get
            {
                return ImageProcessing.CreateMaskCurveTexture(ref m_slopeMaskCurveTexture);
            }
        }

        private Texture2D m_strengthTransformCurveTexture;
        private Texture2D strengthTransformCurveTexture
        {
            get
            {
                return ImageProcessing.CreateMaskCurveTexture(ref m_strengthTransformCurveTexture);
            }
        }

        //collision mask specific
        public bool m_collisionMaskExpanded = true;
        public CollisionMask[] m_collisionMasks = new CollisionMask[0];
#if UNITY_EDITOR
        public ReorderableList m_reorderableCollisionMaskList;
#endif


        #region Erosion Settings

        //Eroder class reference for the erosion feature
#if UNITY_EDITOR && GAIA_PRO_PRESENT
        private HydraulicEroder m_Eroder = null;
#endif

        public GaiaConstants.ErosionMaskOutput m_erosionMaskOutput = GaiaConstants.ErosionMaskOutput.Sediment;
        public float m_erosionSimScale = 9f;
        public float m_erosionHydroTimeDelta = 0.05f;
        public int m_erosionHydroIterations = 15;
        public float m_erosionThermalTimeDelta = 0.01f;
        public int m_erosionThermalIterations = 80;
        public int m_erosionThermalReposeAngle = 80;
        public float m_erosionPrecipRate = 0.5f;
        public float m_erosionEvaporationRate = 0.5f;
        public float m_erosionFlowRate = 0.5f;
        public float m_erosionSedimentCapacity = 0.5f;
        public float m_erosionSedimentDepositRate = 0.8f;
        public float m_erosionSedimentDissolveRate = 0.5f;
        public float m_erosionRiverBankDepositRate = 7.0f;
        public float m_erosionRiverBankDissolveRate = 5.0f;
        public float m_erosionRiverBedDepositRate = 5.0f;
        public float m_erosionRiverBedDissolveRate = 5.0f;
        public bool m_erosionShowAdvancedUI;
        public bool m_erosionShowThermalUI;
        public bool m_erosionShowWaterUI;
        public bool m_erosionShowSedimentUI;
        public bool m_erosionShowRiverBankUI;
        #endregion

        //smooth settings
        public float m_smoothVerticality = 0f;
        public float m_smoothBlurRadius = 1f;

        //Texture mask settings
        //public int m_textureLayerId = 0;
        public string m_textureMaskSpawnRuleGUID = "";
        public static SpawnRule[] m_allTextureSpawnRules;
        public static Spawner[] m_allTextureSpawners;
        public static string[] m_allTextureSpawnRuleNames;
        public static int[] m_allTextureSpawnRuleIndices;

        
        //Convex Concave settings
        public float m_concavity = 1f;
        public float m_concavityFeatureSize = 10f;
        private ComputeShader m_concavityShader;
        public bool m_foldedOut = true;
        public string m_selectedWorldBiomeMaskGUID;


        /// <summary>
        /// Applies the mask to an input render texture and returns the result as render texture.
        /// </summary>
        /// <param name="inputTexture">The input texture</param>
        /// <returns>The processed output in a render texture.</returns>
        public RenderTexture Apply(RenderTexture inputTexture, RenderTexture outputTexture)
        {
            RenderTexture currentRT = RenderTexture.active;
            //ImageProcessing.WriteRenderTexture("D:\\input-"+m_operation.ToString()+".png", inputTexture);
            //RenderTexture outputTexture = RenderTexture.GetTemporary(inputTexture.descriptor);
            //RenderTexture outputTexture = new RenderTexture(inputTexture.descriptor);
#if UNITY_EDITOR
#if GAIA_PRO_PRESENT
            //clean up eroder if not in use anymore
            if (m_Eroder != null && m_operation != ImageMaskOperation.PROHydraulicErosion)
            {
                ClearEroder();
            }
#endif
            Material filterMat = GetCurrentFXFilterMaterial();
            if (filterMat == null)
            {
                Debug.LogWarning("Could not find a filter material for operation " + m_operation.ToString());
                return inputTexture;
            }

            filterMat.SetTexture("_InputTex", inputTexture);
            filterMat.SetFloat("_Strength", m_strength);
            if (m_operation != ImageMaskOperation.PROHydraulicErosion)
            {
                filterMat.SetInt("_Invert", m_invert ? 1 : 0);
            }
            else
            {
                //Special treatement for the hydraulic erosion mask: Flip the invert flag because the erosion map data seems to be inverted already
                filterMat.SetInt("_Invert", m_invert ? 0 : 1);
            }
            if (m_operation == ImageMaskOperation.NoiseMask && IsDefaultStrenghtCurve())
            {
                m_strengthTransformCurve = NewAnimCurveDefaultNoise();

            }
            ImageProcessing.BakeCurveTexture(m_strengthTransformCurve, strengthTransformCurveTexture);
            filterMat.SetTexture("_HeightTransformTex", strengthTransformCurveTexture);

            switch (m_operation)
            {
                case ImageMaskOperation.ImageMask:
#if !GAIA_PRO_PRESENT
                    if (m_imageMaskFilterMode != GaiaConstants.ImageMaskFilterMode.PROColorSelection)
                    {
#endif
                    filterMat.SetTexture("_ImageMaskTex", m_imageMaskTexture);
                    filterMat.SetInt("_FilterMode", (int)m_imageMaskFilterMode);
                    filterMat.SetColor("_Color", m_imageMaskColorSelectionColor);
                    filterMat.SetFloat("_ColorAccuracy", m_imageMaskColorSelectionAccuracy);
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_ImageMaskTex", null);
#if !GAIA_PRO_PRESENT
                    }
                    else
                    {
                        Graphics.Blit(inputTexture, outputTexture);
                    }
#endif
                    break;
                case ImageMaskOperation.DistanceMask:
                    ImageProcessing.BakeCurveTexture(m_distanceMaskCurve, distanceMaskCurveTexture);
                    filterMat.SetTexture("_DistanceMaskTex", distanceMaskCurveTexture);
                    filterMat.SetFloat("_XOffset", m_xOffSetScalar);
                    filterMat.SetFloat("_ZOffset", m_zOffSetScalar);
                    filterMat.SetFloat("_AxisMode", (int)m_distanceMaskAxes);
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_DistanceMaskTex", null);
                    break;
                case ImageMaskOperation.HeightMask:
                    ImageProcessing.BakeCurveTexture(m_heightMaskCurve, heightMaskCurveTexture);
                    filterMat.SetTexture("_HeightMapTex", m_multiTerrainOperation.RTheightmap);
                    filterMat.SetTexture("_HeightMaskTex", heightMaskCurveTexture);

                    //calculate the correct scalar min max height values according to the current terrain and the sea level
                    Terrain currentTerrain = m_multiTerrainOperation.m_originTerrain;

                    float scalarSeaLevel = Mathf.InverseLerp(0, currentTerrain.terrainData.size.y, m_seaLevel);

                    float m_scalarMaxHeight = 0.5f;
                    float m_scalarMinHeight = 0f;

                    if (m_heightMaskType == HeightMaskType.Absolute)
                    {
                        m_scalarMaxHeight = Mathf.InverseLerp(0, currentTerrain.terrainData.size.y, m_absoluteHeightMax);
                        m_scalarMinHeight = Mathf.InverseLerp(0, currentTerrain.terrainData.size.y, m_absoluteHeightMin);
                        //transfer the scalar 0..1 value to -0.5..0.5 as this is how it is used in the shader
                        m_scalarMaxHeight = Mathf.Lerp(0, 0.5f, m_scalarMaxHeight);
                        m_scalarMinHeight = Mathf.Lerp(0, 0.5f, m_scalarMinHeight);
                    }
                    else
                    {
                        float heightDiff = m_maxWorldHeight - m_minWorldHeight;
                        m_scalarMaxHeight = Mathf.Lerp(0, 0.5f, Mathf.InverseLerp(0, currentTerrain.terrainData.size.y, (heightDiff * m_relativeHeightMax / 100f) + m_minWorldHeight));
                        m_scalarMinHeight = Mathf.Lerp(0, 0.5f, Mathf.InverseLerp(0, currentTerrain.terrainData.size.y, (heightDiff * m_relativeHeightMin / 100f) + m_minWorldHeight));
                    }
                    filterMat.SetFloat("_MinHeight", m_scalarMinHeight);
                    filterMat.SetFloat("_MaxHeight", m_scalarMaxHeight);
                    //ImageProcessing.DebugWriteRenderTexture("D:\\input.png",inputTexture);
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_HeightMapTex", null);
                    filterMat.SetTexture("_HeightMaskTex", null);
                    //ImageProcessing.DebugWriteRenderTexture("D:\\output.png", outputTexture);
                    break;
                case ImageMaskOperation.SlopeMask:
                    ImageProcessing.BakeCurveTexture(m_slopeMaskCurve, slopeMaskCurveTexture);

                    filterMat.SetTexture("_NormalMapTex", m_multiTerrainOperation.RTnormalmap);
                    filterMat.SetTexture("_SlopeMaskTex", slopeMaskCurveTexture);
                    filterMat.SetFloat("_MinSlope", m_slopeMin);
                    filterMat.SetFloat("_MaxSlope", m_slopeMax);
                    //ImageProcessing.DebugWriteRenderTexture("D:\\input.png",inputTexture);
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_NormalMapTex", null);
                    filterMat.SetTexture("_SlopeMaskTex", null);
                    //ImageProcessing.DebugWriteRenderTexture("D:\\output.png", outputTexture);
                    break;
                case ImageMaskOperation.NoiseMask:
                    //noise settings can be null when the mask was never viewed in the inspector, e.g. from autospawning
                    if (m_noiseSettings == null)
                    {
                        m_noiseSettings = (NoiseSettings)ScriptableObject.CreateInstance(typeof(NoiseSettings));
                        //Try to initialize from our own Gaia Noise Settings
                        m_noiseSettings.transformSettings.translation = m_gaiaNoiseSettings.m_translation;
                        m_noiseSettings.transformSettings.rotation = m_gaiaNoiseSettings.m_rotation;
                        m_noiseSettings.transformSettings.scale = m_gaiaNoiseSettings.m_scale;
                        m_noiseSettings.domainSettings.noiseTypeName = m_gaiaNoiseSettings.m_noiseTypeName;
                        m_noiseSettings.domainSettings.noiseTypeParams = m_gaiaNoiseSettings.m_noiseTypeParams;
                        m_noiseSettings.domainSettings.fractalTypeName = m_gaiaNoiseSettings.m_fractalTypeName;
                        m_noiseSettings.domainSettings.fractalTypeParams = m_gaiaNoiseSettings.m_fractalTypeParams;
                    }

                    float previewSize = 1 / m_multiTerrainOperation.m_originTerrain.terrainData.size.x;

                    // get proper noise material from current noise settings
                    NoiseSettings noiseSettings = m_noiseSettings;

                    Material matNoise = NoiseUtils.GetDefaultBlitMaterial(m_noiseSettings);

                    // setup the noise material with values in noise settings
                    m_noiseSettings.SetupMaterial(matNoise);

                    // convert brushRotation to radians
                    float brushRotation = 0;
                    //brushRotation *= Mathf.PI / 180;
                    Vector3 brushPosWS = m_multiTerrainOperation.m_originTransform.position + (Vector3)GaiaTerrainLoaderManager.Instance.GetOrigin();
                    float brushSize = m_multiTerrainOperation.m_range;

                    // change pos and scale so they match the noiseSettings preview
                    bool isWorldSpace = (m_noiseToolSettings.coordSpace == CoordinateSpace.World);
                    brushSize = isWorldSpace ? brushSize * previewSize : 1;
                    brushPosWS = isWorldSpace ? brushPosWS * previewSize : Vector3.zero;



                    // // override noise transform
                    Quaternion rotQ = Quaternion.AngleAxis(-brushRotation, Vector3.up);
                    Matrix4x4 translation = Matrix4x4.Translate(brushPosWS);
                    Matrix4x4 rotation = Matrix4x4.Rotate(rotQ);
                    Matrix4x4 scale = Matrix4x4.Scale(Vector3.one * brushSize);
                    Matrix4x4 noiseToWorld = translation * scale;

                    matNoise.SetMatrix(NoiseSettings.ShaderStrings.transform,
                                        m_noiseSettings.trs * noiseToWorld);

                    int rtW = m_multiTerrainOperation.RTheightmap.width;
                    int rtH = m_multiTerrainOperation.RTheightmap.height;
                    RenderTextureFormat rtF = RenderTextureFormat.RFloat;
                    RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(rtW, rtH, rtF);
                    RenderTexture noiseRT = RenderTexture.GetTemporary(rtDesc);

                    RenderTexture tempRT = RenderTexture.GetTemporary(noiseRT.descriptor);
                    RenderTexture prev = RenderTexture.active;
                    RenderTexture.active = tempRT;

                    int noisePass = NoiseUtils.kNumBlitPasses * NoiseLib.GetNoiseIndex(m_noiseSettings.domainSettings.noiseTypeName);

                    Graphics.Blit(tempRT, matNoise, noisePass);

                    RenderTexture.active = noiseRT;

                    Graphics.Blit(tempRT, noiseRT);

                    RenderTexture.active = prev;

                    RenderTexture.ReleaseTemporary(tempRT);

                    //now that we have the noise, put it in a simple image mask operation to get the final result
                    filterMat.SetTexture("_ImageMaskTex", noiseRT);
                    //ImageProcessing.DebugWriteRenderTexture("D:\\input.png",inputTexture);
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_ImageMaskTex", null);
                    //m_noiseSettings.transformSettings.translation = new Vector3(originalTranslation.x, originalTranslation.y, originalTranslation.z);
                    RenderTexture.ReleaseTemporary(noiseRT);

                    break;
                case ImageMaskOperation.CollisionMask:
                    //fetch the combined collision mask from the paint context
                    //RenderTexture collisionMaskTexture = RenderTexture.GetTemporary(inputTexture.descriptor);
                    //RenderTexture currentRT = RenderTexture.active;
                    //RenderTexture.active = collisionMaskTexture;
                    //GL.Clear(true, true, Color.white);
                    //RenderTexture.active = currentRT;
                    RenderTexture.active = currentRT;
                    m_multiTerrainOperation.GetCollisionMask(m_collisionMasks);

                    //ImageProcessing.WriteRenderTexture("D:\\input.png", inputTexture);
                    //ImageProcessing.WriteRenderTexture("D:\\RTcollision.png", m_multiTerrainOperation.RTcollision);

                    filterMat.SetTexture("_ImageMaskTex", m_multiTerrainOperation.RTbakedMask);
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_ImageMaskTex", null);

                    //ImageProcessing.WriteRenderTexture("D:\\output.png", outputTexture);

                    break;
                case ImageMaskOperation.StrengthTransform:
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    break;

                case ImageMaskOperation.PROHydraulicErosion:
#if GAIA_PRO_PRESENT
                    m_multiTerrainOperation.RTheightmap.filterMode = FilterMode.Bilinear;
                    Material erosionMat = new Material(Shader.Find("Hidden/GaiaPro/SimpleHeightBlend"));
                    if (m_Eroder == null)
                    {
                        m_Eroder = new HydraulicEroder();
                        m_Eroder.OnEnable();
                    }
                    //ImageProcessing.WriteRenderTexture("D:\\ErosionHeightInput.png", m_multiTerrainOperation.RTheightmap);
                    m_Eroder.inputTextures["Height"] = m_multiTerrainOperation.RTheightmap;

                    Vector2 texelSize = new Vector2(m_multiTerrainOperation.m_originTerrain.terrainData.size.x / m_multiTerrainOperation.m_originTerrain.terrainData.heightmapResolution,
                                                    m_multiTerrainOperation.m_originTerrain.terrainData.size.z / m_multiTerrainOperation.m_originTerrain.terrainData.heightmapResolution);

                    //apply Erosion settings
                    m_Eroder.m_ErosionSettings.m_SimScale.value = m_erosionSimScale;
                    m_Eroder.m_ErosionSettings.m_HydroTimeDelta.value = m_erosionHydroTimeDelta;
                    m_Eroder.m_ErosionSettings.m_HydroIterations.value = m_erosionHydroIterations;
                    m_Eroder.m_ErosionSettings.m_ThermalTimeDelta = m_erosionThermalTimeDelta;
                    m_Eroder.m_ErosionSettings.m_ThermalIterations = m_erosionThermalIterations;
                    m_Eroder.m_ErosionSettings.m_ThermalReposeAngle = m_erosionThermalReposeAngle;
                    m_Eroder.m_ErosionSettings.m_PrecipRate.value = m_erosionPrecipRate;
                    m_Eroder.m_ErosionSettings.m_EvaporationRate.value = m_erosionEvaporationRate;
                    m_Eroder.m_ErosionSettings.m_FlowRate.value = m_erosionFlowRate;
                    m_Eroder.m_ErosionSettings.m_SedimentCapacity.value = m_erosionSedimentCapacity;
                    m_Eroder.m_ErosionSettings.m_SedimentDepositRate.value = m_erosionSedimentDepositRate;
                    m_Eroder.m_ErosionSettings.m_SedimentDissolveRate.value = m_erosionSedimentDissolveRate;
                    m_Eroder.m_ErosionSettings.m_RiverBankDepositRate.value = m_erosionRiverBankDepositRate;
                    m_Eroder.m_ErosionSettings.m_RiverBankDissolveRate.value = m_erosionRiverBankDissolveRate;
                    m_Eroder.m_ErosionSettings.m_RiverBedDepositRate.value = m_erosionRiverBedDepositRate;
                    m_Eroder.m_ErosionSettings.m_RiverBedDissolveRate.value = m_erosionRiverBedDissolveRate;

                    //and erode
                    m_Eroder.ErodeHeightmap(m_multiTerrainOperation.m_originTerrain.terrainData.size, m_multiTerrainOperation.m_terrainDetailBrushTransform.GetBrushXYBounds(), texelSize, false);
                    Vector4 erosionBrushParams = new Vector4(1f, 0.0f, 0.0f, 0.0f);
                    //if (activeLocalFilters)
                    erosionMat.SetTexture("_BrushTex", inputTexture);
                    //else
                    //    erosionMat.SetTexture("_BrushTex", localinputTexture);
                    switch (m_erosionMaskOutput)
                    {
                        //case GaiaConstants.ErosionMaskOutput.ErodedSediment:
                        //    erosionMat.SetTexture("_NewHeightTex", m_Eroder.outputTextures["Eroded Sediment"]);
                        //    break;
                        //case GaiaConstants.ErosionMaskOutput.Height:
                        //    erosionMat.SetTexture("_NewHeightTex", m_Eroder.outputTextures["Height"]);
                        //    break;
                        case GaiaConstants.ErosionMaskOutput.Sediment:
                            erosionMat.SetTexture("_NewHeightTex", m_Eroder.outputTextures["Sediment"]);
                            break;
                        case GaiaConstants.ErosionMaskOutput.WaterFlux:
                            erosionMat.SetTexture("_NewHeightTex", m_Eroder.outputTextures["Water Flux"]);
                            break;
                        //case GaiaConstants.ErosionMaskOutput.WaterLevel:
                        //    erosionMat.SetTexture("_NewHeightTex", m_Eroder.outputTextures["Water Level"]);
                        //    break;
                        case GaiaConstants.ErosionMaskOutput.WaterVelocity:
                            erosionMat.SetTexture("_NewHeightTex", m_Eroder.outputTextures["Water Velocity"]);
                            break;
                    }

                    erosionMat.SetVector("_BrushParams", erosionBrushParams);

                    RenderTexture eroderTempRT = RenderTexture.GetTemporary(m_Eroder.outputTextures["Height"].descriptor);

                    m_multiTerrainOperation.SetupMaterialProperties(erosionMat, MultiTerrainOperationType.Heightmap);
                    Graphics.Blit(m_multiTerrainOperation.RTheightmap, eroderTempRT, erosionMat, 0);

                    filterMat.SetTexture("_InputTex", eroderTempRT);
                    Graphics.Blit(eroderTempRT, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_InputTex", null);
                    erosionMat.SetTexture("_NewHeightTex", null);
                    m_Eroder.ReleaseRenderTextures();
                    RenderTexture.ReleaseTemporary(eroderTempRT);
                    //ImageProcessing.WriteRenderTexture("D:\\ErosionOutput.png", outputTexture);
#else
                    Graphics.Blit(inputTexture, outputTexture);
#endif
                    break;
                case ImageMaskOperation.Smooth:
                    Vector4 brushParams = new Vector4(Mathf.Clamp(m_strength, 0.0f, 1.0f), 0.0f, 0.0f, 0.0f);
                    //ImageProcessing.WriteRenderTexture("D:\\inputTexture.png", inputTexture);
                    filterMat.SetTexture("_MainTex", inputTexture);
                    filterMat.SetTexture("_BrushTex", Texture2D.whiteTexture);
                    filterMat.SetTexture("_HeightTransformTex", strengthTransformCurveTexture);
                    filterMat.SetVector("_BrushParams", brushParams);
                    Vector4 smoothWeights = new Vector4(
                        Mathf.Clamp01(1.0f - Mathf.Abs(m_smoothVerticality)),   // centered
                        Mathf.Clamp01(-m_smoothVerticality),                    // min
                        Mathf.Clamp01(m_smoothVerticality),                     // max
                        m_smoothBlurRadius);                                  // kernel size
                    filterMat.SetVector("_SmoothWeights", smoothWeights);

                    m_multiTerrainOperation.SetupMaterialProperties(filterMat, MultiTerrainOperationType.Heightmap);

                    // Two pass blur (first horizontal, then vertical)
                    //RenderTexture workaround1 = RenderTexture.GetTemporary(m_multiTerrainOperation.RTheightmap.descriptor);
                    RenderTexture tmpsmoothRT = new RenderTexture(m_multiTerrainOperation.RTheightmap.descriptor);
                    //tmpsmoothRT = RenderTexture.GetTemporary(m_multiTerrainOperation.RTheightmap.descriptor);
                    Graphics.Blit(inputTexture, tmpsmoothRT, filterMat, 0);
                    Graphics.Blit(tmpsmoothRT, outputTexture, filterMat, 1);

                    filterMat.SetTexture("_MainTex", null);
                    filterMat.SetTexture("_BrushTex", null);
                    filterMat.SetTexture("_HeightTransformTex", null);

                    tmpsmoothRT.Release();
                    GameObject.DestroyImmediate(tmpsmoothRT);

                    //RenderTexture.ReleaseTemporary(tmpsmoothRT);
                    //RenderTexture.ReleaseTemporary(workaround1);


                    break;
                case ImageMaskOperation.TerrainTexture:

                    //check if ID is valid within layer range
                    //if (m_textureLayerId < 0 || m_textureLayerId > m_multiTerrainOperation.m_originTerrain.terrainData.terrainLayers.Length - 1)
                    //{
                    //    Debug.LogWarning("Did not find the texture selected in the texture mask on the origin terrain, the texture mask will be ignored");
                    //    return inputTexture;
                    //}
                    SpawnRule sr = m_allTextureSpawnRules.FirstOrDefault(x => x.GUID == m_textureMaskSpawnRuleGUID);
                    if (sr != null)
                    {
                        Spawner spawner = m_allTextureSpawners.FirstOrDefault(x => x.m_settings.m_spawnerRules.Contains(sr));
                        if (spawner != null)
                        {
                            ResourceProtoTexture proto = spawner.m_settings.m_resources.m_texturePrototypes[sr.m_resourceIdx];
                            TerrainLayer layer = TerrainHelper.GetLayerFromPrototype(proto);
                            m_multiTerrainOperation.GetSplatmap(layer);
                            filterMat.SetTexture("_ImageMaskTex", m_multiTerrainOperation.RTtextureSplatmap);
                            Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                            filterMat.SetTexture("_ImageMaskTex", null);
                        }
                    }
                    break;
                case ImageMaskOperation.PROConcaveConvex:
#if GAIA_PRO_PRESENT
                    m_concavityShader = (ComputeShader)(Resources.Load("GaiaConcavity"));
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    int kidx = m_concavityShader.FindKernel("ConcavityMultiply");

                    switch (m_blendMode)
                    {
                        case ImageMaskBlendMode.GreaterThan:
                            kidx = m_concavityShader.FindKernel("ConcavityGreaterThan");
                            break;
                        case ImageMaskBlendMode.SmallerThan:
                            kidx = m_concavityShader.FindKernel("ConcavitySmallerThan");
                            break;
                        case ImageMaskBlendMode.Add:
                            kidx = m_concavityShader.FindKernel("ConcavityAdd");
                            break;
                        case ImageMaskBlendMode.Subtract:
                            kidx = m_concavityShader.FindKernel("ConcavitySubtract");
                            break;
                    }

                    m_concavityShader.SetTexture(kidx, "In_BaseMaskTex", inputTexture);
                    //ImageProcessing.WriteRenderTexture("D:\\tempOutput.png", m_multiTerrainOperation.RTheightmap);

                    m_concavityShader.SetTexture(kidx, "In_HeightTex", m_multiTerrainOperation.RTheightmap);
                    m_concavityShader.SetTexture(kidx, "In_HeightTransformTex", strengthTransformCurveTexture);
                    m_concavityShader.SetInt("HeightTransformTexResolution", strengthTransformCurveTexture.width - 1);
                    m_concavityShader.SetTexture(kidx, "OutputTex", outputTexture);
                    //cs.SetTexture(kidx, "RemapTex", remapTex);

                    m_concavityShader.SetVector("HeightmapResolution", new Vector2(m_multiTerrainOperation.RTheightmap.width, m_multiTerrainOperation.RTheightmap.height));
                    m_concavityShader.SetVector("TextureResolution", new Vector4(inputTexture.width, inputTexture.height, m_concavityFeatureSize, m_concavity));
                    m_concavityShader.Dispatch(kidx, outputTexture.width, outputTexture.height, 1);
                    //Workaround - the output texture of the compute shader can turn up empty sometimes when used directly in the image mask afterwards

                    //filterMat.SetTexture("_InputTexture", concavityTemp2);
                    //filterMat.SetTexture("_ImageMaskTex", inputTexture);
                    //ImageProcessing.WriteRenderTexture("D:\\tempOutput.png", outputTexture);
                    //Graphics.Blit(concavityTemp2, outputTexture);//, filterMat, (int)m_blendMode);
                    //RenderTexture.ReleaseTemporary(concavityTemp1);
                    //RenderTexture.ReleaseTemporary(concavityTemp2);
                    m_concavityShader = null;
#else
                    Graphics.Blit(inputTexture, outputTexture);
#endif
                    break;
                case ImageMaskOperation.WorldBiomeMask:
                    //fetch the world biome mask
                    //RenderTexture.active = currentRT;
                    m_multiTerrainOperation.GetWorldBiomeMask(m_selectedWorldBiomeMaskGUID);

                    //ImageProcessing.WriteRenderTexture("D:\\input.png", inputTexture);
                    //ImageProcessing.WriteRenderTexture("D:\\RTcollision.png", m_multiTerrainOperation.RTcollision);

                    filterMat.SetTexture("_ImageMaskTex", m_multiTerrainOperation.RTbakedMask);
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_ImageMaskTex", null);

                    //ImageProcessing.WriteRenderTexture("D:\\output.png", outputTexture);

                    break;

                default:
                    break;
            }

            filterMat.SetTexture("_InputTex", null);
            filterMat.SetTexture("_HeightTransformTex", null);

            GameObject.DestroyImmediate(filterMat);

            //release input texture
            //if (inputTexture != null)
            //{
            //    inputTexture.Release();
            //    GameObject.DestroyImmediate(inputTexture);
            //    inputTexture = null;
            //}
#endif
            return outputTexture;

        }


        public void ClearEroder()
        {
#if UNITY_EDITOR && GAIA_PRO_PRESENT
            if (m_Eroder != null)
            {
                m_Eroder.ReleaseRenderTextures();
                m_Eroder = null;
            }
#endif
        }

        private float CalculateScalarHeightRelativeToSeaLevel(float heightValue, float scalarSeaLevel)
        {
            if (heightValue < 0.25f)
            {
                //The position is below the marked sea level on the slider -> lerp accordingly
                heightValue = Mathf.Lerp(0f, scalarSeaLevel, Mathf.InverseLerp(0, 0.25f, heightValue));
            }
            else
            {
                //The position is above the marked sea level on the slider -> lerp accordingly
                heightValue = Mathf.Lerp(scalarSeaLevel, 1f, Mathf.InverseLerp(0.25f, 1f, heightValue));
            }

            return heightValue;
        }

        /// <summary>
        /// sets up the default linear strenght transform curve that maps the input 1:1 to the output (equals no transformation at all)
        /// </summary>
        /// <param name="max">The maximum value at which the curve ends.</param>
        /// <returns></returns>
        public static AnimationCurve NewAnimCurveStraightUpwards(float max = 1f)
        {
            return new AnimationCurve(new Keyframe[2] { new Keyframe() {
                                                                        inTangent = 1,
                                                                        inWeight = 0,
                                                                        outTangent = 1,
                                                                        outWeight = 0,
                                                                        time = 0,
                                                                        value = 0,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = 1,
                                                                        inWeight = 0,
                                                                        outTangent = 1,
                                                                        outWeight = 0,
                                                                        time = 1,
                                                                        value = max,
                                                                        weightedMode = WeightedMode.None
                                                                        } }); ;
        }
        /// <summary>
        /// Checks if the current strenght transform curve is still the default linear curve set at initialization
        /// </summary>
        /// <returns>true if original curve, false if the user altered it</returns>
        private bool IsDefaultStrenghtCurve()
        {
            //Get a default anim curve for comparison
            AnimationCurve defaultCurve = NewAnimCurveStraightUpwards();

            //different number of keys? => it is a different curve
            if (m_strengthTransformCurve.keys.Length != defaultCurve.keys.Length)
            {
                return false;
            }
            //keyframe data different from original? => it is a different curve
            for (int i = 0; i < m_strengthTransformCurve.keys.Length; i++)
            {
                Keyframe current = m_strengthTransformCurve.keys[i];
                Keyframe original = defaultCurve.keys[i];
                if (current.inTangent != original.inTangent ||
                    current.inWeight != original.inWeight ||
                    current.outTangent != original.outTangent ||
                    current.outWeight != original.outWeight ||
                    current.time != original.time ||
                    current.value != original.value ||
                    current.weightedMode != original.weightedMode)
                {
                    return false;
                }
            }

            return true;
        }



        /// <summary>
        /// Sets up a distance map curve suitable for the "water border" style for the base map creation in the random terrain generator.
        /// <returns></returns>
        public static AnimationCurve NewAnimCurveWaterBorder()
        {
            AnimationCurve returnCurve = new AnimationCurve(new Keyframe[2] { new Keyframe() {
                                                                        inTangent = 0,
                                                                        inWeight = 0,
                                                                        outTangent = 0,
                                                                        outWeight = 0,
                                                                        time = 0,
                                                                        value = 1,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = -3.183739f,
                                                                        inWeight = 0.02412868f,
                                                                        outTangent = -3.183739f,
                                                                        outWeight = 0,
                                                                        time = 1f,
                                                                        value = 0,
                                                                        weightedMode = WeightedMode.None
                                                                    }
                                                       }); ;
            return returnCurve;
        }

        /// <summary>
        /// Sets up a distance map curve suitable for the "mountain border" style for the base map creation in the random terrain generator.
        /// <returns></returns>
        public static AnimationCurve NewAnimCurveMountainBorderDistance()
        {
            AnimationCurve returnCurve = new AnimationCurve(new Keyframe[2] { new Keyframe() {
                                                                        inTangent = 0,
                                                                        inWeight = 0,
                                                                        outTangent = 0,
                                                                        outWeight = 0,
                                                                        time = 0,
                                                                        value = 0.5f,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = 1.301094f,
                                                                        inWeight = 0.04557639f,
                                                                        outTangent = 1.301094f,
                                                                        outWeight = 0,
                                                                        time = 1f,
                                                                        value = 1f,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                       }); ;
            return returnCurve;
        }

        public static AnimationCurve NewAnimCurveMountainBorderStrength()
        {
            AnimationCurve returnCurve = new AnimationCurve(new Keyframe[3] { new Keyframe() {
                                                                        inTangent = 1.102063f,
                                                                        inWeight = 0,
                                                                        outTangent = 1.102063f,
                                                                        outWeight = 0.2820168f,
                                                                        time = 0,
                                                                        value = 0f,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = 2.96729f,
                                                                        inWeight = 0.08592079f,
                                                                        outTangent = 2.96729f,
                                                                        outWeight = 0.2352394f,
                                                                        time = 0.6f,
                                                                        value = 1f,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = 2.606707f,
                                                                        inWeight = 0.1011895f,
                                                                        outTangent = 2.606707f,
                                                                        outWeight = 0f,
                                                                        time = 1f,
                                                                        value = 2f,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                       }); ;
            return returnCurve;
        }



        /// <summary>
        /// Sets up a better strenght curve for noise that has a steeper cutoff in strength.
        /// This curve is better suited to get small islands or patches of noise.
        /// </summary>
        /// <param name="distanceFromCenter">How far from the center of the curve the cutoff should take place. The smaller this value is, the "sharper" the cutoff will be for the noise pattern</param>
        /// <returns></returns>
        public static AnimationCurve NewAnimCurveDefaultNoise(float distanceFromCenter = 0.2f)
        {
            AnimationCurve returnCurve = new AnimationCurve(new Keyframe[4] { new Keyframe() {
                                                                        inTangent = 0,
                                                                        inWeight = 0,
                                                                        outTangent = 0,
                                                                        outWeight = 0,
                                                                        time = 0,
                                                                        value = 0,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = 0,
                                                                        inWeight = 0f,
                                                                        outTangent = 2.5f,
                                                                        outWeight = 0.3333333f,
                                                                        time = 0.5f - distanceFromCenter,
                                                                        value = 0,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = 2.5f,
                                                                        inWeight = 0f,
                                                                        outTangent = 0,
                                                                        outWeight = 0.3333333f,
                                                                        time = 0.5f + distanceFromCenter,
                                                                        value = 1,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = 0,
                                                                        inWeight = 0,
                                                                        outTangent = 0,
                                                                        outWeight = 0,
                                                                        time = 1,
                                                                        value = 1,
                                                                        weightedMode = WeightedMode.None
                                                                        } }); ;
            return returnCurve;
        }

        private Material GetCurrentFXFilterMaterial()
        {
            string shaderName = "";

            switch (m_operation)
            {
                case ImageMaskOperation.ImageMask:
                    shaderName = "Hidden/Gaia/FilterImageMask";
                    break;
                case ImageMaskOperation.DistanceMask:
                    shaderName = "Hidden/Gaia/FilterDistanceMask";
                    break;
                case ImageMaskOperation.HeightMask:
                    shaderName = "Hidden/Gaia/FilterHeightMask";
                    break;
                case ImageMaskOperation.SlopeMask:
                    shaderName = "Hidden/Gaia/FilterSlopeMask";
                    break;
                case ImageMaskOperation.NoiseMask:
                    //We need the FilterImageMask as material for the final operation AFTER the noise has been calculated
                    //For the shader that creates the noise itself, see the implementation of the noise mask operation
                    //in Apply()
                    shaderName = "Hidden/Gaia/FilterImageMask";
                    break;
                case ImageMaskOperation.CollisionMask:
                    //We need the FilterImageMask as material for the final operation AFTER the Collision mask has been gathered from the operation.
                    //For collection & assembly of the collision mask data, see the implementation of the collision mask operation
                    //in Apply()
                    shaderName = "Hidden/Gaia/FilterImageMask";
                    break;
                case ImageMaskOperation.StrengthTransform:
                    shaderName = "Hidden/GaiaPro/StrengthTransform";
                    break;
                case ImageMaskOperation.PROHydraulicErosion:
                    //We need the Strength Transform as material for the final operation AFTER the Erosion mask has been gathered from the Eroder.
                    //For collection & assembly of the collision mask data, see the implementation of the erosion mask operation
                    //in Apply()
                    shaderName = "Hidden/GaiaPro/StrengthTransform";
                    break;
                case ImageMaskOperation.Smooth:
                    shaderName = "Hidden/Gaia/SmoothHeight";
                    break;
                case ImageMaskOperation.TerrainTexture:
                    //Here we just load the splatmap input into an image mask
                    shaderName = "Hidden/Gaia/FilterImageMask";
                    break;
                case ImageMaskOperation.PROConcaveConvex:
                    //Concave / Convex is calculated in compute shader, we use the strength transform as a workaround in this case
                    shaderName = "Hidden/GaiaPro/StrengthTransform";
                    break;
                case ImageMaskOperation.WorldBiomeMask:
                    //We need the FilterImageMask as material for the final operation AFTER the World Biome mask has been gathered from the operation.
                    //For collection & assembly of the world biome mask data, see the implementation of the world biome mask operation
                    //in Apply()
                    shaderName = "Hidden/Gaia/FilterImageMask";
                    break;
                default:
                    break;

            }

            if (shaderName == "")
            {
                return null;
            }

            return new Material(Shader.Find(shaderName));
        }

        public static void RefreshSpawnRuleGUIDs()
        {
            List<SpawnRule> tempTextureSpawnRules = new List<SpawnRule>();
            List<Spawner> tempTextureSpawner = new List<Spawner>();
            List<string> tempTextureSpawnRuleNames = new List<string>();


            List<SpawnRule> tempTreeSpawnRules = new List<SpawnRule>();
            List<Spawner> tempTreeSpawner = new List<Spawner>();
            List<string> tempTreeSpawnRuleNames = new List<string>();

            Spawner[] allSpawner = Resources.FindObjectsOfTypeAll<Spawner>();
            foreach (Spawner spawner in allSpawner)
            {
                foreach (SpawnRule sr in spawner.m_settings.m_spawnerRules)
                {
                    if (sr.m_resourceType == GaiaConstants.SpawnerResourceType.TerrainTexture)
                    {
                        tempTextureSpawnRules.Add(sr);
                        tempTextureSpawnRuleNames.Add(sr.m_name);
                        if (!tempTextureSpawner.Contains(spawner))
                        {
                            tempTextureSpawner.Add(spawner);
                        }
                    }
                    if (sr.m_resourceType == GaiaConstants.SpawnerResourceType.TerrainTree)
                    {
                        tempTreeSpawnRules.Add(sr);
                        tempTreeSpawnRuleNames.Add(sr.m_name);
                        if (!tempTreeSpawner.Contains(spawner))
                        {
                            tempTreeSpawner.Add(spawner);
                        }
                    }
                }
            }
            m_allTextureSpawnRuleIndices = Enumerable
                                               .Repeat(0, (int)((tempTextureSpawnRules.Count - 0) / 1) + 1)
                                               .Select((tr, ti) => tr + (1 * ti))
                                               .ToArray();

            CollisionMask.m_allTreeSpawnRuleIndices = Enumerable
                                               .Repeat(0, (int)((tempTreeSpawnRules.Count - 0) / 1) + 1)
                                               .Select((tr, ti) => tr + (1 * ti))
                                               .ToArray();

            CollisionMask.m_allTreeSpawnRules = tempTreeSpawnRules.ToArray();
            CollisionMask.m_allTreeSpawners = tempTreeSpawner.ToArray();
            CollisionMask.m_allTreeSpawnRuleNames = tempTreeSpawnRuleNames.ToArray();

            m_allTextureSpawnRules = tempTextureSpawnRules.ToArray();
            m_allTextureSpawners = tempTextureSpawner.ToArray();
            m_allTextureSpawnRuleNames = tempTextureSpawnRuleNames.ToArray();
        }

        public static ImageMask Clone(ImageMask source)
        {

            ImageMask target = new ImageMask();
#if UNITY_EDITOR
            GaiaUtils.CopyFields(source, target);

            //special treatment for the heightmask min max fields - those are properites which will not be copied by the field copy above
            target.m_absoluteHeightMax = source.m_absoluteHeightMax;
            target.m_absoluteHeightMin = source.m_absoluteHeightMin;


            //special treatment for all object fields
            target.m_distanceMaskCurve = new AnimationCurve(source.m_distanceMaskCurve.keys);
            target.m_heightMaskCurve = new AnimationCurve(source.m_heightMaskCurve.keys);
            target.m_slopeMaskCurve = new AnimationCurve(source.m_slopeMaskCurve.keys);
            target.m_strengthTransformCurve = new AnimationCurve(source.m_strengthTransformCurve.keys);

            if (source.m_gaiaNoiseSettings != null)
            {
                target.m_gaiaNoiseSettings = new GaiaNoiseSettings();
                GaiaUtils.CopyFields(source.m_gaiaNoiseSettings, target.m_gaiaNoiseSettings);
            }

            if (source.m_noiseSettings != null)
            {
                target.m_noiseSettings = (NoiseSettings)ScriptableObject.CreateInstance(typeof(NoiseSettings));
                GaiaUtils.CopyFields(source.m_noiseSettings, target.m_noiseSettings);
            }


            target.m_noiseToolSettings = new NoiseToolSettings();
            GaiaUtils.CopyFields(source.m_noiseToolSettings, target.m_noiseToolSettings);

            target.noiseSettingsGUI = null;

            target.m_collisionMasks = new CollisionMask[source.m_collisionMasks.Length];
            //Clone all collision masks as well
            for (int i = 0; i < target.m_collisionMasks.Length; i++)
            {
                target.m_collisionMasks[i] = new CollisionMask();
                GaiaUtils.CopyFields(source.m_collisionMasks[i], target.m_collisionMasks[i]);
            }



            //GaiaUtils.CopyFields(source.m_distanceMaskCurve, target.m_distanceMaskCurve); 
#endif
            return target;

        }


    }

}




