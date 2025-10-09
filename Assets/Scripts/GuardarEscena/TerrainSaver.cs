using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TerrainSaver : MonoBehaviour
{
    public Terrain[] terrains; // Asigná los tres terrenos en el inspector

    public string SaveTerrainsToJson()
    {
        List<TerrainState> terrainStates = new List<TerrainState>();

        foreach (var terrain in terrains)
        {
            TerrainData data = terrain.terrainData;
            TerrainState state = new TerrainState();

            state.heights = data.GetHeights(0, 0, data.heightmapResolution, data.heightmapResolution);
            state.alphamaps = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);

            terrainStates.Add(state);
        }

        return JsonUtility.ToJson(new TerrainStateWrapper { terrains = terrainStates.ToArray() });
    }

    public void LoadTerrainsFromJson(string json)
    {
        TerrainStateWrapper wrapper = JsonUtility.FromJson<TerrainStateWrapper>(json);

        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            TerrainData data = terrain.terrainData;
            TerrainState state = wrapper.terrains[i];

            data.SetHeights(0, 0, state.heights);
            data.SetAlphamaps(0, 0, state.alphamaps);
        }
    }

    [System.Serializable]
    private class TerrainStateWrapper
    {
        public TerrainState[] terrains;
    }
}
