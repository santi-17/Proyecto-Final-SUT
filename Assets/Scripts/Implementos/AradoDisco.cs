using System.Collections;
using UnityEngine;
using TMPro;

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
                profundidadSurco = 0.45f;
                size = 60;
                break;
            case MisionDisquera.Mision2:
                tipoDisco = "Ondulado";
                cultivoObjetivo = "Zanahoria";
                profundidadSurco = 0.30f;
                size = 60;
                break;
            case MisionDisquera.Mision3:
                tipoDisco = "Pesado";
                cultivoObjetivo = "Pastura";
                profundidadSurco = 0.60f;
                size = 60;
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

        if (Physics.Raycast(puntoRaycast.position, Vector3.down, out RaycastHit hit, distanciaDeteccion))
        {
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

                    float variacion = Random.Range(0.9f, 1.1f);
                    heights[z, x] = Mathf.Clamp01(heights[z, x] - profNorm * falloff * variacion);
                }
            }
            data.SetHeights(startHX, startHZ, heights);
        }
    }
}

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class AradoDisco : MonoBehaviour
//{
//    public KeyCode activarArado = KeyCode.G; // Tecla para activar el arado
//    [SerializeField] private Transform puntoRaycast;
//    [SerializeField] private Terrain terrain;
//    [SerializeField] private ParticleSystem particulasTierra; // Partículas de tierra al arado de disco
//    private ParticleSystem instanciaParticulas; // Instancia de las partículas de tierra

//    public float distanciaDeteccion = 5f; // Distancia de detección del arado
//    public LayerMask Suelo; // Capa del suelo para detectar colisiones
//    public int materialAradoIndex = 1; // Índice del material del arado en el array de materiales

//    public float profundidadSurco = 0.3f; // Profundidad del arado en el terreno
//    public int size = 70; // Tamaño del área afectada por el arado

//    bool aradoActivo = false; // Estado del arado

//    //animacion del arado de disco
//    [SerializeField] private Transform modeloVisual; // Parte visual del arado que se baja
//    [SerializeField] private float alturaReposo = 0f; // Altura original del arado
//    [SerializeField] private float alturaTrabajo = -1.2f; // Altura cuando está arando
//    [SerializeField] private float velocidadMovimiento = 2f; // Velocidad de bajada/subida

//    private Coroutine movimientoArado;
//    private float tiempoEntreArados = 0.25f; // Tiempo entre cada arado
//    private float temporizadorArado = 0f;

//    private Vector3 ultimaPosicion;
//    private float velocidadDeMovimiento;



//    // Start is called before the first frame update
//    void Start()
//    {
//        if (terrain != null && terrain.terrainData != null)
//        {
//            TerrainData data = terrain.terrainData;
//            float[,] baseHeights = new float[data.heightmapResolution, data.heightmapResolution];
//            for (int x = 0; x < data.heightmapResolution; x++)
//            {
//                for (int z = 0; z < data.heightmapResolution; z++)
//                {
//                    baseHeights[x, z] = 0.5f; // mitad de la altura máxima
//                }
//            }
//            data.SetHeights(0, 0, baseHeights);
//        }

//        ultimaPosicion = transform.position;
//        if (particulasTierra != null)
//        {
//            instanciaParticulas = Instantiate(particulasTierra, transform);
//            instanciaParticulas.Stop();
//        }
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
//        velocidadDeMovimiento = (transform.position - ultimaPosicion).magnitude / dt;
//        ultimaPosicion = transform.position;
//        if (Input.GetKeyDown(activarArado))
//        {
//            aradoActivo = !aradoActivo; // Cambiar el estado del arado
//            if (movimientoArado != null) StopCoroutine(movimientoArado);
//            float destinoY = aradoActivo ? alturaTrabajo : alturaReposo;
//            movimientoArado = StartCoroutine(MoverArado(destinoY));
//        }

//        if (!aradoActivo)
//        {
//            terrain?.Flush();
//            return; // Si el arado no está activo, salir del método
//        }
//        if (velocidadMovimiento < 0.1f) {return;}

//        // Si el arado está activo, reproducir las partículas de tierra
//        if (aradoActivo && instanciaParticulas != null && !instanciaParticulas.isPlaying) // Verifica si las partículas no están reproduciéndose
//        {
//            instanciaParticulas.transform.position = puntoRaycast.position; // Asegurarse de que las partículas se posicionen correctamente
//            instanciaParticulas.Play(); // Reproducir las partículas de tierra
//        }
//        else if (!aradoActivo && instanciaParticulas != null && instanciaParticulas.isPlaying) // Si el arado no está activo, detener las partículas
//        {
//            instanciaParticulas.Stop(); // Detener las partículas de tierra
//        }

//        temporizadorArado -= Time.deltaTime;
//        if (aradoActivo && temporizadorArado <= 0f)
//        {
//            if (velocidadDeMovimiento > 0.1f) ArarTerreno();
//            temporizadorArado = tiempoEntreArados;
//        }

//    }
//    private IEnumerator MoverArado(float destinoY)
//    {
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

//    private void ArarTerreno() 
//    {
//        if (terrain == null || terrain.terrainData == null)
//        {
//            Debug.LogError("[Disquera] Terrain no asignado o inválido.");
//            return;
//        }
//        if (Physics.Raycast(puntoRaycast.position, Vector3.down, out RaycastHit hit, distanciaDeteccion))
//        {
//            Debug.DrawRay(puntoRaycast.position, Vector3.down * distanciaDeteccion, Color.red);

//            if (terrain != null)
//            {
//                Vector3 terrainPos = hit.point - terrain.transform.position;
//                TerrainData data = terrain.terrainData;

//                int heightmapX = (int)((terrainPos.x / data.size.x) * data.heightmapResolution);
//                int heightmapZ = (int)((terrainPos.z / data.size.z) * data.heightmapResolution);

//                // Asegurarse de que el área afectada no se salga de los límites del heightmap
//                int startX = Mathf.Clamp(heightmapX - size / 2, 0, data.heightmapResolution - 1);
//                int startZ = Mathf.Clamp(heightmapZ - size / 2, 0, data.heightmapResolution - 1);

//                int widthH = Mathf.Min(size, data.heightmapResolution - startX);
//                int heightH = Mathf.Min(size, data.heightmapResolution - startZ);
//                if (widthH <= 0 || heightH <= 0)
//                {
//                    Debug.LogWarning("[Disquera] Área de deformación inválida.");
//                    return;
//                }

//                //pinto la textura del terreno
//                int alphamapX = (int)((terrainPos.x / data.size.x) * data.alphamapWidth);
//                int alphamapZ = (int)((terrainPos.z / data.size.z) * data.alphamapHeight);

//                int startAlphaX = Mathf.Clamp(alphamapX - size / 2, 0, data.alphamapWidth - 1);
//                int startAlphaZ = Mathf.Clamp(alphamapZ - size / 2, 0, data.alphamapHeight - 1);

//                int widthA = Mathf.Min(size, data.alphamapWidth - startAlphaX);
//                int heightA = Mathf.Min(size, data.alphamapHeight - startAlphaZ);

//                //Cambiar la textura del terreno (splatmap)
//                float[,,] splatmap = data.GetAlphamaps(startAlphaX, startAlphaZ, widthA, heightA);

//                for (int x = 0; x < widthA; x++)
//                {
//                    for (int z = 0; z < heightA; z++)
//                    {
//                        for (int i = 0; i < data.alphamapLayers; i++)
//                            splatmap [x, z, i] = 0;

//                        splatmap[x, z, materialAradoIndex] = 1;
//                    }
//                }

//                data.SetAlphamaps(startAlphaX, startAlphaZ, splatmap);

//                //2. deformar el terreno (heigthmap)
//                float[,] heights = data.GetHeights(startX, startZ, widthH, heightH);

//                for (int x = 0; x < widthH; x++)
//                {
//                    for (int z = 0; z < heightH; z++)
//                    {
//                        float distanciaCentro = Vector2.Distance(new Vector2(x, z), new Vector2(widthH / 2f, heightH / 2f)); // Distancia al centro del área afectada
//                        float divisor = Mathf.Max(widthH / 2f, 0.0001f);
//                        float falloff = Mathf.Clamp01(1f - (distanciaCentro / divisor)); // Factor de caída basado en la distancia al centro

//                        float factor = Random.Range(0.7f, 1.3f); //Le doy irregularidad al terreno //1f - (distanciaCentro / (size / 2f));

//                        float profundidadNormalizada = profundidadSurco / Mathf.Max(data.size.y, 0.0001f);
//                        heights[x, z] = Mathf.Clamp01(heights[x, z] - profundidadNormalizada * falloff * factor);
//                    }
//                }
//                data.SetHeights(startX, startZ, heights);
//               //   terrain.Flush();
//            }
//            else
//            {
//                Debug.LogWarning("El objeto no tiene un Renderer para cambiar el material.");
//            }
//            if (instanciaParticulas != null && !instanciaParticulas.isPlaying)
//            {
//                instanciaParticulas.transform.position = puntoRaycast.position;
//                instanciaParticulas.Play();
//            }
//        }
//        else
//        {
//            // Si no se detecta el suelo, desactivar el arado
//            Debug.Log("No hay suelo debajo del arado");
//        }
//    }
//}
