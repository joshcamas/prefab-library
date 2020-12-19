using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace ArdenfallEditor.Utility
{
    public abstract class BaseScriptableObjectAssetLibraryTool : AssetLibraryTool
    {
        protected Dictionary<string, ScriptableObject> scannedAssetObjects;
        protected List<string> filteredAssetPaths;

        private string searchfilter;
        private Dictionary<ScriptableObject, Texture2D> cachedThumbnails;

        protected abstract Type GetScriptableObjectType();

        protected abstract Texture2D GenerateScriptableObjectThumbnail(int index);

        public override string ToolName()
        {
            return "Item";
        }

        protected void ClearThumbnailChache()
        {
            cachedThumbnails = new Dictionary<ScriptableObject, Texture2D>();
        }

        public override void Init()
        {
            cachedThumbnails = new Dictionary<ScriptableObject, Texture2D>();
            ScanAssets();
            SearchAssets(searchfilter);
        }

        public override void DrawTopbar()
        {
            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Refresh"), EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                cachedThumbnails = new Dictionary<ScriptableObject, Texture2D>();

                ScanAssets();
                SearchAssets(searchfilter);
            }

            EditorGUI.BeginChangeCheck();

            searchfilter = EditorGUILayout.TextField(searchfilter, EditorStyles.toolbarSearchField);

            bool search = EditorGUI.EndChangeCheck();
            if (search)
                SearchAssets(searchfilter);

        }

        protected override int GetItemCount()
        {
            return filteredAssetPaths.Count;
        }

        protected override void OnClickItem(int index)
        {
            ScriptableObject asset = scannedAssetObjects[filteredAssetPaths[index]];
            Selection.activeObject = asset;
        }

        public override void OnDestroy()
        {
            if (cachedThumbnails == null)
                return;

            foreach (KeyValuePair<ScriptableObject, Texture2D> pair in cachedThumbnails)
            {
                GameObject.DestroyImmediate(pair.Value);
            }
        }

        protected override LibraryItem GetItem(int index)
        {
            ScriptableObject scriptableObject = scannedAssetObjects[filteredAssetPaths[index]];

            if (scriptableObject == null)
                return new LibraryItem();

            LibraryItem item = new LibraryItem();
            /*
            //Tooltip
            item.tooltip = scriptableObject.name;

            if (item.tooltip.StartsWith("pre_"))
                item.tooltip = item.tooltip.Substring(4, item.tooltip.Length - 4);
            */

            if (!cachedThumbnails.ContainsKey(scriptableObject))
            {
                cachedThumbnails[scriptableObject] = GenerateScriptableObjectThumbnail(index);
            }

            item.thumbnail = cachedThumbnails[scriptableObject];
            item.isSelected = Selection.activeObject == scriptableObject;

            return item;
        }

        private void SearchAssets(string search)
        {
            filteredAssetPaths = new List<string>();

            foreach(var pair in scannedAssetObjects)
            {
                if (search == string.Empty || search == null || System.IO.Path.GetFileName(pair.Key).ToLower().Contains(search))
                {
                    filteredAssetPaths.Add(pair.Key);
                }
            }
        }

        private void ScanAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + GetScriptableObjectType().Name);

            scannedAssetObjects = new Dictionary<string, ScriptableObject>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path.Replace("\\", "/"));

                scannedAssetObjects[path] = obj;
            }
        }
    }
}