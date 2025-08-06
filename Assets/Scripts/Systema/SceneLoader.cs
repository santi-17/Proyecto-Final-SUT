using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    //[DllImport("__Internal")]
    //private static extern void LogDesdeUnity(string menssage);

    // Start is called before the first frame update
    void Start()
    {
        //Llamado a la función de Unity para notificar que el juego está listo
        //Application.ExternalCall("NotifyUnityReady");

    }
     // Update is called once per frame
    void Update()
    {
        
    }
    public void LoadScene(string sceneName)
    {
        Debug.Log("Cargando escena: " + sceneName);
        // Cargar la escena especificada
        SceneManager.LoadScene(sceneName);
    }

   
}
