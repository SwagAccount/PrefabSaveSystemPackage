using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PrefabTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(PrefabUtility.GetCorrespondingObjectFromSource(gameObject));
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(PrefabUtility.GetCorrespondingObjectFromSource(gameObject));
    }
}
