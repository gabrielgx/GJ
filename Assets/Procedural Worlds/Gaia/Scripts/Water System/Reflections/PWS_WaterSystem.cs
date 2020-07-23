using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Gaia;
using UnityEngine.Rendering;

namespace ProcedualWorlds.WaterSystem
{
    /// <summary>
    /// Generates a water reflection.
    /// </summary>
    [ExecuteAlways]
    public class PWS_WaterSystem : MonoBehaviour
    {
        #region PublicVariables
        public static PWS_WaterSystem Instance
        {
            get { return m_instance; }
        }
        [SerializeField]
        private static PWS_WaterSystem m_instance;
        public List<float> ActiveTerrainsDetailDistance = new List<float>();
        public GaiaConstants.EnvironmentRenderer RenderPipeline;
        /// <summary>
        /// Water Profile
        /// </summary>
        public GaiaWaterProfile m_waterProfile;
        public GaiaWaterProfileValues m_waterProfileValues;
        /// <summary>
        /// Sets the player object
        /// </summary>
        public Transform m_player;
        /// <summary>
        /// Allow MSAA.
        /// </summary>
        public bool m_MSAA;
        /// <summary>
        /// Allow HDR.
        /// </summary>
        public bool m_HDR;
        /// <summary>
        /// To decided if shadows are to be rendered
        /// </summary>
        public bool m_shadowRender;
        /// <summary>
        /// Use a custom render path
        /// </summary>
        public bool m_customRenderPath;
        /// <summary>
        /// To decided if shadows are to be rendered
        /// </summary>
        public bool m_skyboxOnly;
        /// <summary>
        /// Enables the use of m_refreshRate (in seconds)
        /// </summary>
        public bool m_useRefreshTime;
        /// <summary>
        /// Custom reflection distance
        /// </summary>
        public bool m_customReflectionDistance;
        public bool m_customReflectionDistances;
        /// <summary>
        /// To decided if pixel lights are to be rendered.
        /// </summary>
        public bool m_disablePixelLights;
        /// <summary>
        /// To decide if the reflections camera should be hidden
        /// </summary>
        public bool m_hideReflectionCamera = true;
        /// <summary>
        /// The texture size to use for rendering.
        /// </summary>
        public int m_renderTextureSize = 256;
        /// <summary>
        /// Refresh rate that can be used with FixedUpdate, Update, OnRender
        /// </summary>
        public float m_refreshRate = 0.25f;
        /// <summary>
        /// Render Distance of the reflections camera
        /// </summary>
        public float m_renderDistance = 150f;
        /// <summary>
        /// Clip plane offset to use.
        /// </summary>
        public float m_clipPlaneOffset = 0.07f;
        /// <summary>
        /// Update theshold for the InvokeRepeating.
        /// </summary>
        public float m_updateThreshold = 0.5f;
        /// <summary>
        /// Camera used to find orentation of reflection.
        /// </summary>
        public Camera m_RenderCamera;
        /// <summary>
        /// LayerMask used for what to actual render.
        /// </summary>
        public LayerMask m_reflectionLayers = -1;
        /// <summary>
        /// Update method used for deciding what update method to use.
        /// </summary>
        public RenderUpdateMode m_RenderUpdate;
        /// <summary>
        /// Render path to use for rendering of the camera.
        /// </summary>
        public RenderingPath m_RenderPath;
        /// <summary>
        /// Distances check
        /// </summary>
        public float[] m_distances = new float[32];
        //[HideInInspector]
        public List<Material> m_waterMaterialInstances = new List<Material>();

        //Disable Height
        public bool m_enableDisabeHeightFeature = true;
        public float m_disableHeight = 100f;

        public float m_currentSmoothnes;
        #if UNITY_EDITOR
        public GaiaSettings m_gaiaSettings;
        #endif
        public Color m_specColor;
        public Gradient m_currentGradient;

        [HideInInspector]
        public Texture2D m_waterTexture;
        [HideInInspector]
        public Light SunLight;
        [HideInInspector]
        public float SeaLevel = 0f;

        #endregion

        #region PrivateVariables

        private int m_renderID;
        public bool m_ableToRender = true;
        private bool m_rebuild;
        private int m_oldRenderTextureSize;
        private float m_oldShadowDistance;
        private float m_tempTime;
        private Vector3 m_worldPosition;
        private Vector3 m_normal;
        private Vector4 m_reflectionPlane;
        private Vector3 m_oldPosition;
        private Vector3 m_newPosition;
        private Vector3 m_euler;
        private Vector4 m_clipPlane;
        private Vector3 m_currentPosition;
        private Vector3 m_currentRotation;
        private Matrix4x4 m_projection;
        private Matrix4x4 m_reflection;
        [SerializeField]
        private RenderTexture m_reflectionTexture;
        [SerializeField]
        private Camera m_reflectionCamera;
        #if UNITY_EDITOR
        [SerializeField]
        private GaiaSessionManager m_gaiaSession;
        #endif
        [SerializeField]
        private Material m_waterMaterial;
        [SerializeField]
        private MeshRenderer m_waterRenderer;
        [SerializeField]
        private Color32 m_lightSpecColor = new Color32(68, 68, 68, 255);

        #endregion

        #region Unity Methods

        /// <summary>
        /// On awake sets up anything that could have changed since enable.
        /// </summary>
        private void Start()
        {
            LoadFromProfile();
            m_instance = this;
            foreach (var terrain in Terrain.activeTerrains)
            {
                ActiveTerrainsDetailDistance.Add(terrain.detailObjectDistance);
            }

            if (m_player == null)
            {
                if (Camera.main != null)
                {
                    m_player = Camera.main.transform;
                }
            }

            m_RenderCamera = GaiaUtils.GetCamera();

            Generate();
        }

        /// <summary>
        /// Update is called every frame
        /// </summary>
        private void Update()
        {
            if (m_waterProfile == null)
            {
                #if UNITY_EDITOR
                if (m_gaiaSettings != null)
                {
                    m_waterProfile = m_gaiaSettings.m_gaiaWaterProfile;
                }
                #endif
            }

            if (m_RenderCamera == null)
            {
                m_RenderCamera = Camera.main;
            }

            if (!Application.isPlaying)
            {
                #if UNITY_EDITOR
                if (m_gaiaSession != null)
                {
                    SeaLevel = m_gaiaSession.GetSeaLevel(SeaLevel);
                }
                #endif
            }

            if (m_player != null)
            {
                gameObject.transform.position = new Vector3(m_player.position.x, SeaLevel, m_player.position.z);
            }

            if (m_waterProfile != null)
            {
                #if UNITY_EDITOR
                m_waterProfileValues = m_waterProfile.m_waterProfiles[m_waterProfile.m_selectedWaterProfileValuesIndex];
#endif
            }

            if (m_waterProfileValues != null)
            {
                BuildWaterColorDepth();
            }
            else
            {
                Debug.LogError("Profile Values not found");
            }

            BuildUpdateModeReflections();
            UpdateShaderValues();
            CalculateSmoothnessRay(m_RenderCamera.farClipPlane);
        }

        /// <summary>
        /// OnWillRenderObject allows us to get a editor reflection working,
        /// Aswell as provides another way to refresh that is more stable.
        /// </summary>
        private void OnWillRenderObject()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                if (SceneView.lastActiveSceneView == null)
                {
                    return;
                }

                m_RenderCamera = SceneView.lastActiveSceneView.camera;
                if (m_RenderCamera != null)
                {
                    if (m_RenderCamera.transform.position != m_currentPosition || m_RenderCamera.transform.rotation.eulerAngles != m_currentRotation)
                    {
                        if (m_RenderCamera == null)
                        {
                            m_RenderCamera = GaiaUtils.GetCamera();
                        }
                        GenerateCamera();
                        ResyncCameraSettings();
                        UpdateCameraModes();
                        BuildReflection(m_skyboxOnly);
                        m_currentPosition = m_RenderCamera.transform.position;
                        m_currentRotation = m_RenderCamera.transform.rotation.eulerAngles;
                    }
                }
            }
#endif
            if (m_RenderUpdate == RenderUpdateMode.OnRender && Application.isPlaying)
            {
                if (m_RenderCamera != null)
                {
                    if (m_RenderCamera.transform.position != m_currentPosition || m_RenderCamera.transform.rotation.eulerAngles != m_currentRotation)
                    {
                        if (m_useRefreshTime)
                        {
                            if (Time.time >= m_tempTime)
                            {
                                m_tempTime += m_refreshRate;
                                BuildReflection(m_skyboxOnly);
                                m_currentPosition = m_RenderCamera.transform.position;
                                m_currentRotation = m_RenderCamera.transform.rotation.eulerAngles;
                            }
                        }
                        else
                        {
                            BuildReflection(m_skyboxOnly);
                            m_currentPosition = m_RenderCamera.transform.position;
                            m_currentRotation = m_RenderCamera.transform.rotation.eulerAngles;
                        }
                    }
                }
                else
                {
                    if (m_RenderCamera == null)
                    {
                        m_RenderCamera = GaiaUtils.GetCamera();
                    }
                }
            }

            if (!Application.isPlaying)
            {
                CalculateSmoothnessRay(m_RenderCamera.farClipPlane);
            }
        }

        /// <summary>
        /// OnDisable ClearData
        /// </summary>
        private void OnDisable()
        {
            m_rebuild = true;
            ClearData();
        }

        /// <summary>
        /// On enable rebuild the required data
        /// </summary>
        private void OnEnable()
        {
#if UNITY_EDITOR
            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = GaiaUtils.GetGaiaSettings();
            }

            if (m_gaiaSession == null)
            {
                m_gaiaSession = FindObjectOfType<GaiaSessionManager>();
            }

            if (m_gaiaSession != null)
            {
                SeaLevel = m_gaiaSession.GetSeaLevel(SeaLevel);
            }
#endif
            m_instance = this;

            m_currentGradient = new Gradient();

            m_rebuild = true;
            Generate();
            m_rebuild = false;
        }

        /// <summary>
        /// OnDestroy ClearData
        /// </summary>
        private void OnDestroy()
        {
            ClearData();
        }

        #endregion

        #region Private Functions

        /// <summary>
        /// Creates water material instances
        /// </summary>
        public void CreateWaterMaterialInstances(List<Material> materials)
        {
            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                m_waterMaterialInstances.Clear();
                for (int i = 0; i < materials.Count; i++)
                {
                    Material material = new Material(Shader.Find(materials[i].shader.name));
                    material.CopyPropertiesFromMaterial(materials[i]);
                    m_waterMaterialInstances.Add(material);
                }

                meshRenderer.sharedMaterials = m_waterMaterialInstances.ToArray();
                if (m_waterMaterialInstances.Count == 2)
                {
                    m_waterMaterial = m_waterMaterialInstances[0];
                }

                for (int i = 0; i < m_waterMaterialInstances.Count; i++)
                {
                    #if UNITY_EDITOR
                    EditorUtility.SetDirty(m_waterMaterialInstances[i]);
                    #endif
                }
            }
        }

        public List<Material> GetWaterMaterialInstances()
        {
            List<Material> materials = new List<Material>();
            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                for (int i = 0; i < meshRenderer.sharedMaterials.Length; i++)
                {
                    materials.Add(meshRenderer.sharedMaterials[i]);
                }
            }

            m_waterMaterialInstances.Clear();
            m_waterMaterialInstances = materials;

            if (m_waterMaterialInstances.Count == 2)
            {
                m_waterMaterial = m_waterMaterialInstances[0];
            }

            return materials;
        }

        /// <summary>
        /// Builds the and setup the color depth texture or sets baked texture
        /// </summary>
        private void BuildWaterColorDepth()
        {
            if (m_waterProfile != null)
            {
#if UNITY_EDITOR
                m_waterProfileValues = m_waterProfile.m_waterProfiles[m_waterProfile.m_selectedWaterProfileValuesIndex];
#endif
            }

            if (m_waterProfileValues == null)
            {
                Debug.Log("Unable To Build");
                return;
            }

            m_currentSmoothnes = m_waterProfileValues.m_smoothness;
            if (m_waterProfileValues.m_waterGradient != null && m_waterProfileValues.m_colorDepthRamp == null)
            {
                GenerateColorDepth();
            }
            else
            {
                m_waterTexture = m_waterProfileValues.m_colorDepthRamp;
            }

            if (m_waterMaterial != null && m_waterTexture != null)
            {
                m_waterMaterial.SetTexture(GaiaWeatherShaderID.m_waterDepthRamp, m_waterTexture);
            }
        }

        /// <summary>
        /// Enables or disables the smoothness based on ray setup
        /// </summary>
        public void CalculateSmoothnessRay(float range)
        {
            if (Application.isPlaying)
            {
                if (m_RenderCamera != null)
                {
                    Ray ray = m_RenderCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
#if GAIA_PRO_PRESENT
                    if (ProceduralWorldsGlobalWeather.Instance != null)
                    {
                        if (ProceduralWorldsGlobalWeather.Instance.CheckIsNight())
                        {
                            if (Physics.Raycast(ray.origin, -ProceduralWorldsGlobalWeather.Instance.m_moonLight.transform.forward, range, m_reflectionLayers))
                            {
                                m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, 1f);
                            }
                            else
                            {
                                m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, m_currentSmoothnes);
                            }
                        }
                        else
                        {
                            if (Physics.Raycast(ray.origin, -ProceduralWorldsGlobalWeather.Instance.m_sunLight.transform.forward, range, m_reflectionLayers))
                            {
                                m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, 1f);
                            }
                            else
                            {
                                m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, m_currentSmoothnes);
                            }
                        }

                    }
                    else
                    {
                        if (SunLight != null)
                        {
                            if (Physics.Raycast(ray.origin, -SunLight.transform.forward, range, m_reflectionLayers))
                            {
                                m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, 1f);
                            }
                            else
                            {
                                m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, m_currentSmoothnes);
                            }
                        }
                        else
                        {
                            SunLight = GaiaUtils.GetMainDirectionalLight();
                        }
                    }
#else
                    if (SunLight != null)
                    {
                        if (Physics.Raycast(ray.origin, -SunLight.transform.forward, range, m_reflectionLayers))
                        {
                            m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, 1f);
                        }
                        else
                        {
                            m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, m_currentSmoothnes);
                        }
                    }
                    else
                    {
                        SunLight = GaiaUtils.GetMainDirectionalLight();
                    }
#endif
                }
            }
            else
            {
#if UNITY_EDITOR
#if GAIA_PRO_PRESENT
                if (ProceduralWorldsGlobalWeather.Instance != null)
                {
                    if (ProceduralWorldsGlobalWeather.Instance.CheckIsNight())
                    {
                        if (UnityEditor.SceneView.lastActiveSceneView != null)
                        {
                            Ray ray = UnityEditor.SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                            if (Physics.Raycast(ray.origin, -ProceduralWorldsGlobalWeather.Instance.m_moonLight.transform.forward, range, m_reflectionLayers))
                            {
                                m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, 1f);
                            }
                            else
                            {
                                m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, m_currentSmoothnes);
                            }
                        }
                    }
                    else
                    {
                        if (UnityEditor.SceneView.lastActiveSceneView != null)
                        {
                            Ray ray = UnityEditor.SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                            if (ProceduralWorldsGlobalWeather.Instance.m_sunLight != null)
                            {
                                if (Physics.Raycast(ray.origin, -ProceduralWorldsGlobalWeather.Instance.m_sunLight.transform.forward, range, m_reflectionLayers))
                                {
                                    m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, 1f);
                                }
                                else
                                {
                                    m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, m_currentSmoothnes);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (UnityEditor.SceneView.lastActiveSceneView != null)
                    {
                        if (SunLight != null)
                        {
                            Ray ray = UnityEditor.SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                            if (Physics.Raycast(ray.origin, -SunLight.transform.forward, range, m_reflectionLayers))
                            {
                                m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, 1f);
                            }
                            else
                            {
                                m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, m_currentSmoothnes);
                            }
                        }
                        else
                        {
                            SunLight = GaiaUtils.GetMainDirectionalLight();
                        }
                    }
                }
#else
                if (UnityEditor.SceneView.lastActiveSceneView != null)
                {
                    if (SunLight != null)
                    {
                        Ray ray = UnityEditor.SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                        if (Physics.Raycast(ray.origin, -SunLight.transform.forward, range, m_reflectionLayers))
                        {
                            m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, 1f);
                        }
                        else
                        {
                            m_waterMaterial.SetFloat(GaiaWeatherShaderID.m_waterSmoothness, m_currentSmoothnes);
                        }
                    }
                    else
                    {
                        SunLight = GaiaUtils.GetMainDirectionalLight();
                    }
                }
#endif
#endif
            }
        }

        /// <summary>
        /// Builds reflection when using update mode
        /// </summary>
        private void BuildUpdateModeReflections()
        {
            if (m_RenderUpdate == RenderUpdateMode.Update)
            {
                if (m_RenderCamera != null)
                {
                    if (!Application.isPlaying)
                    {
#if UNITY_EDITOR
                        if (!EditorApplication.isPlaying)
                        {
                            if (SceneView.lastActiveSceneView == null)
                            {
                                return;
                            }

                            m_RenderCamera = SceneView.lastActiveSceneView.camera;
                            if (m_RenderCamera != null)
                            {
                                if (m_RenderCamera.transform.position != m_currentPosition || m_RenderCamera.transform.rotation.eulerAngles != m_currentRotation)
                                {
                                    if (m_RenderCamera == null)
                                    {
                                        m_RenderCamera = GaiaUtils.GetCamera();
                                    }
                                    GenerateCamera();
                                    ResyncCameraSettings();
                                    UpdateCameraModes();
                                    BuildReflection(m_skyboxOnly);
                                    m_currentPosition = m_RenderCamera.transform.position;
                                    m_currentRotation = m_RenderCamera.transform.rotation.eulerAngles;
                                }
                            }
                        }
#endif
                    }
                    else
                    {
                        if (m_RenderCamera.transform.position != m_currentPosition || m_RenderCamera.transform.rotation.eulerAngles != m_currentRotation)
                        {
                            if (m_useRefreshTime)
                            {
                                if (Time.time >= m_tempTime)
                                {
                                    m_tempTime += m_refreshRate;
                                    BuildReflection(m_skyboxOnly);
                                    m_currentPosition = m_RenderCamera.transform.position;
                                    m_currentRotation = m_RenderCamera.transform.rotation.eulerAngles;
                                }
                            }
                            else
                            {
                                BuildReflection(m_skyboxOnly);
                                m_currentPosition = m_RenderCamera.transform.position;
                                m_currentRotation = m_RenderCamera.transform.rotation.eulerAngles;
                            }
                        }
                    }
                }
                else
                {
                    if (m_RenderCamera == null)
                    {
                        m_RenderCamera = GaiaUtils.GetCamera();
                    }
                }
            }
        }


        /// <summary>
        /// Generates color depth
        /// </summary>
        public void GenerateColorDepth()
        {
            if (m_waterTexture == null || m_waterTexture.wrapMode != TextureWrapMode.Clamp)
            {
                m_waterTexture = new Texture2D(m_waterProfileValues.m_gradientTextureResolution,
                    m_waterProfileValues.m_gradientTextureResolution) {wrapMode = TextureWrapMode.Clamp};
            }
            else if (m_waterProfileValues.m_gradientTextureResolution != m_waterTexture.width)
            {
                m_waterTexture = new Texture2D(m_waterProfileValues.m_gradientTextureResolution,
                    m_waterProfileValues.m_gradientTextureResolution) {wrapMode = TextureWrapMode.Clamp};
            }
            else if (!m_waterTexture.isReadable)
            {
                m_waterTexture = new Texture2D(m_waterProfileValues.m_gradientTextureResolution,
                    m_waterProfileValues.m_gradientTextureResolution) {wrapMode = TextureWrapMode.Clamp};
            }

            if (m_waterProfileValues.m_waterGradient == null)
            {
                return;
            }

            if (m_currentGradient != m_waterProfileValues.m_waterGradient)
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

                if (m_waterRenderer == null)
                {
                    m_waterRenderer = GetComponent<MeshRenderer>();
                }
                else
                {
                    m_waterRenderer.sharedMaterial.SetTexture(GaiaWeatherShaderID.m_waterDepthRamp, m_waterTexture);
                }

                m_currentGradient = m_waterProfileValues.m_waterGradient;
            }
        }

        /// <summary>
        /// Force builds the color depth map texture for the water
        /// </summary>
        public void ForceBuildColorDepth()
        {
            if (m_waterTexture == null || m_waterTexture.wrapMode != TextureWrapMode.Clamp)
            {
                m_waterTexture = new Texture2D(m_waterProfileValues.m_gradientTextureResolution,
                    m_waterProfileValues.m_gradientTextureResolution) {wrapMode = TextureWrapMode.Clamp};
            }
            else if (m_waterProfileValues.m_gradientTextureResolution != m_waterTexture.width)
            {
                m_waterTexture = new Texture2D(m_waterProfileValues.m_gradientTextureResolution,
                    m_waterProfileValues.m_gradientTextureResolution) {wrapMode = TextureWrapMode.Clamp};
            }
            else if (!m_waterTexture.isReadable)
            {
                m_waterTexture = new Texture2D(m_waterProfileValues.m_gradientTextureResolution,
                    m_waterProfileValues.m_gradientTextureResolution) {wrapMode = TextureWrapMode.Clamp};
            }

            if (m_waterProfileValues.m_waterGradient == null)
            {
                return;
            }

            for (int x = 0; x < m_waterProfileValues.m_gradientTextureResolution; x++)
            {
                for (int y = 0; y < m_waterProfileValues.m_gradientTextureResolution; y++)
                {
                    Color color = m_waterProfileValues.m_waterGradient.Evaluate((float)x / (float)m_waterProfileValues.m_gradientTextureResolution);
                    m_waterTexture.SetPixel(x, y, color);
                }
            }
            m_waterTexture.Apply();

            if (m_waterRenderer == null)
            {
                m_waterRenderer = GetComponent<MeshRenderer>();
            }
            if (m_waterRenderer != null)
            {
                m_waterRenderer.sharedMaterial.SetTexture(GaiaWeatherShaderID.m_waterDepthRamp, m_waterTexture);
            }

            m_currentGradient = m_waterProfileValues.m_waterGradient;
        }

        #endregion

        #region Generate

        public void Generate()
        {
            if (GetComponent<Renderer>())
            {
                if (m_RenderCamera == null)
                {
                    m_RenderCamera = GaiaUtils.GetCamera();
                }
                GenerateCamera();
                ResyncCameraSettings();
                UpdateCameraModes();
                CreateMirrorObjects();
                CancelInvoke();
                m_waterMaterialInstances = GetWaterMaterialInstances();
                BuildReflection(m_skyboxOnly);
                if (m_RenderUpdate == RenderUpdateMode.Interval)
                {
                    InvokeRepeating("RefreshReflection", 0, m_updateThreshold);
                }
            }
            else
            {
                Debug.Log("unable to create reflections, render missing");
                return;
            }
        }

        public void LoadFromProfile()
        {
            if (m_waterProfile == null)
            {
                return;
            }

            m_disablePixelLights = m_waterProfile.m_disablePixelLights;
            m_clipPlaneOffset = m_waterProfile.m_clipPlaneOffset;
            m_reflectionLayers = m_waterProfile.m_reflectedLayers;
            m_HDR = m_waterProfile.m_useHDR;
            m_MSAA = m_waterProfile.m_allowMSAA;
            m_RenderUpdate = m_waterProfile.m_waterRenderUpdateMode;
            m_updateThreshold = m_waterProfile.m_interval;
            m_customReflectionDistance = m_waterProfile.m_useCustomRenderDistance;
            m_customReflectionDistances = m_waterProfile.m_enableLayerDistances;
            m_renderDistance = m_waterProfile.m_customRenderDistance;
            m_currentSmoothnes = m_waterProfileValues.m_smoothness;
            if (m_waterProfile.m_enableReflections)
            {
                m_skyboxOnly = false;
            }
            else
            {
                m_skyboxOnly = true;
            }

            switch (m_waterProfile.m_reflectionResolution)
            {
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8:
                    m_renderTextureSize = 8;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution16:
                    m_renderTextureSize = 16;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution32:
                    m_renderTextureSize = 32;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution64:
                    m_renderTextureSize = 64;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution128:
                    m_renderTextureSize = 128;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution256:
                    m_renderTextureSize = 256;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution512:
                    m_renderTextureSize = 512;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution1024:
                    m_renderTextureSize = 1024;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution2048:
                    m_renderTextureSize = 2048;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution4096:
                    m_renderTextureSize = 4096;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8192:
                    m_renderTextureSize = 8192;
                    break;
            }
        }

        #endregion

        #region Camera Setup

        /// <summary>
        /// Used to clear Camera and Render texture
        /// </summary>
        public void ClearData()
        {
            if (m_reflectionTexture)
            {
                m_reflectionTexture.Release();
                DestroyImmediate(m_reflectionTexture);
                m_reflectionTexture = null;
            }
            //DestroyImmediate(m_reflectionCamera);
        }

        #endregion

        #region Refresh Reflections

        /// <summary>
        /// Refresh reflection
        /// </summary>
        private void RefreshReflection()
        {
            if (m_RenderCamera != null)
            {
                if (m_RenderCamera.transform.position != m_currentPosition || m_RenderCamera.transform.rotation.eulerAngles != m_currentRotation)
                {
                    BuildReflection(m_skyboxOnly);
                    m_currentPosition = m_RenderCamera.transform.position;
                    m_currentRotation = m_RenderCamera.transform.rotation.eulerAngles;
                }
            }
            else
            {
                if (m_RenderCamera == null)
                {
                    m_RenderCamera = GaiaUtils.GetCamera();
                }
            }
        }

        #endregion

        #region Build Reflections

        /// <summary>
        /// Builds the necessary steps to produce a reflection
        /// </summary>
        private void BuildReflection(bool renderOnlySkybox)
        {
            if (m_RenderCamera == null)
            {
                m_RenderCamera = GaiaUtils.GetCamera();
            }

            if (Application.isPlaying)
            {
                if (m_enableDisabeHeightFeature)
                {
                    if (m_player!=null && m_player.position.y > m_disableHeight)
                    {
                        renderOnlySkybox = true;
                    }
                }
            }

            if (m_RenderCamera != null)
            {
                if (m_ableToRender)
                {
                    m_ableToRender = false;
                    PlanePosition();
                    GenerateReflection(renderOnlySkybox);
                    CreateMirrorObjects();
                    m_ableToRender = true;
                }
                else
                {
                    return;
                }
            }
            else
            {
                Debug.Log("no rendering camera found");
                return;
            }
        }

        /// <summary>
        /// Position Data
        /// </summary>
        private void PlanePosition()
        {
            m_worldPosition = transform.position;
            m_normal = transform.up;
        }

        /// <summary>
        /// Can be used to keep the cameras settings in check
        /// </summary>
        private void ResyncCameraSettings()
        {
            if (m_RenderCamera == null)
            {
                m_RenderCamera = GaiaUtils.GetCamera();
            }

            if (m_RenderCamera != null)
            {
                m_reflectionCamera.orthographic = m_RenderCamera.orthographic;
                m_reflectionCamera.fieldOfView = m_RenderCamera.fieldOfView;
                m_reflectionCamera.aspect = m_RenderCamera.aspect;
                m_reflectionCamera.orthographicSize = m_RenderCamera.orthographicSize;
                if (m_customRenderPath)
                {
                    m_reflectionCamera.renderingPath = m_RenderPath;
                }
                else
                {
                    m_reflectionCamera.renderingPath = m_RenderCamera.actualRenderingPath;
                }
                m_reflectionCamera.allowHDR = m_HDR;
                m_reflectionCamera.allowMSAA = m_MSAA;
            }
        }

        /// <summary>
        /// Updates Cameras flags, background color & when present the sky
        /// </summary>
        private void UpdateCameraModes()
        {
            m_reflectionCamera.clearFlags = m_RenderCamera.clearFlags;
            m_reflectionCamera.backgroundColor = m_RenderCamera.backgroundColor;
        }

        /// <summary>
        /// Create a mirror texture
        /// </summary>
        private void CreateMirrorObjects()
        {
            if (m_rebuild)
            {
                CreateTexture();
                if (m_hideReflectionCamera)
                {
                    m_reflectionTexture.hideFlags = HideFlags.HideAndDontSave;
                }
                else
                {
                    m_reflectionTexture.hideFlags = HideFlags.DontSave;
                }
            }
            if (m_oldRenderTextureSize != m_renderTextureSize)
            {
                CreateTexture();
                if (m_hideReflectionCamera)
                {
                    m_reflectionTexture.hideFlags = HideFlags.HideAndDontSave;
                }
                else
                {
                    m_reflectionTexture.hideFlags = HideFlags.DontSave;
                }
                m_oldRenderTextureSize = m_renderTextureSize;
            }
        }

        /// <summary>
        /// Creates the render texture
        /// </summary>
        private void CreateTexture()
        {
            m_reflectionTexture = new RenderTexture(m_renderTextureSize, m_renderTextureSize, 16)
            {
                name = "__MirrorReflection" + GetInstanceID(),
                isPowerOfTwo = true
            };
        }

        /// <summary>
        /// Generates the camera used for the mirror
        /// </summary>
        private void GenerateCamera()
        {
            if (m_RenderCamera == null)
            {
                m_RenderCamera = GaiaUtils.GetCamera();
            }

            if (m_reflectionCamera == null && m_RenderCamera != null)
            {
                GameObject MirrorGameObject = new GameObject("Mirror Refl Camera id" + GetInstanceID() + " for " + m_RenderCamera.GetInstanceID(), typeof(Camera), typeof(Skybox));
                m_reflectionCamera = MirrorGameObject.GetComponent<Camera>();
                m_reflectionCamera.enabled = false;
                m_reflectionCamera.transform.position = transform.position;
                m_reflectionCamera.transform.rotation = transform.rotation;
                if (m_hideReflectionCamera)
                {
                    MirrorGameObject.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            else if (m_reflectionCamera != null)
            {
                m_reflectionCamera.enabled = false;
                m_reflectionCamera.transform.position = transform.position;
                m_reflectionCamera.transform.rotation = transform.rotation;
                if (m_hideReflectionCamera)
                {
                    m_reflectionCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
            }
        }

        /// <summary>
        /// Generates the reflection based on the main camera
        /// </summary>
        private void GenerateReflection(bool renderOnlySkybox)
        {
            RenderPipeline = GetActivePipeline();

            // Reflect camera around reflection plane
            float DotProduct = -Vector3.Dot(m_normal, m_worldPosition) - m_clipPlaneOffset;
            Vector4 ReflectionPlane = new Vector4(m_normal.x, m_normal.y, m_normal.z, DotProduct);

            m_reflection = Matrix4x4.zero;
            CalculateReflectionMatrix(ref m_reflection, ReflectionPlane);
            m_oldPosition = m_RenderCamera.transform.position;
            m_newPosition = m_reflection.MultiplyPoint(m_oldPosition);
            m_reflectionCamera.worldToCameraMatrix = m_RenderCamera.worldToCameraMatrix * m_reflection;

            // Setup oblique projection matrix so that near plane is our reflection plane.
            // This way we clip everything below/above it for free.
            m_clipPlane = CameraSpacePlane(m_reflectionCamera, m_worldPosition, m_normal, 1.0f);
            m_projection = m_RenderCamera.CalculateObliqueMatrix(m_clipPlane);
            m_reflectionCamera.projectionMatrix = m_projection;
            // Distances for each layer or general layer
            if (m_customReflectionDistance)
            {
                if (m_customReflectionDistances)
                {
                    m_distances = m_waterProfile.m_customRenderDistances;
                }
                else
                {
                    for (int idx = 0; idx < m_distances.Length; idx++)
                    {
                        m_distances[idx] = m_renderDistance;
                    }
                }

                m_reflectionCamera.layerCullDistances = m_distances;
                m_reflectionCamera.layerCullSpherical = true;
            }
            else
            {
                m_reflectionCamera.layerCullDistances = m_RenderCamera.layerCullDistances;
                m_reflectionCamera.layerCullSpherical = true;
            }
            if (renderOnlySkybox)
            {
                m_reflectionCamera.cullingMask = 0;
            }
            else
            {
                // Never render water layer
                m_reflectionCamera.cullingMask = ~(1 << 4) & m_reflectionLayers.value;
            }

            m_reflectionCamera.targetTexture = m_reflectionTexture;
            GL.invertCulling = true;
            m_reflectionCamera.transform.position = m_newPosition;
            m_euler = m_RenderCamera.transform.eulerAngles;
            m_reflectionCamera.transform.eulerAngles = new Vector3(0, m_euler.y, m_euler.z);

            if (Application.isPlaying)
            {
                /*
                if (ActiveTerrains.Length > 0)
                {
                    for (int i = 0; i < ActiveTerrains.Length; i++)
                    {
                        ActiveTerrains[i].detailObjectDistance = 0;
                    }
                }
                */
                switch (RenderPipeline)
                {
                    case GaiaConstants.EnvironmentRenderer.BuiltIn:
                        m_reflectionCamera.Render();
                        break;
                    case GaiaConstants.EnvironmentRenderer.Universal:
                        Invoke("RenderCameraURP", m_refreshRate);
                        break;
                }

                /*
                if (ActiveTerrains.Length > 0)
                {
                    for (int i = 0; i < ActiveTerrains.Length; i++)
                    {
                        ActiveTerrains[i].detailObjectDistance = ActiveTerrainsDetailDistance[i];
                    }
                }
                */
            }
            else
            {
                switch (RenderPipeline)
                {
                    case GaiaConstants.EnvironmentRenderer.BuiltIn:
                        m_reflectionCamera.Render();
                        break;
                    case GaiaConstants.EnvironmentRenderer.Universal:
                        Invoke("RenderCameraURP", m_refreshRate);
                        break;
                }
            }

            m_reflectionCamera.transform.position = m_oldPosition;
            GL.invertCulling = false;
            if (m_waterMaterial != null)
            {
                m_waterMaterial.SetTexture(GaiaWeatherShaderID.m_globalReflectionTexture, m_reflectionTexture);
            }
        }

        private Camera RenderCameraURP()
        {
            m_reflectionCamera.Render();
            return m_reflectionCamera;
        }

        /// <summary>
        /// Gets the current installed SRP
        /// </summary>
        /// <returns></returns>
        private GaiaConstants.EnvironmentRenderer GetActivePipeline()
        {
            GaiaConstants.EnvironmentRenderer renderer = GaiaConstants.EnvironmentRenderer.BuiltIn;
            //Sets up the render to the correct pipeline
            if (GraphicsSettings.renderPipelineAsset == null)
            {
                renderer = GaiaConstants.EnvironmentRenderer.BuiltIn;
            }
            else if (GraphicsSettings.renderPipelineAsset.GetType().ToString().Contains("HDRenderPipelineAsset"))
            {
                renderer = GaiaConstants.EnvironmentRenderer.HighDefinition;
            }
            else if (GraphicsSettings.renderPipelineAsset.GetType().ToString().Contains("UniversalRenderPipelineAsset"))
            {
                renderer = GaiaConstants.EnvironmentRenderer.Universal;
            }
            else
            {
                renderer = GaiaConstants.EnvironmentRenderer.Lightweight;
            }

            return renderer;
        }

        /// <summary>
        /// Extended sign: returns -1, 0 or 1 based on sign of a
        /// </summary>
        /// <param name="signValue"></param>
        /// <returns></returns>w
        private static float Sign(float signValue)
        {
            if (signValue > 0.0f) return 1.0f;
            if (signValue < 0.0f) return -1.0f;
            return 0.0f;
        }

        /// <summary>
        /// Given position/normal of the plane, calculates plane in camera space.
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="pos"></param>
        /// <param name="normal"></param>
        /// <param name="sideSign"></param>
        /// <returns></returns>
        private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
        {
            Vector3 offsetPos = pos + normal * m_clipPlaneOffset;
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cpos = m.MultiplyPoint(offsetPos);
            Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

        /// <summary>
        /// Calculates reflection matrix around the given plane
        /// </summary>
        /// <param name="reflectionMat"></param>
        /// <param name="plane"></param>
        private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
        {
            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;
        }

        #endregion

        #region Render Update Mode Enum

        /// <summary>
        /// Enum for render update
        /// </summary>
        public enum RenderUpdateMode
        {
            OnEnable, Update, Interval, OnRender
        };

        #endregion

        #region Shader Functions

        /// <summary>
        /// Updates the water shader values
        /// </summary>
        public void UpdateShaderValues()
        {
            if (m_waterMaterial == null)
            {
                if (m_waterMaterialInstances.Count == 2)
                {
                    m_waterMaterial = m_waterMaterialInstances[0];
                }
            }
            else
            {

                m_waterMaterial.SetShaderPassEnabled(GaiaWeatherShaderID.m_waterGrabPass, true);
#if GAIA_PRO_PRESENT
                if (ProceduralWorldsGlobalWeather.Instance != null)
                {
                    if (ProceduralWorldsGlobalWeather.Instance.CheckIsNight())
                    {
                        if (ProceduralWorldsGlobalWeather.Instance.m_moonLight != null)
                        {
                            m_waterMaterial.SetVector(GaiaWeatherShaderID.m_globalLightDirection, -ProceduralWorldsGlobalWeather.Instance.m_moonLight.transform.forward);
                            m_waterMaterial.SetVector(GaiaWeatherShaderID.m_globalLightColor, new Vector4(ProceduralWorldsGlobalWeather.Instance.m_moonLight.color.r * ProceduralWorldsGlobalWeather.Instance.m_moonLight.intensity, ProceduralWorldsGlobalWeather.Instance.m_moonLight.color.g * ProceduralWorldsGlobalWeather.Instance.m_moonLight.intensity, ProceduralWorldsGlobalWeather.Instance.m_moonLight.color.b * ProceduralWorldsGlobalWeather.Instance.m_moonLight.intensity, ProceduralWorldsGlobalWeather.Instance.m_moonLight.color.a * ProceduralWorldsGlobalWeather.Instance.m_moonLight.intensity));
                            m_waterMaterial.SetVector(GaiaWeatherShaderID.m_globalLightSpecColor, new Vector4(ProceduralWorldsGlobalWeather.Instance.m_moonLight.color.r * m_specColor.r, ProceduralWorldsGlobalWeather.Instance.m_moonLight.color.g * m_specColor.g, ProceduralWorldsGlobalWeather.Instance.m_moonLight.color.b * m_specColor.b, ProceduralWorldsGlobalWeather.Instance.m_moonLight.color.a * m_specColor.a));
                        }
                    }
                    else
                    {
                        if (ProceduralWorldsGlobalWeather.Instance.m_sunLight != null)
                        {
                            m_waterMaterial.SetVector(GaiaWeatherShaderID.m_globalLightDirection, -ProceduralWorldsGlobalWeather.Instance.m_sunLight.transform.forward);
                            m_waterMaterial.SetVector(GaiaWeatherShaderID.m_globalLightColor, new Vector4(ProceduralWorldsGlobalWeather.Instance.m_sunLight.color.r * ProceduralWorldsGlobalWeather.Instance.m_sunLight.intensity, ProceduralWorldsGlobalWeather.Instance.m_sunLight.color.g * ProceduralWorldsGlobalWeather.Instance.m_sunLight.intensity, ProceduralWorldsGlobalWeather.Instance.m_sunLight.color.b * ProceduralWorldsGlobalWeather.Instance.m_sunLight.intensity, ProceduralWorldsGlobalWeather.Instance.m_sunLight.color.a * ProceduralWorldsGlobalWeather.Instance.m_sunLight.intensity));
                            m_waterMaterial.SetVector(GaiaWeatherShaderID.m_globalLightSpecColor, new Vector4(ProceduralWorldsGlobalWeather.Instance.m_sunLight.color.r * m_specColor.r, ProceduralWorldsGlobalWeather.Instance.m_sunLight.color.g * m_specColor.g, ProceduralWorldsGlobalWeather.Instance.m_sunLight.color.b * m_specColor.b, ProceduralWorldsGlobalWeather.Instance.m_sunLight.color.a * m_specColor.a));
                        }
                    }

                    m_waterMaterial.SetColor(GaiaGlobal.m_shaderAmbientColor, RenderSettings.ambientSkyColor);
                }
                else
                {
                    if (SunLight != null)
                    {
                        m_specColor = m_lightSpecColor;
                        m_waterMaterial.SetVector(GaiaWeatherShaderID.m_globalLightDirection, -SunLight.transform.forward);
                        m_waterMaterial.SetVector(GaiaWeatherShaderID.m_globalLightColor, new Vector4(SunLight.color.r * SunLight.intensity, SunLight.color.g * SunLight.intensity, SunLight.color.b * SunLight.intensity, SunLight.color.a * SunLight.intensity));
                        m_waterMaterial.SetVector(GaiaWeatherShaderID.m_globalLightSpecColor, new Vector4(SunLight.color.r * m_specColor.r, SunLight.color.g * m_specColor.g, SunLight.color.b * m_specColor.b, SunLight.color.a * m_specColor.a));
                    }
                }
#else
                if (SunLight != null)
                {
                    m_specColor = m_lightSpecColor;
                    m_waterMaterial.SetVector(GaiaWeatherShaderID.m_globalLightDirection, -SunLight.transform.forward);
                    m_waterMaterial.SetVector(GaiaWeatherShaderID.m_globalLightColor, new Vector4(SunLight.color.r * SunLight.intensity, SunLight.color.g * SunLight.intensity, SunLight.color.b * SunLight.intensity, SunLight.color.a * SunLight.intensity));
                    m_waterMaterial.SetVector(GaiaWeatherShaderID.m_globalLightSpecColor, new Vector4(SunLight.color.r * m_specColor.r, SunLight.color.g * m_specColor.g, SunLight.color.b * m_specColor.b, SunLight.color.a * m_specColor.a));
                }
#endif
            }
        }

        #endregion
    }
}