using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetGameObjectsActiveAtLoad : MonoBehaviour
{
    public GameObject[] objectsToSetActive;
    public GameObject[] objectsToSetInactive;

    void Awake()
    {
        foreach (GameObject obj in objectsToSetActive)
        {
            obj.SetActive(true);
        }
        foreach (GameObject obj in objectsToSetInactive)
        {
            obj.SetActive(false);
        }
    }
}
