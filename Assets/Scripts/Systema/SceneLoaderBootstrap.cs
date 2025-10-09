using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoaderBootstrap : MonoBehaviour
{
    private static SceneLoaderBootstrap _instance;

    [Header("UI de carga (opcional)")]
    public GameObject loadingScreen;
    public Slider slider;
    public Text progressText;
    public TextMeshProUGUI textoProgreso;

    [Header("Prueba en Editor (opcional)")]
    public bool autoLoadOnStart = false;
    public string initialScene = ""; // ej: "Disquera"

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void __onUnitySceneReady(string sceneName);
#endif

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        gameObject.name = "SceneLoader"; // requerido por SendMessage desde React
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        Application.logMessageReceived += OnLog;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        Application.logMessageReceived -= OnLog;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (autoLoadOnStart && !string.IsNullOrWhiteSpace(initialScene))
            LoadSceneByName(initialScene);
#endif
    }

    // API para React
    public void LoadSceneByName(string sceneName) => LoadScene(sceneName);

    private void LoadScene(string sceneName)
    {
        sceneName = (sceneName ?? "").Trim();
        if (string.IsNullOrEmpty(sceneName)) { Debug.LogError("[SceneLoader] Nombre de escena vacío."); return; }
        if (!SceneExistsInBuild(sceneName))
        {
            Debug.LogError($"[SceneLoader] La escena '{sceneName}' no está en Build Settings o el nombre no coincide.");
            return;
        }

        if (textoProgreso) textoProgreso.text = $"Cargando escena: {sceneName}";
        Debug.Log($"[SceneLoader] Cargando escena: {sceneName}");
        StartCoroutine(LoadAsynchronously(sceneName));
    }

    private IEnumerator LoadAsynchronously(string sceneName)
    {
        if (loadingScreen) loadingScreen.SetActive(true);
        float startAt = Time.unscaledTime;

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            float p = Mathf.Clamp01(op.progress / 0.9f);
            if (slider) slider.value = p;
            if (progressText) progressText.text = (p * 100f).ToString("F0") + "%";
            if (textoProgreso) textoProgreso.text = $"{(p * 100f):F0}%";

            // Watchdog: si se queda mucho en 0.9 es probable que haya una excepción en Awake/Start
            if (p >= 0.999f && (Time.unscaledTime - startAt) > 20f)
            {
                Debug.LogError("[SceneLoader] Atascado activando escena (>20s). ¿Excepción en Awake/Start?");
                break;
            }
            yield return null;
        }

        if (loadingScreen) loadingScreen.SetActive(false);

#if UNITY_WEBGL && !UNITY_EDITOR
        // Llama a la función JS definida en el .jslib
        try { __onUnitySceneReady(sceneName); } catch { }
#else
        Debug.Log($"[SceneLoader] (Editor) Escena lista → __onUnitySceneReady({sceneName})");
#endif
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[SCENE] Cargada: " + scene.name);
    }

    private void OnLog(string condition, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
            Debug.Log($"[LOG] {type}: {condition}\n{stack}");
    }
}
//using System.Collections;
//using System.IO;
//using System.Linq;
//using TMPro;
//using UnityEngine;
//using UnityEngine.SceneManagement;
//using UnityEngine.UI;

//public class SceneLoaderBootstrap : MonoBehaviour
//{
//    private static SceneLoaderBootstrap _instance;

//    public GameObject loadingScreen;
//    public Slider slider;
//    public Text progressText;
//    public TextMeshProUGUI textoProgreso;
//    private string nombreCampo;

//    [Header("Prueba en Editor (opcional)")]
//    public bool autoLoadOnStart = false;   // marcá en el Inspector si querés probar
//    public string initialScene = "";       // nombre exacto de la escena (p.ej. Arado)

//    private void Awake()
//    {
//        if (_instance != null && _instance != this)
//        {
//            Destroy(gameObject);
//            return;
//        }
//        _instance = this;

//        // 👇 CLAVE: así React puede hacer sendMessage("SceneLoader", ...)
//        gameObject.name = "SceneLoader";

//        DontDestroyOnLoad(gameObject);
//    }

//    private void Start()
//    {
//#if UNITY_EDITOR
//        if (autoLoadOnStart && !string.IsNullOrEmpty(initialScene))
//            LoadScene(initialScene);
//#endif

//#if UNITY_WEBGL && !UNITY_EDITOR
//        // Auto-cargar por URL: /simulator/arado -> "Arado"
//        TryLoadSceneFromUrl();
//#endif
//    }

//    public void LoadScene(string sceneName)
//    {
//        if (string.IsNullOrEmpty(sceneName))
//        {
//            Debug.LogError("[SceneLoader] Nombre de escena vacío.");
//            return;
//        }

//        if (!SceneExistsInBuild(sceneName))
//        {
//            Debug.LogError($"[SceneLoader] La escena '{sceneName}' no está en Build Settings o el nombre no coincide.");
//            return;
//        }
//        //ESTO LO HICE YO SANTI
//        //nombreCampo = ObtenerNombreCampo();



//        if (textoProgreso != null)
//        {
//            if (sceneName == "Disquera")
//                textoProgreso.text = $"Cargando la escena de Disquera";
//            else if (sceneName.Equals("Arado"))
//                textoProgreso.text = $"Cargando la escena de Arado";
//            else if (sceneName.Equals("Sembradora"))
//                textoProgreso.text = $"Cargando la escena de Sembrado"; 
//            else if (sceneName.Equals("Riego"))
//                textoProgreso.text = $"Cargando la escena de Riego";
//            else if (sceneName.Equals("Fitosanitario"))
//                textoProgreso.text = $"Cargando la escena de Fitosanitaria";
//            else
//                textoProgreso.text = $"Campo Desconocido";

//        }
//        StartCoroutine(LoadAsynchronously(sceneName));
//        //ACA TERMINA
//        Debug.Log($"[SceneLoader] Cargando escena: {sceneName}");
//        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
//    }

//    private bool SceneExistsInBuild(string sceneName)
//    {
//        int total = SceneManager.sceneCountInBuildSettings;
//        for (int i = 0; i < total; i++)
//        {
//            string path = SceneUtility.GetScenePathByBuildIndex(i);
//            string name = Path.GetFileNameWithoutExtension(path);
//            if (name == sceneName) return true;
//        }
//        return false;
//    }
//    //ESTO LO HICE YO SANTI
//    IEnumerator LoadAsynchronously(string sceneName)
//    {
//        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName/*, LoadSceneMode.Single*/);
//        loadingScreen.SetActive(true);
//        while (!operation.isDone)
//        {
//            float progress = Mathf.Clamp01(operation.progress / 0.9f);
//            slider.value = progress;
//            progressText.text = (progress * 100f).ToString("F2") + "%";
//            yield return null;
//        }
//    }

//    //ACA TERMINA
//    public class SceneLogger : MonoBehaviour
//    {
//        void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
//        void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

//        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
//        {
//            Debug.Log("[SCENE] Cargada: " + scene.name);
//        }
//    }


//#if UNITY_WEBGL && !UNITY_EDITOR
//    private void TryLoadSceneFromUrl()
//    {
//        // Ej: http://localhost:5173/simulator/arado
//        var uri = new System.Uri(Application.absoluteURL);
//        string last = uri.Segments.Last().Trim('/');   // "arado"

//        if (string.IsNullOrEmpty(last)) return;

//        string scene = MapSlugToScene(last);           // "Arado", "AradoDisco", etc.
//        if (!string.IsNullOrEmpty(scene) && scene != "Bootstrap")
//            LoadScene(scene);
//    }

//    private string MapSlugToScene(string slug)
//    {
//        slug = slug.ToLower();
//        switch (slug)
//        {
//            case "arado": return "Arado";
//            case "disquera": return "Disquera";
//            case "sembradora": return "Sembradora";
//            case "riego": return "Riego";
//            case "fitosanitario": return "Fitosanitario";
//            case "bootstrap": return "Bootstrap";
//            default: return char.ToUpper(slug[0]) + slug.Substring(1); // fallback simple
//        }
//    }
//#endif
//}
