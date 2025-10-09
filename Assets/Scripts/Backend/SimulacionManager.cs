using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

[Serializable]
public class TerrenoDTO
{
    public int heightWidth;
    public int heightHeight;
    public float[] heights;

    public int alphaWidth;
    public int alphaHeight;
    public int alphaLayers;
    public float[] alphamaps;
}

[Serializable]
public class EscenaDTO
{
    public float c1, c2, c3;
    public TerrenoDTO[] terrenos;
}

public class SimulacionManager : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void onUnitySendData(string json);
#endif

    [Header("Terrenos (en orden)")]
    [SerializeField] private Terrain[] terrenos;

    [Header("Trackers (0..1 → porcentaje *100)")]
    [SerializeField] private CampoTracker CampoTrackerA;
    [SerializeField] private CampoTracker CampoTrackerB;
    [SerializeField] private CampoTracker CampoTrackerC;

    [Header("labels TMP (fallback si no hay trackers)")]
    [SerializeField] private TextMeshProUGUI porcentajeTerreno1Text;
    [SerializeField] private TextMeshProUGUI porcentajeTerreno2Text;
    [SerializeField] private TextMeshProUGUI porcentajeTerreno3Text;

    private void Awake()
    {
        if (terrenos == null || terrenos.Length == 0)
        {
            terrenos = GameObject.FindObjectsOfType<Terrain>()
                                 .OrderBy(t => t.transform.GetSiblingIndex())
                                 .ToArray();
        }
    }
    // JS → SendMessage("SimulacionManager", "LoadSceneState", json)
    public void LoadSceneState(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("[SimMgr] JSON vacío, nada que restaurar.");
            return;
        }

        EscenaDTO dto;
        try { dto = JsonUtility.FromJson<EscenaDTO>(json); }
        catch (Exception e) { Debug.LogError("[SimMgr] Error deserializando JSON: " + e); return; }

        if (dto?.terrenos == null || dto.terrenos.Length == 0)
        {
            Debug.LogWarning("[SimMgr] JSON sin 'terrenos'.");
            return;
        }

        int count = Mathf.Min(terrenos.Length, dto.terrenos.Length);
        for (int i = 0; i < count; i++)
        {
            var terrain = terrenos[i]; if (!terrain) continue;
            var td = terrain.terrainData; if (!td) continue;
            var t = dto.terrenos[i];

            // ===== ALTURAS =====
            if (t.heights != null && t.heights.Length == t.heightWidth * t.heightHeight)
            {
                if (td.heightmapResolution != t.heightWidth || t.heightWidth != t.heightHeight)
                    td.heightmapResolution = t.heightWidth; // 513

                int w = t.heightWidth, h = t.heightHeight;
                float[,] h2 = new float[h, w];
                int idx = 0;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        h2[y, x] = Safe01(t.heights[idx++]);

                td.SetHeights(0, 0, h2);
                td.SyncHeightmap(); // ✅ reemplaza método obsoleto
            }
            else Debug.LogWarning($"[SimMgr] Terreno {i}: heights inválidos o tamaño no coincide.");

            // ===== ALPHAMAPS =====
            if (t.alphamaps != null && t.alphamaps.Length == t.alphaWidth * t.alphaHeight * t.alphaLayers)
            {
                if (td.alphamapResolution != t.alphaWidth)
                    td.alphamapResolution = t.alphaWidth; // 512

                int aw = t.alphaWidth, ah = t.alphaHeight, al = t.alphaLayers;
                float[,,] a3 = new float[ah, aw, al];
                int idx = 0;
                for (int y = 0; y < ah; y++)
                    for (int x = 0; x < aw; x++)
                        for (int l = 0; l < al; l++)
                            a3[y, x, l] = Safe01(t.alphamaps[idx++]);

                td.SetAlphamaps(0, 0, a3);
            }
            else Debug.LogWarning($"[SimMgr] Terreno {i}: alphamaps inválidos o tamaño no coincide.");
        }

        Debug.Log("[SimMgr] Simulación restaurada correctamente.");
    }
    public void EnviarDatosSimulacion()
    {
        StartCoroutine(EnviarCuandoListo(1.0f));
    }

    private IEnumerator EnviarCuandoListo(float timeoutSeg)
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        float deadline = Time.time + timeoutSeg;

        while (Time.time < deadline)
        {
            if (TryLeerCoberturas(out float p1, out float p2, out float p3))
            {
                bool hayDato = (p1 > 0.001f) || (p2 > 0.001f) || (p3 > 0.001f);
                if (hayDato)
                {
                    EnviarJson(p1, p2, p3);
                    yield break;
                }
            }
            yield return null;
        }

        // Timeout: enviar lo que haya
        TryLeerCoberturas(out float f1, out float f2, out float f3);
        EnviarJson(f1, f2, f3);
    }

    private void EnviarJson(float p1, float p2, float p3)
    {
        p1 = Mathf.Clamp(p1, 0f, 100f);
        p2 = Mathf.Clamp(p2, 0f, 100f);
        p3 = Mathf.Clamp(p3, 0f, 100f);

        var dto = new EscenaDTO { c1 = p1, c2 = p2, c3 = p3 };

        // Recolectar todos los terrenos
        var lista = new System.Collections.Generic.List<TerrenoDTO>();
        foreach (var t in terrenos)
        {
            if (!t) continue;
            var terrenoDto = new TerrenoDTO();
            var data = t.terrainData;

            // === Heightmap ===
            int w = data.heightmapResolution;
            int h = data.heightmapResolution;
            float[,] heights = data.GetHeights(0, 0, w, h);
            terrenoDto.heightWidth = w;
            terrenoDto.heightHeight = h;
            terrenoDto.heights = Flatten(heights);

            // === Alphamap ===
            int aw = data.alphamapWidth;
            int ah = data.alphamapHeight;
            int al = data.alphamapLayers;
            float[,,] alphas = data.GetAlphamaps(0, 0, aw, ah);
            terrenoDto.alphaWidth = aw;
            terrenoDto.alphaHeight = ah;
            terrenoDto.alphaLayers = al;
            terrenoDto.alphamaps = Flatten(alphas);

            lista.Add(terrenoDto);
        }
        dto.terrenos = lista.ToArray();

        string json = JsonUtility.ToJson(dto);

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            onUnitySendData(json);
            Debug.Log($"[SimMgr] Datos enviados a JS: {json.Substring(0, Mathf.Min(200, json.Length))}...");
        }
        catch (Exception e)
        {
            Debug.LogError("[SimMgr] Error enviando datos a JS: " + e);
        }
#else
        Debug.Log("[SimMgr] (Editor) JSON generado: " + json);
#endif
    }

    private bool TryLeerCoberturas(out float p1, out float p2, out float p3)
    {
        // Trackers (prioritario)
        if (CampoTrackerA && CampoTrackerB && CampoTrackerC)
        {
            p1 = Mathf.Clamp01(CampoTrackerA.progresoActual) * 100f;
            p2 = Mathf.Clamp01(CampoTrackerB.progresoActual) * 100f;
            p3 = Mathf.Clamp01(CampoTrackerC.progresoActual) * 100f;
            return true;
        }

        // Fallback TMP labels
        bool ok1 = TryReadFromLabel(porcentajeTerreno1Text, out p1);
        bool ok2 = TryReadFromLabel(porcentajeTerreno2Text, out p2);
        bool ok3 = TryReadFromLabel(porcentajeTerreno3Text, out p3);
        return ok1 || ok2 || ok3;
    }

    private bool TryReadFromLabel(TextMeshProUGUI lbl, out float pct)
    {
        pct = 0f;
        if (!lbl) return false;
        string s = lbl.text ?? "";
        int i = s.IndexOf(':');
        string sub = (i >= 0 ? s[(i + 1)..] : s);
        sub = sub.Replace("%", "").Trim().Replace(',', '.');

        if (!float.TryParse(sub, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v))
            return false;

        pct = Mathf.Clamp(v, 0f, 100f);
        return true;
    }

    private static float Safe01(float v)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return 0f;
        return Mathf.Clamp01(v);
    }

    private float[] Flatten(float[,] input)
    {
        int w = input.GetLength(0);
        int h = input.GetLength(1);
        float[] flat = new float[w * h];
        int k = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                flat[k++] = input[x, y];
        return flat;
    }

    private float[] Flatten(float[,,] input)
    {
        int w = input.GetLength(0);
        int h = input.GetLength(1);
        int l = input.GetLength(2);
        float[] flat = new float[w * h * l];
        int k = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                for (int z = 0; z < l; z++)
                    flat[k++] = input[x, y, z];
        return flat;
    }
}

//using System;
//using System.Collections;
//using System.Linq;
//using System.Runtime.InteropServices;
//using TMPro;
//using UnityEngine;

//[Serializable]
//public class TerrenoDTO
//{
//    public int heightWidth;   // 513
//    public int heightHeight;  // 513
//    public float[] heights;   // 513*513

//    public int alphaWidth;    // 512
//    public int alphaHeight;   // 512
//    public int alphaLayers;   // 2
//    public float[] alphamaps; // 512*512*2
//}

//[Serializable]
//public class EscenaDTO
//{
//    public float c1, c2, c3;
//    public TerrenoDTO[] terrenos;
//}

//public class SimulacionManager : MonoBehaviour
//{
//#if UNITY_WEBGL && !UNITY_EDITOR
//    // 🔗 Llama al método JS del archivo onUnitySendData.jslib
//    [DllImport("__Internal")]
//    private static extern void onUnitySendData(string json);
//#endif

//    [Header("Terrenos (en orden)")]
//    [SerializeField] private Terrain[] terrenos;

//    [Header("Trackers (0..1) → porcentaje *100")]
//    [SerializeField] private CampoTracker CampoTrackerA;
//    [SerializeField] private CampoTracker CampoTrackerB;
//    [SerializeField] private CampoTracker CampoTrackerC;

//    [Header("labels tmp (fallback si no hay trackers)")]
//    [SerializeField] private TextMeshProUGUI porcentajeTerreno1Text;
//    [SerializeField] private TextMeshProUGUI porcentajeTerreno2Text;
//    [SerializeField] private TextMeshProUGUI porcentajeTerreno3Text;

//    private void Awake()
//    {
//        if (terrenos == null || terrenos.Length == 0)
//            terrenos = GameObject.FindObjectsOfType<Terrain>()
//                                  .OrderBy(t => t.transform.GetSiblingIndex())
//                                  .ToArray();
//    }

//    // JS → SendMessage("SimulacionManager", "LoadSceneState", json)
//    public void LoadSceneState(string json)
//    {
//        if (string.IsNullOrWhiteSpace(json))
//        {
//            Debug.LogWarning("[SimMgr] JSON vacío, nada que restaurar.");
//            return;
//        }

//        EscenaDTO dto;
//        try { dto = JsonUtility.FromJson<EscenaDTO>(json); }
//        catch (Exception e) { Debug.LogError("[SimMgr] Error deserializando JSON: " + e); return; }

//        if (dto?.terrenos == null || dto.terrenos.Length == 0)
//        {
//            Debug.LogWarning("[SimMgr] JSON sin 'terrenos'.");
//            return;
//        }

//        int count = Mathf.Min(terrenos.Length, dto.terrenos.Length);
//        for (int i = 0; i < count; i++)
//        {
//            var terrain = terrenos[i]; if (!terrain) continue;
//            var td = terrain.terrainData; if (!td) continue;
//            var t = dto.terrenos[i];

//            // ===== ALTURAS =====
//            if (t.heights != null && t.heights.Length == t.heightWidth * t.heightHeight)
//            {
//                if (td.heightmapResolution != t.heightWidth || t.heightWidth != t.heightHeight)
//                    td.heightmapResolution = t.heightWidth; // 513

//                int w = t.heightWidth, h = t.heightHeight;
//                float[,] h2 = new float[h, w];
//                int idx = 0;
//                for (int y = 0; y < h; y++)
//                    for (int x = 0; x < w; x++)
//                        h2[y, x] = Safe01(t.heights[idx++]);

//                td.SetHeights(0, 0, h2);
//                td.SyncHeightmap(); // ✅ reemplaza método obsoleto
//            }
//            else Debug.LogWarning($"[SimMgr] Terreno {i}: heights inválidos o tamaño no coincide.");

//            // ===== ALPHAMAPS =====
//            if (t.alphamaps != null && t.alphamaps.Length == t.alphaWidth * t.alphaHeight * t.alphaLayers)
//            {
//                if (td.alphamapResolution != t.alphaWidth)
//                    td.alphamapResolution = t.alphaWidth; // 512

//                int aw = t.alphaWidth, ah = t.alphaHeight, al = t.alphaLayers;
//                float[,,] a3 = new float[ah, aw, al];
//                int idx = 0;
//                for (int y = 0; y < ah; y++)
//                    for (int x = 0; x < aw; x++)
//                        for (int l = 0; l < al; l++)
//                            a3[y, x, l] = Safe01(t.alphamaps[idx++]);

//                td.SetAlphamaps(0, 0, a3);
//            }
//            else Debug.LogWarning($"[SimMgr] Terreno {i}: alphamaps inválidos o tamaño no coincide.");
//        }

//        Debug.Log("[SimMgr] Simulación restaurada correctamente.");
//    }

//    private static float Safe01(float v)
//    {
//        if (float.IsNaN(v) || float.IsInfinity(v)) return 0f;
//        return Mathf.Clamp01(v);
//    }

//    // Si querés que Unity envíe datos a JS, implementá y llamá:
//    public void EnviarDatosSimulacion()
//    {
//        StartCoroutine (EnviarCuandoListo(1.5f));
//        // Ejemplo: mandar coberturas/estado a window.onUnitySendData
//        // (Implementación opcional)
//    }
//    private IEnumerator EnviarCuandoListo(float timeoutSeg)
//    {
//        // Dejá correr al menos 2 ticks para que Update/LateUpdate de tus trackers actualicen
//        yield return null;
//        yield return new WaitForEndOfFrame();

//        float deadline = Time.time + timeoutSeg;

//        while (Time.time < deadline)
//        {
//            if (TryLeerCoberturas(out float p1, out float p2, out float p3))
//            {
//                bool hayDato = (p1 > 0.0001f) || (p2 > 0.0001f) || (p3 > 0.0001f);
//                if (hayDato) { EnviarJson(p1, p2, p3); yield break; }
//            }
//            // todavía están en 0 ? esperamos próximo frame
//            yield return null;
//        }

//        // Timeout: enviar lo que haya (aunque sea 0)
//        TryLeerCoberturas(out float f1, out float f2, out float f3);

//        EnviarJson(f1, f2, f3);
//    }
//    private void EnviarJson(float p1, float p2, float p3)
//    {
//        p1 = Mathf.Clamp(p1, 0f, 100f);
//        p2 = Mathf.Clamp(p2, 0f, 100f);
//        p3 = Mathf.Clamp(p3, 0f, 100f);

//        var dto = new EscenaDTO { c1 = p1, c2 = p2, c3 = p3 };

//        // Capturo los datos de cada terreno
//        foreach (var t in terrenos)
//        {
//            var terrenoDto = new TerrenoDTO();
//            var data = t.terrainData;

//            // === Heightmap ===
//            int w = data.heightmapResolution;
//            int h = data.heightmapResolution;
//            float[,] heights = data.GetHeights(0, 0, w, h);
//            terrenoDto.heightWidth = w;
//            terrenoDto.heightHeight = h;
//            terrenoDto.heights = Flatten(heights);

//            // === Splatmap ===
//            int aw = data.alphamapWidth;
//            int ah = data.alphamapHeight;
//            int al = data.alphamapLayers;
//            float[,,] alphas = data.GetAlphamaps(0, 0, aw, ah);
//            terrenoDto.alphaWidth = aw;
//            terrenoDto.alphaHeight = ah;
//            terrenoDto.alphaLayers = al;
//            terrenoDto.alphamaps = Flatten(alphas);

//            var listaTerrenos = dto.terrenos?.ToList() ?? new System.Collections.Generic.List<TerrenoDTO>();
//            listaTerrenos.Add(terrenoDto);
//            dto.terrenos = listaTerrenos.ToArray();
//        }

//        string json = JsonUtility.ToJson(dto);

//#if UNITY_WEBGL && !UNITY_EDITOR
//         try { onUnitySendData(json); }
//        catch (Exception e) { Debug.LogError("[SimMgr] Error enviando datos a JS: " + e); }
//#else
//        Debug.Log("[SimMgr] Datos simulación → " + json);
//#endif
//    }

//    private bool TryLeerCoberturas(out float p1, out float p2, out float p3)
//    {
//        // 1) Intentar trackers (0..1 ? *100)
//        if (CampoTrackerA && CampoTrackerB && CampoTrackerC)
//        {
//            p1 = Mathf.Clamp01(CampoTrackerA.progresoActual) * 100f;
//            p2 = Mathf.Clamp01(CampoTrackerB.progresoActual) * 100f;
//            p3 = Mathf.Clamp01(CampoTrackerC.progresoActual) * 100f;
//            // Debug.Log($"[SimMgr] Trackers: {p1:0.##} / {p2:0.##} / {p3:0.##}");
//            return true;
//        }

//        // 2) Fallback: leer labels TMP
//        bool ok1 = TryReadFromLabel(porcentajeTerreno1Text, out p1);
//        bool ok2 = TryReadFromLabel(porcentajeTerreno2Text, out p2);
//        bool ok3 = TryReadFromLabel(porcentajeTerreno3Text, out p3);
//        // Debug.Log($"[SimMgr] Labels: {p1:0.##} / {p2:0.##} / {p3:0.##}");
//        return ok1 || ok2 || ok3;
//    }

//    private bool TryReadFromLabel(TextMeshProUGUI lbl, out float pct)
//    {
//        pct = 0f;
//        if (!lbl) return false;

//        string s = lbl.text ?? "";
//        int i = s.IndexOf(':');
//        string sub = (i >= 0 ? s.Substring(i + 1) : s);
//        sub = sub.Replace("%", "").Trim().Replace(',', '.');

//        if (!float.TryParse(sub,
//                System.Globalization.NumberStyles.Float,
//                System.Globalization.CultureInfo.InvariantCulture,
//                out var v)) return false;

//        pct = Mathf.Clamp(v, 0f, 100f);
//        return true;
//    }



//    // ====== Helpers para convertir arrays 2D/3D a 1D (JsonUtility no soporta multidimensionales) ======
//        private float[] Flatten(float[,] input)
//        {
//            int w = input.GetLength(0);
//            int h = input.GetLength(1);
//            float[] flat = new float[w * h];
//            int k = 0;
//            for (int y = 0; y < h; y++)
//                for (int x = 0; x < w; x++)
//                    flat[k++] = input[x, y];
//            return flat;
//        }

//    private float[] Flatten(float[,,] input)
//    {
//        int w = input.GetLength(0);
//        int h = input.GetLength(1);
//        int l = input.GetLength(2);
//        float[] flat = new float[w * h * l];
//        int k = 0;
//        for (int y = 0; y < h; y++)
//            for (int x = 0; x < w; x++)
//                for (int z = 0; z < l; z++)
//                    flat[k++] = input[x, y, z];
//        return flat;
//    }
//}
//using System.Collections;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;
//using TMPro;
//using UnityEngine;

//public class SimulacionManager : MonoBehaviour
//{
//    // Asigná los 3 terrenos en el inspector
//    [Header("Terrenos a guardar")]
//    [SerializeField] private Terrain[] terrenos;

//    // ===== Opción A: leer directo desde tus trackers (0..1) =====
//    // Dejá vacíos estos 3 si preferís la Opción B (labels).
//    [Header("Trackers (0..1) ? porcentaje *100")]
//    [SerializeField] private CampoTracker CampoTrackerA;
//    [SerializeField] private CampoTracker CampoTrackerB;
//    [SerializeField] private CampoTracker CampoTrackerC;

//    // ===== Opción B: leer desde los textos TMP "Campo Arado: XX,YY%" =====
//    // Usado si los trackers están vacíos o no existen.
//    [Header("Labels TMP (fallback si no hay trackers)")]
//    [SerializeField] private TextMeshProUGUI porcentajeTerreno1Text;
//    [SerializeField] private TextMeshProUGUI porcentajeTerreno2Text;
//    [SerializeField] private TextMeshProUGUI porcentajeTerreno3Text;

//    [System.Serializable]
//    private class DatosSimulacion 
//    { 
//        public float cobertura1, cobertura2, cobertura3;
//        public List<DatosTerrenoDto> terrenos = new();
//    }

//    [System.Serializable]
//    private class DatosTerrenoDto
//    {
//        public float[] heights;      // Flattened heightmap
//        public int heightWidth;
//        public int heightHeight;

//        public float[] alphamaps;    // Flattened splatmap
//        public int alphaWidth;
//        public int alphaHeight;
//        public int alphaLayers;
//    }


//#if UNITY_WEBGL && !UNITY_EDITOR
//    [DllImport("__Internal")] private static extern void onUnitySendData(string json);
//#endif

//    // Llamado desde React: SendMessage("SimulacionManager","EnviarDatosSimulacion")
//    public void EnviarDatosSimulacion() => StartCoroutine(EnviarCuandoListo(1.5f));
//    public void LoadSceneState(string json)
//    {
//        Debug.Log("[SceneStateManager] Cargando estado desde JSON...");
//        StartCoroutine(RestaurarSimulacion(json));
//    }

//    private IEnumerator EnviarCuandoListo(float timeoutSeg)
//    {
//        // Dejá correr al menos 2 ticks para que Update/LateUpdate de tus trackers actualicen
//        yield return null;
//        yield return new WaitForEndOfFrame();

//        float deadline = Time.time + timeoutSeg;

//        while (Time.time < deadline)
//        {
//            if (TryLeerCoberturas(out float p1, out float p2, out float p3))
//            {
//                bool hayDato = (p1 > 0.0001f) || (p2 > 0.0001f) || (p3 > 0.0001f);
//                if (hayDato) { EnviarJson(p1, p2, p3); yield break; }
//            }
//            // todavía están en 0 ? esperamos próximo frame
//            yield return null;
//        }

//        // Timeout: enviar lo que haya (aunque sea 0)
//        TryLeerCoberturas(out float f1, out float f2, out float f3);

//        EnviarJson(f1, f2, f3);
//    }

//    private bool TryLeerCoberturas(out float p1, out float p2, out float p3)
//    {
//        // 1) Intentar trackers (0..1 ? *100)
//        if (TrackersAsignados())
//        {
//            p1 = Mathf.Clamp01(CampoTrackerA.progresoActual) * 100f;
//            p2 = Mathf.Clamp01(CampoTrackerB.progresoActual) * 100f;
//            p3 = Mathf.Clamp01(CampoTrackerC.progresoActual) * 100f;
//            // Debug.Log($"[SimMgr] Trackers: {p1:0.##} / {p2:0.##} / {p3:0.##}");
//            return true;
//        }

//        // 2) Fallback: leer labels TMP
//        bool ok1 = TryReadFromLabel(porcentajeTerreno1Text, out p1);
//        bool ok2 = TryReadFromLabel(porcentajeTerreno2Text, out p2);
//        bool ok3 = TryReadFromLabel(porcentajeTerreno3Text, out p3);
//        // Debug.Log($"[SimMgr] Labels: {p1:0.##} / {p2:0.##} / {p3:0.##}");
//        return ok1 || ok2 || ok3;
//    }

//    private bool TrackersAsignados()
//        => CampoTrackerA && CampoTrackerB && CampoTrackerC;

//    private bool TryReadFromLabel(TextMeshProUGUI lbl, out float pct)
//    {
//        pct = 0f;
//        if (!lbl) return false;

//        string s = lbl.text ?? "";
//        int i = s.IndexOf(':');
//        string sub = (i >= 0 ? s.Substring(i + 1) : s);
//        sub = sub.Replace("%", "").Trim().Replace(',', '.');

//        if (!float.TryParse(sub,
//                System.Globalization.NumberStyles.Float,
//                System.Globalization.CultureInfo.InvariantCulture,
//                out var v)) return false;

//        pct = Mathf.Clamp(v, 0f, 100f);
//        return true;
//    }

//    private void EnviarJson(float p1, float p2, float p3)
//    {
//        p1 = Mathf.Clamp(p1, 0f, 100f);
//        p2 = Mathf.Clamp(p2, 0f, 100f);
//        p3 = Mathf.Clamp(p3, 0f, 100f);

//        var dto = new DatosSimulacion { cobertura1 = p1, cobertura2 = p2, cobertura3 = p3 };

//        //capturo los datos de cada terreno
//        foreach (var t in terrenos)
//        { 
//            var terrenoDto = new DatosTerrenoDto(); 
//            var data = t.terrainData;

//            // === Heightmap ===
//            int w = data.heightmapResolution;
//            int h = data.heightmapResolution;
//            float[,] heights = data.GetHeights(0, 0, w, h);
//            terrenoDto.heightWidth = w;
//            terrenoDto.heightHeight = h;
//            terrenoDto.heights = Flatten(heights);

//            // === Splatmap ===
//            int aw = data.alphamapWidth;
//            int ah = data.alphamapHeight;
//            int al = data.alphamapLayers;
//            float[,,] alphas = data.GetAlphamaps(0, 0, aw, ah);
//            terrenoDto.alphaWidth = aw;
//            terrenoDto.alphaHeight = ah;
//            terrenoDto.alphaLayers = al;
//            terrenoDto.alphamaps = Flatten(alphas);

//            dto.terrenos.Add(terrenoDto);
//        }

//        string json = JsonUtility.ToJson(dto);

//#if UNITY_WEBGL && !UNITY_EDITOR
//        onUnitySendData(json);
//#else
//        Debug.Log("[UNITY->JS] " + json);
//#endif
//    }

//    public IEnumerator RestaurarSimulacion(string json)
//    {
//        try
//        {
//            DatosSimulacion dto = JsonUtility.FromJson<DatosSimulacion>(json);
//            if (dto == null || dto.terrenos == null || dto.terrenos.Count != terrenos.Length)
//            {
//                Debug.LogWarning("[SimMgr] Datos inválidos o cantidad de terrenos no coincide.");
//                //esto lo puse por el ienumerator
//                yield break;
//            }
//            for (int i = 0; i < terrenos.Length && i < dto.terrenos.Count; i++)
//            {
//                Terrain t = terrenos[i];
//                var data = t.terrainData;
//                var tdto = dto.terrenos[i];

//                // === Heightmap ===
//                if (tdto.heights != null && tdto.heights.Length == tdto.heightWidth * tdto.heightHeight)
//                {
//                    float[,] heights = new float[tdto.heightWidth, tdto.heightHeight];
//                    int k = 0;
//                    for (int y = 0; y < tdto.heightHeight; y++)
//                        for (int x = 0; x < tdto.heightWidth; x++)
//                            heights[x, y] = tdto.heights[k++];
//                    data.SetHeights(0, 0, heights);
//                }
//                else
//                {
//                    Debug.LogWarning($"[SimMgr] Datos de heightmap inválidos para terreno {i}.");
//                }
//                // === Splatmap ===
//                if (tdto.alphamaps != null && tdto.alphamaps.Length == tdto.alphaWidth * tdto.alphaHeight * tdto.alphaLayers)
//                {
//                    float[,,] alphas = new float[tdto.alphaWidth, tdto.alphaHeight, tdto.alphaLayers];
//                    int k = 0;
//                    for (int y = 0; y < tdto.alphaHeight; y++)
//                        for (int x = 0; x < tdto.alphaWidth; x++)
//                            for (int z = 0; z < tdto.alphaLayers; z++)
//                                alphas[x, y, z] = tdto.alphamaps[k++];
//                    data.SetAlphamaps(0, 0, alphas);
//                }
//                else
//                {
//                    Debug.LogWarning($"[SimMgr] Datos de splatmap inválidos para terreno {i}.");
//                }
//                // Forzar actualización visual
//                t.Flush();
//            }
//            Debug.Log("[SimMgr] Simulación restaurada correctamente.");
//        }
//        catch (System.Exception ex)
//        {
//            Debug.LogError("[SimMgr] Error al restaurar: " + ex.Message);
//        }
//        //esto lo puse por el ienumerator
//        yield break;
//    }


//    // ====== Helpers para convertir arrays 2D/3D a 1D (JsonUtility no soporta multidimensionales) ======
//    private float[] Flatten(float[,] input)
//    {
//        int w = input.GetLength(0);
//        int h = input.GetLength(1);
//        float[] flat = new float[w * h];
//        int k = 0;
//        for (int y = 0; y < h; y++)
//            for (int x = 0; x < w; x++)
//                flat[k++] = input[x, y];
//        return flat;
//    }

//    private float[] Flatten(float[,,] input)
//    {
//        int w = input.GetLength(0);
//        int h = input.GetLength(1);
//        int l = input.GetLength(2);
//        float[] flat = new float[w * h * l];
//        int k = 0;
//        for (int y = 0; y < h; y++)
//            for (int x = 0; x < w; x++)
//                for (int z = 0; z < l; z++)
//                    flat[k++] = input[x, y, z];
//        return flat;
//    }
//}