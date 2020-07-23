using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    /// <summary>
    /// Utility class to provide the functionality of performing an "orthographic bake" from anywhere and return a render texture as result. 
    /// In an orthographic bake an orthographic camera is placed above the terrain pointing straight downwards to render the current view to a render texture.
    /// </summary>
    public class OrthographicBake
    {
        static Camera m_orthoCamera;
        static RenderTexture m_tmpRenderTexture;

        public static Camera CreateOrthoCam(Vector3 position, float nearClipping, float farClipping, float size, LayerMask cullingMask)
        {
            //existing ortho cam? Try to recycle
            GameObject gameObject = GameObject.Find("OrthoCaptureCam");

            if(gameObject==null)
            {
                gameObject = new GameObject("OrthoCaptureCam");
            }
            gameObject.transform.position = position;
            //facing straight downwards
#if UPPipeline
            //the 180 on y axis is to offset the rotation that occurs when capturing in URP for some reason
            gameObject.transform.rotation = Quaternion.Euler(90f, 180f, 0f);
#elif HDPipeline
            //the 180 on y axis is to offset the rotation that occurs when capturing in HDRP for some reason
            gameObject.transform.rotation = Quaternion.Euler(90f, 180f, 0f);
#else
             gameObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
#endif

            //existing Camera? Try to recycle
            Camera cam = gameObject.GetComponent<Camera>();

            if (cam == null)
            {
                cam = gameObject.AddComponent<Camera>();
            }

            //setup camera the way we need it for the ortho bake - adjust everything to default to make sure there is no interference
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = Color.black;
            cam.cullingMask = cullingMask;
            cam.orthographic = true;
            cam.orthographicSize = size;
            cam.nearClipPlane = nearClipping;
            cam.farClipPlane = farClipping;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.depth = 0f;
            cam.renderingPath = RenderingPath.Forward; //Forward rendering required for orthographic
            cam.useOcclusionCulling = true;

#if HDPipeline
            cam.gameObject.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
#endif

            m_orthoCamera = cam;
            return cam;

        }

        public static void RemoveOrthoCam()
        {
            if (m_orthoCamera == null)
            {
                return;
            }

            if (m_orthoCamera.targetTexture != null)
            {
                RenderTexture.ReleaseTemporary(m_orthoCamera.targetTexture);
                m_orthoCamera.targetTexture = null;
            }

            GameObject.DestroyImmediate(m_orthoCamera.gameObject);
        }

        public static void BakeTerrain(Terrain terrain, int Xresolution, int Yresolution, string path, LayerMask cullingMask)
        {
            CreateOrthoCam(terrain.GetPosition() + new Vector3(terrain.terrainData.size.x / 2f, 0f, terrain.terrainData.size.z / 2f), -(terrain.terrainData.size.y + 200f), 1f, terrain.terrainData.size.x / 2f, cullingMask);
            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor();
            rtDesc.autoGenerateMips = true;
            rtDesc.bindMS = false;
            rtDesc.colorFormat = RenderTextureFormat.Default;
            rtDesc.depthBufferBits = 24;
            rtDesc.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            rtDesc.enableRandomWrite = false;
            //rtDesc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8_SRGB;
            rtDesc.height = Yresolution;
            rtDesc.memoryless = RenderTextureMemoryless.None;
            rtDesc.msaaSamples = 1;
            rtDesc.sRGB = false;
            rtDesc.shadowSamplingMode = UnityEngine.Rendering.ShadowSamplingMode.None;
            rtDesc.useDynamicScale = false;
            rtDesc.useMipMap = false;
            rtDesc.volumeDepth = 1;
            rtDesc.vrUsage = VRTextureUsage.None;
            rtDesc.width = Xresolution;
            m_tmpRenderTexture = RenderTexture.GetTemporary(rtDesc);
            RenderToPng(path);
        }

        private static void RenderToPng(string path)
        {
            if (m_orthoCamera == null)
            {
                Debug.LogError("Orthographic Bake: Camera does not exist!");
                return;
            }

            m_orthoCamera.targetTexture = m_tmpRenderTexture;
            m_orthoCamera.Render();
            RenderTexture.active = m_tmpRenderTexture;
            ImageProcessing.WriteRenderTexture(path, m_tmpRenderTexture, GaiaConstants.ImageFileType.Png, TextureFormat.RGBA32);
            m_orthoCamera.targetTexture = null;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(m_tmpRenderTexture);


        }
    }

}