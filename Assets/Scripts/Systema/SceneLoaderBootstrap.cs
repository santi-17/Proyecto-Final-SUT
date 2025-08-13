using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoaderBootstrap : MonoBehaviour
{
    private static SceneLoaderBootstrap _instance;

    public GameObject loadingScreen;
    public Slider slider;
    public Text progressText;
    public TextMeshProUGUI textoProgreso;
    private string nombreCampo;

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
        //ESTO LO HICE YO SANTI
        //nombreCampo = ObtenerNombreCampo();

        

        if (textoProgreso != null)
        {
            if (sceneName == "AradoDisco")
                textoProgreso.text = $"Cargando la escena de Disquera";
            else if (sceneName.Equals("Arado"))
                textoProgreso.text = $"Cargando la escena de Arado";
            else if (sceneName.Equals("Sembradora"))
                textoProgreso.text = $"Cargando la escena de Sembrado"; 
            else if (sceneName.Equals("Riego"))
                textoProgreso.text = $"Cargando la escena de Riego";
            else if (sceneName.Equals("Fitosanitario"))
                textoProgreso.text = $"Cargando la escena de Fitosanitaria";
            else
                textoProgreso.text = $"Campo Desconocido";
            
        }
        StartCoroutine(LoadAsynchronously(sceneName));
        //ACA TERMINA
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
    //ESTO LO HICE YO SANTI
    IEnumerator LoadAsynchronously(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName/*, LoadSceneMode.Single*/);
        loadingScreen.SetActive(true);
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            slider.value = progress;
            progressText.text = (progress * 100f).ToString("F2") + "%";
            yield return null;
        }
    }

    string ObtenerNombreCampo()
    {
        string sceneName = SceneManager.GetActiveScene().name.ToLower();

        if (sceneName.Contains("aradodisco"))
            return "Disquera";
        else if (sceneName.Contains("arado"))
            return "Arado";
        else if (sceneName.Contains("sembradora"))
            return "Sembradora";
        else if (sceneName.Contains("riego"))
            return "Regadora";
        else if (sceneName.Contains("fitosanitario"))
            return "Fitosanitario";
        else
            return "Campo Desconocido";
    }
    //ACA TERMINA
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
