using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Ardenfall.Utility
{
    public class AssetLibrary : ScriptableObject
    {
        private static AssetLibrary instance;
#if UNITY_EDITOR
        public static AssetLibrary Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = Resources.Load<AssetLibrary>("assetlibrary");

                if (instance == null)
                {
                    instance = ScriptableObject.CreateInstance<AssetLibrary>();
                    instance.rootFolders = new string[1] { "Assets/" };
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                    UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/Resources/assetlibrary.asset");
                    UnityEditor.AssetDatabase.Refresh();
                }

                return instance;
            }
        }
#endif
        public string[] rootFolders = new string[0];
        public string[] blacklistFolders = new string[0];
        public string[] blacklistLabels = new string[0];
    }



}
