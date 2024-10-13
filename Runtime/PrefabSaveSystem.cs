using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TFT.PrefabBasedSaveSystem
{
    public class PrefabSaveSystem : MonoBehaviour
    {

        string path => $"{(Application.isEditor ? "Assets/" : "")}saves/";

        string filePath => $"{path}{SceneManager.GetActiveScene().name}-{gameObject.name}.json";

        public bool SaveNow;
        public bool LoadNow;

        void Start()
        {

        }


        void Update()
        {
            if (SaveNow || Input.GetKey(KeyCode.F5))
            {
                Save();
                SaveNow = false;
            }

            if (LoadNow || Input.GetKey(KeyCode.F6))
            {
                Load();
                LoadNow = false;
            }
        }

        void Save()
        {
            List<SavedPrefab> savedPrefabs = new List<SavedPrefab>();

            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject child = transform.GetChild(i).gameObject;

                var prefabReference = child.GetComponent<PrefabReference>();

                if (prefabReference == null) continue;

                var savedVariableSystems = new List<PrefabVariableSystem.SavedPrefabVariableSystem>();

                var allSystems = GetComponentsInChildren<PrefabVariableSystem>(true);

                foreach (var system in allSystems)
                {
                    savedVariableSystems.Add(system.Save());
                }

                SavedPrefab savedPrefab = new SavedPrefab();

                savedPrefab.PrefabRefGUID = prefabReference.PrefabGUID;

                savedPrefab.Pos = new List<float>
                    {
                        child.transform.position.x,child.transform.position.y,child.transform.position.z
                    };

                savedPrefab.Rot = new List<float>
                    {
                        child.transform.rotation.x,child.transform.rotation.y,child.transform.rotation.z, child.transform.rotation.w
                    };

                savedPrefab.savedVariableSystems = savedVariableSystems;

                savedPrefabs.Add(savedPrefab);
            }

            var newSavedPrefabs = new SavedPrefabs
            {
                savedPrefabs = savedPrefabs
            };

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            File.WriteAllText(filePath, JsonUtility.ToJson(newSavedPrefabs, true));
        }

        void Load()
        {
            if (!File.Exists(filePath)) return;

            var children = new List<Transform>();
            foreach (Transform t in transform)
            {
                children.Add(t);
            }

            foreach (Transform t in children)
            {
                DestroyImmediate(t.gameObject);
            }

            string jsonString = File.ReadAllText(filePath);

            SavedPrefabs savedPrefabs = JsonUtility.FromJson<SavedPrefabs>(jsonString);

            foreach (SavedPrefab savedPrefab in savedPrefabs.savedPrefabs)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(savedPrefab.PrefabRefGUID);
                GameObject loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (loadedPrefab == null) return;
                GameObject prefab = Instantiate(loadedPrefab);
                prefab.transform.SetParent(transform);
                prefab.transform.position = new Vector3(savedPrefab.Pos[0], savedPrefab.Pos[1], savedPrefab.Pos[2]);
                prefab.transform.rotation = new Quaternion(savedPrefab.Rot[0], savedPrefab.Rot[1], savedPrefab.Rot[2], savedPrefab.Rot[3]);

                var PrefabRef = prefab.GetComponent<PrefabReference>();
                if (PrefabRef == null)
                    PrefabRef = prefab.AddComponent<PrefabReference>();

                PrefabRef.PrefabGUID = savedPrefab.PrefabRefGUID;

                foreach (var savedSystem in savedPrefab.savedVariableSystems)
                {
                    PrefabVariableSystem system = SystemByGuid(prefab, savedSystem.gUID);
                    if (system == null) return;
                    system.DeclareVariables();
                    system.Load(savedSystem);
                }
            }
        }

        PrefabVariableSystem SystemByGuid(GameObject child, string gUID)
        {
            var allSystems = child.GetComponentsInChildren<PrefabVariableSystem>(true);
            foreach (var system in allSystems)
            {
                if (system.gUID == gUID)
                    return system;
            }
            return null;
        }

        [Serializable]
        public class SavedPrefabs
        {
            public List<SavedPrefab> savedPrefabs;
        }

        [Serializable]
        public class SavedPrefab
        {
            public string PrefabRefGUID;

            public List<float> Pos;
            public List<float> Rot;

            public List<PrefabVariableSystem.SavedPrefabVariableSystem> savedVariableSystems;
        }
    }
}
