using System.Collections;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class SimulacionManager : MonoBehaviour
{
    public TextMeshProUGUI porcentajeTerreno1Text;
    public TextMeshProUGUI porcentajeTerreno2Text;
    public TextMeshProUGUI porcentajeTerreno3Text;

    // Clase para parsear respuesta API
    [System.Serializable]
    public class  DatosSimulacion
    {
        public float porcentajeCobertura1;
        public float porcentajeCobertura2;
        public float porcentajeCobertura3;
    }


#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void onUnitySendData(string json);
#endif
    public IEnumerator EnviarDatosSimulacion()
    {
        yield return new WaitForEndOfFrame();
        Debug.Log("EnviarSimulacion llamado");

        float c1 = ExtraerPorcentaje(porcentajeTerreno1Text.text);
        float c2 = ExtraerPorcentaje(porcentajeTerreno2Text.text);
        float c3 = ExtraerPorcentaje(porcentajeTerreno3Text.text);

        var datos = new DatosSimulacion
        {
            porcentajeCobertura1 = c1,
            porcentajeCobertura2 = c2,
            porcentajeCobertura3 = c3
        };

        //StartCoroutine(PostSimulacion(dto));
        string json = JsonUtility.ToJson(datos);

        //Enviar a React Llamo a la funcion en React los datos
        //Application.ExternalCall("onUnitySendData", json);
        //onUnitySendData(json);
#if UNITY_WEBGL && !UNITY_EDITOR
            onUnitySendData(json);
#else
        Debug.Log("[UNITY->JS] " + json);
#endif
    }

    private float ExtraerPorcentaje(string texto)
    {
        int idx = texto.IndexOf(':');
        if (idx < 0) return 0f;
        string sub = texto.Substring(idx + 1).Replace("%", "").Trim();
        if (float.TryParse(sub, out float val))
            return val;
        return 0f;
    }

}