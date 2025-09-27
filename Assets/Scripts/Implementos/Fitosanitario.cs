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
        }
        data = terreno.terrainData;
        // Verificación: Asegura que haya al menos 2 capas (0 y 1)
        if (data.alphamapLayers < 2)
        {
            Debug.LogError("[Fitosanitario] El Terrain Data no tiene suficientes capas. Necesitas al menos 2 (base + humedad).");
        }
        else
        {
            Debug.Log($"[Fitosanitario] Configurado para pintar en capa {indiceCapaHumedad} (total de capas: {data.alphamapLayers}).");
        }
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

        // Calcular velocidad (distancia recorrida por segundo)
        velocidadActual = (transform.position - ultimaPosicion).magnitude / Time.deltaTime;
        ultimaPosicion = transform.position;

        if (fitosanitarioActivo && velocidadActual > velocidadUmbral && Time.time - tiempoUltimaActualizacion >= intervaloDeActualizacion)
        {
            PintarTerreno();
            tiempoUltimaActualizacion = Time.time;
        }

    }

    void ActivarAspersores(bool activo)
    {
        foreach (var aspersor in aspersores)
        {
            if (activo) aspersor.Play();
            else aspersor.Stop();
        }
    }

    void PintarTerreno()
    {
        int size = Mathf.RoundToInt((radioFitosanitario / data.size.x) * data.alphamapWidth); // tamaño del área a pintar
        int paintSize = size * 2 + 1; // tamaño del área a pintar (diámetro)
     
        foreach (var aspersor in aspersores)
        {
            Vector3 posicion = aspersor.transform.position - terreno.transform.position;

            int mapX = Mathf.RoundToInt((posicion.x / data.size.x) * data.alphamapWidth);
            int mapZ = Mathf.RoundToInt((posicion.z / data.size.z) * data.alphamapHeight);


            //clamp para evitar que se salga del terreno
            int StartX = Mathf.Clamp(mapX - size, 0, data.alphamapWidth - paintSize);
            int StartZ = Mathf.Clamp(mapZ - size, 0, data.alphamapHeight - paintSize);

            float[,,] alphas = data.GetAlphamaps(StartX, StartZ, paintSize, paintSize);

            for (int x = 0; x < paintSize; x++) 
            {
                for (int z = 0; z < paintSize; z++)
                {
                    float dist = Vector2.Distance (new Vector2(x, z), new Vector2(size, size)); // Distancia al centro del área afectada
                    if (dist <= size)
                    {
                    //    float mezcla = 1f - (dist / size); // Mezcla basada en la distancia al centro
                    //    mezcla *= intensidadRiego; // Aplica la intensidad del riego

                        for (int i = 0; i < data.alphamapLayers; i++)
                        {
                            //if (i == indiceCapaHumedad)
                            //{
                            //    // Aumenta la capa de humedad
                            //    alphas[z, x, i] = Mathf.Lerp(alphas[z, x, i], 1f, mezcla);
                            //}
                            //else
                            //{
                            //    // Reduce las otras capas proporcionalmente
                            //    alphas[z, x, i] = Mathf.Lerp(alphas[z, x, i], 0f, mezcla * 0.5f); // 0.5f para no eliminar completament
                            //    //alphas[z, x, i] *= (1f - mezcla); // Reduce las otras capas
                            //}
                            alphas[z, x, i] = (i == indiceCapaHumedad) ? 1f : 0f;
                        }
                    }
                }
            }
            data.SetAlphamaps(StartX, StartZ, alphas);
        }
    }
}
