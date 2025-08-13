using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelLoader : MonoBehaviour
{
    public GameObject loadingScreen;
    public Slider slider;
    public Text progressText;
    public TextMeshProUGUI textoProgreso;  
    private string nombreCampo;
    public void LoadLevel(int sceneIndex)
    {
        nombreCampo = ObtenerNombreCampo();
        if (textoProgreso != null)
        {
            textoProgreso.text = $"Cargando la escena de {nombreCampo}";
        }
        StartCoroutine(LoadAsynchronously(sceneIndex));
        
    }
    IEnumerator LoadAsynchronously(int sceneIndex)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
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



}
