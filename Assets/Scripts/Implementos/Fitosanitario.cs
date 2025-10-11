using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fitosanitario : MonoBehaviour
{
    public KeyCode teclaActivar = KeyCode.T; // La tecla que se debe presionar para activar el fitosanitario
    public bool fitosanitarioActivo = false; // El estado del fitosanitario, activo o inactivo


    public ParticleSystem[] aspersores; // Array de las particulas de los aspersores que se activarán al presionar la tecla
    public Terrain terreno; // El terreno donde se aplicará el fitosanitario
    public int indiceCapaHumedad = 1; // El índice de la capa de humedad en el terreno
    public float radioFitosanitario = 5f; // El radio del área del fitosanitario
    public float intervaloDeActualizacion = 0.5f; // Intervalo de actualización del fitosanitario
    public float velocidadUmbral = 0.1f; // Velocidad mínima para que el riego funcione
    //public float intensidadRiego = 0.8f; // Intensidad del riego, es para una transición más suave de la textura entre una y otra

    private TerrainData data;
    private float tiempoUltimaActualizacion; // Tiempo de la última actualización del fitosanitario 
    private Vector3 ultimaPosicion;
    private float velocidadActual;

    // Start is called before the first frame update
    void Start()
    {
        if (terreno == null)
        {
            terreno = FindObjectOfType<Terrain>(); // Auto-busca el Terrain si no está asignado
            if (terreno == null)
            {
                Debug.LogError("[Fitosanitario] No se encontró ningún Terrain en la escena. Asigna uno manualmente.");
                enabled = false;
                return;
            }
        }
        data = terreno.terrainData;
        if (data == null)
        {
            Debug.LogError("[Fitosanitario] El Terrain no tiene TerrainData asignado.");
            enabled = false;
            return;
        }
        if (aspersores == null || aspersores.Length == 0)
            Debug.LogWarning("[Fitosanitario] No hay aspersores asignados. El fitosanitario no tendrá efecto visual.");
        // Verificación: Asegura que haya al menos 2 capas (0 y 1)
        if (data.alphamapLayers <= indiceCapaHumedad)
        {
            Debug.LogWarning($"[Fitosanitario] Índice de capa {indiceCapaHumedad} fuera de rango (capas totales: {data.alphamapLayers}). Se usará 0.");
            indiceCapaHumedad = Mathf.Clamp(indiceCapaHumedad, 0, data.alphamapLayers - 1);
        }
        ultimaPosicion = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(teclaActivar))
        {
            fitosanitarioActivo = !fitosanitarioActivo;
            ActivarAspersores(fitosanitarioActivo);
            Debug.Log($"[Fitosanitario] Activado: {fitosanitarioActivo}");
        }
        if(ultimaPosicion != Vector3.zero)
            velocidadActual = (transform.position - ultimaPosicion).magnitude / Mathf.Max(Time.deltaTime, 0.0001f) ;
        // Calcular velocidad (distancia recorrida por segundo)
        ultimaPosicion = transform.position;

        if (fitosanitarioActivo && velocidadActual > velocidadUmbral && Time.time - tiempoUltimaActualizacion >= intervaloDeActualizacion)
        {
            PintarTerreno();
            tiempoUltimaActualizacion = Time.time;
        }

    }

    void ActivarAspersores(bool activo)
    {
        if (aspersores == null) return;
        foreach (var aspersor in aspersores)
        {
            if (aspersor == null) continue;

            if (activo && !aspersor.isPlaying) aspersor.Play();
            else if (!activo && aspersor.isPlaying) aspersor.Stop();
        }
    }

    void PintarTerreno()
    {
        if (terreno == null || data == null)
        {
            Debug.LogError("[Fitosanitario] No hay terrain valido para pintar.");
            return;
        }
        float sizeX = Mathf.Max(data.size.x, 0.0001f);
        float sizeZ = Mathf.Max(data.size.z, 0.0001f);

        int paintRadius = Mathf.RoundToInt((radioFitosanitario / sizeX) * data.alphamapWidth); // tamaño del área a pintar
        paintRadius = Mathf.Max(paintRadius, 1); // Asegura que el radio sea al menos 1
        int paintSize = paintRadius * 2 + 1; // tamaño del área a pintar (diámetro)
     
        foreach (var aspersor in aspersores)
        {
            if (aspersor == null) continue;
            Vector3 posicion = aspersor.transform.position - terreno.transform.position;

            int mapX = Mathf.RoundToInt((posicion.x / sizeX) * data.alphamapWidth);
            int mapZ = Mathf.RoundToInt((posicion.z / sizeZ) * data.alphamapHeight);


            //clamp para evitar que se salga del terreno
            int StartX = Mathf.Clamp(mapX - paintRadius, 0, data.alphamapWidth - paintSize);
            int StartZ = Mathf.Clamp(mapZ - paintRadius, 0, data.alphamapHeight - paintSize);

            if (StartX < 0 || StartZ < 0) continue;

            float[,,] alphas = data.GetAlphamaps(StartX, StartZ, paintSize, paintSize);

            for (int x = 0; x < paintSize; x++) 
            {
                for (int z = 0; z < paintSize; z++)
                {
                    float dist = Vector2.Distance (new Vector2(x, z), new Vector2(paintRadius, paintRadius)); // Distancia al centro del área afectada
                    if (dist <= paintRadius)
                    {
                        for (int i = 0; i < data.alphamapLayers; i++)
                        {
                            alphas[z, x, i] = (i == indiceCapaHumedad) ? 1f : 0f;
                        }
                    }
                }
            }
            data.SetAlphamaps(StartX, StartZ, alphas);
        }
    }
}
