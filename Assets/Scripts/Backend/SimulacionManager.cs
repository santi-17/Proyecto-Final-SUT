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
        //Inverti el h y el w  
        int h = input.GetLength(0);
        int w = input.GetLength(1);
        float[] flat = new float[h * w];
        int k = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                flat[k++] = input[y, x];
        return flat;
    }

    private float[] Flatten(float[,,] input)
    {
        //Inverti el h y el w 
        int h = input.GetLength(0);
        int w = input.GetLength(1);
        int l = input.GetLength(2);
        float[] flat = new float[h * w * l];
        int k = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                for (int z = 0; z < l; z++)
                    flat[k++] = input[y, x, z];
        return flat;
    }
}
