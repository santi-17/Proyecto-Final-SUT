using UnityEngine;
//public enum TipoTerreno { A, B, C, D }

public class MapaTerreno : MonoBehaviour
{
    public TipoDeTerreno.TipoTerreno[,] tipoPorPosicion; // tamaño: ancho x largo del terrain

    public Terrain terrain;

    private void Awake()
    {
        int width = terrain.terrainData.alphamapWidth;
        int height = terrain.terrainData.alphamapHeight;
       // tipoPorPosicion = new TipoTerreno[width, height];

        // Ejemplo: asignar todo como tipo A (después lo podés pintar a mano o desde editor)
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                //tipoPorPosicion[x, z] = TipoTerreno.A;
                return;
    }

    //public TipoTerreno GetTipoEn(float worldX, float worldZ)
    //{
    //    Vector3 terrainPos = terrain.transform.position;
    //    int mapX = Mathf.FloorToInt((worldX - terrainPos.x) / terrain.terrainData.size.x * terrain.terrainData.alphamapWidth);
    //    int mapZ = Mathf.FloorToInt((worldZ - terrainPos.z) / terrain.terrainData.size.z * terrain.terrainData.alphamapHeight);

    //    mapX = Mathf.Clamp(mapX, 0, tipoPorPosicion.GetLength(0) - 1);
    //    mapZ = Mathf.Clamp(mapZ, 0, tipoPorPosicion.GetLength(1) - 1);

    //    return tipoPorPosicion[mapX, mapZ];
    //}
}

