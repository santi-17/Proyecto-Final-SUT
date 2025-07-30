using TMPro;
using UnityEngine;

public class CampoTracker : MonoBehaviour
{
    public Terrain terreno;
    public string nombreLayerArado = "TerrainLayer_B";
    public float progresoActual;

    private int indexLayerArado;
    private int ancho;
    private int alto;


    [SerializeField] public TextMeshProUGUI textoProgreso; // Asignalo desde el Inspector

    private void Start()
    {
        if (terreno == null) terreno = GetComponent<Terrain>();
        if (terreno == null)
        {
            Debug.LogError("No se encontró el componente Terrain.");
            return;
        }

        indexLayerArado = ObtenerIndiceLayerArado();
        ancho = terreno.terrainData.alphamapWidth;
        alto = terreno.terrainData.alphamapHeight;

        if (indexLayerArado == -1)
        {
            Debug.LogError($"No se encontró el TerrainLayer con nombre '{nombreLayerArado}'.");
            return;
        }

        InvokeRepeating(nameof(CalcularProgreso), 1f, 2f); // cada 2 segundos
    }

    int ObtenerIndiceLayerArado()
    {
        TerrainLayer[] layers = terreno.terrainData.terrainLayers;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].name == nombreLayerArado)
                return i;
        }
        return -1;
    }

    void CalcularProgreso()
    {
        float[,,] alphaMaps = terreno.terrainData.GetAlphamaps(0, 0, ancho, alto);
        int totalCeldas = ancho * alto;
        int celdasAradas = 0;

        for (int x = 0; x < ancho; x++)
        {
            for (int y = 0; y < alto; y++)
            {
                if (alphaMaps[x, y, indexLayerArado] > 0.5f)
                {
                    celdasAradas++;
                }
            }
        }

        progresoActual = (float)celdasAradas / totalCeldas;
        float porcentaje = progresoActual * 100f;
        Debug.Log($"Progreso del campo arado: {(progresoActual * 100f):F2}%");


        if (textoProgreso != null)
        {
            textoProgreso.text = $"Campo Arado: {porcentaje:F2}%";
        }
    }
}
