using UnityEngine;
using Gaia;

namespace ProcedualWorlds.WaterSystem
{
    public class PW_WaterReflectionZone : MonoBehaviour
    {
        public GameObject m_player;
        public GaiaConstants.GaiaProWaterReflectionsQuality m_reflectionQuality = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution32;
        public float m_renderDistance = 150f;
        public float[] m_distances = new float[32];
        public LayerMask m_reflectionLayers;

        [SerializeField]
        private PWS_WaterSystem m_waterSystem;
#if UNITY_EDITOR
        private static Vector3 m_colliderSize = new Vector3(40f, 40f, 40f);
        private static float m_colliderRadiusSize = 40f;
#endif
        [SerializeField]
        private GaiaConstants.GaiaProWaterReflectionsQuality m_currentReflectionQuality;
        [SerializeField]
        private float m_currentRenderDistance;
        [SerializeField]
        private float[] m_currentDistances;
        [SerializeField]
        private LayerMask m_currentReflectionLayers;

        #region Unity Functions

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Draw gizmo when object is selected
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                Gizmos.color = new Color(1f, 0.137112f, 0f, 0.4f);
                Gizmos.matrix = gameObject.transform.localToWorldMatrix;
                Gizmos.DrawCube(Vector3.zero, boxCollider.size);
            }

            SphereCollider sphereCollider = gameObject.GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                Gizmos.color = new Color(1f, 0.137112f, 0f, 0.4f);
                Gizmos.matrix = gameObject.transform.localToWorldMatrix;
                Gizmos.DrawSphere(Vector3.zero, sphereCollider.radius);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == m_player.tag)
            {
                ProcessWater(false);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.tag == m_player.tag)
            {
                ProcessWater(true);
            }
        }

        #endregion

        #region Utils

        private void ProcessWater(bool enabled)
        {
            if (m_waterSystem == null)
            {
                Debug.LogError("Unable to find the water system for Gaia");
                return;
            }
            else
            {
                if (enabled)
                {
                    //Set
                    SetWaterQuality(m_currentReflectionQuality);
                    m_waterSystem.m_renderDistance = m_currentRenderDistance;
                    m_waterSystem.m_distances = m_currentDistances;
                    m_waterSystem.m_reflectionLayers = m_currentReflectionLayers;
                }
                else
                {
                    //Get
                    m_currentReflectionQuality = GetWaterQuality();
                    m_currentRenderDistance = m_waterSystem.m_renderDistance;
                    m_currentDistances = m_waterSystem.m_distances;

                    //Set
                    SetWaterQuality(m_reflectionQuality);
                    m_waterSystem.m_renderDistance = m_renderDistance;
                    m_waterSystem.m_distances = m_distances;
                    m_waterSystem.m_reflectionLayers = m_reflectionLayers;
                }
            }
        }

        public void Initialize()
        {
            m_waterSystem = FindObjectOfType<PWS_WaterSystem>();
            if (m_waterSystem != null)
            {
                m_currentReflectionQuality = GetWaterQuality();
                m_currentRenderDistance = m_waterSystem.m_renderDistance;
                m_currentDistances = m_waterSystem.m_distances;
                m_currentReflectionLayers = m_waterSystem.m_reflectionLayers;
            }

            m_player = Camera.main.gameObject;
        }

        private GaiaConstants.GaiaProWaterReflectionsQuality GetWaterQuality()
        {
            GaiaConstants.GaiaProWaterReflectionsQuality reflectionsQuality = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8;
            if (m_waterSystem != null)
            {
                if (m_waterSystem.m_renderTextureSize == 16)
                {
                    reflectionsQuality = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution16;
                }
                else if (m_waterSystem.m_renderTextureSize == 32)
                {
                    reflectionsQuality = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution32;
                }
                else if (m_waterSystem.m_renderTextureSize == 64)
                {
                    reflectionsQuality = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution64;
                }
                else if (m_waterSystem.m_renderTextureSize == 128)
                {
                    reflectionsQuality = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution128;
                }
                else if (m_waterSystem.m_renderTextureSize == 256)
                {
                    reflectionsQuality = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution256;
                }
                else if (m_waterSystem.m_renderTextureSize == 512)
                {
                    reflectionsQuality = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution512;
                }
                else if (m_waterSystem.m_renderTextureSize == 1024)
                {
                    reflectionsQuality = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution1024;
                }
                else if (m_waterSystem.m_renderTextureSize == 2048)
                {
                    reflectionsQuality = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution2048;
                }
                else if (m_waterSystem.m_renderTextureSize == 4096)
                {
                    reflectionsQuality = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution4096;
                }
                else
                {
                    reflectionsQuality = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8192;
                }
            }

            return reflectionsQuality;
        }

        private void SetWaterQuality(GaiaConstants.GaiaProWaterReflectionsQuality reflectionQuality)
        {
            switch (reflectionQuality)
            {
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8:
                    m_waterSystem.m_renderTextureSize = 8;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution16:
                    m_waterSystem.m_renderTextureSize = 16;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution32:
                    m_waterSystem.m_renderTextureSize = 32;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution64:
                    m_waterSystem.m_renderTextureSize = 64;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution128:
                    m_waterSystem.m_renderTextureSize = 128;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution256:
                    m_waterSystem.m_renderTextureSize = 256;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution512:
                    m_waterSystem.m_renderTextureSize = 512;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution1024:
                    m_waterSystem.m_renderTextureSize = 1024;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution2048:
                    m_waterSystem.m_renderTextureSize = 2048;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution4096:
                    m_waterSystem.m_renderTextureSize = 4096;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8192:
                    m_waterSystem.m_renderTextureSize = 8192;
                    break;
            }
        }

        #endregion

        #region Public Static Functions

#if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/Procedural Worlds/Gaia/Create Water Reflection Zone")]
        public static void CreateVolume()
        {
            GameObject newObject = new GameObject("PW Reflection Zone");
            newObject.transform.position = UnityEditor.SceneView.lastActiveSceneView.camera.transform.position;

            PW_WaterReflectionZone reflectionZone = newObject.AddComponent<PW_WaterReflectionZone>();
            reflectionZone.Initialize();
            reflectionZone.m_reflectionLayers = 1;
            for (int i = 0; i < reflectionZone.m_distances.Length; i++)
            {
                reflectionZone.m_distances[i] = 300f;
            }

            if (UnityEditor.EditorUtility.DisplayDialog("Select Mode", "Select which trigger type you'd like to use", "Box", "Sphere"))
            {
                BoxCollider collider = newObject.AddComponent<BoxCollider>();
                collider.size = m_colliderSize;
                collider.isTrigger = true;
            }
            else
            {
                SphereCollider collider = newObject.AddComponent<SphereCollider>();
                collider.radius = m_colliderRadiusSize;
                collider.isTrigger = true;
            }

            UnityEditor.Selection.activeObject = newObject;
            UnityEditor.EditorGUIUtility.PingObject(newObject);
            UnityEditor.SceneView.lastActiveSceneView.FrameSelected();
        }
#endif

        #endregion
    }
}