using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    [System.Serializable]
    public class BakedMaskCacheEntry
    {
        public string fileName;
        public string assetPath;
        public string fullPath;
        public RenderTexture texture;
    }

    [System.Serializable]
    public class BakedMaskCache : UnityEngine.Object
    {
        public bool m_autoSpawnRunning = false;
        //bool m_cacheClearedForAutoSpawn = false;
        public GaiaSession m_gaiaSession;
        public GaiaSessionManager m_gaiaSessionManager;
        private Terrain m_worldMapTerrain;

        public BakedMaskCacheEntry[] m_cacheEntries {
            get
            {
                if (m_gaiaSession == null)
                {
                    m_gaiaSession = GaiaSessionManager.GetSessionManager(false).m_session;
                }
                return m_gaiaSession.m_bakedMaskCacheEntries;
            }
            set
            {
                if (m_gaiaSession == null)
                {
                    m_gaiaSession = GaiaSessionManager.GetSessionManager(false).m_session;
                }
                m_gaiaSession.m_bakedMaskCacheEntries = value;
            }
        }
       

        public RenderTexture BakeTerrainTagCollisions(Terrain terrain, string tag, float radius)
        {
            GameObject[] allGOsWithTag = new GameObject[0];

            bool tagFound = false;
            try
            {
                allGOsWithTag = GameObject.FindGameObjectsWithTag(tag);
                tagFound = true;
            }
            catch
            {
                tagFound = false;
            }

            if (!tagFound)
            {
                Debug.LogWarning("Could not find Game Objects for tag: " + tag + " when trying to bake a collision mask. Does this tag exist?" );
                return null;
            }


            //Array to store all positions and radi of the objects
            Vector4[] posAndRad = new Vector4[allGOsWithTag.Length];
            for (int i = 0; i < allGOsWithTag.Length; i++)
            {
                //need scalar position on the terrain from 0..1
                //get relative positon on the terrain first
                float scaleFactor = (allGOsWithTag[i].transform.localScale.x + allGOsWithTag[i].transform.localScale.z) / 2f;
                Vector4 v4 = new Vector4(allGOsWithTag[i].transform.position.x, allGOsWithTag[i].transform.position.y, allGOsWithTag[i].transform.position.z, WorldDistance2UVDistance(terrain, radius * scaleFactor));
                v4.x -= terrain.transform.position.x;
                v4.y -= terrain.transform.position.y;
                v4.z -= terrain.transform.position.z;
                //now make that relative position scalar (0..1)
                v4.x /= terrain.terrainData.size.x;
                v4.y /= terrain.terrainData.size.y;
                v4.z /= terrain.terrainData.size.z;
                //flip on z-axis
                v4.z = 1 - v4.z;

                posAndRad[i] = v4;
            }

            string fileName = GetTagCollisionMaskFileName(terrain, tag, radius);

            return BakeVectorArrayForTerrain(posAndRad, terrain, fileName);
        }

        public void WriteCacheToDisk()
        {
#if UNITY_EDITOR

            int length = m_cacheEntries.Length;
            for (int i = 0; i < length; i++)
            {
                ImageProcessing.WriteRenderTexture(m_cacheEntries[i].fullPath, m_cacheEntries[i].texture);

                //var importer = AssetImporter.GetAtPath(collisionMaskCache[i].assetPath) as TextureImporter;
                ////Set texture up as readable, otherwise it can create issues when overwriting later
                //if (importer != null)
                //{
                //    importer.textureType = TextureImporterType.Default;
                //    importer.isReadable = true;
                //}

                //Refresh mask immediately by reimporting it in the same step
                AssetDatabase.ImportAsset(m_cacheEntries[i].assetPath, ImportAssetOptions.ForceUpdate);

                m_cacheEntries[i].texture = null;

            }

            //clear the cache for a clean start, this will free the cache from outdated entries, etc.
            //the cache will be rebuilt quickly only with the relevant files which are saved to disk now.
            m_cacheEntries = new BakedMaskCacheEntry[0];
#endif
        }

        public RenderTexture BakeTerrainWorldBiomeMask(Terrain terrain, string worldBiomeMaskGUID)
        {
            if (m_gaiaSessionManager == null)
            {
                m_gaiaSessionManager = GaiaSessionManager.GetSessionManager(false);
            }
            if (m_worldMapTerrain == null)
            {
                m_worldMapTerrain = TerrainHelper.GetWorldMapTerrain();
            }
            if (m_worldMapTerrain ==null)
            {
                Debug.LogWarning("Found no world map terrain for baking a world biome mask.");
                return null;
            }
            if (String.IsNullOrEmpty(worldBiomeMaskGUID))
            {
                return null;
            }

            if (m_gaiaSessionManager.m_session == null)
            {
                Debug.LogWarning("Found no session for baking a world biome mask.");
                return null;
            }

            if (m_gaiaSessionManager.m_session.m_worldBiomeMaskSettings == null)
            {
                Debug.LogWarning("Found no world designer settings in the session for baking a world biome mask.");
                return null;
            }

            //we need to apply the mask stack for this biome mask on the world map to get the result, then copy the appropiate rectangle for the queried terrain into the cache & return it.
            bool worldMapActiveState = m_worldMapTerrain.gameObject.activeInHierarchy;
            m_worldMapTerrain.gameObject.SetActive(true);
            GameObject emptyGO = new GameObject();
            emptyGO.transform.position = new Vector3(m_worldMapTerrain.transform.position.x + m_worldMapTerrain.terrainData.size.x / 2f, m_worldMapTerrain.transform.position.y, m_worldMapTerrain.transform.position.z + +m_worldMapTerrain.terrainData.size.z / 2f);
            GaiaMultiTerrainOperation operation = new GaiaMultiTerrainOperation(m_worldMapTerrain, emptyGO.transform, m_worldMapTerrain.terrainData.size.x);
            operation.m_isWorldMapOperation = true;
            operation.GetHeightmap();
            operation.GetNormalmap();
            operation.CollectTerrainBakedMasks();
            RenderTextureDescriptor rtDescriptor = operation.RTbakedMask.descriptor;

            RenderTexture inputTexture = RenderTexture.GetTemporary(rtDescriptor);

            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = inputTexture;
            GL.Clear(true, true, Color.white);
            RenderTexture.active = currentRT;
            RenderTexture ruleOutputTexture = RenderTexture.GetTemporary(rtDescriptor);

            ImageMask[] maskStack = m_gaiaSessionManager.m_session.m_worldBiomeMaskSettings.m_spawnerRules.Find(x=>x.GUID == worldBiomeMaskGUID).m_imageMasks;

            //Calculate Target position &/ resolution
            BoundsDouble bounds = new BoundsDouble();
            TerrainHelper.GetTerrainBounds(ref bounds);
            bounds.center -= GaiaTerrainLoaderManager.Instance.GetOrigin();
            RenderTexture chunkContent = new RenderTexture(terrain.terrainData.heightmapTexture.descriptor);
            int maxTilesX = Mathf.RoundToInt((float)bounds.size.x / terrain.terrainData.size.x);
            int maxTilesZ = Mathf.RoundToInt((float)bounds.size.z / terrain.terrainData.size.z);
            int currentTileX = Mathf.RoundToInt((terrain.transform.position.x - (float)bounds.min.x) / terrain.terrainData.size.x);
            int currentTileZ = Mathf.RoundToInt((terrain.transform.position.z - (float)bounds.min.z) / terrain.terrainData.size.z);
            float res = (terrain.terrainData.heightmapResolution) / ((float)bounds.size.x / terrain.terrainData.bounds.size.x * (terrain.terrainData.heightmapResolution - 1));


            Bounds worldSpaceBounds = terrain.terrainData.bounds;
            worldSpaceBounds.center = new Vector3(worldSpaceBounds.center.x + terrain.transform.position.x, worldSpaceBounds.center.y + terrain.transform.position.y, worldSpaceBounds.center.z + terrain.transform.position.z);
            float xPos = ((float)currentTileX * terrain.terrainData.heightmapResolution) / (maxTilesX * terrain.terrainData.heightmapResolution);
            float zPos = ((float)currentTileZ * terrain.terrainData.heightmapResolution) / (maxTilesZ * terrain.terrainData.heightmapResolution);
            Vector2 pos = new Vector2(xPos, zPos);

            //If we have a mask stack, we need to process it
            if (maskStack != null && maskStack.Length > 0)
            {

                //We start from a white texture, so we need the first mask action in the stack to always be "Multiply", otherwise there will be no result.
                maskStack[0].m_blendMode = ImageMaskBlendMode.Multiply;

                float maxWorldHeight = 0f;
                float minWorldHeight = 0f;
                m_gaiaSessionManager.GetWorldMinMax(ref minWorldHeight, ref maxWorldHeight, true);

                //Iterate through all image masks and set up the current paint context in case the shader uses heightmap data
                foreach (ImageMask mask in maskStack)
                {
                    mask.m_multiTerrainOperation = operation;
                    mask.m_seaLevel = m_gaiaSessionManager.GetSeaLevel(true);
                    mask.m_maxWorldHeight = maxWorldHeight;
                    mask.m_minWorldHeight = minWorldHeight;
                }

                Graphics.Blit(ImageProcessing.ApplyMaskStack(inputTexture, ruleOutputTexture, maskStack, ImageMaskInfluence.Local), ruleOutputTexture);
            }
            else
            {
                //no mask stack -> just blit the white input texture over as the output
                Graphics.Blit(inputTexture, ruleOutputTexture);
            }
            operation.CloseOperation();
            DestroyImmediate(emptyGO);
            RenderTexture.ReleaseTemporary(inputTexture);
            m_worldMapTerrain.gameObject.SetActive(worldMapActiveState);

            //copy the rule output into the right size for the requested terrain chunk
            Graphics.Blit(ruleOutputTexture, chunkContent, new Vector2(res, res), pos);
            //ImageProcessing.WriteRenderTexture("D:\\chunkContent" + terrain.name, chunkContent);


            string filename = GetWorldBiomeMaskFilename(terrain, worldBiomeMaskGUID);
            SaveBakedMaskInCache(chunkContent, terrain, filename);

            RenderTexture.ReleaseTemporary(ruleOutputTexture);

            return chunkContent;
        }

        public RenderTexture BakeTerrainTreeCollisions(Terrain terrain, int treePrototypeId, string spawnRuleGUID, float boundsRadius)
        {
            //Iterate through the trees and build a v4 array with the positions

            TreeInstance[] allRelevantTrees = terrain.terrainData.treeInstances.Where(x => x.prototypeIndex == treePrototypeId).ToArray();
            //Array to store all positions and radi of the tree
            Vector4[] posAndRad = new Vector4[allRelevantTrees.Length];

            for (int i = 0; i < allRelevantTrees.Length; i++)
            {
                //note the flip on the z-axis
                posAndRad[i] = new Vector4(allRelevantTrees[i].position.x, allRelevantTrees[i].position.y, 1-allRelevantTrees[i].position.z, WorldDistance2UVDistance(terrain,boundsRadius * allRelevantTrees[i].widthScale));
            }

            string fileName = GetTreeCollisionMaskFileName(terrain, spawnRuleGUID, boundsRadius);

            return BakeVectorArrayForTerrain(posAndRad, terrain, fileName);

        }

        /// <summary>
        /// Converts a regular world space unity unit distance to a distance in scalar (0-1) UV-space on a terrain.
        /// </summary>
        /// <param name="Terrain">The terrain on which the conversion takes place</param>
        /// <param name="distance">The distance to convert.</param>
        /// <returns></returns>
        private float WorldDistance2UVDistance(Terrain terrain, float distance)
        {
            float longerSideLength = Mathf.Max(terrain.terrainData.size.x, terrain.terrainData.size.z);
            return Mathf.InverseLerp(0, longerSideLength, distance);
        }

        private RenderTexture BakeVectorArrayForTerrain(Vector4[] posAndRad, Terrain terrain, string filename)
        {
            //setting up with default settings from the paint context source render texture
            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor();
            rtDesc.autoGenerateMips = true;
            rtDesc.bindMS = false;
            rtDesc.colorFormat = RenderTextureFormat.R16;
            rtDesc.depthBufferBits = 0;
            rtDesc.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            rtDesc.enableRandomWrite = true;
            rtDesc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UNorm;
            rtDesc.height = Mathf.RoundToInt(terrain.terrainData.size.z);
            rtDesc.memoryless = RenderTextureMemoryless.None;
            rtDesc.msaaSamples = 1;
            rtDesc.sRGB = false;
            rtDesc.shadowSamplingMode = UnityEngine.Rendering.ShadowSamplingMode.None;
            rtDesc.useDynamicScale = false;
            rtDesc.useMipMap = false;
            rtDesc.volumeDepth = 1;
            rtDesc.vrUsage = VRTextureUsage.None;
            rtDesc.width = Mathf.RoundToInt(terrain.terrainData.size.x);

            RenderTexture collisionMask = new RenderTexture(rtDesc);
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = collisionMask;
            //we start with a full white texture, the areas that would create a collision are painted in black
            GL.Clear(true, true, Color.white);
            RenderTexture.active = currentRT;

            //We need a buffer texture to continously feed the output of the tree collision baking back into the shader.
            //while still iterating through the trees.
            RenderTexture collisionMaskBuffer = RenderTexture.GetTemporary(collisionMask.descriptor);
            //Clear the buffer for a clean start
            Graphics.Blit(collisionMask, collisionMaskBuffer);
            //Prepare a material that will bake the trees into a mask as we spawn them
            Material treeBakeMat = new Material(Shader.Find("Hidden/Gaia/TreeCollisionMaskBaking"));
            treeBakeMat.SetFloat("_Strength", 1f);
            AnimationCurve tempCurve = new AnimationCurve(new Keyframe[] { new Keyframe { time = 0, value = 1 }, new Keyframe { time = 1, value = 0 } });
            Texture2D tempTexture = new Texture2D(256, 1);
            ImageProcessing.CreateMaskCurveTexture(ref tempTexture);
            ImageProcessing.BakeCurveTexture(tempCurve, tempTexture);
            treeBakeMat.SetTexture("_DistanceMaskTex", tempTexture);


            treeBakeMat.SetInt("_Invert", 0);

            //Iterate through the trees and bake
            foreach (Vector4 v4 in posAndRad)
            {
                treeBakeMat.SetFloat("_BoundsRadius", v4.w);
                treeBakeMat.SetVector("_TargetPosition", new Vector4(v4.x, v4.z, 0, 0));
                treeBakeMat.SetTexture("_InputTex", collisionMaskBuffer);
                Graphics.Blit(collisionMaskBuffer, collisionMask, treeBakeMat, 2);
                Graphics.Blit(collisionMask, collisionMaskBuffer);
            }

            //Store the final result in the appropiate file

            SaveBakedMaskInCache(collisionMask, terrain, filename);
            //Clean Up
            DestroyImmediate(tempTexture);
            tempTexture = null;
            //RenderTexture.ReleaseTemporary(collisionMask);
            RenderTexture.ReleaseTemporary(collisionMaskBuffer);
            return collisionMask;
        }


        private string GetTreeCollisionMaskFileName(Terrain terrain, string spawnRuleGUID, float radius)
        {
            return terrain.name + "_" + BakedMaskTypeInternal.TerrainTree.ToString() + "_" + spawnRuleGUID + "_" + radius.ToString();
        }

        private string GetTreeCollisionMaskSearchString(string spawnRuleGUID)
        {
            return BakedMaskTypeInternal.TerrainTree.ToString() + "_" + spawnRuleGUID.ToString();
        }


        private string GetTagCollisionMaskFileName(Terrain terrain, string m_tag, float radius)
        {
            return terrain.name + "_" + BakedMaskTypeInternal.Tag.ToString() + "_" + m_tag + "_" + radius.ToString();
        }


        private string GetTagCollisionMaskSearchString(string tag)
        {
            return BakedMaskTypeInternal.Tag.ToString() + "_" + tag;
        }

        private string GetWorldBiomeMaskFilename(Terrain terrain, string GUID)
        {
            return terrain.name + "_" + BakedMaskTypeInternal.WorldBiomeMask.ToString() + "_" + GUID;
        }

        private string GetWorldBiomeMaskSearchString(string GUID)
        {
            return BakedMaskTypeInternal.WorldBiomeMask.ToString() + "_" + GUID;
        }

        private void SaveBakedMaskInCache(RenderTexture renderTexture, Terrain terrain, string fileName)
        {
            if (m_cacheEntries == null)
            {
                m_cacheEntries = new BakedMaskCacheEntry[0];
            }

            //Convert the RT
            //Texture2D texture = GaiaUtils.ConvertRenderTextureToTexture2D(renderTexture);

            //texture.name = fileName;

            //Check if there is an entry already
            int length = m_cacheEntries.Length;
            bool found = false;
            for (int i = 0; i < length; i++)
            {
                if (m_cacheEntries[i].fileName == fileName)
                {
                    //found an entry, update with new render texture
                    found = true;
                    if (m_cacheEntries[i].texture != null)
                    {
                        m_cacheEntries[i].texture.Release();
                        DestroyImmediate(m_cacheEntries[i].texture);
                    }
                    WriteCacheEntry(m_cacheEntries[i], renderTexture, terrain, fileName);
                    break;
                }
            }

            //Not found? Need to append cache array and write the new entry at the end
            if (!found)
            {
                AddNewCollisionMaskCacheEntry(renderTexture, terrain, fileName);
            }


        }

        private void AddNewCollisionMaskCacheEntry(RenderTexture texture, Terrain terrain, string fileName)
        {
            BakedMaskCacheEntry[] newArray = new BakedMaskCacheEntry[m_cacheEntries.Length + 1];
            int length2 = m_cacheEntries.Length;
            for (int i = 0; i < length2; i++)
            {
                newArray[i] = m_cacheEntries[i];
            }
            newArray[newArray.Length - 1] = new BakedMaskCacheEntry();
            WriteCacheEntry(newArray[newArray.Length - 1], texture, terrain, fileName);
            m_cacheEntries = newArray;
            //EditorUtility.SetDirty(this);
#if UNITY_EDITOR
            EditorUtility.SetDirty(GaiaSessionManager.GetSessionManager(false));
#endif
        }

        private void WriteCacheEntry(BakedMaskCacheEntry entry, RenderTexture texture, Terrain terrain, string fileName)
        {

            //build the paths
            string assetPath = GaiaDirectories.GetTerrainCollisionDirectory(terrain) + "/" + fileName;
            string fullPath = assetPath.Replace("Assets", Application.dataPath);

            //clear old texture
            if (entry.texture != null && entry.texture != Texture2D.whiteTexture)
            {
                UnityEngine.Object.DestroyImmediate(entry.texture);
                entry.texture = null;
            }
            //overwrite texture contents
            entry.texture = texture;

            //assign Paths & name
            entry.fileName = fileName;
            entry.assetPath = assetPath;
            entry.fullPath = fullPath;
            //EditorUtility.SetDirty(entry);
            //ImageProcessing.WriteTexture2D(entry.fullPath, entry.texture);
            //AssetDatabase.ImportAsset(entry.assetPath, ImportAssetOptions.ForceUpdate);
        }

        public RenderTexture LoadBakedMask(Terrain terrain, BakedMaskTypeInternal type, float radius, int id = 0, string tag = "", string GUID ="")
        {

            if (m_cacheEntries == null)
            {
                m_cacheEntries = new BakedMaskCacheEntry[0];
            }

            string fileName = "";
            switch (type)
            {
                case BakedMaskTypeInternal.TerrainTree:
                    fileName = GetTreeCollisionMaskFileName(terrain, GUID, radius);
                    break;

                case BakedMaskTypeInternal.Tag:
                    fileName = GetTagCollisionMaskFileName(terrain, tag, radius);
                    break;
                case BakedMaskTypeInternal.WorldBiomeMask:
                    fileName = GetWorldBiomeMaskFilename(terrain, GUID);
                    break;
                default:
                    return null;
            }

            if (fileName != "")
            {
                //file in cache? If yes, return from there
                int length = m_cacheEntries.Length;
                for (int i = 0; i < length; i++)
                {
                    if (m_cacheEntries[i].fileName == fileName)
                        //does it have a texture as well?
                        if (m_cacheEntries[i].texture != null)
                        {
                            //found it, we can return it & are done
                            return m_cacheEntries[i].texture;
                        }
                }

                //not in cache? We need to bake it then!
                switch (type)
                {
                    case BakedMaskTypeInternal.TerrainTree:
                        return BakeTerrainTreeCollisions(terrain, id, GUID, radius);
                    case BakedMaskTypeInternal.Tag:
                        return BakeTerrainTagCollisions(terrain, tag, radius);
                    case BakedMaskTypeInternal.WorldBiomeMask:
                        return BakeTerrainWorldBiomeMask(terrain, GUID);
                    default:
                        return null;
                }


                //not in cache? try reading from disk...
#if UNITY_EDITOR
                //Texture2D texture = (Texture2D)AssetDatabase.LoadAssetAtPath(GaiaDirectories.GetTerrainCollisionDirectory(terrain) + "/" + GetTreeCollisionMaskFileName(terrain, id, radius), typeof(Texture2D));


                //if (texture != null)
                //{
                //    //found it on disk, write a copy of it in cache for next access attempt, then return the copy.
                //    //We need to work with a copy here because otherwise we can run into issue when trying to overwrite the image file
                //    Texture2D copy = new Texture2D(texture.width, texture.height, texture.format, true);
                //    Graphics.CopyTexture(texture, copy);
                //    AddNewCollisionMaskCacheEntry(copy, terrain, fileName);
                //    return copy;
                //}
#endif

            }
            else return null;


        }



        public void BakeAllTreeCollisions(string spawnRuleGUID, float radius)
        {
            SpawnRule sr = CollisionMask.m_allTreeSpawnRules.FirstOrDefault(x => x.GUID == spawnRuleGUID);
            if (sr == null)
            {
                return;
            }

            foreach (Terrain t in Terrain.activeTerrains)
            {
                int treePrototypeID = TerrainHelper.GetTreePrototypeIDFromSpawnRule(sr, t);
                if (treePrototypeID != -1)
                {
                    BakeTerrainTreeCollisions(t, treePrototypeID, spawnRuleGUID, radius);
                }
            }
        }

        public void BakeAllTagCollisions(string m_tag, float m_tagRadius)
        {
            foreach (Terrain t in Terrain.activeTerrains)
            {
                BakeTerrainTagCollisions(t, m_tag, m_tagRadius);
            }
        }

        public void ClearCache()
        {
#if UNITY_EDITOR

            int length = m_cacheEntries.Length;
            for (int i = 0; i < length; i++)
            {
                if (m_cacheEntries[i].texture != null)
                {
                    m_cacheEntries[i].texture.Release();
                    //DestroyImmediate(m_cacheEntries[i].texture);
                    m_cacheEntries[i].texture = null;
                }

            }

            m_cacheEntries = new BakedMaskCacheEntry[0];
#endif
        }

        public void ClearCacheForSpawn()
        {
            //if (!m_autoSpawnRunning)
            //{
            //    ClearCache();
            //}
            //else
            //{
            //    if (!m_cacheClearedForAutoSpawn)
            //    {
            //        ClearCache();
            //        m_cacheClearedForAutoSpawn = true;
            //    }
            //}
            ClearCache();
        }



        public void SetWorldBiomeMaskDirty(string worldBiomeMaskGUID)
        {
            string searchString = GetWorldBiomeMaskSearchString(worldBiomeMaskGUID);
            SetDirty(searchString);

        }

        public void SetTreeDirty(string spawnRuleGUID)
        {
            string searchString = GetTreeCollisionMaskSearchString(spawnRuleGUID);
            SetDirty(searchString);

        }

        public void SetTagDirty(string tag)
        {
            string searchString = GetTagCollisionMaskSearchString(tag);
            SetDirty(searchString);
        }


        private void SetDirty(string searchString)
        {
            //Release the affected render textures
            for (int i = 0; i < m_cacheEntries.Length; i++)
            {
                if (m_cacheEntries[i].fileName.Contains(searchString))
                {
                    if (m_cacheEntries[i].texture != null)
                    {
                        m_cacheEntries[i].texture.Release();
                        //DestroyImmediate(m_cacheEntries[i].texture);
                        m_cacheEntries[i].texture = null;
                    }
                }
            }
            //Remove the affected array entries
            m_cacheEntries = m_cacheEntries.Where(x => x.fileName.Contains(searchString) != true).ToArray();
        }

        //public void BeginAutoSpawn()
        //{
        //    m_autoSpawnRunning = true;
        //    m_cacheClearedForAutoSpawn = false;
        //}

        //public void EndAutoSpawn()
        //{
        //    m_autoSpawnRunning = false;
        //    m_cacheClearedForAutoSpawn = false;
        //}

    }
}