using UnityEngine;
using UnityEditor;
using Gaia.Internal;
using PWCommon2;
using ProcedualWorlds.HierachySystem;

namespace Gaia
{
    [CustomEditor(typeof(GaiaHierarchyUtils))]
    public class GaiaHierarchyUtilsEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private GaiaHierarchyUtils m_profile;
        private Color m_guiBackground;
        private Color m_redGUI = new Color(0.905f, 0.415f, 0.396f, 1f);
        private Color m_greenGUI = new Color(0.696f, 0.905f, 0.397f, 1f);

        private void OnEnable()
        {
            //Get GaiaHierarchyUtils Profile object
            m_profile = (GaiaHierarchyUtils)target;

            //Gets all the objects
            m_profile.m_gameObjects = m_profile.GetAllParentObjects();

            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            m_guiBackground = GUI.backgroundColor;
        }

        public override void OnInspectorGUI()
        {
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            if (m_profile == null)
            {
                //Get GaiaHierarchyUtils Profile object
                m_profile = (GaiaHierarchyUtils)target;
            }

            //Monitor for changes
            EditorGUI.BeginChangeCheck();

            m_editorUtils.Panel("GlobalSettings", GlobalSettings, true);

            //Check for changes, make undo record, make changes and let editor know we are dirty
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_profile, "Made changes");
                EditorUtility.SetDirty(m_profile);

                m_profile.SetupHideInHierarchy();
            }
        }

        /// <summary>
        /// Global settings
        /// </summary>
        /// <param name="helpEnabled"></param>
        private void GlobalSettings(bool helpEnabled)
        {
            //m_profile.m_hideAllParentsInHierarchy = m_editorUtils.ToggleLeft("HideAllParentsInHierarchy", m_profile.m_hideAllParentsInHierarchy, helpEnabled);

            if (m_profile.m_gameObjects != null)
            {
                int count = m_profile.m_gameObjects.Length - 1;
                if (count == 0)
                {
                    EditorGUILayout.HelpBox("No Objects parented to this object to use this system please add some parent objects to this gameobject.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField("Object count in parent: " + count);
                    if (m_profile.m_hideAllParentsInHierarchy)
                    {
                        GUI.backgroundColor = m_redGUI;
                        if (m_editorUtils.Button("Show"))
                        {
                            m_profile.m_hideAllParentsInHierarchy = false;
                        }
                    }
                    else
                    {
                        GUI.backgroundColor = m_greenGUI;
                        if (m_editorUtils.Button("Hide"))
                        {
                            m_profile.m_hideAllParentsInHierarchy = true;
                        }
                    }

                    GUI.backgroundColor = m_guiBackground;

                    m_editorUtils.InlineHelp("HideAllParentsInHierarchy", helpEnabled);

                    if (m_editorUtils.Button("ConfigureAllInScene"))
                    {
                        m_profile.SetupAllHideInHierarchy(m_profile.m_hideAllParentsInHierarchy);
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("Object count in parent: 0");
            }
        }
    }
}