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

