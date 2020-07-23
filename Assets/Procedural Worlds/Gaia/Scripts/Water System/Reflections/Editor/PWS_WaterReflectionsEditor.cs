using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using PWCommon2;
using Gaia.Internal;
using Gaia;

namespace ProcedualWorlds.WaterSystem
{
    /// <summary>
    /// Editor for the PWS_WaterReflections
    /// </summary>
    [CustomEditor(typeof(PWS_WaterSystem))]
    public class PWS_WaterReflectionsEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private PWS_WaterSystem WaterReflections;

        private void OnEnable()
        {
            WaterReflections = (PWS_WaterSystem)target;

            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
        }

        #region Inspector Region

        /// <summary>
        /// Custom editor for PWS_WaterReflections
        /// </summary
        public override void OnInspectorGUI()
        {
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            if (WaterReflections == null)
            {
                WaterReflections = (PWS_WaterSystem)target;
            }

            m_editorUtils.Panel("GlobalSettings", GlobalSettings, true);
        }

        #endregion

        #region Panel

        /// <summary>
        /// Global Main Panel
        /// </summary>
        /// <param name="helpEnabled"></param>
        private void GlobalSettings(bool helpEnabled)
        {
            if (m_editorUtils.Button("EditReflectionSettings"))
            {
                GaiaUtils.FocusWaterProfile();
            }
        }

        #endregion
    }
}
