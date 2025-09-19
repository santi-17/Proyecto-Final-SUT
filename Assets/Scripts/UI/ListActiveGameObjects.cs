using UnityEngine;

public class ListActiveGameObjects : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Listado de GameObjects activos en la escena:");

        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.activeInHierarchy)
            {
                Debug.Log($"- {obj.name}");
            }
        }
    }
}
