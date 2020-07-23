#if HDPipeline
using UnityEngine;
#if !UNITY_2019_3_OR_NEWER
using UnityEngine.Experimental.Rendering.HDPipeline;
#else
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Gaia
{
    public class PlanarReflections : MonoBehaviour
    {
        [HideInInspector]
        public PlanarReflectionProbe m_planarReflection;
        [HideInInspector]
        public GaiaSceneInfo m_sceneInfo;

        private Vector3 m_reflectionLocation;
        private float m_seaLevel;

        private void OnEnable()
        {
            PlanarReflectionsConfiguration();
        }

        public void PlanarReflectionsConfiguration()
        {
            //Gets all paramaters
            GetParamaters();

            if (m_planarReflection == null || m_sceneInfo == null)
            {
                Debug.LogError("Missing either " + m_planarReflection + " or " + m_sceneInfo);
                return;
            }
            else
            {
                UpdatePlanarSettings(m_planarReflection, m_sceneInfo);
            }
        }

        private void GetParamaters()
        {
            if (gameObject.name != "HD Planar Water Reflections")
            {
                gameObject.name = "HD Planar Water Reflections";
            }

            if (m_planarReflection == null)
            {
                m_planarReflection = GetComponent<PlanarReflectionProbe>();
                if (m_planarReflection == null)
                {
                    m_planarReflection = gameObject.AddComponent<PlanarReflectionProbe>();
                }
            }

            if (m_sceneInfo == null)
            {
                m_sceneInfo = GaiaSceneInfo.GetSceneInfo();
            }
        }

        private void UpdatePlanarSettings(PlanarReflectionProbe planarReflection, GaiaSceneInfo sceneInfo)
        {
            planarReflection.mode = ProbeSettings.Mode.Realtime;
            planarReflection.realtimeMode = ProbeSettings.RealtimeMode.EveryFrame;
            planarReflection.influenceVolume.boxSize = new Vector3(10000f, 30f, 10000f);

            if (m_seaLevel != sceneInfo.m_seaLevel)
            {
                m_reflectionLocation = new Vector3(0f, 0f, 0f);
                m_reflectionLocation.y = sceneInfo.m_seaLevel + 1f;
                m_seaLevel = sceneInfo.m_seaLevel;
            }
        }
    }
}
#endif