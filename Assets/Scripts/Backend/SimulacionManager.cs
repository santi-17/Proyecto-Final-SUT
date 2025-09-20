//using System.Collections;
//using System.Runtime.InteropServices;
//using TMPro;
//using UnityEngine;
//using UnityEngine.Networking;
//using UnityEngine.SceneManagement;
//using UnityEngine.UI;
//public class SimulacionManager : MonoBehaviour
//{
//    public TextMeshProUGUI porcentajeTerreno1Text;
//    public TextMeshProUGUI porcentajeTerreno2Text;
//    public TextMeshProUGUI porcentajeTerreno3Text;

//    // Clase para parsear respuesta API
//    [System.Serializable]
//    public class  DatosSimulacion
//    {
//        public float porcentajeCobertura1;
//        public float porcentajeCobertura2;
//        public float porcentajeCobertura3;
//    }


//#if UNITY_WEBGL && !UNITY_EDITOR
//    [DllImport("__Internal")]
//    private static extern void onUnitySendData(string json);
//#endif
//    public void EnviarDatosSimulacion()
//    {
//        StartCoroutine(EnviarDatosSimulacionCoroutine());
//    }
//    private IEnumerator EnviarDatosSimulacionCoroutine()
//    {
//        yield return new WaitForEndOfFrame();
//        Debug.Log("EnviarSimulacion llamado");

//        float c1 = ExtraerPorcentaje(porcentajeTerreno1Text.text);
//        float c2 = ExtraerPorcentaje(porcentajeTerreno2Text.text);
//        float c3 = ExtraerPorcentaje(porcentajeTerreno3Text.text);

//        var datos = new DatosSimulacion
//        {
//            porcentajeCobertura1 = c1,
//            porcentajeCobertura2 = c2,
//            porcentajeCobertura3 = c3
//        };

//        //StartCoroutine(PostSimulacion(dto));
//        string json = JsonUtility.ToJson(datos);

//        //Enviar a React Llamo a la funcion en React los datos
//        //Application.ExternalCall("onUnitySendData", json);
//        //onUnitySendData(json);
//#if UNITY_WEBGL && !UNITY_EDITOR
//            onUnitySendData(json);
//#else
//        Debug.Log("[UNITY->JS] " + json);
//#endif
//    }

//    private float ExtraerPorcentaje(string texto)
//    {
//        int idx = texto.IndexOf(':');
//        if (idx < 0) return 0f;
//        string sub = texto.Substring(idx + 1).Replace("%", "").Trim();
//        if (float.TryParse(sub, out float val))
//            return val;
//        return 0f;
//    }


//}


using System.Collections;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

public class SimulacionManager : MonoBehaviour
{
    // ===== Opción A: leer directo desde tus trackers (0..1) =====
    // Dejá vacíos estos 3 si preferís la Opción B (labels).
    [Header("Trackers (0..1) ? porcentaje *100")]
    [SerializeField] private CampoTracker CampoTrackerA;
    [SerializeField] private CampoTracker CampoTrackerB;
    [SerializeField] private CampoTracker CampoTrackerC;

    // ===== Opción B: leer desde los textos TMP "Campo Arado: XX,YY%" =====
    // Usado si los trackers están vacíos o no existen.
    [Header("Labels TMP (fallback si no hay trackers)")]
    [SerializeField] private TextMeshProUGUI porcentajeTerreno1Text;
    [SerializeField] private TextMeshProUGUI porcentajeTerreno2Text;
    [SerializeField] private TextMeshProUGUI porcentajeTerreno3Text;

    [System.Serializable]
    private class DatosSimulacion { public float cobertura1, cobertura2, cobertura3; }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void onUnitySendData(string json);
#endif

    // Llamado desde React: SendMessage("SimulacionManager","EnviarDatosSimulacion")
    public void EnviarDatosSimulacion() => StartCoroutine(EnviarCuandoListo(1.5f));

    private IEnumerator EnviarCuandoListo(float timeoutSeg)
    {
        // Dejá correr al menos 2 ticks para que Update/LateUpdate de tus trackers actualicen
        yield return null;
        yield return new WaitForEndOfFrame();

        float deadline = Time.time + timeoutSeg;

        while (Time.time < deadline)
        {
            if (TryLeerCoberturas(out float p1, out float p2, out float p3))
            {
                bool hayDato = (p1 > 0.0001f) || (p2 > 0.0001f) || (p3 > 0.0001f);
                if (hayDato) { EnviarJson(p1, p2, p3); yield break; }
            }
            // todavía están en 0 ? esperamos próximo frame
            yield return null;
        }

        // Timeout: enviar lo que haya (aunque sea 0)
        TryLeerCoberturas(out float f1, out float f2, out float f3);
        EnviarJson(f1, f2, f3);
    }

    private bool TryLeerCoberturas(out float p1, out float p2, out float p3)
    {
        // 1) Intentar trackers (0..1 ? *100)
        if (TrackersAsignados())
        {
            p1 = Mathf.Clamp01(CampoTrackerA.progresoActual) * 100f;
            p2 = Mathf.Clamp01(CampoTrackerB.progresoActual) * 100f;
            p3 = Mathf.Clamp01(CampoTrackerC.progresoActual) * 100f;
            // Debug.Log($"[SimMgr] Trackers: {p1:0.##} / {p2:0.##} / {p3:0.##}");
            return true;
        }

        // 2) Fallback: leer labels TMP
        bool ok1 = TryReadFromLabel(porcentajeTerreno1Text, out p1);
        bool ok2 = TryReadFromLabel(porcentajeTerreno2Text, out p2);
        bool ok3 = TryReadFromLabel(porcentajeTerreno3Text, out p3);
        // Debug.Log($"[SimMgr] Labels: {p1:0.##} / {p2:0.##} / {p3:0.##}");
        return ok1 || ok2 || ok3;
    }

    private bool TrackersAsignados()
        => CampoTrackerA && CampoTrackerB && CampoTrackerC;

    private bool TryReadFromLabel(TextMeshProUGUI lbl, out float pct)
    {
        pct = 0f;
        if (!lbl) return false;

        string s = lbl.text ?? "";
        int i = s.IndexOf(':');
        string sub = (i >= 0 ? s.Substring(i + 1) : s);
        sub = sub.Replace("%", "").Trim().Replace(',', '.');

        if (!float.TryParse(sub,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var v)) return false;

        pct = Mathf.Clamp(v, 0f, 100f);
        return true;
    }

    private void EnviarJson(float p1, float p2, float p3)
    {
        p1 = Mathf.Clamp(p1, 0f, 100f);
        p2 = Mathf.Clamp(p2, 0f, 100f);
        p3 = Mathf.Clamp(p3, 0f, 100f);

        var dto = new DatosSimulacion { cobertura1 = p1, cobertura2 = p2, cobertura3 = p3 };
        string json = JsonUtility.ToJson(dto);

#if UNITY_WEBGL && !UNITY_EDITOR
        onUnitySendData(json);
#else
        Debug.Log("[UNITY->JS] " + json);
#endif
    }
}