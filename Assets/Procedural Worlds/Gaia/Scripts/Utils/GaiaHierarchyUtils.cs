using UnityEngine;

namespace ProcedualWorlds.HierachySystem
{
    public class GaiaHierarchyUtils : MonoBehaviour
    {
        //Sets the hide flag to hide or show all objects within parent of this object in the Hierarchy
        [HideInInspector]
        public bool m_hideAllParentsInHierarchy = false;
        //All objects in the parent transforms
        [HideInInspector]
        public Transform[] m_gameObjects;

        /// <summary>
        /// Initilize on enable
        /// </summary>
        private void OnEnable()
        {
            //Get all objects
            m_gameObjects = GetAllParentObjects();
        }

        /// <summary>
        /// Configures the objects showed in Hierarchy to be shown or hidden
        /// </summary>
        public void SetupHideInHierarchy()
        {
            //Get all objects
            m_gameObjects = GetAllParentObjects();

            //Proceed if objects exist
            if (m_gameObjects != null)
            {
                foreach (Transform activeObject in m_gameObjects)
                {
                    if (activeObject.name != gameObject.name)
                    {
                        if (m_hideAllParentsInHierarchy)
                        {
                            //Hide
                            activeObject.gameObject.hideFlags = HideFlags.HideInHierarchy;
                        }
                        else
                        {
                            //Show
                            activeObject.gameObject.hideFlags = HideFlags.None;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Configures all GaiaHierarchyUtils components and in each of the objects showed in Hierarchy to be shown or hidden
        /// </summary>
        public void SetupAllHideInHierarchy(bool enabled)
        {
            //Get all GaiaHierarchyUtils
            GaiaHierarchyUtils[] hierarchyUtils = FindObjectsOfType<GaiaHierarchyUtils>();

            //Proceed if objects exist
            if (hierarchyUtils != null)
            {
                foreach (GaiaHierarchyUtils utils in hierarchyUtils)
                {
                    utils.m_hideAllParentsInHierarchy = enabled;
                    utils.m_gameObjects = utils.GetComponentsInChildren<Transform>();
                    utils.SetupHideInHierarchy();
                }
            }
        }

        /// <summary>
        /// Gets all the obejcts parented to this object
        /// </summary>
        /// <returns></returns>
        public Transform[] GetAllParentObjects()
        {
            Transform[] parentObjects = this.GetComponentsInChildren<Transform>();

            if (parentObjects != null)
            {
                return parentObjects;
            }
            else
            {
                return null;
            }
        }
    }
}