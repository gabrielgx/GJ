using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using PWCommon2;
using Gaia.Internal;
using UnityEditorInternal;

namespace Gaia
{

    /// <summary>
    /// Editor for resource manager
    /// </summary>
    [CustomEditor(typeof(GaiaResource))]
    public class GaiaResourceEditor : PWEditor, IPWEditor
    {

        GUIStyle m_boxStyle = null;
        GUIStyle m_wrapStyle;
        GaiaResource m_resource = new GaiaResource();
        private DateTime m_lastSaveDT = DateTime.Now;
        private EditorUtils m_editorUtils = null;
        private bool[] m_resourceProtoFoldOutStatus;
        private bool[] m_resourceProtoMasksExpanded;
        private ReorderableList[] m_resourceProtoReorderableLists;
        private ImageMask[] m_maskListBeingDrawn;
        private CollisionMask[] m_collisionMaskListBeingDrawn;
        private int m_resourceIndexBeingDrawn;
        private int m_resourceMaskIndexBeingDrawn;

        void OnEnable()
        {
            //GaiaResource resource = (GaiaResource)target;
            //int totalNumberOfResources = resource.m_texturePrototypes.Length + resource.m_detailPrototypes.Length + resource.m_treePrototypes.Length + resource.m_gameObjectPrototypes.Length;

            //m_resourceProtoFoldOutStatus = new bool[totalNumberOfResources];
            ////m_resourceProtoMasksExpanded = new bool[totalNumberOfResources];
            ////m_resourceProtoReorderableLists = new ReorderableList[totalNumberOfResources];

            //////Iterate through all 4 prototype arrays to get a complete collection of all reorderable lists inside those arrays
            //////We keep an independent resource Index for this
            ////int resourceIndex = 0;

            ////for (int i = 0; i < resource.m_texturePrototypes.Length; i++)
            ////{
            ////    m_resourceProtoReorderableLists[resourceIndex] = CreateMaskList(((GaiaResource)target).m_texturePrototypes[i].m_imageMasks);
            ////    m_resourceProtoMasksExpanded[resourceIndex] = true;
            ////    resourceIndex++;
            ////}

            ////for (int i = 0; i < resource.m_detailPrototypes.Length; i++)
            ////{
            ////    m_resourceProtoReorderableLists[resourceIndex] = CreateMaskList(((GaiaResource)target).m_detailPrototypes[i].m_imageMasks);
            ////    m_resourceProtoMasksExpanded[resourceIndex] = true;
            ////    resourceIndex++;
            ////}

            ////for (int i = 0; i < resource.m_treePrototypes.Length; i++)
            ////{
            ////    m_resourceProtoReorderableLists[resourceIndex] = CreateMaskList(((GaiaResource)target).m_treePrototypes[i].m_imageMasks);
            ////    m_resourceProtoMasksExpanded[resourceIndex] = true;
            ////    resourceIndex++;
            ////}

            ////for (int i = 0; i < resource.m_gameObjectPrototypes.Length; i++)
            ////{
            ////    m_resourceProtoReorderableLists[resourceIndex] = CreateMaskList(((GaiaResource)target).m_gameObjectPrototypes[i].m_imageMasks);
            ////    m_resourceProtoMasksExpanded[resourceIndex] = true;
            ////    resourceIndex++;
            ////}






            ////Init editor utils
            //if (m_editorUtils == null)
            //{
            //    // Get editor utils for this
            //    m_editorUtils = PWApp.GetEditorUtils(this);
            //}
        }

        //private ReorderableList CreateMaskList(Array data)
        //{
        //    ReorderableList list = new UnityEditorInternal.ReorderableList(data, typeof(ImageMask), true, true, true, true);
        //    list.elementHeightCallback = OnElementHeightMaskList;
        //    list.drawElementCallback = DrawMaskListElement;
        //    list.drawHeaderCallback = DrawMaskListHeader;
        //    list.onAddCallback = OnAddMaskListEntry;
        //    list.onRemoveCallback = OnRemoveMaskListEntry;
        //    list.onReorderCallback = OnReorderMaskList;

        //    foreach (ImageMask mask in list.list)
        //    {
        //        mask.m_reorderableCollisionMaskList = CreateResourceCollisionMaskList(mask.m_reorderableCollisionMaskList, mask.m_collisionMasks);
        //    }

        //    return list;
        //}

        //private float OnElementHeightMaskList(int index)
        //{
        //    return ImageMaskListEditor.OnElementHeight(index, m_maskListBeingDrawn[index]);
        //}

        //private void DrawMaskListElement(Rect rect, int index, bool isActive, bool isFocused)
        //{
        //    ImageMaskListEditor.DrawMaskListElement(rect,index,m_maskListBeingDrawn[index], ref m_collisionMaskListBeingDrawn,  m_editorUtils,Terrain.activeTerrain,GaiaConstants.FeatureOperation.Contrast);
        //}

        //private void OnReorderMaskList(ReorderableList list)
        //{
        //    //refresh the stamp preview when the order of filter operations has changed
        //    //m_stamper.m_stampDirty = true;
        //    //m_stamper.DrawStampPreview();
        //}

        //private void OnRemoveMaskListEntry(ReorderableList list)
        //{
        //    //find image mask that is being edited
        //    ImageMask[] imageMasks = GetImageMasksByResourceID(m_resourceIndexBeingDrawn);
        //    imageMasks = ImageMaskListEditor.OnRemoveMaskListEntry(imageMasks, list.index);
        //    list.list = imageMasks;
        //}


        //void OnAddMaskListEntry(UnityEditorInternal.ReorderableList list)
        //{
        //    //find image mask that is being edited
        //    ImageMask[] imageMasks = GetImageMasksByResourceID(m_resourceIndexBeingDrawn);
        //    //As this is an abstract resource editor, We don't have actual terrain values for min / max height & sea level, so let's just put something in that allows the user to edit for now.
        //    imageMasks = ImageMaskListEditor.OnAddMaskListEntry(imageMasks, 0f, 100f, 200f);
        //    //set up the new collision mask inside the newly added mask
        //    ImageMask lastElement = imageMasks[imageMasks.Length - 1];
        //    lastElement.m_reorderableCollisionMaskList = CreateResourceCollisionMaskList(lastElement.m_reorderableCollisionMaskList, lastElement.m_collisionMasks);
        //    list.list = imageMasks;
        //}

        //void DrawMaskListHeader(Rect rect)
        //{
        //    ImageMask[] imageMasks = GetImageMasksByResourceID(m_resourceIndexBeingDrawn);
        //    m_resourceProtoMasksExpanded[m_resourceIndexBeingDrawn] = ImageMaskListEditor.DrawFilterListHeader(rect, m_resourceProtoMasksExpanded[m_resourceIndexBeingDrawn], imageMasks, m_editorUtils);
        //}




        ///// <summary>
        ///// Creates the reorderable collision mask list for collision masks in the spawn rules.
        ///// </summary>
        //public ReorderableList CreateResourceCollisionMaskList(ReorderableList list, CollisionMask[] collisionMasks)
        //{
        //    list = new ReorderableList(collisionMasks, typeof(CollisionMask), true, true, true, true);
        //    list.elementHeightCallback = OnElementHeightCollisionMaskList;
        //    list.drawElementCallback = DrawResourceCollisionMaskElement;
        //    list.drawHeaderCallback = DrawResourceCollisionMaskListHeader;
        //    list.onAddCallback = OnAddResourceCollisionMaskListEntry;
        //    list.onRemoveCallback = OnRemoveResourceCollisionMaskMaskListEntry;
        //    return list;
        //}

        //private void OnRemoveResourceCollisionMaskMaskListEntry(ReorderableList list)
        //{
        //    //find spawn rule index & mask index which are being edited, so we know who this list of collision masks belongs to
        //    int maskIndex = -99;
        //    int resourceIndex = FindResourceIndexByReorderableCollisionMaskList(list, ref maskIndex);
        //    SetResourceCollisionMasksByIndices(resourceIndex, maskIndex, CollisionMaskListEditor.OnRemoveMaskListEntry(GetCollisionMasksByResourceID(resourceIndex, maskIndex), list.index));
        //    list.list = GetCollisionMasksByResourceID(resourceIndex, maskIndex);
        //}



        //    private void OnAddResourceCollisionMaskListEntry(ReorderableList list)
        //{
        //    //find spawn rule index & mask index which are being edited, so we know who this list of collision masks belongs to
        //    int maskIndex = -99;
        //    int resourceIndex = FindResourceIndexByReorderableCollisionMaskList(list, ref maskIndex);
        //    SetResourceCollisionMasksByIndices(resourceIndex, maskIndex, CollisionMaskListEditor.OnAddMaskListEntry(GetCollisionMasksByResourceID(resourceIndex, maskIndex)));
        //    list.list = GetCollisionMasksByResourceID(resourceIndex, maskIndex);
        //}

        //private void DrawResourceCollisionMaskListHeader(Rect rect)
        //{
        //    GetImageMasksByResourceID(m_resourceIndexBeingDrawn)[m_resourceMaskIndexBeingDrawn].m_collisionMaskExpanded = CollisionMaskListEditor.DrawFilterListHeader(rect, GetImageMasksByResourceID(m_resourceIndexBeingDrawn)[m_resourceMaskIndexBeingDrawn].m_collisionMaskExpanded, GetCollisionMasksByResourceID(m_resourceIndexBeingDrawn, m_resourceMaskIndexBeingDrawn), m_editorUtils);
        //}

        //private void DrawResourceCollisionMaskElement(Rect rect, int index, bool isActive, bool isFocused)
        //{
        //    m_resourceMaskIndexBeingDrawn = index;
        //    if (m_collisionMaskListBeingDrawn != null && m_collisionMaskListBeingDrawn.Length > index && m_collisionMaskListBeingDrawn[index] != null)
        //    {
        //        CollisionMaskListEditor.DrawMaskListElement(rect, index, m_collisionMaskListBeingDrawn[index], m_editorUtils, Terrain.activeTerrain, GaiaConstants.FeatureOperation.Contrast);
        //    }
        //}

        //private float OnElementHeightCollisionMaskList(int index)
        //{
        //    return CollisionMaskListEditor.OnElementHeight(index, m_collisionMaskListBeingDrawn);
        //}



        //    int FindTextureIndexByReorderableMaskList(ReorderableList maskList)
        //{
        //    //find texture index that is being edited
        //    int textureindex = -99;

        //    for (int i = 0; i < m_resourceProtoReorderableLists.Length; i++)
        //    {
        //        if (m_resourceProtoReorderableLists[i] == maskList)
        //        {
        //            textureindex = i;
        //        }
        //    }
        //    return textureindex;
        //}

        public override void OnInspectorGUI()
        {

            //m_editorUtils.Initialize();
            //serializedObject.Update();

            //Get our resource
            //m_resource = (GaiaResource)target;

            //Set up the box style
            //if (m_boxStyle == null)
            //{
            //    m_boxStyle = new GUIStyle(GUI.skin.box);
            //    m_boxStyle.normal.textColor = GUI.skin.label.normal.textColor;
            //    m_boxStyle.fontStyle = FontStyle.Bold;
            //    m_boxStyle.alignment = TextAnchor.UpperLeft;
            //}

            //Setup the wrap style
            //if (m_wrapStyle == null)
            //{
            //    m_wrapStyle = new GUIStyle(GUI.skin.label);
            //    m_wrapStyle.wordWrap = true;
            //}

            //Create a nice text intro
            //GUILayout.BeginVertical("Gaia Resource", m_boxStyle);
            //GUILayout.Space(20);
            //EditorGUILayout.LabelField("The resource manager allows you to manage the resources used by your terrain and spawner. To see what every setting does you can hover over it.\n\nGet From Terrain - Pick up resources and settings from the current terrain.\n\nUpdate DNA - Updates DNA for all resources and automatically calculate sizes.\n\nApply To Terrain - Apply terrain specific settings such as texture, detail and tree prototypes back into the terrain. Prefab this settings to save time creating your next terrain.", m_wrapStyle);
            //EditorGUILayout.LabelField("These are the resources used by the Spawning & Stamping system. Create a terrain, add textures, details and trees, then press Get Resources From Terrain to load. To see how the settings influence the system you can hover over them.", m_wrapStyle);
            //GUILayout.EndVertical();

            //float oldSeaLevel = m_resource.m_seaLevel;
            //float oldHeight = m_resource.m_terrainHeight;

            //EditorGUI.BeginChangeCheck();

            //m_editorUtils.Panel("Textures", DrawTextures, false);
            //m_editorUtils.Panel("Terrain Details", DrawTerrainDetails, false);
            //m_editorUtils.Panel("Trees", DrawTrees, false);
            //m_editorUtils.Panel("GameObjects", DrawGameObjects, false);




            //DrawDefaultInspector();

            //DropAreaGUI();

            //GUILayout.BeginVertical("Resource Controller", m_boxStyle);
            //GUILayout.Space(20);

            //if (GUILayout.Button(GetLabel("Set Asset Associations")))
            //{
            //    if (EditorUtility.DisplayDialog("Set Asset Associations", "This will update your asset associations and can not be undone ! Here temporarily until hidden.", "Yes", "No"))
            //    {
            //        if (m_resource.SetAssetAssociations())
            //        {
            //            EditorUtility.SetDirty(m_resource);
            //        }
            //    }
            //}


            //if (GUILayout.Button(GetLabel("Associate Assets")))
            //{
            //    if (EditorUtility.DisplayDialog("Associate Assets", "This will locate and associate the first resource found that matches your asset and can not be undone !", "Yes", "No"))
            //    {
            //        if (m_resource.AssociateAssets())
            //        {
            //            EditorUtility.SetDirty(m_resource);
            //        }
            //    }
            //}


            //if (GUILayout.Button(GetLabel("Get Resources From Terrain")))
            //{
            //    if (EditorUtility.DisplayDialog("Get Resources from Terrain ?", "Are you sure you want to get / update your resource prototypes from the terrain ? This will update your settings and can not be undone !", "Yes", "No"))
            //    {
            //        m_resource.UpdatePrototypesFromTerrain();
            //        EditorUtility.SetDirty(m_resource);
            //    }
            //}

            //if (GUILayout.Button(GetLabel("Replace Resources In Terrains")))
            //{
            //    if (EditorUtility.DisplayDialog("Replace Resources in ALL Terrains ?", "Are you sure you want to replace the resources in ALL terrains with these? This can not be undone !", "Yes", "No"))
            //    {
            //        m_resource.ApplyPrototypesToTerrain();
            //    }
            //}

            //if (GUILayout.Button(GetLabel("Add Missing Resources To Terrains")))
            //{
            //    if (EditorUtility.DisplayDialog("Add Missing Resources to ALL Terrains ?", "Are you sure you want to add your missing resource prototypes to ALL terrains ? This can not be undone !", "Yes", "No"))
            //    {
            //        m_resource.AddMissingPrototypesToTerrain();
            //    }
            //}


            //if (m_resource.m_texturePrototypes.GetLength(0) == 0)
            //{
            //    GUI.enabled = false;
            //}
            //if (GUILayout.Button(GetLabel("Create Coverage Texture Spawner")))
            //{
            //    m_resource.CreateCoverageTextureSpawner(GetRangeFromTerrain(), GetTextureIncrementFromTerrain());
            //}
            //GUI.enabled = true;


            //if (m_resource.m_detailPrototypes.GetLength(0) == 0)
            //{
            //    GUI.enabled = false;
            //}
            //if (GUILayout.Button(GetLabel("Create Clustered Grass Spawner")))
            //{
            //    m_resource.CreateClusteredDetailSpawner(GetRangeFromTerrain(), GetDetailIncrementFromTerrain());
            //}
            //if (GUILayout.Button(GetLabel("Create Coverage Grass Spawner")))
            //{
            //    m_resource.CreateCoverageDetailSpawner(GetRangeFromTerrain(), GetDetailIncrementFromTerrain());
            //}
            //GUI.enabled = true;


            //if (m_resource.m_treePrototypes.GetLength(0) == 0)
            //{
            //    GUI.enabled = false;
            //}
            //if (GUILayout.Button(GetLabel("Create Clustered Terrain Tree Spawner")))
            //{
            //    m_resource.CreateClusteredTreeSpawner(GetRangeFromTerrain());
            //}
            //if (GUILayout.Button(GetLabel("Create Coverage Terrain Tree Spawner")))
            //{
            //    m_resource.CreateCoverageTreeSpawner(GetRangeFromTerrain());
            //}
            //GUI.enabled = true;

            //if (m_resource.m_gameObjectPrototypes.GetLength(0) == 0)
            //{
            //    GUI.enabled = false;
            //}
            //if (GUILayout.Button(GetLabel("Create Clustered Prefab Spawner")))
            //{
            //    m_resource.CreateClusteredGameObjectSpawner(GetRangeFromTerrain());
            //}
            //if (GUILayout.Button(GetLabel("Create Coverage Prefab Spawner")))
            //{
            //    m_resource.CreateCoverageGameObjectSpawner(GetRangeFromTerrain());
            //}
            //GUI.enabled = true;

            //if (GUILayout.Button(GetLabel("Visualise")))
            //{
            //    GameObject gaiaObj = GameObject.Find("Gaia");
            //    if (gaiaObj == null)
            //    {
            //        gaiaObj = new GameObject("Gaia");
            //    }
            //    GameObject visualiserObj = GameObject.Find("Visualiser");
            //    if (visualiserObj == null)
            //    {
            //        visualiserObj = new GameObject("Visualiser");
            //        visualiserObj.AddComponent<ResourceVisualiser>();
            //        visualiserObj.transform.parent = gaiaObj.transform;
            //    }
            //    ResourceVisualiser visualiser = visualiserObj.GetComponent<ResourceVisualiser>();
            //    visualiser.m_resources = m_resource;
            //    Selection.activeGameObject = visualiserObj;
            //}

            //GUILayout.Space(5f);
            //GUILayout.EndVertical();

            //Check for changes, make undo record, make changes and let editor know we are dirty
            //if (EditorGUI.EndChangeCheck())
            //{
            //    if (oldHeight != m_resource.m_terrainHeight)
            //    {
            //        m_resource.ChangeHeight(oldHeight, m_resource.m_terrainHeight);
            //    }

            //    if (oldSeaLevel != m_resource.m_seaLevel)
            //    {
            //        m_resource.ChangeSeaLevel(oldSeaLevel, m_resource.m_seaLevel);
            //    }
            //    Undo.RecordObject(m_resource, "Made resource changes");
            //    EditorUtility.SetDirty(m_resource);

            //    Stop the save from going nuts
            //    if ((DateTime.Now - m_lastSaveDT).Seconds > 5)
            //    {
            //        m_lastSaveDT = DateTime.Now;
            //        AssetDatabase.SaveAssets();
            //    }
            //}
        }





        private int GetResourceIndexFromPrototypeIndex(GaiaConstants.SpawnerResourceType resourceType, int prototypeIndex)
        {
            //We have the following Resource types in this order: Textures, Terrain Details, Trees, GameObjects
            //To get the resource index we need to add the amount of other resources on top of the prototype index
            switch (resourceType)
            {
                case GaiaConstants.SpawnerResourceType.TerrainTexture:
                    return prototypeIndex;
                case GaiaConstants.SpawnerResourceType.TerrainDetail:
                    return m_resource.m_texturePrototypes.Length + prototypeIndex;
                case GaiaConstants.SpawnerResourceType.TerrainTree:
                    return m_resource.m_detailPrototypes.Length + m_resource.m_texturePrototypes.Length + prototypeIndex;
                case GaiaConstants.SpawnerResourceType.GameObject:
                    return m_resource.m_gameObjectPrototypes.Length + m_resource.m_detailPrototypes.Length + m_resource.m_texturePrototypes.Length + prototypeIndex;
                default:
                    return prototypeIndex;
            }


        }


        //private ImageMask[] GetImageMasksByResourceID(int resourceID)
        //{
        //    //We have the following Resource types in this order: Textures, Terrain Details, Trees, GameObjects
        //    //To get the Image Mask according to the resource ID, we can check in which "ID range" the passed ID falls
        //    int textureIDRange = Math.Max(0,m_resource.m_texturePrototypes.Length -1);
        //    int detailIDRange = Math.Max(0, m_resource.m_texturePrototypes.Length + m_resource.m_detailPrototypes.Length -1);
        //    int treeIDRange = Math.Max(0, m_resource.m_texturePrototypes.Length + m_resource.m_detailPrototypes.Length + m_resource.m_treePrototypes.Length -1);
        //    int gameObjectIDRange = Math.Max(0, m_resource.m_texturePrototypes.Length + m_resource.m_detailPrototypes.Length + m_resource.m_treePrototypes.Length + m_resource.m_gameObjectPrototypes.Length -1);

        //    if (m_resource.m_texturePrototypes.Length > 0 && resourceID <= textureIDRange)
        //    {
        //        //Must be a texture prototype
        //        return m_resource.m_texturePrototypes[resourceID].m_imageMasks;
        //    }
        //    else if (m_resource.m_detailPrototypes.Length > 0 && resourceID <= detailIDRange)
        //    {
        //        //Must be a terrain detail prototype
        //        return m_resource.m_detailPrototypes[resourceID - textureIDRange].m_imageMasks;
        //    }
        //    else if (m_resource.m_treePrototypes.Length > 0 && resourceID <= treeIDRange)
        //    {
        //        //Must be a terrain tree prototype
        //        return m_resource.m_treePrototypes[resourceID - detailIDRange].m_imageMasks;
        //    }
        //    else 
        //    {
        //        //Must be a game object prototype
        //        return m_resource.m_gameObjectPrototypes[resourceID - treeIDRange].m_imageMasks;
        //    }

        //}

        //private CollisionMask[] GetCollisionMasksByResourceID(int resourceID, int maskListID)
        //{
        //    //We have the following Resource types in this order: Textures, Terrain Details, Trees, GameObjects
        //    //To get the Image Mask according to the resource ID, we can check in which "ID range" the passed ID falls
        //    int textureIDRange = Math.Max(0, m_resource.m_texturePrototypes.Length - 1);
        //    int detailIDRange = Math.Max(0, m_resource.m_texturePrototypes.Length + m_resource.m_detailPrototypes.Length - 1);
        //    int treeIDRange = Math.Max(0, m_resource.m_texturePrototypes.Length + m_resource.m_detailPrototypes.Length + m_resource.m_treePrototypes.Length - 1);
        //    int gameObjectIDRange = Math.Max(0, m_resource.m_texturePrototypes.Length + m_resource.m_detailPrototypes.Length + m_resource.m_treePrototypes.Length + m_resource.m_gameObjectPrototypes.Length - 1);

        //    if (m_resource.m_texturePrototypes.Length > 0 && resourceID <= textureIDRange)
        //    {
        //        //Must be a texture prototype
        //        return m_resource.m_texturePrototypes[resourceID].m_imageMasks[maskListID].m_collisionMasks;
        //    }
        //    else if (m_resource.m_detailPrototypes.Length > 0 && resourceID <= detailIDRange)
        //    {
        //        //Must be a terrain detail prototype
        //        return m_resource.m_detailPrototypes[resourceID - textureIDRange].m_imageMasks[maskListID].m_collisionMasks;
        //    }
        //    else if (m_resource.m_treePrototypes.Length > 0 && resourceID <= treeIDRange)
        //    {
        //        //Must be a terrain tree prototype
        //        return m_resource.m_treePrototypes[resourceID - detailIDRange].m_imageMasks[maskListID].m_collisionMasks;
        //    }
        //    else
        //    {
        //        //Must be a game object prototype
        //        return m_resource.m_gameObjectPrototypes[resourceID - treeIDRange].m_imageMasks[maskListID].m_collisionMasks;
        //    }

        //}


        //private void SetResourceCollisionMasksByIndices(int resourceID, int maskListID, CollisionMask[] collisionMasks)
        //{
        //    //We have the following Resource types in this order: Textures, Terrain Details, Trees, GameObjects
        //    //To get the Image Mask according to the resource ID, we can check in which "ID range" the passed ID falls
        //    int textureIDRange = Math.Max(0, m_resource.m_texturePrototypes.Length - 1);
        //    int detailIDRange = Math.Max(0, m_resource.m_texturePrototypes.Length + m_resource.m_detailPrototypes.Length - 1);
        //    int treeIDRange = Math.Max(0, m_resource.m_texturePrototypes.Length + m_resource.m_detailPrototypes.Length + m_resource.m_treePrototypes.Length - 1);
        //    int gameObjectIDRange = Math.Max(0, m_resource.m_texturePrototypes.Length + m_resource.m_detailPrototypes.Length + m_resource.m_treePrototypes.Length + m_resource.m_gameObjectPrototypes.Length - 1);

        //    if (m_resource.m_texturePrototypes.Length > 0 && resourceID <= textureIDRange)
        //    {
        //        //Must be a texture prototype
        //         m_resource.m_texturePrototypes[resourceID].m_imageMasks[maskListID].m_collisionMasks = collisionMasks;
        //    }
        //    else if (m_resource.m_detailPrototypes.Length > 0 && resourceID <= detailIDRange)
        //    {
        //        //Must be a terrain detail prototype
        //        m_resource.m_detailPrototypes[resourceID - textureIDRange].m_imageMasks[maskListID].m_collisionMasks = collisionMasks;
        //    }
        //    else if (m_resource.m_treePrototypes.Length > 0 && resourceID <= treeIDRange)
        //    {
        //        //Must be a terrain tree prototype
        //        m_resource.m_treePrototypes[resourceID - detailIDRange].m_imageMasks[maskListID].m_collisionMasks = collisionMasks;
        //    }
        //    else
        //    {
        //        //Must be a game object prototype
        //         m_resource.m_gameObjectPrototypes[resourceID - treeIDRange].m_imageMasks[maskListID].m_collisionMasks = collisionMasks;
        //    }
        //}



        //private int FindResourceIndexByReorderableCollisionMaskList(ReorderableList list, ref int maskIndex)
        //{
        //    int returnIndex = -1;

        //    for (int i = 0; i < m_resource.m_texturePrototypes.Length; i++)
        //    {
        //        returnIndex++;
        //        for (int k = 0; i < m_resource.m_texturePrototypes[i].m_imageMasks.Length; k++)
        //        {
        //            if (m_resource.m_texturePrototypes[i].m_imageMasks[k].m_collisionMasks == list.list)
        //            {
        //                maskIndex = k;
        //                return returnIndex;
        //            }
        //        }
        //    }

        //    for (int i = 0; i < m_resource.m_detailPrototypes.Length; i++)
        //    {
        //        returnIndex++;
        //        for (int k = 0; i < m_resource.m_detailPrototypes[i].m_imageMasks.Length; k++)
        //        {
        //            if (m_resource.m_detailPrototypes[i].m_imageMasks[k].m_collisionMasks == list.list)
        //            {
        //                maskIndex = k;
        //                return returnIndex;
        //            }
        //        }
        //    }

        //    for (int i = 0; i < m_resource.m_treePrototypes.Length; i++)
        //    {
        //        returnIndex++;
        //        for (int k = 0; i < m_resource.m_treePrototypes[i].m_imageMasks.Length; k++)
        //        {
        //            if (m_resource.m_treePrototypes[i].m_imageMasks[k].m_collisionMasks == list.list)
        //            {
        //                maskIndex = k;
        //                return returnIndex;
        //            }
        //        }
        //    }

        //    for (int i = 0; i < m_resource.m_gameObjectPrototypes.Length; i++)
        //    {
        //        returnIndex++;
        //        for (int k = 0; i < m_resource.m_gameObjectPrototypes[i].m_imageMasks.Length; k++)
        //        {
        //            if (m_resource.m_gameObjectPrototypes[i].m_imageMasks[k].m_collisionMasks == list.list)
        //            {
        //                maskIndex = k;
        //                return returnIndex;
        //            }
        //        }
        //    }

        //    return 0;

        //}

        private void DrawTextures(bool showHelp)
        {
            EditorGUI.indentLevel++;
            for (int textureProtoIndex = 0; textureProtoIndex < m_resource.m_texturePrototypes.Length; textureProtoIndex++)
            {
                int resourceIndex = GetResourceIndexFromPrototypeIndex(GaiaConstants.SpawnerResourceType.TerrainTexture, textureProtoIndex);

                m_resourceProtoFoldOutStatus[resourceIndex] = EditorGUILayout.Foldout(m_resourceProtoFoldOutStatus[resourceIndex], m_resource.m_texturePrototypes[textureProtoIndex].m_name);
                if (m_resourceProtoFoldOutStatus[resourceIndex])
                {
                    DrawTexturePrototype(m_resource.m_texturePrototypes[textureProtoIndex], m_editorUtils, showHelp);
                    if (m_editorUtils.Button("DeleteTexture"))
                    {
                        m_resource.m_texturePrototypes = GaiaUtils.RemoveArrayIndexAt<ResourceProtoTexture>(m_resource.m_texturePrototypes, textureProtoIndex);
                        m_resourceProtoFoldOutStatus = GaiaUtils.RemoveArrayIndexAt<bool>(m_resourceProtoFoldOutStatus, resourceIndex);
                        //Correct the index since we just removed one texture
                        textureProtoIndex--;
                    }

                    //Rect maskRect;
                    //m_resourceIndexBeingDrawn = resourceIndex;
                    //if (m_resourceProtoMasksExpanded[resourceIndex])
                    //{
                    //    m_maskListBeingDrawn = m_resource.m_texturePrototypes[textureProtoIndex].m_imageMasks;
                    //    maskRect = EditorGUILayout.GetControlRect(true, m_resourceProtoReorderableLists[resourceIndex].GetHeight());
                    //    m_resourceProtoReorderableLists[resourceIndex].DoList(maskRect);
                    //}
                    //else
                    //{
                    //    int oldIndent = EditorGUI.indentLevel;
                    //    EditorGUI.indentLevel = 1;
                    //    m_resourceProtoMasksExpanded[resourceIndex] = EditorGUILayout.Foldout(m_resourceProtoMasksExpanded[resourceIndex], ImageMaskListEditor.PropertyCount("MaskSettings", m_resource.m_texturePrototypes[textureProtoIndex].m_imageMasks, m_editorUtils), true);
                    //    maskRect = GUILayoutUtility.GetLastRect();
                    //    EditorGUI.indentLevel = oldIndent;
                    //}
                }
            }
            EditorGUI.indentLevel--;
            if (m_editorUtils.Button("AddTexture"))
            {
                m_resource.m_texturePrototypes = GaiaUtils.AddElementToArray<ResourceProtoTexture>(m_resource.m_texturePrototypes, new ResourceProtoTexture());
                m_resource.m_texturePrototypes[m_resource.m_texturePrototypes.Length - 1].m_name = "New Texture Prototype";
                m_resourceProtoFoldOutStatus = GaiaUtils.AddElementToArray<bool>(m_resourceProtoFoldOutStatus, false);
            }
        }


        public static void DrawTexturePrototype(ResourceProtoTexture resourceProtoTexture, EditorUtils editorUtils, bool showHelp)
        {
            resourceProtoTexture.m_name = editorUtils.TextField("TextureProtoName", resourceProtoTexture.m_name, showHelp);
#if SUBSTANCE_PLUGIN_ENABLED
            if (resourceProtoTexture.m_substanceMaterial == null)
            {
                resourceProtoTexture.m_texture = (Texture2D)editorUtils.ObjectField("TextureProtoTexture", resourceProtoTexture.m_texture, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
                resourceProtoTexture.m_normal = (Texture2D)editorUtils.ObjectField("TextureProtoNormal", resourceProtoTexture.m_normal, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
                resourceProtoTexture.m_maskmap = (Texture2D)editorUtils.ObjectField("TextureProtoMaskMap", resourceProtoTexture.m_maskmap, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
            }
            else
            {
                EditorGUILayout.HelpBox(editorUtils.GetTextValue("SubstanceActiveHelp"), MessageType.Info);
                if (resourceProtoTexture.m_substanceMaterial.graphs.Count > 1)
                {
                    resourceProtoTexture.substanceSourceIndex = editorUtils.IntSlider("SubstanceGraphSelection", resourceProtoTexture.substanceSourceIndex, 1, resourceProtoTexture.m_substanceMaterial.graphs.Count, showHelp);
                }
                else
                {
                    resourceProtoTexture.substanceSourceIndex = 1;
                }
            }

            resourceProtoTexture.m_substanceMaterial = (Substance.Game.Substance)editorUtils.ObjectField("TextureProtoSubstance", resourceProtoTexture.m_substanceMaterial, typeof(Substance.Game.Substance), false, showHelp, GUILayout.MaxHeight(16));
#else
            resourceProtoTexture.m_texture = (Texture2D)editorUtils.ObjectField("TextureProtoTexture", resourceProtoTexture.m_texture, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
            resourceProtoTexture.m_normal = (Texture2D)editorUtils.ObjectField("TextureProtoNormal", resourceProtoTexture.m_normal, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
            resourceProtoTexture.m_maskmap = (Texture2D)editorUtils.ObjectField("TextureProtoMaskMap", resourceProtoTexture.m_maskmap, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
#endif
            resourceProtoTexture.m_sizeX = editorUtils.FloatField("TextureProtoSizeX", resourceProtoTexture.m_sizeX, showHelp);
            resourceProtoTexture.m_sizeY = editorUtils.FloatField("TextureProtoSizeY", resourceProtoTexture.m_sizeY, showHelp);
            resourceProtoTexture.m_offsetX = editorUtils.FloatField("TextureProtoOffsetX", resourceProtoTexture.m_offsetX, showHelp);
            resourceProtoTexture.m_offsetY = editorUtils.FloatField("TextureProtoOffsetY", resourceProtoTexture.m_offsetY, showHelp);
            resourceProtoTexture.m_normalScale = editorUtils.Slider("TextureProtoNormalScale", resourceProtoTexture.m_normalScale, 0f, 10f, showHelp);
            resourceProtoTexture.m_metallic = editorUtils.Slider("TextureProtoOffsetMetallic", resourceProtoTexture.m_metallic, 0f, 1f, showHelp);
            resourceProtoTexture.m_smoothness = editorUtils.Slider("TextureProtoOffsetSmoothness", resourceProtoTexture.m_smoothness, 0f, 1f, showHelp);
        }


        private void DrawGameObjects(bool showHelp)
        {
            EditorGUI.indentLevel++;
            for (int gameObjectProtoIndex = 0; gameObjectProtoIndex < m_resource.m_gameObjectPrototypes.Length; gameObjectProtoIndex++)
            {
                int resourceIndex = GetResourceIndexFromPrototypeIndex(GaiaConstants.SpawnerResourceType.TerrainTexture, gameObjectProtoIndex);

                m_resourceProtoFoldOutStatus[resourceIndex] = EditorGUILayout.Foldout(m_resourceProtoFoldOutStatus[resourceIndex], m_resource.m_gameObjectPrototypes[gameObjectProtoIndex].m_name);
                if (m_resourceProtoFoldOutStatus[resourceIndex])
                {
                    DrawGameObjectPrototype(m_resource.m_gameObjectPrototypes[gameObjectProtoIndex], m_editorUtils, showHelp);

                    Rect buttonRect = EditorGUILayout.GetControlRect();
                    buttonRect.x += 15 * EditorGUI.indentLevel;
                    buttonRect.width -= 15 * EditorGUI.indentLevel;
                    if (GUI.Button(buttonRect, m_editorUtils.GetContent("DeleteGameObject")))
                    {
                        m_resource.m_gameObjectPrototypes = GaiaUtils.RemoveArrayIndexAt<ResourceProtoGameObject>(m_resource.m_gameObjectPrototypes, gameObjectProtoIndex);
                        m_resourceProtoFoldOutStatus = GaiaUtils.RemoveArrayIndexAt<bool>(m_resourceProtoFoldOutStatus, resourceIndex);
                        //Correct the index since we just removed one texture
                        gameObjectProtoIndex--;
                    }

                    //Rect maskRect;
                    //m_resourceIndexBeingDrawn = gameObjectProtoIndex;
                    //if (m_resourceProtoMasksExpanded[resourceIndex])
                    //{
                    //    m_maskListBeingDrawn = m_resource.m_gameObjectPrototypes[gameObjectProtoIndex].m_imageMasks;
                    //    maskRect = EditorGUILayout.GetControlRect(true, m_resourceProtoReorderableLists[resourceIndex].GetHeight());
                    //    m_resourceProtoReorderableLists[resourceIndex].DoList(maskRect);
                    //}
                    //else
                    //{
                    //    int oldIndent = EditorGUI.indentLevel;
                    //    EditorGUI.indentLevel = 1;
                    //    m_resourceProtoMasksExpanded[resourceIndex] = EditorGUILayout.Foldout(m_resourceProtoMasksExpanded[resourceIndex], ImageMaskListEditor.PropertyCount("MaskSettings", m_resource.m_gameObjectPrototypes[gameObjectProtoIndex].m_imageMasks, m_editorUtils), true);
                    //    maskRect = GUILayoutUtility.GetLastRect();
                    //    EditorGUI.indentLevel = oldIndent;
                    //}
                }
            }
            EditorGUI.indentLevel--;
            if (m_editorUtils.Button("AddGameObject"))
            {
                m_resource.m_gameObjectPrototypes = GaiaUtils.AddElementToArray<ResourceProtoGameObject>(m_resource.m_gameObjectPrototypes, new ResourceProtoGameObject());
                m_resource.m_gameObjectPrototypes[m_resource.m_gameObjectPrototypes.Length - 1].m_name = "New Game Object Prototype";
                m_resourceProtoFoldOutStatus = GaiaUtils.AddElementToArray<bool>(m_resourceProtoFoldOutStatus, false);
            }
        }

        public static void DrawGameObjectPrototype(ResourceProtoGameObject resourceProtoGameObject, EditorUtils editorUtils, bool showHelp)
        {
            resourceProtoGameObject.m_name = editorUtils.TextField("GameObjectProtoName", resourceProtoGameObject.m_name, showHelp);
            EditorGUI.indentLevel++;
            resourceProtoGameObject.m_instancesFoldOut = editorUtils.Foldout("GameObjectInstances", resourceProtoGameObject.m_instancesFoldOut, showHelp);
            //Iterate through instances
            if (resourceProtoGameObject.m_instancesFoldOut)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < resourceProtoGameObject.m_instances.Length; i++)
                {
                    var instance = resourceProtoGameObject.m_instances[i];
                    instance.m_foldedOut = editorUtils.Foldout(instance.m_foldedOut, new GUIContent(instance.m_name), showHelp);
                    if (instance.m_foldedOut)
                    {
                        EditorGUI.indentLevel++;
                        instance.m_name = editorUtils.TextField("GameObjectProtoName", instance.m_name, showHelp);
                        instance.m_desktopPrefab = (GameObject)editorUtils.ObjectField("GameObjectProtoInstanceDesktop", instance.m_desktopPrefab, typeof(GameObject), false, showHelp);
                        //instance.m_mobilePrefab = (GameObject)editorUtils.ObjectField("GameObjectProtoInstanceMobile", instance.m_mobilePrefab, typeof(GameObject), false, showHelp);
                        instance.m_minInstances = editorUtils.IntField("GameObjectProtoInstanceMinInstances", instance.m_minInstances, showHelp);
                        instance.m_maxInstances = editorUtils.IntField("GameObjectProtoInstanceMaxInstances", instance.m_maxInstances, showHelp);
                        instance.m_failureRate = editorUtils.Slider("GameObjectProtoInstanceFailureRate", instance.m_failureRate, 0, 1, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceSpawnOffsetX", ref instance.m_minSpawnOffsetX, ref instance.m_maxSpawnOffsetX, -100, 100, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceSpawnOffsetY", ref instance.m_minSpawnOffsetY, ref instance.m_maxSpawnOffsetY, -100, 100, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceSpawnOffsetZ", ref instance.m_minSpawnOffsetZ, ref instance.m_maxSpawnOffsetZ, -100, 100, showHelp);
                        instance.m_rotateToSlope = editorUtils.Toggle("GameObjectProtoInstanceRotateToSlope", instance.m_rotateToSlope, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceRotationOffsetX", ref instance.m_minRotationOffsetX, ref instance.m_maxRotationOffsetX, 0, 360, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceRotationOffsetY", ref instance.m_minRotationOffsetY, ref instance.m_maxRotationOffsetY, 0, 360, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceRotationOffsetZ", ref instance.m_minRotationOffsetZ, ref instance.m_maxRotationOffsetZ, 0, 360, showHelp);
                        instance.m_spawnScale = (SpawnScale)editorUtils.EnumPopup("ProtoSpawnScale", instance.m_spawnScale, showHelp);
                        EditorGUI.indentLevel++;
                        switch (instance.m_spawnScale)
                        {
                            case SpawnScale.Fixed:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceScale", instance.m_minScale, 0, 100, showHelp);
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceScale", instance.m_minXYZScale);
                                }
                                break;
                            case SpawnScale.Random:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceMinScale", instance.m_minScale, 0, 100, showHelp);
                                    instance.m_maxScale = editorUtils.Slider("GameObjectProtoInstanceMaxScale", instance.m_maxScale, 0, 100, showHelp);
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMinScale", instance.m_minXYZScale, showHelp);
                                    instance.m_maxXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMaxScale", instance.m_maxXYZScale, showHelp);
                                }
                                break;
                            case SpawnScale.Fitness:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceMinScale", instance.m_minScale, 0, 100, showHelp);
                                    instance.m_maxScale = editorUtils.Slider("GameObjectProtoInstanceMaxScale", instance.m_maxScale, 0, 100, showHelp);
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMinScale", instance.m_minXYZScale, showHelp);
                                    instance.m_maxXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMaxScale", instance.m_maxXYZScale, showHelp);
                                }
                                break;
                            case SpawnScale.FitnessRandomized:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceMinScale", instance.m_minScale, 0, 100, showHelp);
                                    instance.m_maxScale = editorUtils.Slider("GameObjectProtoInstanceMaxScale", instance.m_maxScale, 0, 100, showHelp);
                                    instance.m_scaleRandomPercentage = editorUtils.Slider("GameObjectProtoInstanceRandomScalePercentage", instance.m_scaleRandomPercentage, 0, 1, showHelp);
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMinScale", instance.m_minXYZScale, showHelp);
                                    instance.m_maxXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMaxScale", instance.m_maxXYZScale, showHelp);
                                    instance.m_XYZScaleRandomPercentage = editorUtils.Vector3Field("GameObjectProtoInstanceRandomScalePercentage", instance.m_XYZScaleRandomPercentage, showHelp);
                                }
                                break;
                        }
                        EditorGUI.indentLevel--;
                        
                        instance.m_scaleByDistance = editorUtils.CurveField("GameObjectProtoInstanceScaleByDistance", instance.m_scaleByDistance);

                        //instance.m_localBounds = editorUtils.FloatField("GameObjectProtoInstanceLocalBounds", instance.m_localBounds);
                        Rect removeButtonRect = EditorGUILayout.GetControlRect();
                        removeButtonRect.x += 15 * EditorGUI.indentLevel;
                        removeButtonRect.width -= 15 * EditorGUI.indentLevel;
                        if (GUI.Button(removeButtonRect, editorUtils.GetContent("GameObjectRemoveInstance")))
                        {
                            resourceProtoGameObject.m_instances = GaiaUtils.RemoveArrayIndexAt<ResourceProtoGameObjectInstance>(resourceProtoGameObject.m_instances, i);
                        }
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUI.indentLevel--;
                Rect buttonRect = EditorGUILayout.GetControlRect();
                buttonRect.x += 15 * EditorGUI.indentLevel;
                buttonRect.width -= 15 * EditorGUI.indentLevel;
                if (GUI.Button(buttonRect, editorUtils.GetContent("GameObjectAddInstance")))
                {
                    resourceProtoGameObject.m_instances = GaiaUtils.AddElementToArray<ResourceProtoGameObjectInstance>(resourceProtoGameObject.m_instances, new ResourceProtoGameObjectInstance() { m_name = "New Instance" });
                }
            }
            resourceProtoGameObject.m_dnaFoldedOut = editorUtils.Foldout("GameObjectProtoDNA", resourceProtoGameObject.m_dnaFoldedOut, showHelp);
            if (resourceProtoGameObject.m_dnaFoldedOut)
            {
                DrawDNA(resourceProtoGameObject.m_dna, editorUtils, showHelp);
            }
            EditorGUI.indentLevel--;
        }

        public static void DrawSpawnExtensionPrototype(ResourceProtoSpawnExtension resourceProtoSpawnExtension, EditorUtils editorUtils, bool showHelp)
        {
            resourceProtoSpawnExtension.m_name = editorUtils.TextField("SpawnExtensionProtoName", resourceProtoSpawnExtension.m_name, showHelp);
            EditorGUI.indentLevel++;
            resourceProtoSpawnExtension.m_instancesFoldOut = editorUtils.Foldout("SpawnExtensionProtoInstances", resourceProtoSpawnExtension.m_instancesFoldOut, showHelp);
            //Iterate through instances
            if (resourceProtoSpawnExtension.m_instancesFoldOut)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < resourceProtoSpawnExtension.m_instances.Length; i++)
                {
                    var instance = resourceProtoSpawnExtension.m_instances[i];
                    instance.m_foldedOut = editorUtils.Foldout(instance.m_foldedOut, new GUIContent(instance.m_name), showHelp);
                    if (instance.m_foldedOut)
                    {
                        EditorGUI.indentLevel++;
                        instance.m_name = editorUtils.TextField("SpawnExtensionProtoName", instance.m_name, showHelp);

                        GameObject oldPrefab = instance.m_spawnerPrefab;

                        if (instance.m_invalidPrefabSupplied)
                        {
                            EditorGUILayout.HelpBox(editorUtils.GetTextValue("SpawnExtensionNoSpawnExtension"), MessageType.Error);
                        }

                        instance.m_spawnerPrefab = (GameObject)editorUtils.ObjectField("SpawnExtensionProtoPrefab", instance.m_spawnerPrefab, typeof(GameObject), false, showHelp);

                        //New Prefab submitted - check if it actually contains a Spawn Extension
                        if (oldPrefab != instance.m_spawnerPrefab)
                        {
                            if (instance.m_spawnerPrefab.GetComponent<ISpawnExtension>() != null)
                            {
                                instance.m_name = instance.m_spawnerPrefab.name;
                                instance.m_invalidPrefabSupplied = false;
                            }
                            else
                            {
                                instance.m_spawnerPrefab = null;
                                instance.m_invalidPrefabSupplied = true;
                            }
                        }
                        //instance.m_mobilePrefab = (GameObject)editorUtils.ObjectField("GameObjectProtoInstanceMobile", instance.m_mobilePrefab, typeof(GameObject), false, showHelp);
                        instance.m_minSpawnerRuns = editorUtils.IntField("SpawnExtensionProtoMinSpawns", instance.m_minSpawnerRuns, showHelp);
                        instance.m_maxSpawnerRuns = editorUtils.IntField("SpawnExtensionProtoMaxSpawns", instance.m_maxSpawnerRuns, showHelp);
                        instance.m_failureRate = editorUtils.Slider("SpawnExtensionProtoFailureRate", instance.m_failureRate, 0, 1, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceSpawnOffsetX", ref instance.m_minSpawnOffsetX, ref instance.m_maxSpawnOffsetX, -100, 100, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceSpawnOffsetY", ref instance.m_minSpawnOffsetY, ref instance.m_maxSpawnOffsetY, -100, 100, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceSpawnOffsetZ", ref instance.m_minSpawnOffsetZ, ref instance.m_maxSpawnOffsetZ, -100, 100, showHelp);
                        //instance.m_rotateToSlope = editorUtils.Toggle("GameObjectProtoInstanceRotateToSlope", instance.m_rotateToSlope, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceRotationOffsetX", ref instance.m_minRotationOffsetX, ref instance.m_maxRotationOffsetX, 0, 360, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceRotationOffsetY", ref instance.m_minRotationOffsetY, ref instance.m_maxRotationOffsetY, 0, 360, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceRotationOffsetZ", ref instance.m_minRotationOffsetZ, ref instance.m_maxRotationOffsetZ, 0, 360, showHelp);
                        //instance.m_useParentScale = editorUtils.Toggle("GameObjectProtoInstanceRotateToSlope", instance.m_useParentScale, showHelp);
                        instance.m_spawnScale = (SpawnScale)editorUtils.EnumPopup("ProtoSpawnScale", instance.m_spawnScale, showHelp);
                        EditorGUI.indentLevel++;
                        switch (instance.m_spawnScale)
                        {
                            case SpawnScale.Fixed:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceScale", instance.m_minScale, 0, 100, showHelp);
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceScale", instance.m_minXYZScale);
                                }
                                break;
                            case SpawnScale.Random:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceMinScale", instance.m_minScale, 0, 100, showHelp);
                                    instance.m_maxScale = editorUtils.Slider("GameObjectProtoInstanceMaxScale", instance.m_maxScale, 0, 100, showHelp);
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMinScale", instance.m_minXYZScale, showHelp);
                                    instance.m_maxXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMaxScale", instance.m_maxXYZScale, showHelp);
                                }
                                break;
                            case SpawnScale.Fitness:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceMinScale", instance.m_minScale, 0, 100, showHelp);
                                    instance.m_maxScale = editorUtils.Slider("GameObjectProtoInstanceMaxScale", instance.m_maxScale, 0, 100, showHelp);
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMinScale", instance.m_minXYZScale, showHelp);
                                    instance.m_maxXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMaxScale", instance.m_maxXYZScale, showHelp);
                                }
                                break;
                            case SpawnScale.FitnessRandomized:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceMinScale", instance.m_minScale, 0, 100, showHelp);
                                    instance.m_maxScale = editorUtils.Slider("GameObjectProtoInstanceMaxScale", instance.m_maxScale, 0, 100, showHelp);
                                    instance.m_scaleRandomPercentage = editorUtils.Slider("GameObjectProtoInstanceRandomScalePercentage", instance.m_scaleRandomPercentage, 0, 1, showHelp);
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMinScale", instance.m_minXYZScale, showHelp);
                                    instance.m_maxXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMaxScale", instance.m_maxXYZScale, showHelp);
                                    instance.m_XYZScaleRandomPercentage = editorUtils.Vector3Field("GameObjectProtoInstanceRandomScalePercentage", instance.m_XYZScaleRandomPercentage, showHelp);
                                }
                                break;
                        }
                        EditorGUI.indentLevel--;

                        instance.m_scaleByDistance = editorUtils.CurveField("GameObjectProtoInstanceScaleByDistance", instance.m_scaleByDistance);

                        //instance.m_localBounds = editorUtils.FloatField("GameObjectProtoInstanceLocalBounds", instance.m_localBounds);
                        Rect removeButtonRect = EditorGUILayout.GetControlRect();
                        removeButtonRect.x += 15 * EditorGUI.indentLevel;
                        removeButtonRect.width -= 15 * EditorGUI.indentLevel;
                        if (GUI.Button(removeButtonRect, editorUtils.GetContent("SpawnExtensionProtoRemoveInstance")))
                        {
                            resourceProtoSpawnExtension.m_instances = GaiaUtils.RemoveArrayIndexAt<ResourceProtoSpawnExtensionInstance>(resourceProtoSpawnExtension.m_instances, i);
                        }
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUI.indentLevel--;
                Rect buttonRect = EditorGUILayout.GetControlRect();
                buttonRect.x += 15 * EditorGUI.indentLevel;
                buttonRect.width -= 15 * EditorGUI.indentLevel;
                if (GUI.Button(buttonRect, editorUtils.GetContent("SpawnExtensionProtoAddInstance")))
                {
                    resourceProtoSpawnExtension.m_instances = GaiaUtils.AddElementToArray<ResourceProtoSpawnExtensionInstance>(resourceProtoSpawnExtension.m_instances, new ResourceProtoSpawnExtensionInstance() { m_name = "New Spawn Extension" });
                }
            }
            resourceProtoSpawnExtension.m_dnaFoldedOut = editorUtils.Foldout("GameObjectProtoDNA", resourceProtoSpawnExtension.m_dnaFoldedOut, showHelp);
            if (resourceProtoSpawnExtension.m_dnaFoldedOut)
            {
                resourceProtoSpawnExtension.m_dna.m_boundsRadius = editorUtils.FloatField("GameObjectProtoDNABoundsRadius", resourceProtoSpawnExtension.m_dna.m_boundsRadius, showHelp);
            }
            EditorGUI.indentLevel--;
        }

        public static void DrawStampDistributionPrototype(ResourceProtoStampDistribution resourceProtoStampDistribution, EditorUtils editorUtils, List<string> stampCategoryNames, int[]stampCategoryIDs, bool showHelp)
        {
            resourceProtoStampDistribution.m_name = editorUtils.TextField("SpawnExtensionProtoName", resourceProtoStampDistribution.m_name, showHelp);
            int deletionIndex = -99;
            for (int i=0;i<resourceProtoStampDistribution.m_featureSettings.Count;i++)
            {
                StampFeatureSettings featureChance = resourceProtoStampDistribution.m_featureSettings[i];
                EditorGUI.indentLevel++;
                featureChance.m_isFoldedOut = editorUtils.Foldout(featureChance.m_isFoldedOut, new GUIContent(featureChance.m_featureType), showHelp);
                if (featureChance.m_isFoldedOut)
                {
                    //we need to set the id initially according to the stored string for the category
                    //we can't rely on the IDs being the same every time, since the user might have added additional category folders in the meantime
                    int selectedStampCategoryID = -99;
                    selectedStampCategoryID = stampCategoryNames.IndexOf(featureChance.m_featureType);
                    selectedStampCategoryID = EditorGUILayout.IntPopup("Feature Type:", selectedStampCategoryID, stampCategoryNames.ToArray(), stampCategoryIDs);

                    featureChance.m_borderMaskStyle = (BorderMaskStyle)editorUtils.EnumPopup("FeatureTypeBorderMaskType", featureChance.m_borderMaskStyle, showHelp);
                    int selectedBorderMaskCategoryID = 0;
                    if (featureChance.m_borderMaskStyle == BorderMaskStyle.ImageMask)
                    {
                        
                        selectedBorderMaskCategoryID = Math.Max(0,stampCategoryNames.IndexOf(featureChance.m_borderMaskType));
                        selectedBorderMaskCategoryID = EditorGUILayout.IntPopup("Border Mask:", selectedBorderMaskCategoryID, stampCategoryNames.ToArray(), stampCategoryIDs);
                    }

                    featureChance.m_operation  = (GaiaConstants.TerrainGeneratorFeatureOperation)EditorGUILayout.Popup("Operation Type:", (int)featureChance.m_operation, GaiaConstants.TerrainGeneratorFeatureOperationNames);
                    if (selectedStampCategoryID >= 0)
                    {
                        featureChance.m_featureType = stampCategoryNames[selectedStampCategoryID];
                        featureChance.m_stampInfluence = (ImageMaskInfluence)editorUtils.EnumPopup("MaskInfluence", featureChance.m_stampInfluence, showHelp);
                        featureChance.m_borderMaskType = stampCategoryNames[selectedBorderMaskCategoryID];
                        featureChance.m_chanceStrengthMapping = editorUtils.CurveField("FeatureTypeStrengthRemap", featureChance.m_chanceStrengthMapping, showHelp);
                        featureChance.m_invertChance =  editorUtils.Slider("FeatureTypeInvertChance", featureChance.m_invertChance, 0, 100, showHelp);
                        editorUtils.MinMaxSliderWithFields("FeatureTypeWidth", ref featureChance.m_minWidth, ref featureChance.m_maxWidth, 0, 100, showHelp);
                        featureChance.m_tieWidthToStrength = editorUtils.Toggle("FeatureTypeTieWidth", featureChance.m_tieWidthToStrength, showHelp);
                        editorUtils.MinMaxSliderWithFields("FeatureTypeHeight", ref featureChance.m_minHeight, ref featureChance.m_maxHeight, 0, 20, showHelp);
                        featureChance.m_tieHeightToStrength = editorUtils.Toggle("FeatureTypeTieHeight", featureChance.m_tieHeightToStrength, showHelp);
                        editorUtils.MinMaxSliderWithFields("FeatureTypeYOffset", ref featureChance.m_minYOffset, ref featureChance.m_maxYOffset, -150, 150, showHelp);
                        
                    }
                    if (editorUtils.ButtonRight("RemoveFeatureType"))
                    {
                        deletionIndex = i;
                    }
                }

                EditorGUI.indentLevel--;
            }
            if (deletionIndex != -99)
            {
                resourceProtoStampDistribution.m_featureSettings.RemoveAt(deletionIndex);
            }

            if (editorUtils.ButtonAutoIndent("AddFeatureType"))
            {
                resourceProtoStampDistribution.m_featureSettings.Add(new StampFeatureSettings() { m_featureType = stampCategoryNames[0]});
            }

        }

        public static void DrawWorldBiomeMaskPrototype(ResourceProtoWorldBiomeMask worldBiomeMaskPrototype, EditorUtils editorUtils, bool showHelp)
        {
            worldBiomeMaskPrototype.m_name = editorUtils.TextField("SpawnExtensionProtoName", worldBiomeMaskPrototype.m_name, showHelp);
            worldBiomeMaskPrototype.m_biomePreset = (BiomePreset)editorUtils.ObjectField("WorldBiomeMaskBiomePreset", worldBiomeMaskPrototype.m_biomePreset, typeof(BiomePreset), false, showHelp);

        }


        private static void DrawDNA(ResourceProtoDNA dna, EditorUtils editorUtils, bool showHelp)
        {
            //dna.m_width = editorUtils.FloatField("GameObjectProtoDNAWidth", dna.m_width, showHelp);
            //dna.m_height = editorUtils.FloatField("GameObjectProtoDNAHeight", dna.m_height, showHelp);
            dna.m_boundsRadius = editorUtils.FloatField("GameObjectProtoDNABoundsRadius", dna.m_boundsRadius, showHelp);
            dna.m_scaleMultiplier = editorUtils.Slider("ProtoDNAScaleMultiplier", dna.m_scaleMultiplier, 0f, 10f, showHelp);
            //editorUtils.SliderRange("GameObjectProtoDNAMinMaxScale", ref dna.m_minScale, ref dna.m_maxScale, 0, 100, showHelp);
            //dna.m_rndScaleInfluence = editorUtils.Toggle("GameObjectProtoDNARndScaleInfluence", dna.m_rndScaleInfluence, showHelp);
        }


        private void DrawTrees(bool showHelp)
        {
            EditorGUI.indentLevel++;
            for (int treeProtoIndex = 0; treeProtoIndex < m_resource.m_treePrototypes.Length; treeProtoIndex++)
            {
                int resourceIndex = GetResourceIndexFromPrototypeIndex(GaiaConstants.SpawnerResourceType.TerrainTree, treeProtoIndex);

                m_resourceProtoFoldOutStatus[resourceIndex] = EditorGUILayout.Foldout(m_resourceProtoFoldOutStatus[resourceIndex], m_resource.m_treePrototypes[treeProtoIndex].m_name);
                if (m_resourceProtoFoldOutStatus[resourceIndex])
                {
                    DrawTreePrototype(m_resource.m_treePrototypes[treeProtoIndex], m_editorUtils, showHelp);

                    if (m_editorUtils.Button("DeleteTree"))
                    {
                        m_resource.m_treePrototypes = GaiaUtils.RemoveArrayIndexAt<ResourceProtoTree>(m_resource.m_treePrototypes, treeProtoIndex);
                        m_resourceProtoFoldOutStatus = GaiaUtils.RemoveArrayIndexAt<bool>(m_resourceProtoFoldOutStatus, resourceIndex);
                        //Correct the index since we just removed one texture
                        treeProtoIndex--;
                    }

                    //Rect maskRect;
                    //m_resourceIndexBeingDrawn = treeProtoIndex;
                    //if (m_resourceProtoMasksExpanded[resourceIndex])
                    //{
                    //    m_maskListBeingDrawn = m_resource.m_treePrototypes[treeProtoIndex].m_imageMasks;
                    //    maskRect = EditorGUILayout.GetControlRect(true, m_resourceProtoReorderableLists[resourceIndex].GetHeight());
                    //    m_resourceProtoReorderableLists[resourceIndex].DoList(maskRect);
                    //}
                    //else
                    //{
                    //    int oldIndent = EditorGUI.indentLevel;
                    //    EditorGUI.indentLevel = 1;
                    //    m_resourceProtoMasksExpanded[resourceIndex] = EditorGUILayout.Foldout(m_resourceProtoMasksExpanded[resourceIndex], ImageMaskListEditor.PropertyCount("MaskSettings", m_resource.m_treePrototypes[treeProtoIndex].m_imageMasks, m_editorUtils), true);
                    //    maskRect = GUILayoutUtility.GetLastRect();
                    //    EditorGUI.indentLevel = oldIndent;
                    //}
                }
            }
            EditorGUI.indentLevel--;
            if (m_editorUtils.Button("AddTree"))
            {
                m_resource.m_treePrototypes = GaiaUtils.AddElementToArray<ResourceProtoTree>(m_resource.m_treePrototypes, new ResourceProtoTree());
                m_resource.m_treePrototypes[m_resource.m_treePrototypes.Length - 1].m_name = "New Tree Prototype";
                m_resourceProtoFoldOutStatus = GaiaUtils.AddElementToArray<bool>(m_resourceProtoFoldOutStatus, false);
            }
        }

        public static void DrawTreePrototype(ResourceProtoTree resourceProtoTree, EditorUtils editorUtils, bool showHelp)
        {
            resourceProtoTree.m_name = editorUtils.TextField("GameObjectProtoName", resourceProtoTree.m_name, showHelp);
            resourceProtoTree.m_desktopPrefab = (GameObject)editorUtils.ObjectField("TreeProtoDesktopPrefab", resourceProtoTree.m_desktopPrefab, typeof(GameObject), false, showHelp);
            //resourceProtoTree.m_mobilePrefab = (GameObject)editorUtils.ObjectField("TreeProtoMobilePrefab", resourceProtoTree.m_mobilePrefab, typeof(GameObject), false, showHelp);
            resourceProtoTree.m_bendFactor = editorUtils.Slider("TreeProtoBendFactor", resourceProtoTree.m_bendFactor, 0, 100, showHelp);
            resourceProtoTree.m_healthyColour = editorUtils.ColorField("TreeProtoHealthyColour", resourceProtoTree.m_healthyColour, showHelp);
            resourceProtoTree.m_dryColour = editorUtils.ColorField("TreeProtoDryColour", resourceProtoTree.m_dryColour, showHelp);
            resourceProtoTree.m_spawnScale = (SpawnScale)editorUtils.EnumPopup("ProtoSpawnScale", resourceProtoTree.m_spawnScale,showHelp);
            EditorGUI.indentLevel++;
            switch (resourceProtoTree.m_spawnScale)
            {
                case SpawnScale.Fixed:
                    resourceProtoTree.m_minWidth = editorUtils.FloatField("TreeProtoWidth", resourceProtoTree.m_minWidth, showHelp);
                    resourceProtoTree.m_minHeight = editorUtils.FloatField("TreeProtoHeight", resourceProtoTree.m_minHeight, showHelp);
                    break;
                case SpawnScale.Random:
                    editorUtils.MinMaxSliderWithFields("TreeProtoMinMaxWidth", ref resourceProtoTree.m_minWidth, ref resourceProtoTree.m_maxWidth, 0f, 10f, showHelp);
                    editorUtils.MinMaxSliderWithFields("TreeProtoMinMaxHeight", ref resourceProtoTree.m_minHeight, ref resourceProtoTree.m_maxHeight, 0f, 10f, showHelp);
                    break;
                case SpawnScale.Fitness:
                    editorUtils.MinMaxSliderWithFields("TreeProtoMinMaxWidth", ref resourceProtoTree.m_minWidth, ref resourceProtoTree.m_maxWidth, 0f, 10f, showHelp);
                    editorUtils.MinMaxSliderWithFields("TreeProtoMinMaxHeight", ref resourceProtoTree.m_minHeight, ref resourceProtoTree.m_maxHeight, 0f, 10f, showHelp);
                    break;
                case SpawnScale.FitnessRandomized:
                    editorUtils.MinMaxSliderWithFields("TreeProtoMinMaxWidth", ref resourceProtoTree.m_minWidth, ref resourceProtoTree.m_maxWidth, 0f, 10f, showHelp);
                    resourceProtoTree.m_widthRandomPercentage = editorUtils.Slider("TreeProtoWidthRandomPercentage", resourceProtoTree.m_widthRandomPercentage, 0f, 1f);
                    editorUtils.MinMaxSliderWithFields("TreeProtoMinMaxHeight", ref resourceProtoTree.m_minHeight, ref resourceProtoTree.m_maxHeight, 0f, 10f, showHelp);
                    resourceProtoTree.m_heightRandomPercentage = editorUtils.Slider("TreeProtoHeightRandomPercentage", resourceProtoTree.m_heightRandomPercentage, 0f, 1f);
                    break;
            }
            EditorGUI.indentLevel--;
            //resourceProtoTree.m_dna.m_boundsRadius = editorUtils.FloatField("GameObjectProtoDNABoundsRadius", resourceProtoTree.m_dna.m_boundsRadius, showHelp);

            //resourceProtoTree.m_dnaFoldedOut = editorUtils.Foldout("GameObjectProtoDNA", resourceProtoTree.m_dnaFoldedOut, showHelp);
            //if (resourceProtoTree.m_dnaFoldedOut)
            //{
            //    DrawDNA(resourceProtoTree.m_dna, editorUtils, showHelp);
            //}
        }

        private void DrawTerrainDetails(bool showHelp)
        {
            EditorGUI.indentLevel++;
            for (int terrainDetailProtoIndex = 0; terrainDetailProtoIndex < m_resource.m_detailPrototypes.Length; terrainDetailProtoIndex++)
            {
                int resourceIndex = GetResourceIndexFromPrototypeIndex(GaiaConstants.SpawnerResourceType.TerrainDetail, terrainDetailProtoIndex);

                m_resourceProtoFoldOutStatus[resourceIndex] = EditorGUILayout.Foldout(m_resourceProtoFoldOutStatus[resourceIndex], m_resource.m_detailPrototypes[terrainDetailProtoIndex].m_name);
                if (m_resourceProtoFoldOutStatus[resourceIndex])
                {
                    DrawTerrainDetailPrototype(m_resource.m_detailPrototypes[terrainDetailProtoIndex], m_editorUtils, showHelp);
                    if (m_editorUtils.Button("DeleteTerrainDetail"))
                    {
                        m_resource.m_detailPrototypes = GaiaUtils.RemoveArrayIndexAt<ResourceProtoDetail>(m_resource.m_detailPrototypes, terrainDetailProtoIndex);
                        m_resourceProtoFoldOutStatus = GaiaUtils.RemoveArrayIndexAt<bool>(m_resourceProtoFoldOutStatus, resourceIndex);
                        //Correct the index since we just removed one texture
                        terrainDetailProtoIndex--;
                    }

                    //Rect maskRect;
                    //m_resourceIndexBeingDrawn = terrainDetailProtoIndex;
                    //if (m_resourceProtoMasksExpanded[resourceIndex])
                    //{
                    //    m_maskListBeingDrawn = m_resource.m_detailPrototypes[terrainDetailProtoIndex].m_imageMasks;
                    //    maskRect = EditorGUILayout.GetControlRect(true, m_resourceProtoReorderableLists[resourceIndex].GetHeight());
                    //    m_resourceProtoReorderableLists[resourceIndex].DoList(maskRect);
                    //}
                    //else
                    //{
                    //    int oldIndent = EditorGUI.indentLevel;
                    //    EditorGUI.indentLevel = 1;
                    //    m_resourceProtoMasksExpanded[resourceIndex] = EditorGUILayout.Foldout(m_resourceProtoMasksExpanded[resourceIndex], ImageMaskListEditor.PropertyCount("MaskSettings", m_resource.m_detailPrototypes[terrainDetailProtoIndex].m_imageMasks, m_editorUtils), true);
                    //    maskRect = GUILayoutUtility.GetLastRect();
                    //    EditorGUI.indentLevel = oldIndent;
                    //}
                }
            }
            EditorGUI.indentLevel--;

            if (m_editorUtils.Button("AddTerrainDetail"))
            {
                m_resource.m_detailPrototypes = GaiaUtils.AddElementToArray<ResourceProtoDetail>(m_resource.m_detailPrototypes, new ResourceProtoDetail());
                m_resource.m_detailPrototypes[m_resource.m_detailPrototypes.Length - 1].m_name = "New Terrain Detail Prototype";
                m_resourceProtoFoldOutStatus = GaiaUtils.AddElementToArray<bool>(m_resourceProtoFoldOutStatus, false);
            }
        }

        public static void DrawTerrainDetailPrototype(ResourceProtoDetail resourceProtoDetail, EditorUtils editorUtils, bool showHelp)
        {
            resourceProtoDetail.m_name = editorUtils.TextField("GameObjectProtoName", resourceProtoDetail.m_name, showHelp);
            resourceProtoDetail.m_renderMode = (DetailRenderMode)editorUtils.EnumPopup("DetailProtoRenderMode", resourceProtoDetail.m_renderMode, showHelp);
            if (resourceProtoDetail.m_renderMode == DetailRenderMode.VertexLit || resourceProtoDetail.m_renderMode == DetailRenderMode.Grass)
            {
                resourceProtoDetail.m_detailProtoype = (GameObject)editorUtils.ObjectField("DetailProtoModel", resourceProtoDetail.m_detailProtoype, typeof(GameObject), false, showHelp);
            }
            if (resourceProtoDetail.m_renderMode != DetailRenderMode.VertexLit)
            {
                resourceProtoDetail.m_detailTexture = (Texture2D)editorUtils.ObjectField("DetailProtoTexture", resourceProtoDetail.m_detailTexture, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
            }
            editorUtils.SliderRange("DetailProtoMinMaxWidth", ref resourceProtoDetail.m_minWidth, ref resourceProtoDetail.m_maxWidth, 0, 20, showHelp);
            editorUtils.SliderRange("DetailProtoMinMaxHeight", ref resourceProtoDetail.m_minHeight, ref resourceProtoDetail.m_maxHeight, 0, 20, showHelp);
            resourceProtoDetail.m_noiseSpread = editorUtils.FloatField("DetailProtoNoiseSpread", resourceProtoDetail.m_noiseSpread, showHelp);
            resourceProtoDetail.m_bendFactor = editorUtils.FloatField("DetailProtoBendFactor", resourceProtoDetail.m_bendFactor, showHelp);
            resourceProtoDetail.m_healthyColour = editorUtils.ColorField("DetailProtoHealthyColour", resourceProtoDetail.m_healthyColour, showHelp);
            resourceProtoDetail.m_dryColour = editorUtils.ColorField("DetailProtoDryColour", resourceProtoDetail.m_dryColour, showHelp);
            //resourceProtoDetail.m_dnaFoldedOut = editorUtils.Foldout("GameObjectProtoDNA", resourceProtoDetail.m_dnaFoldedOut, showHelp);
            //if (resourceProtoDetail.m_dnaFoldedOut)
            //{
            //    DrawDNA(resourceProtoDetail.m_dna, editorUtils, showHelp);
            //}
        }


        public void DropAreaGUI()
        {
            //Drop out if no resource selected
            if (m_resource == null)
            {
                return;
            }

            //Ok - set up for drag and drop
            Event evt = Event.current;
            Rect drop_area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(drop_area, "Drop Game Objects / Prefabs Here", m_boxStyle);

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!drop_area.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

#if UNITY_2018_3_OR_NEWER
                        //Work out if we have prefab instances or prefab objects
                        bool havePrefabInstances = false;
                        foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                        {
                            PrefabAssetType pt = PrefabUtility.GetPrefabAssetType(dragged_object);

                            if (pt == PrefabAssetType.Regular || pt == PrefabAssetType.Model)
                            {
                                havePrefabInstances = true;
                                break;
                            }
                        }

                        if (havePrefabInstances)
                        {
                            List<GameObject> prototypes = new List<GameObject>();

                            foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                            {
                                PrefabAssetType pt = PrefabUtility.GetPrefabAssetType(dragged_object);

                                if (pt == PrefabAssetType.Regular || pt == PrefabAssetType.Model)
                                {
                                    prototypes.Add(dragged_object as GameObject);
                                }
                                else
                                {
                                    Debug.LogWarning("You may only add prefab instances!");
                                }
                            }

                            //Same them as a single entity
                            if (prototypes.Count > 0)
                            {
                                m_resource.AddGameObject(prototypes);
                            }
                        }
                        else
                        {
                            foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                            {
                                if (PrefabUtility.GetPrefabAssetType(dragged_object) == PrefabAssetType.Regular)
                                {
                                    m_resource.AddGameObject(dragged_object as GameObject);
                                }
                                else
                                {
                                    Debug.LogWarning("You may only add prefabs or game objects attached to prefabs!");
                                }
                            }
                        }
#else

                        //Work out if we have prefab instances or prefab objects
                        bool havePrefabInstances = false;
                        foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                        {
                            PrefabType pt = PrefabUtility.GetPrefabType(dragged_object);

                            if (pt == PrefabType.PrefabInstance || pt == PrefabType.ModelPrefabInstance)
                            {
                                havePrefabInstances = true;
                                break;
                            }
                        }

                        if (havePrefabInstances)
                        {
                            List<GameObject> prototypes = new List<GameObject>();

                            foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                            {
                                PrefabType pt = PrefabUtility.GetPrefabType(dragged_object);

                                if (pt == PrefabType.PrefabInstance || pt == PrefabType.ModelPrefabInstance)
                                {
                                    prototypes.Add(dragged_object as GameObject);
                                }
                                else
                                {
                                    Debug.LogWarning("You may only add prefab instances!");
                                }
                            }

                            //Same them as a single entity
                            if (prototypes.Count > 0)
                            {
                                m_resource.AddGameObject(prototypes);
                            }
                        }
                        else
                        {
                            foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                            {
                                if (PrefabUtility.GetPrefabType(dragged_object) == PrefabType.Prefab)
                                {
                                    m_resource.AddGameObject(dragged_object as GameObject);
                                }
                                else
                                {
                                    Debug.LogWarning("You may only add prefabs or game objects attached to prefabs!");
                                }
                            }
                        }
#endif
                    }
                    break;
            }
        }


        /// <summary>
        /// Get the range from the terrain
        /// </summary>
        /// <returns>Range from currently active terrain or 1024f</returns>
        private float GetRangeFromTerrain()
        {
            float range = 1024f;
            Terrain t = Gaia.TerrainHelper.GetActiveTerrain();
            if (t != null)
            {
                range = Mathf.Max(t.terrainData.size.x, t.terrainData.size.z) / 2f;
            }
            return range;
        }

        /// <summary>
        /// Get texture increment from terrain
        /// </summary>
        /// <returns></returns>
        private float GetTextureIncrementFromTerrain()
        {
            float increment = 1f;
            Terrain t = Gaia.TerrainHelper.GetActiveTerrain();
            if (t != null)
            {
                if (t.terrainData != null)
                {
                    increment = Mathf.Max(t.terrainData.size.x, t.terrainData.size.z) / (float)t.terrainData.alphamapResolution;
                }
            }
            return increment;
        }

        /// <summary>
        /// Get detail increment from terrain
        /// </summary>
        /// <returns></returns>
        private float GetDetailIncrementFromTerrain()
        {
            float increment = 1f;
            Terrain t = Gaia.TerrainHelper.GetActiveTerrain();
            if (t != null)
            {
                if (t.terrainData != null)
                {
                    increment = Mathf.Max(t.terrainData.size.x, t.terrainData.size.z) / (float)t.terrainData.detailResolution;
                }
            }
            return increment;
        }


        /// <summary>
        /// Get a content label - look the tooltip up if possible
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        GUIContent GetLabel(string name)
        {
            string tooltip = "";
            if (m_tooltips.TryGetValue(name, out tooltip))
            {
                return new GUIContent(name, tooltip);
            }
            else
            {
                return new GUIContent(name);
            }
        }

        /// <summary>
        /// The tooltips
        /// </summary>
        static Dictionary<string, string> m_tooltips = new Dictionary<string, string>
        {
            { "Get From Terrain", "Get or update the resource prototypes from the current terrain." },
            { "Apply To Terrains", "Apply the resource prototypes into all existing terrains." },
            { "Visualise", "Visualise the fitness of resource prototypes." },
        };


    }
}
