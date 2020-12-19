using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Ardenfall.Utility;
using System.Linq;
using System;

namespace ArdenfallEditor.Utility
{
    public class PrefabAssetLibraryTool : AssetLibraryTool
    {
        private List<string> allProjectLabels;
        private List<string> prefabLabels;
        private List<string> filteredLabels;
        private string singleFilteredLabel;

        private bool singleTag = false;
        private string searchfilter;
        private bool enableBlacklistTags = true;

        private List<string> scannedAssetPaths;
        private Dictionary<string, GameObject> scannedAssetObjects;

        private List<string> filteredAssetPaths;

        private Dictionary<GameObject, Texture2D> cachedThumbnails;

        public override string ToolName()
        {
            return "Prefab";
        }

        public override void Init()
        {
            cachedThumbnails = new Dictionary<GameObject, Texture2D>();

            try
            {
                ScanAssets();
                ScanAllLabels();
            }
            catch
            {
                EditorUtility.ClearProgressBar();
            }

            SearchAssets(searchfilter);
        }

        public override void DrawTopbar()
        {
            bool search = false;

            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Refresh"), EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                //Clear thumbnail cache
                cachedThumbnails = new Dictionary<GameObject, Texture2D>();

                try
                {
                    ScanAssets();
                    ScanAllLabels();
                } catch(Exception e)
                {
                    Debug.LogError(e.Message + "\n\n" + e.StackTrace);
                    EditorUtility.ClearProgressBar();
                }

                //Search
                SearchAssets(searchfilter);
            }

            if (GUILayout.Toggle(enableBlacklistTags, new GUIContent("BL","Hide Blacklisted Labels"), EditorStyles.toolbarButton, GUILayout.Width(40)) != enableBlacklistTags)
            {
                enableBlacklistTags = !enableBlacklistTags;
                search = true;
            }

            EditorGUI.BeginChangeCheck();

            searchfilter = EditorGUILayout.TextField(searchfilter, EditorStyles.toolbarSearchField);

            if (singleTag)
                singleFilteredLabel = SingleSelectDropdown("", singleFilteredLabel, prefabLabels, EditorStyles.toolbarDropDown);
            else
                filteredLabels = MultiSelectDropdown("", filteredLabels, prefabLabels, EditorStyles.toolbarDropDown, GUILayout.MaxWidth(200));

            if (EditorGUI.EndChangeCheck())
                search = true;

            if (search)
                SearchAssets(searchfilter);

        }

        protected override bool EnableHoverContent()
        {
            return true;
        }

        protected override void DrawHoverContent(int index)
        {
            GameObject prefab = scannedAssetObjects[filteredAssetPaths[index]];

            string name = prefab.name;

            if (name.StartsWith("pre_"))
                name = name.Substring(4, name.Length - 4);

            var labelst = new GUIStyle(EditorStyles.boldLabel);
            labelst.wordWrap = true;

            EditorGUILayout.LabelField(name, labelst);

            var labels = AssetDatabase.GetLabels(prefab);

            if (labels.Length > 0)
            {
                string labelName = "";

                foreach (string label in AssetDatabase.GetLabels(prefab))
                    labelName += "[" + label + "] ";

                EditorGUILayout.LabelField(labelName);
            }
        }

        protected override int GetItemCount()
        {
            return filteredAssetPaths.Count;
        }

        protected override void OnClickItem(int index)
        {
            GameObject prefab = scannedAssetObjects[filteredAssetPaths[index]];
            Selection.activeGameObject = prefab;
        }

        protected override LibraryItem GetItem(int index)
        {
            GameObject prefab = scannedAssetObjects[filteredAssetPaths[index]];

            if (prefab == null)
                return new LibraryItem();

            LibraryItem item = new LibraryItem();

            if (!cachedThumbnails.ContainsKey(prefab))
                cachedThumbnails[prefab] = AssetPreview.GetAssetPreview(prefab);

            item.thumbnail = cachedThumbnails[prefab];

            item.isSelected = Selection.activeObject == prefab;

            return item;
        }

        protected override bool EnableDragIntoScene()
        {
            return true;
        }

        protected override GameObject CreateGhostPrefab(int index)
        {
            GameObject prefab = scannedAssetObjects[filteredAssetPaths[index]];
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        protected override void OnPlaceInScene(int index, Vector3 position)
        {
            GameObject prefab = scannedAssetObjects[filteredAssetPaths[index]];

            var spawnedObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            spawnedObject.name = prefab.name;
            spawnedObject.transform.position = position;
            spawnedObject.transform.rotation = Quaternion.identity;

            Selection.activeObject = spawnedObject;

            Undo.RegisterCreatedObjectUndo(spawnedObject, "Drop Prefab Into Scene");
        }

        public override void OnDestroy()
        {
            if (cachedThumbnails == null)
                return;

            foreach (KeyValuePair<GameObject, Texture2D> pair in cachedThumbnails)
            {
                GameObject.DestroyImmediate(pair.Value);
            }
        }

        private List<string> GetFilteredLabels()
        {
            if (singleTag)
                if (singleFilteredLabel != null)
                    return new List<string>(new string[] { singleFilteredLabel });
                else
                    return new List<string>();

            return filteredLabels;
        }

        private void SearchAssets(string search = "")
        {
            filteredAssetPaths = new List<string>();

            if (search == null)
                search = "";

            search = search.ToLower();
            foreach (string path in scannedAssetPaths)
            {
                //Check search string
                if (search == string.Empty || System.IO.Path.GetFileName(path).ToLower().Contains(search))
                {
                    //Check labels
                    string[] labels = AssetDatabase.GetLabels(scannedAssetObjects[path]);
                    bool success = false;

                    //No filtered labels, no problem
                    if (GetFilteredLabels().Count == 0)
                        success = true;

                    foreach (string label in GetFilteredLabels())
                    {
                        if (labels.Contains(label))
                        {
                            success = true;
                            break;
                        }
                    }

                    //Make sure is not in blacklisted label
                    foreach (string blacklistLabel in AssetLibrary.Instance.blacklistLabels)
                    {
                        if (labels.Contains(blacklistLabel))
                        {
                            success = false;
                            break;
                        }
                    }

                    if (success)
                        filteredAssetPaths.Add(path);
                }
            }
        }

        private void ScanAssets()
        {
            EditorUtility.DisplayProgressBar("Scanning Assets", "Finding Prefabs", 0.1f);

            string[] guids = AssetDatabase.FindAssets("t:prefab");

            scannedAssetObjects = new Dictionary<string, GameObject>();
            scannedAssetPaths = new List<string>();
            filteredAssetPaths = new List<string>();

            int total = guids.Length;
            int sofar = 0;

            foreach (string guid in guids)
            {
                EditorUtility.DisplayProgressBar("Scanning Assets", "Loading Prefabs", (float)sofar / (float)total);
                sofar++;

                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (AssetPassesScan(path))
                {
                    GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace("\\", "/"));

                    if (obj != null)
                    {
                        scannedAssetPaths.Add(path);
                        scannedAssetObjects.Add(path, obj);
                    }
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private bool AssetPassesScan(string path)
        {
                //File is in blacklist
            foreach (string black in AssetLibrary.Instance.blacklistFolders)
            {
                if (path.ToLower().StartsWith(black.ToLower()))
                    return false;
            }

            foreach (string root in AssetLibrary.Instance.rootFolders)
            {
                if (path.ToLower().StartsWith(root.ToLower()))
                    return true;
            }

            return false;
        }

        private void ScanAllLabels()
        {
            allProjectLabels = new List<string>();

            int total = scannedAssetObjects.Count;
            int sofar = 0;

            foreach (var pair in scannedAssetObjects)
            {
                EditorUtility.DisplayProgressBar("Scanning Assets", "Building Labels", (float)sofar/ (float)total);
                sofar++;

                var labels = AssetDatabase.GetLabels(pair.Value);

                foreach (string label in labels)
                {
                    if (!allProjectLabels.Contains(label))
                        allProjectLabels.Add(label);
                }
            }

            prefabLabels = allProjectLabels; //Temporary

            if (filteredLabels == null)
                filteredLabels = new List<string>();

            EditorUtility.ClearProgressBar();
        }

    }
}