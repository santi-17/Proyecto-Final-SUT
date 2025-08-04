using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Riego : MonoBehaviour
{

    public KeyCode teclaActivar = KeyCode.R; // La tecla que se debe presionar para activar el riego
    public bool riegoActivo = false; // El estado del riego, activo o inactivo


    public ParticleSystem[] aspersores; // Array de las particulas de los aspersores que se activarán al presionar la tecla
    public Terrain terreno; // El terreno donde se aplicará el riego
    public int indiceCapaHumedad = 2; // El índice de la capa de humedad en el terreno
    public float radioRiego = 15f; // El radio del área de riego


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(teclaActivar))
        {
            riegoActivo = !riegoActivo;
            ActivarAspersores(riegoActivo);
        }

        if (riegoActivo)
        {
            PintarTerreno();
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
        foreach (var aspersor in aspersores)
        {
            Vector3 posicion = aspersor.transform.position;
            Vector3Int mapaCoord;
            float[,,] alphas = terreno.terrainData.GetAlphamaps(0, 0, terreno.terrainData.alphamapWidth, terreno.terrainData.alphamapHeight);

            int mapX = Mathf.FloorToInt((posicion.x - terreno.transform.position.x) / terreno.terrainData.size.x * terreno.terrainData.alphamapWidth);
            int mapZ = Mathf.FloorToInt((posicion.z - terreno.transform.position.z) / terreno.terrainData.size.z * terreno.terrainData.alphamapHeight);

            int size = 15; // tamaño del área a pintar

            for (int x = -size; x <= size; x++)
            {
                for (int z = -size; z <= size; z++)
                {
                    int px = mapX + x;
                    int pz = mapZ + z;

                    if (px >= 0 && px < terreno.terrainData.alphamapWidth && pz >= 0 && pz < terreno.terrainData.alphamapHeight)
                    {
                        for (int i = 0; i < terreno.terrainData.alphamapLayers; i++)
                        {
                            alphas[pz, px, i] = (i == indiceCapaHumedad) ? 1f : 0f;
                        }
                    }
                }
            }

            terreno.terrainData.SetAlphamaps(0, 0, alphas);
        }
    }
}
