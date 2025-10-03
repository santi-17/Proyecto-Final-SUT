using UnityEngine;

public class CultivoSpawner : MonoBehaviour
{
    [Header("Terreno donde se van a plantar")]
    public Terrain terreno;

    [Header("Prefab del cultivo")]
    public GameObject cultivoPrefab;

    [Header("Área de spawn (en coordenadas del mundo)")]
    public Vector2 areaMin = new Vector2(0, 0);
    public Vector2 areaMax = new Vector2(50, 50);

    [Header("Configuración de plantado")]
    public float distanciaEntreCultivos = 2f; // separación entre cultivos
    public bool aleatorio = false; // si querés randomizar un poco la posición

    void Start()
    {
        SpawnearCultivos();
    }

    void SpawnearCultivos()
    {
        if (terreno == null || cultivoPrefab == null) return;

        for (float x = areaMin.x; x < areaMax.x; x += distanciaEntreCultivos)
        {
            for (float z = areaMin.y; z < areaMax.y; z += distanciaEntreCultivos)
            {
                // Coordenada en el mundo
                Vector3 posicion = new Vector3(x, 0, z);

                // Altura del terreno en esa posición
                float y = terreno.SampleHeight(posicion);
                posicion.y = y;

                // Random pequeño para no quedar en grilla perfecta
                if (aleatorio)
                {
                    posicion.x += Random.Range(-0.5f, 0.5f);
                    posicion.z += Random.Range(-0.5f, 0.5f);
                }

                // Instanciar cultivo
                Instantiate(cultivoPrefab, posicion, Quaternion.identity, transform);
            }
        }
    }
}

