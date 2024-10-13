using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TFT.PrefabBasedSaveSystem
{
    [ExecuteInEditMode]
    public class PrefabReference : MonoBehaviour
    {
        public GameObject GotPrefab;
        public bool getGUID;
        public string PrefabGUID;

        private void Update()
        {
            if (getGUID)
            {
                string prefabPath = AssetDatabase.GetAssetPath(GotPrefab);
                PrefabGUID = AssetDatabase.AssetPathToGUID(prefabPath);
                getGUID = false;
            }
        }
    }
}
