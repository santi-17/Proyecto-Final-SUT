using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class AradoDisco : MonoBehaviour
{
    public enum MisionDisquera { Mision1, Mision2, Mision3 }
    public MisionDisquera misionActual = MisionDisquera.Mision1;

    [Header("Control")]
    public KeyCode teclaActivar = KeyCode.G;
    private bool disqueraActiva = false;

    [Header("Referencias")]
    [SerializeField] private Transform puntoRaycast;
    [SerializeField] private Terrain terreno;
    [SerializeField] private ParticleSystem particulasTierra;
    [SerializeField] private Transform modeloVisual;

    private ParticleSystem instanciaParticulas;
    private TextMeshPro etiquetaTipoDisquera;

    [Header("Parámetros Generales")]
    public float distanciaDeteccion = 5f;
    public int materialIndex = 1; // debe haber 2 capas en el Terrain
    public float tiempoEntreDisqueos = 0.1f;
    private float temporizador = 0f;

    private float profundidadSurco;
    private int size;
    private string tipoDisco;
    private string cultivoObjetivo;

    private float alturaReposo = 0f;
    private float alturaTrabajo = -1f;
    private float velocidadMovimiento = 2f;

    private Coroutine movimientoDisquera;
    private Vector3 ultimaPosicion;
    private float velocidadMovimientoTractor;

    [Header("Etiqueta visual")]
    [SerializeField] private Vector3 offsetEtiqueta = new Vector3(0, 3.5f, 0);

    [Header("UI")]
    [SerializeField] private GameObject cartelAdvertencia;
    [SerializeField] private TextMeshProUGUI textoAdvertencia;

    void Start()
    {
        ConfigurarMision();

        if (particulasTierra != null)
        {
            instanciaParticulas = Instantiate(particulasTierra, transform);
            instanciaParticulas.Stop();
        }


        if (etiquetaTipoDisquera == null)
        {
            GameObject textoObj = new GameObject("EtiquetaTipoArado");
            textoObj.transform.SetParent(transform);
            textoObj.transform.localPosition = offsetEtiqueta;

            etiquetaTipoDisquera = textoObj.AddComponent<TextMeshPro>();
            etiquetaTipoDisquera.alignment = TextAlignmentOptions.Center;
            etiquetaTipoDisquera.fontSize = 8f;
            etiquetaTipoDisquera.color = Color.white;
        }

        etiquetaTipoDisquera.text = $"Disco: {tipoDisco}\nCultivo: {cultivoObjetivo}"; ;
    }

    void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        velocidadMovimientoTractor = (transform.position - ultimaPosicion).magnitude / dt;
        ultimaPosicion = transform.position;

        if (Input.GetKeyDown(teclaActivar))
        {
            disqueraActiva = !disqueraActiva;
            if (movimientoDisquera != null) StopCoroutine(movimientoDisquera);
            float destinoY = disqueraActiva ? alturaTrabajo : alturaReposo;
            movimientoDisquera = StartCoroutine(MoverDisquera(destinoY));
        }

        if (!disqueraActiva || velocidadMovimientoTractor < 0.1f) return;

        if (instanciaParticulas != null && !instanciaParticulas.isPlaying)
        {
            instanciaParticulas.transform.position = puntoRaycast.position;
            instanciaParticulas.Play();
        }

        temporizador -= Time.deltaTime;
        if (temporizador <= 0f)
        {
            ProcesarTerreno();
            temporizador = tiempoEntreDisqueos;
        }
    }

    private void LateUpdate()
    {
        if (etiquetaTipoDisquera != null && Camera.main != null)
        {
            etiquetaTipoDisquera.transform.LookAt(Camera.main.transform);
            etiquetaTipoDisquera.transform.Rotate(0, 180f, 0);
        }
    }

    private void ConfigurarMision()
    {
        switch (misionActual)
        {
            case MisionDisquera.Mision1:
                tipoDisco = "Dentado";
                cultivoObjetivo = "Soja";
                profundidadSurco = 0.30f;
                size = 80;
                break;
            case MisionDisquera.Mision2:
                tipoDisco = "Ondulado";
                cultivoObjetivo = "Zanahoria";
                profundidadSurco = 0.20f;
                size = 80;
                break;
            case MisionDisquera.Mision3:
                tipoDisco = "Pesado";
                cultivoObjetivo = "Pastura";
                profundidadSurco = 0.35f;
                size = 80;
                break;
        }
    }

    private IEnumerator MoverDisquera(float destinoY)
    {
        Vector3 inicio = modeloVisual.localPosition;
        Vector3 destino = new Vector3(inicio.x, destinoY, inicio.z);
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * velocidadMovimiento;
            modeloVisual.localPosition = Vector3.Lerp(inicio, destino, t);
            yield return null;
        }
    }

    private void ProcesarTerreno()
    {
        if (terreno == null) return;
        if(!Physics.Raycast(puntoRaycast.position, Vector3.down, out RaycastHit hit, distanciaDeteccion))
            return;

        if (hit.collider == null)
        {
            Debug.LogWarning("[Disquera] El Raycast no golpeó nada.");
            return;
        }

        Terrain hitTerrain = hit.collider.GetComponent<Terrain>();
        if (hitTerrain == null) return;

        // No limitar al terrain asignado, detectar cualquier terrain
        TerrainInfo info = hitTerrain.GetComponent<TerrainInfo>();
        if (info == null) return;

        if (disqueraActiva && !string.Equals(info.tipoEsperado, tipoDisco, StringComparison.OrdinalIgnoreCase))
        {
            MostrarAdvertencia($"Atención: este terreno requiere una disquera tipo '{info.tipoEsperado}', no '{tipoDisco}'. " + System.Environment.NewLine + "¡Por Favor cambia la herramienta!");

            return;
        }
        TerrainData data = terreno.terrainData;
        Vector3 pos = hit.point - terreno.transform.position;

        // --- TEXTURA ---
        if (data.alphamapLayers >= 2)
        {
            int mapX = Mathf.RoundToInt((pos.x / data.size.x) * data.alphamapWidth);
            int mapZ = Mathf.RoundToInt((pos.z / data.size.z) * data.alphamapHeight);

            int startX = Mathf.Clamp(mapX - size / 2, 0, data.alphamapWidth - 1);
            int startZ = Mathf.Clamp(mapZ - size / 2, 0, data.alphamapHeight - 1);

            int widthA = Mathf.Min(size, data.alphamapWidth - startX);
            int heightA = Mathf.Min(size, data.alphamapHeight - startZ);

            float[,,] mapa = data.GetAlphamaps(startX, startZ, widthA, heightA);

            for (int x = 0; x < widthA; x++)
            {
                for (int z = 0; z < heightA; z++)
                {
                    for (int l = 0; l < data.alphamapLayers; l++)
                        mapa[z, x, l] = 0; 

                    mapa[z, x, materialIndex] = 1;
                }
            }
            data.SetAlphamaps(startX, startZ, mapa);
        }

        // --- DEFORMACIÓN ---
        int hx = Mathf.RoundToInt((pos.x / data.size.x) * data.heightmapResolution);
        int hz = Mathf.RoundToInt((pos.z / data.size.z) * data.heightmapResolution);

        int startHX = Mathf.Clamp(hx - size / 2, 0, data.heightmapResolution - 1);
        int startHZ = Mathf.Clamp(hz - size / 2, 0, data.heightmapResolution - 1);

        int widthH = Mathf.Min(size, data.heightmapResolution - startHX);
        int heightH = Mathf.Min(size, data.heightmapResolution - startHZ);

        float[,] heights = data.GetHeights(startHX, startHZ, widthH, heightH);
        float profNorm = profundidadSurco / Mathf.Max(data.size.y, 0.0001f); // conversión a normalizado

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                float distanciaCentro = Vector2.Distance(new Vector2(x, z), new Vector2(widthH / 2f, heightH / 2f)); // Distancia al centro del área afectada
                float divisor = Mathf.Max(widthH / 2f, 0.0001f);
                float falloff = Mathf.Clamp01(1f - (distanciaCentro / divisor)); // Factor de caída basado en la distancia al centro

                float variacion = UnityEngine.Random.Range(0.9f, 1.1f);
                heights[z, x] = Mathf.Clamp01(heights[z, x] - profNorm * falloff * variacion);
            }
        }
        data.SetHeights(startHX, startHZ, heights);
        
    }

    private void MostrarAdvertencia(string mensaje)
    {
        if (cartelAdvertencia == null || textoAdvertencia == null)
        {
            Debug.LogError("[Arado] No se asignó el CartelAdvertencia o el TextoAdvertencia en el inspector.");
            return;
        }

        textoAdvertencia.text = mensaje;
        if (cartelAdvertencia != null)
            Debug.Log("[Arado] Se activó cartel de advertencia: " + mensaje);
        else
            Debug.LogWarning("[Arado] CartelAdvertencia no asignado en inspector.");

        cartelAdvertencia.SetActive(true);

        // Si ya hay una corrutina de ocultar en curso, la reiniciamos
        StopCoroutine(nameof(EsconderCartel));
        StartCoroutine(EsconderCartel(5f));
    }

    private IEnumerator EsconderCartel(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (cartelAdvertencia != null)
            cartelAdvertencia.SetActive(false);
    }
}
