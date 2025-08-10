using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO; // para extraer el nombre de la escena sin la extensión .unity

    // Llamado desde React: se carga en la primera escena 
    // recibe comandos desde react para cambiar de escena

public class SceneLoader : MonoBehaviour
{

    //Guarda la única instancia de este script que debe existir.
    private static SceneLoader _instance;


    //Evita duplicados: si ya hay otro SceneLoader, destruye el nuevo.
    //DontDestroyOnLoad: mantiene este GameObject vivo al cambiar de escena
    private void Awake()
    {
        // Singleton para evitar duplicados al volver a escenas iniciales
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject); 
    }


    // Llamado desde React/JS: unityInstance.SendMessage("SceneLoader", "LoadScene", "Riego");
    // Parámetro: sceneName (string). Debe ser el nombre exacto de la escena 

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] Nombre de escena vacío.");
            return;
        }

        //Si existe → carga la escena
        if (!SceneExistsInBuild(sceneName))
        {
            Debug.LogError($"[SceneLoader] La escena '{sceneName}' no está en Build Settings o el nombre no coincide.");
            return;
        }

        Debug.Log($"[SceneLoader] Cargando escena: {sceneName}");
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); // no bloqueante

    }

    // Comprueba que la escena exista en Build Settings por NOMBRE
    // Recorre todas las escenas listadas en Build Settings.
    // De cada ruta obtiene solo el nombre y lo compara con sceneName.
    private bool SceneExistsInBuild(string sceneName)
    {
        int total = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < total; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i); // "Assets/Scenes/Riego.unity"
            string name = Path.GetFileNameWithoutExtension(path);   // "Riego"
            if (name == sceneName) return true;
        }
        return false;
    }

}

