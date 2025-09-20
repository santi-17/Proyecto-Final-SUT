using System.Collections;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

public class SimulacionControlador : MonoBehaviour
{
    public TextMeshProUGUI porcentajeTerreno1Text;
    public TextMeshProUGUI porcentajeTerreno2Text;
    public TextMeshProUGUI porcentajeTerreno3Text;

    [System.Serializable]
    private class CoberturaDTO
    {
        public float cobertura1, cobertura2, cobertura3;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void onUnitySendData(string json);
#endif

    // Llamado por el botón "Enviar Resultado"
    public void EnviarDatosSimulacion() => StartCoroutine(EnviarAlFinalDelFrame());

    private IEnumerator EnviarAlFinalDelFrame()
    {
        
        yield return new WaitForEndOfFrame();

        float c1 = ExtraerPorcentaje(porcentajeTerreno1Text ? porcentajeTerreno1Text.text : null);
        float c2 = ExtraerPorcentaje(porcentajeTerreno2Text ? porcentajeTerreno2Text.text : null);
        float c3 = ExtraerPorcentaje(porcentajeTerreno3Text ? porcentajeTerreno3Text.text : null);

        var dto = new CoberturaDTO { cobertura1 = c1, cobertura2 = c2, cobertura3 = c3 };
        string json = JsonUtility.ToJson(dto);

#if UNITY_WEBGL && !UNITY_EDITOR
        onUnitySendData(json);
#else
        Debug.Log("[UNITY->JS] " + json);
#endif
    }

    private static float ExtraerPorcentaje(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return 0f;
        int idx = texto.IndexOf(':');
        string sub = (idx >= 0 ? texto.Substring(idx + 1) : texto).Replace("%", "").Trim();
        sub = sub.Replace(',', '.'); // soporta coma decimal
        return float.TryParse(sub, System.Globalization.NumberStyles.Float,
                              System.Globalization.CultureInfo.InvariantCulture, out var v)
               ? v : 0f;
    }
}
