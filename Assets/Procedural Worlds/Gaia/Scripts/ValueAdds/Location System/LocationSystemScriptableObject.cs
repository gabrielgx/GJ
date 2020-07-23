#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace Gaia
{
    [System.Serializable]
    public class LocationBookmarkSettings
    {
        public Vector3 m_savedCameraPosition = Vector3.zero;
        public Quaternion m_savedCameraRotation = Quaternion.identity;
        public Vector3 m_savedPlayerPosition = Vector3.zero;
        public Quaternion m_savedPlayerRotation = Quaternion.identity;
    }

    public class LocationSystemScriptableObject : ScriptableObject
    {
        [HideInInspector]
        public Vector3 m_savedCameraPosition = Vector3.zero;
        [HideInInspector]
        public Quaternion m_savedCameraRotation = Quaternion.identity;
        [HideInInspector]
        public Vector3 m_savedPlayerPosition = Vector3.zero;
        [HideInInspector]
        public Quaternion m_savedPlayerRotation = Quaternion.identity;
        [HideInInspector]
        public bool m_hasBeenSaved = false;
        [HideInInspector]
        public List<string> m_bookmarkedLocationNames = new List<string>();
        [HideInInspector]
        public List<LocationBookmarkSettings> m_bookmarkedSettings = new List<LocationBookmarkSettings>();
        [HideInInspector]
        public bool m_autoLoad = true;

        [HideInInspector]
        public KeyCode m_mainKey = KeyCode.LeftShift;
        [HideInInspector]
        public KeyCode m_addBookmarkKey = KeyCode.B;
        [HideInInspector]
        public KeyCode m_prevBookmark = KeyCode.LeftArrow;
        [HideInInspector]
        public KeyCode m_nextBookmark = KeyCode.RightArrow;

        public bool HasBeenSaved()
        {
            return m_hasBeenSaved;
        }

        public void SaveLocation(Transform camera, Transform player = null)
        {
            if (camera == null)
            {
                Debug.LogError("No camera transform provided");
                return;
            }

#if UNITY_EDITOR

            if (SceneView.lastActiveSceneView != null)
            {
                if (SceneView.lastActiveSceneView.camera == null)
                {
                    return;
                }
            }
            else
            {
                return;
            }

            #endif

            if (Application.isPlaying)
            {
                m_savedCameraPosition = camera.position;
                m_savedCameraRotation = camera.rotation;

                if (player != null)
                {
                    m_savedPlayerPosition = player.position;
                    m_savedPlayerRotation = player.rotation;
                }
            }
            else
            {
#if UNITY_EDITOR
                m_savedCameraPosition = SceneView.lastActiveSceneView.camera.transform.position;
                m_savedCameraRotation = SceneView.lastActiveSceneView.camera.transform.rotation;
#endif
            }

            m_hasBeenSaved = true;


            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            #endif
        }

        public void LoadLocation(Transform camera, Transform player = null)
        {
            if (camera == null)
            {
                Debug.LogError("No camera transform provided");
                return;
            }

            camera.position = m_savedCameraPosition;
            camera.rotation = m_savedCameraRotation;

            if (player != null)
            {
                player.position = m_savedPlayerPosition;
                player.rotation = m_savedPlayerRotation;
            }

            m_hasBeenSaved = false;
        }

        public void AddNewBookmark(LocationSystem locationSystem, string overrideName = null)
        {
            bool addBookmark = true;
            for (int i = 0; i < m_bookmarkedLocationNames.Count; i++)
            {
                if (overrideName != null)
                {
                    if (m_bookmarkedLocationNames[i] == overrideName)
                    {
                        addBookmark = false;
                    }
                }
                else
                {
                    if (m_bookmarkedLocationNames[i] == locationSystem.m_bookmarkName)
                    {
                        addBookmark = false;
                    }
                }
            }

            if (addBookmark)
            {
                if (overrideName != null)
                {
                    m_bookmarkedLocationNames.Add(overrideName);
                }
                else
                {
                    m_bookmarkedLocationNames.Add(locationSystem.m_bookmarkName);
                }

                m_bookmarkedSettings.Add(GetCurrentLocation(locationSystem));
            }
            else
            {
                Debug.LogWarning("Location name " + locationSystem.m_bookmarkName + " already exists please change the name then add bookmark again.");
            }
        }

        public void RemoveAllBookmarks()
        {
            m_bookmarkedLocationNames.Clear();
            m_bookmarkedSettings.Clear();
        }

        public void RemoveBookmark(int index, LocationSystem locationSystem)
        {
            if (locationSystem.m_selectedBookmark == m_bookmarkedLocationNames.Count - 1)
            {
                locationSystem.m_selectedBookmark--;
            }
            m_bookmarkedLocationNames.RemoveAt(index);
            m_bookmarkedSettings.RemoveAt(index);
        }

        public void LoadBookmark(LocationSystem locationSystem)
        {
            LocationBookmarkSettings settings = m_bookmarkedSettings[locationSystem.m_selectedBookmark];
            if (settings != null)
            {
                if (locationSystem.m_camera != null)
                {
                    if (Application.isPlaying)
                    {
                        locationSystem.m_camera.SetPositionAndRotation(settings.m_savedCameraPosition, settings.m_savedCameraRotation);
                        if (locationSystem.m_player != null)
                        {
                            locationSystem.m_player.SetPositionAndRotation(settings.m_savedPlayerPosition, settings.m_savedPlayerRotation);
                        }
                    }
                    else
                    {
#if UNITY_EDITOR
                        if (SceneView.lastActiveSceneView != null)
                        {
                            if (SceneView.lastActiveSceneView.camera != null)
                            {
                                Camera target = SceneView.lastActiveSceneView.camera;
                                Transform temp = target.transform;
                                temp.position = settings.m_savedCameraPosition;
                                temp.rotation = settings.m_savedCameraRotation;

                                SceneView.lastActiveSceneView.AlignViewToObject(temp);
                            }
                        }
#endif
                    }
                }
            }
        }

        public static LocationBookmarkSettings GetCurrentLocation(LocationSystem locationSystem)
        {
            LocationBookmarkSettings settings = new LocationBookmarkSettings();
            if (Application.isPlaying)
            {
                if (locationSystem.m_camera != null)
                {
                    settings.m_savedCameraPosition = locationSystem.m_camera.localPosition;
                    settings.m_savedCameraRotation = locationSystem.m_camera.localRotation;
                }

                if (locationSystem.m_player != null)
                {
                    settings.m_savedPlayerPosition = locationSystem.m_player.localPosition;
                    settings.m_savedPlayerRotation = locationSystem.m_player.localRotation;
                }
            }
            else
            {
#if UNITY_EDITOR
                if (SceneView.lastActiveSceneView != null)
                {
                    if (SceneView.lastActiveSceneView.camera != null)
                    {
                        settings.m_savedCameraPosition = SceneView.lastActiveSceneView.camera.transform.localPosition;
                        settings.m_savedCameraRotation = SceneView.lastActiveSceneView.camera.transform.localRotation;
                    }
                }
#endif
            }

            return settings;
        }

        /// <summary>
        /// Create sky profile asset
        /// </summary>
#if UNITY_EDITOR
        [MenuItem("Assets/Create/Procedural Worlds/Gaia/Location Profile")]
        public static void CreateLocationProfiles()
        {
            LocationSystemScriptableObject asset = ScriptableObject.CreateInstance<LocationSystemScriptableObject>();
            AssetDatabase.CreateAsset(asset, "Assets/Location Profile.asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
#endif
    }
}