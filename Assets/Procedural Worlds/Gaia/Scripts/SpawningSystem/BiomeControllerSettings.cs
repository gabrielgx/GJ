// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using System.Collections.Generic;
using static Gaia.GaiaConstants;
using UnityEditor;

/*
 * Scriptable Object containing settings for a Biome Controller
 */

namespace Gaia
{
    [System.Serializable]
    public class BiomeControllerSettings : ScriptableObject, ISerializationCallbackReceiver
    {

        #region Public Variables
        ///// <summary>
        ///// Biome x location 
        ///// </summary>
        public float m_x = 0f;

        ///// <summary>
        ///// Biome y location 
        ///// </summary>
        public float m_y = 50f;

        ///// <summary>
        ///// Biome z location 
        ///// </summary>
        public float m_z = 0f;

        /// <summary>
        /// Range of the biome controller when spawning locally
        /// </summary>
        public float m_range = 1024;

        public float m_postProcessBlenddDistance;

        public Color m_visualisationColor = GaiaConstants.spawnerInitColor;

        public bool m_removeForeignGameObjects;
        public float m_removeForeignGameObjectStrength = 0.2f;
        public bool m_removeForeignTrees;
        public float m_removeForeignTreesStrength = 0.2f;
        public bool m_removeForeignTerrainDetails;
        public float m_removeForeignTerrainDetailsStrength = 0.2f;

        [SerializeField]
        private ImageMask[] imageMasks = new ImageMask[0];
        public string m_lastSavePath;

        //Clear flags
        public bool m_clearSpawnsToggleTrees;
        public bool m_clearSpawnsToggleDetails;
        public bool m_clearSpawnsToggleGOs;
        public bool m_clearSpawnsToggleSpawnExtensions;
        public ClearSpawnFor m_clearSpawnsFor;
        public ClearSpawnFromBiomes m_clearSpawnsFrom;

        //Using a property to make sure the image mask list is always initialized
        //<summary>All image filters that are being applied in this spawning process</summary>

        public ImageMask[] m_imageMasks {
                                            get
                                            {
                                                if (imageMasks == null)
                                                {
                                                    imageMasks = new ImageMask[0];
                                                }
                                                return imageMasks;
                                            }
                                            set
                                            {
                                                imageMasks = value;  
                                            }
        }
        
        //public  float m_powerOf;



        #endregion
        #region Serialization

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
        }

        #endregion
    }
}
