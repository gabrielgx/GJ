#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Gaia
{
    [ExecuteInEditMode]
    public class CameraLayerCulling : MonoBehaviour
    {
        #region Variables
        public bool m_applyToEditorCamera = false;
        public bool m_realtimeUpdate = false;
        public Camera m_mainCamera;
        public float[] m_layerDistances = new float[32];
        public float[] m_shadowLayerDistances = new float[32];

        public bool WeatherSystemPresent = false;
        public Light SunLight;
        #endregion

        private void OnEnable()
        {
            //Get main camera if we dont have one
            if (m_mainCamera == null)
            {
                //See if we can get it from this component or its children
                m_mainCamera = GetComponentInChildren<Camera>();

                //Try and find anything that is called "Camera"
                if (m_mainCamera == null)
                {
                    Camera[] cameras = FindObjectsOfType<Camera>();

                    //First see if we can find something called Camera
                    foreach (Camera camera in cameras)
                    {
                        if (camera.name == "Camera" && camera.isActiveAndEnabled)
                        {
                            m_mainCamera = camera;
                            break;
                        }
                    }
                }

                //Otherwise get anything
                if (m_mainCamera == null)
                {
                    m_mainCamera = FindObjectOfType<Camera>();
                }
            }

#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                WeatherSystemPresent = true;
            }
            else
            {
                WeatherSystemPresent = false;
            }
#endif

            UpdateCullingDistances();
        }

        private void Update()
        {
            if (m_realtimeUpdate && !Application.isPlaying)
            {
                UpdateCullingDistances();
            }
        }

        public void UpdateCullingDistances()
        {
            if (m_mainCamera == null)
            {
                return;
            }

#if GAIA_PRO_PRESENT
            if (WeatherSystemPresent)
            {
                if (ProceduralWorldsGlobalWeather.Instance.CheckIsNight())
                {
                    SunLight = ProceduralWorldsGlobalWeather.Instance.m_moonLight;
                }
                else
                {
                    SunLight = ProceduralWorldsGlobalWeather.Instance.m_sunLight;
                }
            }
            else
            {
                if (SunLight == null)
                {
                    SunLight = GaiaUtils.GetMainDirectionalLight();
                }
            }
#else
            if (SunLight == null)
            {
                SunLight = GaiaUtils.GetMainDirectionalLight();
            }
#endif

            //Make sure we have distances
            if (m_layerDistances == null || m_layerDistances.Length != 32)
            {
                return;
            }

            //Apply to main camera
            m_mainCamera.layerCullDistances = m_layerDistances;

            if (SunLight != null)
            {
                SunLight.layerShadowCullDistances = m_shadowLayerDistances;
            }
        }

        public void ApplySceneSetup(bool active)
        {
            //Apply to editor camera
#if UNITY_EDITOR
            if (active)
            {
                foreach (var sceneCamera in SceneView.GetAllSceneCameras())
                {
                    sceneCamera.layerCullDistances = m_layerDistances;
                }

                if (SunLight != null)
                {
                    SunLight.layerShadowCullDistances = m_shadowLayerDistances;
                }
            }
            else
            {
                foreach (var sceneCamera in SceneView.GetAllSceneCameras())
                {
                    float[] layers = new float[32];
                    for (int i = 0; i < layers.Length; i++)
                    {
                        layers[i] = 0f;
                    }

                    sceneCamera.layerCullDistances = layers;
                }

                if (SunLight != null)
                {
                    float[] layers = new float[32];
                    for (int i = 0; i < layers.Length; i++)
                    {
                        layers[i] = 0f;
                    }
                    SunLight.layerShadowCullDistances = layers;
                }
            }
#endif
        }

        public void UpdateDefaults()
        {
            if (m_mainCamera == null)
            {
                m_mainCamera = GaiaUtils.GetCamera();
            }

            //Objects
            m_layerDistances = new float[32];
            for (int i = 0; i < m_layerDistances.Length; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName == "Default")
                {
                    if (m_mainCamera != null)
                    {
                        m_layerDistances[i] = m_mainCamera.farClipPlane;
                    }
                    else
                    {
                        m_layerDistances[i] = 2000f;
                    }
                }
                else if (layerName == "Water")
                {
                    if (m_mainCamera != null)
                    {
                        m_layerDistances[i] = m_mainCamera.farClipPlane;
                    }
                    else
                    {
                        m_layerDistances[i] = 0f;
                    }
                }
                else if (layerName == "PW_VFX")
                {
                    m_layerDistances[i] = 0f;
                }
                else if (layerName == "PW_Object_Small")
                {
                    m_layerDistances[i] = 150f;
                }
                else if (layerName == "PW_Object_Medium")
                {
                    m_layerDistances[i] = 500f;
                }
                else if (layerName == "PW_Object_Large")
                {
                    if (m_mainCamera != null)
                    {
                        m_layerDistances[i] = m_mainCamera.farClipPlane;
                    }
                    else
                    {
                        m_layerDistances[i] = 1500f;
                    }
                }
                else
                {
                    m_layerDistances[i] = 0f;
                }
            }

            //Shadows
            m_shadowLayerDistances = new float[32];
            for (int i = 0; i < m_shadowLayerDistances.Length; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName == "Default")
                {
                    m_shadowLayerDistances[i] = 0;
                }
                else if (layerName == "Water")
                {
                    m_shadowLayerDistances[i] = 0f;
                }
                else if (layerName == "PW_VFX")
                {
                    m_shadowLayerDistances[i] = 0f;
                }
                else if (layerName == "PW_Object_Small")
                {
                    m_shadowLayerDistances[i] = 30f;
                }
                else if (layerName == "PW_Object_Medium")
                {
                    m_shadowLayerDistances[i] = 60f;
                }
                else if (layerName == "PW_Object_Large")
                {
                    if (m_mainCamera != null)
                    {
                        m_shadowLayerDistances[i] = m_mainCamera.farClipPlane;
                    }
                    else
                    {
                        m_shadowLayerDistances[i] = 0f;
                    }
                }
                else
                {
                    m_shadowLayerDistances[i] = 0f;
                }
            }
        }
    }
}