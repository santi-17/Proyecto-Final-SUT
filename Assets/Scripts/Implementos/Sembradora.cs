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
    [SerializeField] private Transform puntoRaycast;
    [SerializeField] private Transform modeloVisual;
    [SerializeField] private ParticleSystem particulasSembrado;
    [SerializeField] private GameObject prefabSemilla;
    [SerializeField] private Vector3 offsetEtiqueta = new Vector3(0, 3.5f, 0);

    private TerrainData data;
    private bool sembradoraActiva = false;
    private Vector3 ultimaPosicionSembrada;
    private float distanciaSemilla;
    private Coroutine movimientoSembradora;
    private Queue<GameObject> poolSemillas = new Queue<GameObject>();

    private TMP_Text etiquetaTipoSembradora;

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

        data = terreno.terrainData;

        // Crear pool de semillas
        for (int i = 0; i < 200; i++)
        {
            GameObject semilla = Instantiate(prefabSemilla);
            semilla.SetActive(false);
            poolSemillas.Enqueue(semilla);
        }

        // Crear etiqueta visual
        GameObject textoObj = new GameObject("EtiquetaTipoSembradora");
        textoObj.transform.SetParent(transform);
        textoObj.transform.localPosition = offsetEtiqueta;

        etiquetaTipoSembradora = textoObj.AddComponent<TextMeshPro>();
        etiquetaTipoSembradora.alignment = TextAlignmentOptions.Center;
        etiquetaTipoSembradora.fontSize = 8f;
        etiquetaTipoSembradora.color = Color.white;
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
        Terrain hitTerrain = hit.collider.GetComponent<Terrain>();
        if (hitTerrain == null || hitTerrain != terreno)
            return;
        if (!hit.collider.TryGetComponent(out Terrain _))
            return;

        // Pintar capa sembrada
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
        StartCoroutine(CubrirSemilla(hit.point, 0.3f, profundidadSiembra / data.size.y));
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

    IEnumerator CubrirSemilla(Vector3 posicion, float radio, float altura)
    {
        yield return new WaitForSeconds(0.3f);
        if (terreno == null || data == null)
            yield break;
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
}

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class Sembradora : MonoBehaviour
//{
//    public KeyCode activarSembradora = KeyCode.H; // Tecla para activar el arado
//    [SerializeField] private Terrain terreno;
//    public TerrainLayer capaSembrada;
//    public float velocidadSembrado = 5f;
//    public ParticleSystem particulasSembrado;
//    [SerializeField] private Transform puntoRaycast;
//    public float distanciaDeteccion = 5f;
//    [SerializeField] private float anchoSembradoraMetros = 9f;


//    private TerrainData data;
//    private int terrainHeightmapWidth;
//    private int terrainHeightmapHeight;

//    bool sembradoraActivo = false; // Estado de la sembradora

//    //spawn de semillas 
//    public GameObject prefabSemilla; // Prefab visual de la semilla
//    [SerializeField] private float radioPozo = 0.5f;
//    private Vector3 ultimaPosicionSembrada;
//    public float distanciaEntreSemillas = 2.0f;

//    //para uqe se mueva el arado es una animacion 
//    [SerializeField] private Transform modeloVisual; // Parte visual del arado que se baja
//    [SerializeField] private float alturaReposo = 0f; // Altura original del arado
//    [SerializeField] private float alturaTrabajo = -1f; // Altura cuando está arando
//    [SerializeField] private float velocidadMovimiento = 2f; // Velocidad de bajada/subida

//    private Coroutine movimientoSembradora;

//    // Pool opcional para mejorar rendimiento
//    private Queue<GameObject> poolSemillas = new Queue<GameObject>();
//    public int cantidadSemillasPool = 200;

//    void Start()
//    {
//        if (terreno == null)
//        {
//            Debug.LogError("[Sembradora] No hay terreno asignado.");
//            enabled = false;
//            return;
//        }
//        data = terreno.terrainData;
//        if (data == null)
//        {
//            Debug.LogError("[Sembradora] El terreno no tiene TerrainData.");
//            enabled = false;
//            return;
//        }
//        terrainHeightmapWidth = Mathf.Max(1 , data.heightmapResolution);
//        terrainHeightmapHeight = Mathf.Max(1 , data.heightmapResolution);
//        // Precrear semillas para evitar Instantiate en runtime
//        if (prefabSemilla != null)
//        {
//            for (int i = 0; i < cantidadSemillasPool; i++)
//            {
//                GameObject semilla = Instantiate(prefabSemilla);
//                semilla.SetActive(false);
//                poolSemillas.Enqueue(semilla);
//            }
//        }
//    }

//    void Update()
//    {
//        //RaycastHit hit;   
//        if (Input.GetKeyDown(activarSembradora))
//        {
//            sembradoraActivo = !sembradoraActivo; // Cambiar el estado de la sembradora
//            if (movimientoSembradora != null) StopCoroutine(movimientoSembradora);

//            float destinoY = sembradoraActivo ? alturaTrabajo : alturaReposo;
//            movimientoSembradora = StartCoroutine(MoverSembradora(destinoY));

//            if (sembradoraActivo)
//            { 
//                ultimaPosicionSembrada = transform.position; // Actualizar la última posición sembrada al activar
//                if (particulasSembrado && !particulasSembrado.isPlaying)
//                    particulasSembrado.Play();
//            }
//            else if (particulasSembrado != null) particulasSembrado.Stop(); // Detener las partículas al desactivar
//        }

//        if (!sembradoraActivo) return; // Si el arado no está activo, salir del método

//        float distancia = Vector3.Distance(transform.position, ultimaPosicionSembrada);
//        if (distancia >= distanciaEntreSemillas)
//        {
//            ProcesarSiembra();
//            ultimaPosicionSembrada = transform.position; // Actualizar la última posición sembrada
//        }
//    }

//    private void ProcesarSiembra()
//    {
//        if (terreno == null || data == null)
//        {
//            Debug.LogError("[Sembradora] Terreno no asignado o inválido.");
//            return;
//        }
//        if (Physics.Raycast(puntoRaycast.position, Vector3.down, out RaycastHit hit, distanciaDeteccion))
//        {

//            if (hit.collider.GetComponent<Terrain>())
//            {
//                Vector3 pos = hit.point - terreno.transform.position;

//                float sizeX = Mathf.Max(data.size.x, 0.0001f);
//                float sizeZ = Mathf.Max(data.size.z, 0.0001f);
//                // Convertir coordenadas a índices del mapa
//                int mapX = Mathf.RoundToInt((pos.x / sizeX) * data.alphamapWidth);
//                int mapZ = Mathf.RoundToInt((pos.z / sizeZ) * data.alphamapHeight);

//                int size = Mathf.RoundToInt((anchoSembradoraMetros / sizeX) * data.alphamapWidth);
//                size = Mathf.Max(1, size); // Asegurarse de que el tamaño sea al menos 1
//                int halfSize = size / 2;

//                mapX = Mathf.Clamp(mapX - halfSize, 0, data.alphamapWidth - size);
//                mapZ = Mathf.Clamp(mapZ - halfSize, 0, data.alphamapHeight - size);

//                float[,,] mapa = data.GetAlphamaps(mapX, mapZ, size, size);
//                int capaSembradaIndex = getLayerIndex(capaSembrada.name); //int capaSembradaIndex = GetLayerIndex("TerrenoSembradoLayer");

//                for (int x = 0; x < size; x++)
//                {
//                    for (int z = 0; z < size; z++)
//                    {
//                        for (int l = 0; l < data.alphamapLayers; l++)
//                        {
//                            mapa[x, z, l] = (l == capaSembradaIndex) ? 1 : 0;
//                        }
//                    }
//                }
//                data.SetAlphamaps(mapX, mapZ, mapa);

//                //para spawnear las semillas
//                if (prefabSemilla != null)
//                {
//                    GameObject semilla = ObtenerSemilla();
//                    semilla.transform.position = hit.point + Vector3.up * 0.1f; // Ajustar la altura para que caiga
//                    semilla.SetActive(true);
//                    //HacerSurcos(hit.point, anchoSembradoraMetros, 0.3f, profundidadPozo);
//                    StartCoroutine(CubrirSemilla(hit.point, radioPozo, 0.005f));
//                }

//                if (particulasSembrado && !particulasSembrado.isPlaying)
//                    particulasSembrado.Play();

//            }
//        }
//    }

//    GameObject ObtenerSemilla()
//    {
//        if (poolSemillas.Count > 0)
//        {
//            GameObject semilla = poolSemillas.Dequeue();
//            poolSemillas.Enqueue(semilla); // Reingresar al pool
//            return semilla;
//        }
//        return Instantiate(prefabSemilla); // Fallback si el pool está vacío
//    }

//    int getLayerIndex(string nombre)
//    {
//        if(data?.terrainLayers == null || data.terrainLayers.Length == 0)
//        {
//            Debug.LogError("El terreno no tiene capas asignadas.");
//            return 0;
//        }
//        for (int i = 0; i < data.terrainLayers.Length; i++)
//            if (data.terrainLayers[i].name == nombre)
//                return i;

//        Debug.LogWarning("No se encontró la capa: " + nombre);
//        return 0;
//    }

//    private IEnumerator MoverSembradora(float destinoY)
//    {
//        if (modeloVisual == null) yield break; // Si no hay modelo visual, salir
//        Vector3 inicio = modeloVisual.localPosition;
//        Vector3 destino = new Vector3(inicio.x, destinoY, inicio.z);

//        float t = 0;
//        while (t < 1)
//        {
//            t += Time.deltaTime * velocidadMovimiento;
//            modeloVisual.localPosition = Vector3.Lerp(inicio, destino, t);
//            yield return null;
//        }
//    }

//    IEnumerator CubrirSemilla(Vector3 posicion, float radio, float alturaCobertura)
//    {
//        yield return new WaitForSeconds(0.3f); // espera a que la semilla caiga
//        if (terreno == null || data == null)
//            yield break;
//        Vector3 terrainPosition = posicion - terreno.transform.position;

//        float sizeX = Mathf.Max(data.size.x, 0.0001f);
//        float sizeZ = Mathf.Max(data.size.z, 0.0001f);

//        int mapX = Mathf.RoundToInt((terrainPosition.x / sizeX) * terrainHeightmapWidth);
//        int mapZ = Mathf.RoundToInt((terrainPosition.z / sizeZ) * terrainHeightmapHeight);
//        int coberturaRadius = Mathf.Max(1, Mathf.RoundToInt(radio * terrainHeightmapWidth / sizeX));

//        int starX = Mathf.Clamp(mapX - coberturaRadius / 2, 0, terrainHeightmapWidth - coberturaRadius);
//        int starZ = Mathf.Clamp(mapZ - coberturaRadius / 2, 0, terrainHeightmapHeight - coberturaRadius);

//        float[,] alturas = data.GetHeights(starX, starZ, coberturaRadius, coberturaRadius);

//        float divisor = Mathf.Max(coberturaRadius / 2f, 0.0001f);

//        for (int x = 0; x < coberturaRadius; x++)
//        {
//            for (int z = 0; z < coberturaRadius; z++)
//            {
//                float distance = Vector2.Distance(new Vector2(x, z), new Vector2(coberturaRadius / 2, coberturaRadius / 2));
//                float falloff = Mathf.Clamp01(1f - distance / divisor);
//                alturas[x, z] += alturaCobertura * falloff;
//            }
//        }

//        data.SetHeights(starX, starZ, alturas);
//    }

//}
