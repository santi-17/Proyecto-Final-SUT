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

    private TerrainData data;
    private int terrainHeightmapWidth;
    private int terrainHeightmapHeight;

    bool sembradoraActivo = false; // Estado de la sembradora

    //spawn de semillas 
    public GameObject prefabSemilla; // Prefab visual de la semilla
    public float espacioEntreSemillas = 1f; // Separación entre semillas visuales
    [SerializeField] private float radioPozo = 0.5f;
    [SerializeField] private float profundidadPozo = 0.01f; // Ojo, estos valores son proporcionales a la altura total del terrain


    //    private List<GameObject> semillasInstanciadas = new List<GameObject>();

    private Vector3 ultimaPosicionSembrada;
    public float distanciaEntreSemillas = 1.0f;



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
            sembradoraActivo = !sembradoraActivo; // Cambiar el estado del arado
            //GetComponent<Renderer>().material = aradoActivo ? materialArado : null; // Cambiar el material del arado
            Debug.Log("Arado de disco " + (sembradoraActivo ? "activado" : "desactivado"));
        }

        if (!sembradoraActivo) return; // Si el arado no está activo, salir del método
        // Si el arado está activo, reproducir las partículas de tierra
        
        if (sembradoraActivo && particulasSembrado != null && !particulasSembrado.isPlaying) // Verifica si las partículas no están reproduciéndose
        {
            particulasSembrado.transform.position = puntoRaycast.position; // Asegurarse de que las partículas se posicionen correctamente
            particulasSembrado.Play(); // Reproducir las partículas de tierra
        }
        else if (!sembradoraActivo /*&& instanciaParticulas != null && instanciaParticulas.isPlaying*/) // Si el arado no está activo, detener las partículas
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

                int size = Mathf.RoundToInt(radioSembrado);

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

                float[,,] mapa = data.GetAlphamaps(mapX, mapZ, clampedSizeX, clampedSizeZ);


                int capaSembradaIndex = GetLayerIndex("TerrenoSembradoLayer");

                for (int x = 0; x < clampedSizeX; x++)
                {
                    for (int z = 0; z < clampedSizeZ; z++)
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
                    HacerPozoEnTerreno(hit.point, radioPozo, profundidadPozo);
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

    private void PintarTerreno(RaycastHit hit)
    {
        Terrain terrain = Terrain.activeTerrain;
        TerrainData terrainData = terrain.terrainData;

        Vector3 terrainPosition = hit.point - terrain.transform.position;

        int mapX = Mathf.FloorToInt((terrainPosition.x / terrainData.size.x) * terrainData.alphamapWidth);
        int mapZ = Mathf.FloorToInt((terrainPosition.z / terrainData.size.z) * terrainData.alphamapHeight);

        int size = 5; // tamaño del área a pintar
        int layerIndex = GetLayerIndex("TerrenoSembradoLayer");

        if (layerIndex == -1) return;

        float[,,] alphas = terrainData.GetAlphamaps(mapX, mapZ, size, size);

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                for (int i = 0; i < terrainData.alphamapLayers; i++)
                {
                    alphas[x, z, i] = (i == layerIndex) ? 1f : 0f;
                }
            }
        }

        terrainData.SetAlphamaps(mapX, mapZ, alphas);
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


}
