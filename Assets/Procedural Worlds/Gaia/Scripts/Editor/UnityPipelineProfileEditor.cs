using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PWCommon2;
using Gaia.Internal;
using System;
using System.Linq;
using System.IO;
using ProcedualWorlds.Gaia.PackageSystem;

namespace Gaia.Pipeline
{
    [CustomEditor(typeof(UnityPipelineProfile))]
    public class UnityPipelineProfileEditor : PWEditor
    {
        private GUIStyle m_boxStyle;
        private bool m_showOptions;
        private Color defaultBackground;
        private EditorUtils m_editorUtils;
        private UnityPipelineProfile m_profile;
        private string m_version;
        private bool[] m_materialLibraryFoldouts;
        private GUIStyle m_matlibButtonStyle;

        private void OnEnable()
        {
            //Initialization
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            //Get Gaia Lighting Profile object
            m_profile = (UnityPipelineProfile)target;

            m_version = PWApp.CONF.Version;

            m_materialLibraryFoldouts = new bool[m_profile.m_materialLibrary.Length];
        }

        public override void OnInspectorGUI()
        {           
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            //Set up the box style
            if (m_boxStyle == null)
            {
                m_boxStyle = new GUIStyle(GUI.skin.box);
                m_boxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                m_boxStyle.fontStyle = FontStyle.Bold;
                m_boxStyle.alignment = TextAnchor.UpperLeft;
            }

            //Monitor for changes
            EditorGUI.BeginChangeCheck();

            defaultBackground = GUI.backgroundColor;

            EditorGUILayout.LabelField("Profile Version: " + m_version);

            bool enableEditMode = System.IO.Directory.Exists(GaiaUtils.GetAssetPath("Dev Utilities"));
            if (enableEditMode)
            {
                m_profile.m_editorUpdates = EditorGUILayout.ToggleLeft("Use Procedural Worlds Editor Settings", m_profile.m_editorUpdates);
            }
            else
            {
                m_profile.m_editorUpdates = false;
            }

            m_editorUtils.Panel("PipelineVersionSupport", PipelineVersionSettingsEnabled, false);
            if (m_profile.m_editorUpdates)
            {
                m_editorUtils.Panel("ProfileSettings", ProfileSettingsEnabled, false);
            }

            //Check for changes, make undo record, make changes and let editor know we are dirty
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_profile, "Made changes");
                EditorUtility.SetDirty(m_profile);
            }
        }

        private void PipelineVersionSettingsEnabled(bool helpEnabled)
        {
            if (Application.unityVersion.Contains("2019.1"))
            {
                m_editorUtils.Heading("LWRPSupport");
                GUILayout.BeginHorizontal();
                m_editorUtils.Label("MinimumLWRPVersion");
                EditorGUILayout.LabelField(m_profile.m_min2019_1LWVersion);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                m_editorUtils.Label("MaximumLWRPVersion");
                EditorGUILayout.LabelField(m_profile.m_max2019_1LWVersion);
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();

                m_editorUtils.Heading("HDRPSupport");
                GUILayout.BeginHorizontal();
                m_editorUtils.Label("MinimumHDRPVersion");
                EditorGUILayout.LabelField(m_profile.m_min2019_1HDVersion);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                m_editorUtils.Label("MaximumHDRPVersion");
                EditorGUILayout.LabelField(m_profile.m_max2019_1HDVersion);
                GUILayout.EndHorizontal();
            }

            if (Application.unityVersion.Contains("2019.2"))
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Minimum LWRP Version " + m_profile.m_min2019_2LWVersion);
                EditorGUILayout.LabelField("Maximum LWRP Version " + m_profile.m_max2019_2LWVersion);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Minimum HDRP Version " + m_profile.m_min2019_2HDVersion);
                EditorGUILayout.LabelField("Maximum HDRP Version " + m_profile.m_max2019_2HDVersion);
                GUILayout.EndHorizontal();
            }

            if (Application.unityVersion.Contains("2019.3"))
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Minimum HDRP Version " + m_profile.m_min2019_3HDVersion);
                EditorGUILayout.LabelField("Maximum HDRP Version " + m_profile.m_max2019_3HDVersion);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Minimum UPRP Version " + m_profile.m_min2019_3UPVersion);
                EditorGUILayout.LabelField("Maximum UPRP Version " + m_profile.m_max2019_3UPVersion);
                GUILayout.EndHorizontal();
            }

            if (Application.unityVersion.Contains("2019.4"))
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Minimum HDRP Version " + m_profile.m_min2019_4HDVersion);
                EditorGUILayout.LabelField("Maximum HDRP Version " + m_profile.m_max2019_4HDVersion);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Minimum UPRP Version " + m_profile.m_min2019_4UPVersion);
                EditorGUILayout.LabelField("Maximum UPRP Version " + m_profile.m_max2019_4UPVersion);
                GUILayout.EndHorizontal();
            }
        }

        private void ProfileSettingsEnabled(bool helpEnabled)
        {
            GUILayout.BeginVertical("Procedural Worlds Editor Settings", m_boxStyle);
            GUILayout.Space(20);

            EditorGUILayout.LabelField("2019.1");
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Minimum LWRP Version ");
            m_profile.m_min2019_1LWVersion = EditorGUILayout.TextArea(m_profile.m_min2019_1LWVersion);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Maximum LWRP Version ");
            m_profile.m_max2019_1LWVersion = EditorGUILayout.TextArea(m_profile.m_max2019_1LWVersion);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Minimum HDRP Version ");
            m_profile.m_min2019_1HDVersion = EditorGUILayout.TextArea(m_profile.m_min2019_1HDVersion);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Maximum HDRP Version ");
            m_profile.m_max2019_1HDVersion = EditorGUILayout.TextArea(m_profile.m_max2019_1HDVersion);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("2019.2");
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Minimum LWRP Version ");
            m_profile.m_min2019_2LWVersion = EditorGUILayout.TextArea(m_profile.m_min2019_2LWVersion);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Maximum LWRP Version ");
            m_profile.m_max2019_2LWVersion = EditorGUILayout.TextArea(m_profile.m_max2019_2LWVersion);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Minimum HDRP Version ");
            m_profile.m_min2019_2HDVersion = EditorGUILayout.TextArea(m_profile.m_min2019_2HDVersion);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Maximum HDRP Version ");
            m_profile.m_max2019_2HDVersion = EditorGUILayout.TextArea(m_profile.m_max2019_2HDVersion);
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("2019.3");

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Minimum URP Version ");
            m_profile.m_min2019_3UPVersion = EditorGUILayout.TextArea(m_profile.m_min2019_3UPVersion);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Maximum URP Version ");
            m_profile.m_max2019_3UPVersion = EditorGUILayout.TextArea(m_profile.m_max2019_3UPVersion);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Minimum HDRP Version ");
            m_profile.m_min2019_3HDVersion = EditorGUILayout.TextArea(m_profile.m_min2019_3HDVersion);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Maximum HDRP Version ");
            m_profile.m_max2019_3HDVersion = EditorGUILayout.TextArea(m_profile.m_max2019_3HDVersion);
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("2019.4");

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Minimum URP Version ");
            m_profile.m_min2019_4UPVersion = EditorGUILayout.TextArea(m_profile.m_min2019_4UPVersion);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Maximum URP Version ");
            m_profile.m_max2019_4UPVersion = EditorGUILayout.TextArea(m_profile.m_max2019_4UPVersion);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Minimum HDRP Version ");
            m_profile.m_min2019_4HDVersion = EditorGUILayout.TextArea(m_profile.m_min2019_4HDVersion);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Maximum HDRP Version ");
            m_profile.m_max2019_4HDVersion = EditorGUILayout.TextArea(m_profile.m_max2019_4HDVersion);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginVertical("Pipeline Profile Settings", m_boxStyle);
            GUILayout.Space(20);
            EditorGUI.indentLevel++;
            DrawDefaultInspector();
            EditorGUI.indentLevel--;

            if (m_matlibButtonStyle == null)
            {
                m_matlibButtonStyle = new GUIStyle(GUI.skin.button);
                m_matlibButtonStyle.margin = new RectOffset(40, m_matlibButtonStyle.margin.right, m_matlibButtonStyle.margin.top, m_matlibButtonStyle.margin.bottom);
            }

            //Draw the Material library settings
            EditorGUILayout.LabelField("Material Library");
            EditorGUI.indentLevel++;
            for (int i=0; i < m_profile.m_materialLibrary.Length; i++)
            {
                MaterialLibraryEntry entry = m_profile.m_materialLibrary[i];
                int materialCount = entry.m_materials == null ? 0 : entry.m_materials.Length;
                m_materialLibraryFoldouts[i] = EditorGUILayout.Foldout(m_materialLibraryFoldouts[i], entry.m_name + " [" + materialCount.ToString() + "]");
                if (m_materialLibraryFoldouts[i])
                {
                    EditorGUI.indentLevel++;
                    string oldbuiltInShader = entry.m_builtInShaderName;
                    entry.m_name = EditorGUILayout.TextField("Name", entry.m_name);
                    entry.m_builtInShaderName = EditorGUILayout.TextField("Builtin Shader", entry.m_builtInShaderName);
                    if (oldbuiltInShader != entry.m_builtInShaderName)
                    {
                        entry.m_name = entry.m_builtInShaderName;
                    }
                    entry.m_URPReplacementShaderName = EditorGUILayout.TextField("URP Shader", entry.m_URPReplacementShaderName);
                    entry.m_HDRPReplacementShaderName = EditorGUILayout.TextField("HDRP Shader", entry.m_HDRPReplacementShaderName);
                    EditorGUI.indentLevel++;
                    entry.m_floatChecksFoldedOut = EditorGUILayout.Foldout(entry.m_floatChecksFoldedOut, "Float Checks [" + entry.m_floatChecks.Count.ToString() + "]");
                    int floatCheckDeleteIndex = -99;
                    if (entry.m_floatChecksFoldedOut)
                    {
                        EditorGUI.indentLevel++;
                        for (int j = 0; j < entry.m_floatChecks.Count; j++)
                        {
                            Color regularColor = GUI.color;
                            if (String.IsNullOrEmpty(entry.m_floatChecks[j].m_floatValue) || String.IsNullOrEmpty(entry.m_floatChecks[j].m_shaderKeyWord))
                            {
                                GUI.color = Color.red;
                            }
                            Rect rect = EditorGUILayout.GetControlRect();
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Float", GUILayout.MinWidth(rect.width/5f));
                            entry.m_floatChecks[j].m_floatValue = EditorGUILayout.TextField(entry.m_floatChecks[j].m_floatValue, GUILayout.MinWidth(rect.width / 5f));
                            EditorGUILayout.LabelField("KeyWord", GUILayout.MinWidth(rect.width / 5f));
                            entry.m_floatChecks[j].m_shaderKeyWord = EditorGUILayout.TextField(entry.m_floatChecks[j].m_shaderKeyWord, GUILayout.MinWidth(rect.width / 5f));
                            if (GUILayout.Button("Delete", m_matlibButtonStyle, GUILayout.MaxWidth(50)))
                            {
                                floatCheckDeleteIndex = j;
                            }
                            EditorGUILayout.EndHorizontal();
                            GUI.color = regularColor;
                            
                        }
                        if (floatCheckDeleteIndex != -99)
                        {
                            entry.m_floatChecks.RemoveAt(floatCheckDeleteIndex);
                        }
                        if (GUILayout.Button("Add new Float Check", m_matlibButtonStyle))
                        {
                            entry.m_floatChecks.Add(new ShaderFloatCheck());
                        }
                        
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;

                    EditorGUI.indentLevel++;
                    entry.m_materialsFoldedOut = EditorGUILayout.Foldout(entry.m_materialsFoldedOut, "Materials [" + materialCount.ToString() +"]");
                    if (entry.m_materialsFoldedOut)
                    {
                        EditorGUI.indentLevel++;
                        for (int j = 0; j < entry.m_materials.Length; j++)
                        {
                            Color regularColor = GUI.color;
                            if (entry.m_materials[j] == null)
                            {
                                GUI.color = Color.red;
                            }
                            entry.m_materials[j] = (Material)EditorGUILayout.ObjectField("Material " + j.ToString(), entry.m_materials[j], typeof(Material),false);
                            GUI.color = regularColor;
                        }
                        EditorGUI.indentLevel--;
                    }
                    if (GUILayout.Button("Load Materials for " + entry.m_name, m_matlibButtonStyle))
                    {
                        LoadMaterials(entry);
                    }
                    if (GUILayout.Button("Remove " + entry.m_name, m_matlibButtonStyle))
                    {
                        if (EditorUtility.DisplayDialog("Delete Material Library Entry", "Are you sure you want to delete the entire entry for '" + entry.m_name + "' ?", "OK", "Cancel"))
                        {
                            m_profile.m_materialLibrary = GaiaUtils.RemoveArrayIndexAt(m_profile.m_materialLibrary, i);
                            m_materialLibraryFoldouts = GaiaUtils.RemoveArrayIndexAt(m_materialLibraryFoldouts, i);
                        }

                    }
                    if (GUILayout.Button("Insert entry below", m_matlibButtonStyle))
                    {
                        m_profile.m_materialLibrary = GaiaUtils.InsertElementInArray(m_profile.m_materialLibrary, new MaterialLibraryEntry() { m_name = "New Entry" } ,i+1);
                        m_materialLibraryFoldouts = GaiaUtils.InsertElementInArray(m_materialLibraryFoldouts, true, i+1);

                    }
                    EditorGUI.indentLevel--;
                    EditorGUI.indentLevel--;
                }
            }
            if (GUILayout.Button("Add new Material Library entry"))
            {
                m_profile.m_materialLibrary = GaiaUtils.AddElementToArray(m_profile.m_materialLibrary, new MaterialLibraryEntry() { m_name = "New Entry" });
                m_materialLibraryFoldouts = GaiaUtils.AddElementToArray(m_materialLibraryFoldouts, true);
            }
            EditorGUI.indentLevel--;

            GUILayout.EndVertical();
        }

        private void LoadMaterials(MaterialLibraryEntry entry)
        {
            ////First we need to refresh the shader to look for - could have changed with the newest settings
            //string shaderRootFolder = GaiaDirectories.GetShaderDirectory();
            //string[] folderPaths = Directory.GetDirectories(shaderRootFolder, ".", SearchOption.AllDirectories);
            //string unityVersion = Application.unityVersion;
            //unityVersion = unityVersion.Remove(unityVersion.LastIndexOf(".")).Replace(".", "_0");
            ////string keyWordToSearch = "";

            //GaiaConstants.EnvironmentRenderer renderPipeline = GaiaUtils.GetGaiaSettings().m_currentRenderer;

            //if (renderPipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
            //{
            //    keyWordToSearch = GaiaConstants.builtInKeyWord;
            //}
            //else if (renderPipeline == GaiaConstants.EnvironmentRenderer.Lightweight)
            //{
            //    keyWordToSearch = GaiaConstants.lightweightKeyWord;
            //}
            //else if (renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
            //{
            //    keyWordToSearch = GaiaConstants.universalKeyWord;
            //}
            //else
            //{
            //    keyWordToSearch = GaiaConstants.highDefinitionKeyWord;
            //}

            //foreach (string folderpath in folderPaths)
            //{
            //    string finalFolderName = folderpath.Substring(folderpath.LastIndexOf("\\"));

            //    if (!folderpath.EndsWith("PWSF Functions") && !folderpath.EndsWith("PWS Procedural") && !folderpath.EndsWith("PWS Water Pro"))
            //    {
            //        if (finalFolderName.Contains(keyWordToSearch + " " + unityVersion))
            //        {
            //            if (folderpath.Contains(entry.m_folderKeyWord))
            //            {
            //                entry.m_shaderToInstall = PackageInstallerUtils.GetShaderFile(folderpath, entry.m_shaderKeyWord);
            //                break;
            //            }
            //        }
            //    }
            //}

            //We got the shader, now get all material GUIDs in the project
            string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
            List<Material> collectedMaterials = new List<Material>();
            //Iterate through the guids, load the material, if it uses our shader we collect it for the array

            Shader targetShader = Shader.Find(entry.m_builtInShaderName);

            if (targetShader == null)
            {
                Debug.LogError("Could not find shader for the name: '" + entry.m_builtInShaderName + "'");
                return;
            }

            foreach (string guid in materialGUIDs)
            {
                Material mat = (Material)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(Material));
                if (mat.shader == targetShader)
                {
                    collectedMaterials.Add(mat);
                }
            }
            //all collected materials go in the array and we are done
            entry.m_materials = collectedMaterials.ToArray();
        }
    }
}