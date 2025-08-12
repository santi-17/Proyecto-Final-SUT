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
    public int indiceCapaHumedad = 2; // El índice de la capa de humedad en el terreno
    public float radioFitosanitario = 5f; // El radio del área del fitosanitario
    public float intervaloDeActualizacion = 0.5f; // Intervalo de actualización del fitosanitario

    private TerrainData data; 
    private float tiempoUltimaActualizacion; // Tiempo de la última actualización del fitosanitario 


    // Start is called before the first frame update
    void Start()
    {  
        data = terreno.terrainData;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(teclaActivar))
        {
            fitosanitarioActivo = !fitosanitarioActivo;
            ActivarAspersores(fitosanitarioActivo);
        }

        if (fitosanitarioActivo && Time.time - tiempoUltimaActualizacion >= intervaloDeActualizacion)
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
                        for (int i = 0; i < data.alphamapLayers; i++)
                            alphas[z, x, i] = (i == indiceCapaHumedad) ? 1f : 0f;
                    }
                        
                }
            }
            data.SetAlphamaps(StartX, StartZ, alphas);
        }
    }
}
