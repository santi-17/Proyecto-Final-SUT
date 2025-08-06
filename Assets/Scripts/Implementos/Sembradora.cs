using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sembradora : MonoBehaviour
{
    public KeyCode activarSembradora = KeyCode.H; // Tecla para activar el arado
    public Terrain terreno;
    public TerrainLayer capaSembrada;
    public float radioSembrado = 75f;
    public float velocidadSembrado = 5f;
    public ParticleSystem particulasSembrado;
    public Transform puntoRaycast;
    public float distanciaDeteccion = 5f;
    [SerializeField] private float anchoSembradoraMetros = 9f; // Ajustalo al tamaño real


    private TerrainData data;
    private int terrainHeightmapWidth;
    private int terrainHeightmapHeight;

    bool sembradoraActivo = false; // Estado de la sembradora

    //spawn de semillas 
    public GameObject prefabSemilla; // Prefab visual de la semilla
    public float espacioEntreSemillas = 2f; // Separación entre semillas visuales
    [SerializeField] private float radioPozo = 0.5f;
    [SerializeField] private float profundidadPozo = 0.001f; // Ojo, estos valores son proporcionales a la altura total del terrain


    //    private List<GameObject> semillasInstanciadas = new List<GameObject>();

    private Vector3 ultimaPosicionSembrada;
    public float distanciaEntreSemillas = 1.0f;

    //para uqe se mueva el arado es una animacion 
    [SerializeField] private Transform modeloVisual; // Parte visual del arado que se baja
    [SerializeField] private float alturaReposo = 0f; // Altura original del arado
    [SerializeField] private float alturaTrabajo = -1f; // Altura cuando está arando
    [SerializeField] private float velocidadMovimiento = 2f; // Velocidad de bajada/subida

    private Coroutine movimientoSembradora;



    void Start()
    {
        data = terreno.terrainData;
        terrainHeightmapWidth = data.heightmapResolution;
        terrainHeightmapHeight = data.heightmapResolution;
        if (terreno != null)
        {
            TerrainData data = terreno.terrainData;

            float[,] alturas = data.GetHeights(0, 0, data.heightmapResolution, data.heightmapResolution);
            for (int x = 0; x < data.heightmapResolution; x++)
            {
                for (int z = 0; z < data.heightmapResolution; z++)
                {
                    alturas[z, x] = 0.5f; // mitad de la altura máxima
                }
            }

            data.SetHeights(0, 0, alturas);
            terreno.Flush();
            Debug.Log("Terreno elevado a 0.5f");
        }
    }

    void Update()
    {
        RaycastHit hit;
       // Vector3 origen = transform.position + Vector3.up;
        if (Input.GetKeyDown(activarSembradora))
        {
            sembradoraActivo = !sembradoraActivo; // Cambiar el estado de la sembradora
            Debug.Log("Sembradora" + (sembradoraActivo ? "activado" : "desactivado"));
            if (movimientoSembradora != null) StopCoroutine(movimientoSembradora);

            float destinoY = sembradoraActivo ? alturaTrabajo : alturaReposo;
            movimientoSembradora = StartCoroutine(MoverSembradora(destinoY));

            if (sembradoraActivo)
            {
                ultimaPosicionSembrada = transform.position; // Actualizar la última posición sembrada al activar
            }

         
        }

        if (!sembradoraActivo) return; // Si el arado no está activo, salir del método
        // Si el arado está activo, reproducir las partículas de tierra
        
        if (sembradoraActivo && particulasSembrado != null) // Verifica si las partículas no están reproduciéndose
        {
            particulasSembrado.transform.position = puntoRaycast.position; // Asegurarse de que las partículas se posicionen correctamente
            particulasSembrado.Play(); // Reproducir las partículas de tierra
        }
        else if (!sembradoraActivo && particulasSembrado != null) // Si el arado no está activo, detener las partículas
        {
            particulasSembrado.Stop(); // Detener las partículas de tierra
        }
        float distancia = Vector3.Distance(transform.position, ultimaPosicionSembrada);
        if (distancia >= distanciaEntreSemillas )
        {
        
        if (Physics.Raycast(puntoRaycast.position, Vector3.down, out hit, distanciaDeteccion))
        {
            Debug.DrawRay(puntoRaycast.position, Vector3.down * distanciaDeteccion, Color.green);
            Debug.Log("Hit en: " + hit.point);

            if (hit.collider.GetComponent<Terrain>())
            {
                Vector3 pos = hit.point - terreno.transform.position;

                // Convertir coordenadas a índices del mapa
                int mapX = Mathf.RoundToInt((pos.x / data.size.x) * data.alphamapWidth);
                int mapZ = Mathf.RoundToInt((pos.z / data.size.z) * data.alphamapHeight);

                //int size = Mathf.RoundToInt(radioSembrado);
                int size = Mathf.RoundToInt((anchoSembradoraMetros / data.size.x) * data.alphamapWidth);


                    mapX = Mathf.Clamp(mapX, 0, data.alphamapWidth - 1);
                mapZ = Mathf.Clamp(mapZ, 0, data.alphamapHeight - 1);

                int clampedSizeX = Mathf.Min(size, data.alphamapWidth - mapX);
                int clampedSizeZ = Mathf.Min(size, data.alphamapHeight - mapZ);

                if (clampedSizeX <= 0 || clampedSizeZ <= 0)
                {
                    Debug.LogWarning("Tamaño clampeado inválido para alpha map.");
                    return; // salí sin hacer nada para evitar crash
                }


                Debug.Log($"mapX: {mapX}, mapZ: {mapZ}, size: {size}, width: {data.alphamapWidth}, height: {data.alphamapHeight}");

                //float[,,] mapa = data.GetAlphamaps(mapX, mapZ, clampedSizeX, clampedSizeZ);
                int halfSize = size / 2;
                mapX = Mathf.Clamp(mapX - halfSize, 0, data.alphamapWidth - size);
                mapZ = Mathf.Clamp(mapZ - halfSize, 0, data.alphamapHeight - size);

                float[,,] mapa = data.GetAlphamaps(mapX, mapZ, size, size);

                int capaSembradaIndex = GetLayerIndex("TerrenoSembradoLayer");

                for (int x = 0; x < size; x++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        for (int l = 0; l < data.alphamapLayers; l++)
                        {
                            mapa[x, z, l] = (l == capaSembradaIndex) ? 1 : 0;
                        }
                    }
                }


                data.SetAlphamaps(mapX, mapZ, mapa);

                //para spawnear las semillas
                if (prefabSemilla != null)
                {
                    Vector3 spwanposition = hit.point + Vector3.up * 0.1f; // Ajustar la posición para que no esté enterrada
                    GameObject semilla = Instantiate(prefabSemilla, spwanposition, Quaternion.identity);

                    HacerSurcos(hit.point, anchoSembradoraMetros, 0.3f, profundidadPozo);

                    StartCoroutine(CubrirSemilla(hit.point, radioPozo, 0.005f));
                        //HacerPozoEnTerreno(hit.point, radioPozo, profundidadPozo);

                        //semilla.transform.SetParent(this.transform); // Opcional
                    }

                if (particulasSembrado && !particulasSembrado.isPlaying)
                {
                    particulasSembrado.Play();
                }
                ultimaPosicionSembrada = transform.position;
            }
        }
        }
        else
        {
            if (particulasSembrado && particulasSembrado.isPlaying)
            {
                particulasSembrado.Stop();
            }
        }
    }

    int GetLayerIndex(string nombre)
    {
        for (int i = 0; i < data.terrainLayers.Length; i++)
        {
            if (data.terrainLayers[i].name == nombre)
                return i;
        }
        Debug.LogError("No se encontró la capa: " + nombre);
        return 0;
    }

    private void HacerPozoEnTerreno(Vector3 posicion, float radio, float profundidad)
    {
        Vector3 terrainPosition = posicion - terreno.transform.position;

        int mapX = Mathf.RoundToInt((terrainPosition.x / terreno.terrainData.size.x) * terrainHeightmapWidth);
        int mapZ = Mathf.RoundToInt((terrainPosition.z / terreno.terrainData.size.z) * terrainHeightmapHeight);

        int pozoRadius = Mathf.RoundToInt(radio * terrainHeightmapWidth / terreno.terrainData.size.x);

        float[,] heights = data.GetHeights(
            mapX - pozoRadius / 2,
            mapZ - pozoRadius / 2,
            pozoRadius,
            pozoRadius
        );

        for (int x = 0; x < pozoRadius; x++)
        {
            for (int z = 0; z < pozoRadius; z++)
            {
                float distance = Vector2.Distance(new Vector2(x, z), new Vector2(pozoRadius / 2, pozoRadius / 2));
                float falloff = Mathf.Clamp01(1f - distance / (pozoRadius / 2f));
                heights[x, z] -= profundidad * falloff;
            }
        }

        data.SetHeights(
            mapX - pozoRadius / 2,
            mapZ - pozoRadius / 2,
            heights
        );
    }

    private void HacerSurcos(Vector3 posicionCentral, float anchoTotal, float espacioEntreSurcos, float profundidad)
    {
        int cantidadSurcos = Mathf.FloorToInt(anchoTotal / espacioEntreSurcos);
        Vector3 direccionDerecha = transform.forward;

        for (int i = 0; i < cantidadSurcos; i++)
        {
            float offset = (-anchoTotal / 2f) + (i * espacioEntreSurcos);
            Vector3 posicionSurco = posicionCentral + direccionDerecha * offset;

            // Generar pozo en esa posición
            HacerPozoEnTerreno(posicionSurco, 0.3f, profundidad);
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
    IEnumerator CubrirSemilla(Vector3 posicion, float radio, float alturaCobertura)
    {
        yield return new WaitForSeconds(0.5f); // espera a que la semilla caiga

        Vector3 terrainPosition = posicion - terreno.transform.position;

        int mapX = Mathf.RoundToInt((terrainPosition.x / terreno.terrainData.size.x) * terrainHeightmapWidth);
        int mapZ = Mathf.RoundToInt((terrainPosition.z / terreno.terrainData.size.z) * terrainHeightmapHeight);

        int coberturaRadius = Mathf.RoundToInt(radio * terrainHeightmapWidth / terreno.terrainData.size.x);

        float[,] alturas = data.GetHeights(
            mapX - coberturaRadius / 2,
            mapZ - coberturaRadius / 2,
            coberturaRadius,
            coberturaRadius
        );

        for (int x = 0; x < coberturaRadius; x++)
        {
            for (int z = 0; z < coberturaRadius; z++)
            {
                float distance = Vector2.Distance(new Vector2(x, z), new Vector2(coberturaRadius / 2, coberturaRadius / 2));
                float falloff = Mathf.Clamp01(1f - distance / (coberturaRadius / 2f));
                alturas[x, z] += alturaCobertura * falloff;
            }
        }

        data.SetHeights(
            mapX - coberturaRadius / 2,
            mapZ - coberturaRadius / 2,
            alturas
        );
    }


}
