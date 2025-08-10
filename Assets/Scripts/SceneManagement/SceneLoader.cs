using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Llamado desde React:
    // sendMessage("SceneLoader", "LoadSceneByName", "Arado")
    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("LoadSceneByName: nombre de escena vacío.");
            return;
        }
        // La escena debe estar en File → Build Settings → Scenes In Build
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        Debug.Log("Cargando escena: " + sceneName);
    }
}
