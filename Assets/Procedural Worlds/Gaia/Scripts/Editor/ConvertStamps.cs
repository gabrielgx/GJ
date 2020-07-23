using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;

namespace Gaia
{
   /// <summary>
   /// Utility to convert stamps from the old data format into .exr images
   /// </summary>

    class ConvertStamps : EditorWindow
    {
        //The folder path we will process
        string folderPath = "";

        //Are subfolders included?
        bool includeSubFolders = true;

        //Remove the source files after conversion?
        bool deleteSourceFiles = true;

        //The absolute number of stamp preview jpgs we are going to process
        int jpgCount = 0;

        //The current jpg we are at for the progress bar
        int jpgIndex = 0;

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Folder", folderPath);
            if (GUILayout.Button("Select Folder"))
            {
                folderPath = EditorUtility.OpenFolderPanel("Select Stamp Folder", "", "");
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(15f);
            includeSubFolders = EditorGUILayout.Toggle("Include Subfolders",includeSubFolders);
            deleteSourceFiles = EditorGUILayout.Toggle("Remove Old Stamp Data", deleteSourceFiles);
            GUILayout.Space(25f);

            if (GUILayout.Button("Start Conversion"))
            {
                string popupText = "This conversion process will look for old Gaia Stamps in the folder\n\n" + folderPath + 
                                    "\n\n and convert them into the new format. Running this process is only required if you " + 
                                    "still have stamps that consist of a preview picture and the actual data in a 'Data' Folder.";

                if (deleteSourceFiles)
                {
                    popupText += "\n\nWARNING: You selected to delete the source files. This process will delete all .jpg files and all 'Data' folders in the given directory after the conversion.";
                }
                popupText += "\n\nContinue?";


                if (EditorUtility.DisplayDialog("Starting Conversion",   popupText, "OK", "Cancel"))
                {
                    StartConversion(folderPath);
                }
            }
        }

        private void StartConversion(string folderPath)
        {
            EditorUtility.ClearProgressBar();
            //Get the count of jpg files for the progress bar
            jpgIndex = 1;
            jpgCount = Directory.GetFiles(folderPath, "*.jpg", SearchOption.AllDirectories).Length;

            //Begin with root folder, the subfolders will be handled automatically with recursion
            ConvertFolder(folderPath);

            jpgIndex = 1;

            if (deleteSourceFiles)
            {
                RemoveSourceData(folderPath);
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        private void RemoveSourceData(string folderPath)
        {
            //Handle Subfolders recursively
            if (includeSubFolders)
            {
                string[] subFolders = Directory.GetDirectories(folderPath);
                foreach (string s in subFolders)
                {
                    //Did we find a data folder? Delete it
                    if (s.EndsWith("\\Data"))
                    {
                        FileUtil.DeleteFileOrDirectory(s);
                    }
                    else //no data folder -> search inside
                    {
                        RemoveSourceData(s);
                    }
                }
            }

            //Delete all stamp preview files
            var allJPGs = Directory.GetFiles(folderPath, "*.jpg", SearchOption.TopDirectoryOnly);
            foreach (string filename in allJPGs)
            {
                EditorUtility.DisplayProgressBar("Deleting Source Files...", "Deleting File " + jpgIndex.ToString() + " of " + jpgCount.ToString(), Mathf.InverseLerp(0, jpgCount, jpgIndex));
                FileUtil.DeleteFileOrDirectory(filename);
            }

        }

        private void ConvertFolder(string folderPath)
        {
            //Handle Subfolders recursively
            if (includeSubFolders)
            {
                string[] subFolders = Directory.GetDirectories(folderPath);
                foreach (string s in subFolders)
                {
                    ConvertFolder(s);
                }
            }

            //Do the actual file conversion in the current folder
            string[] files = Directory.GetFiles(folderPath);

            foreach (string file in files)
            {
                if (file.EndsWith(".jpg"))
                {
                    EditorUtility.DisplayProgressBar("Converting Stamps", "Processing Stamp " + jpgIndex.ToString() + " of " + jpgCount.ToString(), Mathf.InverseLerp(0, jpgCount, jpgIndex));
                    //Get path relative to project folder else the function to load the asset will create a warning in the console
                    string relativePath = file.Substring(file.IndexOf("Assets", 0));
                    var tex = (Texture2D)GaiaUtils.GetAsset(relativePath, typeof(Texture2D));
                    if (GaiaUtils.CheckValidGaiaStampPath(tex))
                    {
                        var heightMap = new UnityHeightMap(GaiaUtils.GetGaiaStampPath(tex));
                        GaiaUtils.CompressToMultiChannelFileImage(file.Replace(".jpg", ""), heightMap, heightMap, heightMap, null, TextureFormat.RGBAFloat, GaiaConstants.ImageFileType.Exr);
                        jpgIndex++;
                    }
                }
            }

        }

    }
}