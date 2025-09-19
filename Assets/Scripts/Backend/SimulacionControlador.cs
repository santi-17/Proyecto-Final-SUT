using System.Collections;
using TMPro;
using UnityEngine;

// NOTA: si más adelante agregás la .jslib, descomentá la línea de abajo
// #define HAS_JS_BRIDGE   // <- activa el puente cuando exista OnUnitySendData en una .jslib

public class ControladorSimulacion : MonoBehaviour
{
    public TextMeshProUGUI porcentajeTerreno1Text;
    public TextMeshProUGUI porcentajeTerreno2Text;
    public TextMeshProUGUI porcentajeTerreno3Text;

#if UNITY_WEBGL && !UNITY_EDITOR && HAS_JS_BRIDGE
    // Solo si TENÉS la .jslib con una función OnUnitySendData
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void OnUnitySendData(string json);
#endif

    [System.Serializable]
    private class CoberturaDTO { public float cobertura1, cobertura2, cobertura3; }

    public void EnviarDatosSimulacion()
    {
        StartCoroutine(EnviarAlFinalDelFrame());
    }

    private IEnumerator EnviarAlFinalDelFrame()
    {
        yield return new WaitForEndOfFrame();

        float c1 = ExtraerPorcentaje(porcentajeTerreno1Text ? porcentajeTerreno1Text.text : null);
        float c2 = ExtraerPorcentaje(porcentajeTerreno2Text ? porcentajeTerreno2Text.text : null);
        float c3 = ExtraerPorcentaje(porcentajeTerreno3Text ? porcentajeTerreno3Text.text : null);

        c1 = Mathf.Clamp(c1, 0f, 100f);
        c2 = Mathf.Clamp(c2, 0f, 100f);
        c3 = Mathf.Clamp(c3, 0f, 100f);

        string json = JsonUtility.ToJson(new CoberturaDTO { cobertura1 = c1, cobertura2 = c2, cobertura3 = c3 });

        SendToBrowser(json); // llamada  
    }

    // —— Wrapper: si no existe la .jslib, solo hace Debug.Log y el juego compila/carga ——
    private static void SendToBrowser(string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR && HAS_JS_BRIDGE
        OnUnitySendData(json);
#else
        Debug.Log($"[UNITY->JS] {json}");
#endif
    }

    private float ExtraerPorcentaje(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return 0f;
        int idx = texto.IndexOf(':');
        string sub = (idx >= 0 ? texto[(idx + 1)..] : texto).Replace("%", "").Trim();
        string normalizado = sub.Replace(',', '.');
        return float.TryParse(normalizado, System.Globalization.NumberStyles.Float,
                              System.Globalization.CultureInfo.InvariantCulture, out float v)
               ? v : 0f;
    }
}
