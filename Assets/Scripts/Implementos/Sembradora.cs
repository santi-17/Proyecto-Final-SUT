using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Sembradora : MonoBehaviour
{
    public enum MisionSiembra { Mision1, Mision2, Mision3 }
    public MisionSiembra misionActual = MisionSiembra.Mision1;

    [Header("Control")]
    public KeyCode activarSembradora = KeyCode.H;

    [Header("Referencias")]
    [SerializeField] private Terrain terreno;
    [SerializeField] public Transform puntoRaycast;
    [SerializeField] private Transform modeloVisual;
    [SerializeField] public ParticleSystem particulasSembrado;
    [SerializeField] private GameObject prefabSemilla;
    [SerializeField] private TextMeshPro etiquetaTipoSembradora;
     private Vector3 offsetEtiqueta = new Vector3(0, 5f, 0);

    //private TerrainData data;
    private bool sembradoraActiva = false;
    private Vector3 ultimaPosicionSembrada;
    private float distanciaSemilla;
    private Coroutine movimientoSembradora;
    private Queue<GameObject> poolSemillas = new Queue<GameObject>();

    //private TMP_Text etiquetaTipoSembradora;

    [Header("Parámetros dinámicos")]
    public float distanciaDeteccion = 5f;
    private float profundidadSiembra;
    private float anchoSembradora;
    private float distanciaEntreSemillas;
    private string tipoSembradora;
    private string caudalSemilla;
    private string cultivoObjetivo;

    [Header("Animación")]
    private float alturaReposo = 0f;
    private float alturaTrabajo = -0.8f;
    private float velocidadMovimiento = 2f;

    [Header("UI")]
    [SerializeField] private GameObject cartelAdvertencia;
    [SerializeField] private TextMeshProUGUI textoAdvertencia;

    void Start()
    {
        ConfigurarMision();

        if (terreno == null)
        {
            terreno = FindObjectOfType<Terrain>();
        }

        if (terreno == null)
        {
            Debug.LogError("[Sembradora] No se encontró un Terrain en la escena.");
            enabled = false;
            return;
        }

        //data = terreno.terrainData;

        // Crear pool de semillas
        for (int i = 0; i < 200; i++)
        {
            GameObject semilla = Instantiate(prefabSemilla);
            semilla.SetActive(false);
            poolSemillas.Enqueue(semilla);
        }

        // Crear etiqueta visual
        if (etiquetaTipoSembradora == null)
        {

            GameObject textoObj = new GameObject("EtiquetaTipoSembradora");
            textoObj.transform.SetParent(transform);
            textoObj.transform.localPosition = offsetEtiqueta;

            etiquetaTipoSembradora = textoObj.AddComponent<TextMeshPro>();
            etiquetaTipoSembradora.alignment = TextAlignmentOptions.Center;
            etiquetaTipoSembradora.fontSize = 8f;
            
        etiquetaTipoSembradora.color = Color.white;
        }
        etiquetaTipoSembradora.text = $"{tipoSembradora}\nCaudal: {caudalSemilla}\nCultivo: {cultivoObjetivo}";
    }

    void Update()
    {
        if (Input.GetKeyDown(activarSembradora))
        {
            sembradoraActiva = !sembradoraActiva;

            if (movimientoSembradora != null)
                StopCoroutine(movimientoSembradora);

            float destinoY = sembradoraActiva ? alturaTrabajo : alturaReposo;
            movimientoSembradora = StartCoroutine(MoverSembradora(destinoY));

            if (sembradoraActiva)
            {
                ultimaPosicionSembrada = transform.position;
                if (particulasSembrado && !particulasSembrado.isPlaying)
                    particulasSembrado.Play();
            }
            else
            {
                if (particulasSembrado)
                    particulasSembrado.Stop();
            }
        }

        if (!sembradoraActiva) return;

        float distancia = Vector3.Distance(transform.position, ultimaPosicionSembrada);
        if (distancia >= distanciaEntreSemillas)
        {
            ProcesarSiembra();
            ultimaPosicionSembrada = transform.position;
        }
    }

    private void LateUpdate()
    {
        if (etiquetaTipoSembradora != null && Camera.main != null)
        {
            etiquetaTipoSembradora.transform.LookAt(Camera.main.transform);
            etiquetaTipoSembradora.transform.Rotate(0, 180f, 0);
        }
    }

    private void ConfigurarMision()
    {
        switch (misionActual)
        {
            case MisionSiembra.Mision1:
                tipoSembradora = "Grano Fino";
                caudalSemilla = "Medio";
                cultivoObjetivo = "Trigo";
                profundidadSiembra = 0.03f;
                distanciaEntreSemillas = 1.5f;
                anchoSembradora = 12f;
                break;

            case MisionSiembra.Mision2:
                tipoSembradora = "Grano Grueso";
                caudalSemilla = "Alto";
                cultivoObjetivo = "Soja";
                profundidadSiembra = 0.05f;
                distanciaEntreSemillas = 2.0f;
                anchoSembradora = 13f;
                break;

            case MisionSiembra.Mision3:
                tipoSembradora = "Grano Fino";
                caudalSemilla = "Medio";
                cultivoObjetivo = "Zanahoria";
                profundidadSiembra = 0.01f;
                distanciaEntreSemillas = 1.0f;
                anchoSembradora = 10f;
                break;
        }
    }

    private IEnumerator MoverSembradora(float destinoY)
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

    private void ProcesarSiembra()
    {
        if (!Physics.Raycast(puntoRaycast.position, Vector3.down, out RaycastHit hit, distanciaDeteccion))
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

        if (sembradoraActiva && (!string.Equals(info.tipoEsperado, tipoSembradora, StringComparison.OrdinalIgnoreCase) || !string.Equals(info.cultivoEsperado, cultivoObjetivo, StringComparison.OrdinalIgnoreCase)))
        {
            MostrarAdvertencia($"Atención: este terreno requiere una sembradora '{info.tipoEsperado}' para '{info.cultivoEsperado}', no '{tipoSembradora}' para '{cultivoObjetivo}'.\n¡Por favor cambia la herramienta!");
            return;
        }

        if (!hit.collider.TryGetComponent(out Terrain _))
            return;

        // Pintar capa sembrada
        TerrainData data = hitTerrain.terrainData;
        Vector3 pos = hit.point - terreno.transform.position;
        float sizeX = Mathf.Max(data.size.x, 0.0001f);
        float sizeZ = Mathf.Max(data.size.z, 0.0001f);
        int mapX = Mathf.RoundToInt((pos.x / sizeX) * data.alphamapWidth);
        int mapZ = Mathf.RoundToInt((pos.z / sizeZ) * data.alphamapHeight);

        int size = Mathf.RoundToInt((anchoSembradora / data.size.x) * data.alphamapWidth);
        int halfSize = size / 2;

        mapX = Mathf.Clamp(mapX - halfSize, 0, data.alphamapWidth - size);
        mapZ = Mathf.Clamp(mapZ - halfSize, 0, data.alphamapHeight - size);

        float[,,] mapa = data.GetAlphamaps(mapX, mapZ, size, size);
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                for (int l = 0; l < data.alphamapLayers; l++)
                    mapa[z, x, l] = 0;

                mapa[z, x, 1] = 1; // Capa 1 = sembrada
            }
        }
        data.SetAlphamaps(mapX, mapZ, mapa);

        // Spawn de semillas visuales
        GameObject semilla = ObtenerSemilla();
        semilla.transform.position = hit.point + Vector3.up * 0.05f;
        semilla.SetActive(true);

        // Pequeña deformación visual
        StartCoroutine(CubrirSemilla(hitTerrain ,hit.point, 0.3f, profundidadSiembra / data.size.y));
    }

    GameObject ObtenerSemilla()
    {
        if (poolSemillas.Count > 0)
        {
            GameObject s = poolSemillas.Dequeue();
            poolSemillas.Enqueue(s);
            return s;
        }
        return Instantiate(prefabSemilla);
    }

    IEnumerator CubrirSemilla(Terrain hitTerrain,Vector3 posicion, float radio, float altura)
    {
        yield return new WaitForSeconds(0.3f);
        if (hitTerrain == null)
            yield break;
        TerrainData data = hitTerrain.terrainData;
        float sizeX = Mathf.Max(data.size.x, 0.0001f);
        float sizeZ = Mathf.Max(data.size.z, 0.0001f);
        Vector3 terrainPosition = posicion - terreno.transform.position;
        int mapX = Mathf.RoundToInt((terrainPosition.x / sizeX) * data.heightmapResolution);
        int mapZ = Mathf.RoundToInt((terrainPosition.z / sizeZ) * data.heightmapResolution);
        int r = Mathf.Max(1, Mathf.RoundToInt(radio * data.heightmapResolution / sizeX));

        int startX = Mathf.Clamp(mapX - r / 2, 0, data.heightmapResolution - r);
        int startZ = Mathf.Clamp(mapZ - r / 2, 0, data.heightmapResolution - r);

        float[,] alturas = data.GetHeights(startX, startZ, r, r);
        float divisor = Mathf.Max(r / 2f, 0.0001f);

        for (int x = 0; x < r; x++)
        {
            for (int z = 0; z < r; z++)
            {
                float dist = Vector2.Distance(new Vector2(x, z), new Vector2(divisor, divisor));

                float falloff = Mathf.Clamp01(1f - dist / divisor);
                alturas[z, x] += altura * falloff;
            }
        }
        data.SetHeights(startX, startZ, alturas);
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
