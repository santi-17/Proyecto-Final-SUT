using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;

public class SceneLoaderBootstrap : MonoBehaviour
{
    private static SceneLoaderBootstrap _instance;

    [Header("Prueba en Editor (opcional)")]
    public bool autoLoadOnStart = false;   // marcá en el Inspector si querés probar
    public string initialScene = "";       // nombre exacto de la escena (p.ej. Arado)

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // 👇 CLAVE: así React puede hacer sendMessage("SceneLoader", ...)
        gameObject.name = "SceneLoader";

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (autoLoadOnStart && !string.IsNullOrEmpty(initialScene))
            LoadScene(initialScene);
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        // Auto-cargar por URL: /simulator/arado -> "Arado"
        TryLoadSceneFromUrl();
#endif
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] Nombre de escena vacío.");
            return;
        }

        if (!SceneExistsInBuild(sceneName))
        {
            Debug.LogError($"[SceneLoader] La escena '{sceneName}' no está en Build Settings o el nombre no coincide.");
            return;
        }

        Debug.Log($"[SceneLoader] Cargando escena: {sceneName}");
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }

    private bool SceneExistsInBuild(string sceneName)
    {
        int total = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < total; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        return false;
    }


    public class SceneLogger : MonoBehaviour
    {
        void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log("[SCENE] Cargada: " + scene.name);
        }
    }


#if UNITY_WEBGL && !UNITY_EDITOR
    private void TryLoadSceneFromUrl()
    {
        // Ej: http://localhost:5173/simulator/arado
        var uri = new System.Uri(Application.absoluteURL);
        string last = uri.Segments.Last().Trim('/');   // "arado"

        if (string.IsNullOrEmpty(last)) return;

        string scene = MapSlugToScene(last);           // "Arado", "AradoDisco", etc.
        if (!string.IsNullOrEmpty(scene) && scene != "Bootstrap")
            LoadScene(scene);
    }

    private string MapSlugToScene(string slug)
    {
        slug = slug.ToLower();
        switch (slug)
        {
            case "arado": return "Arado";
            case "aradodisco": return "AradoDisco";
            case "sembradora": return "Sembradora";
            case "riego": return "Riego";
            case "fitosanitario": return "Fitosanitario";
            case "bootstrap": return "Bootstrap";
            default: return char.ToUpper(slug[0]) + slug.Substring(1); // fallback simple
        }
    }
#endif
}
