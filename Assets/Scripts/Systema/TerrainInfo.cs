using TMPro;
using UnityEngine;

public class TerrainInfo : MonoBehaviour
{
    [TextArea]
    public string description; // Texto descriptivo del terreno
    public string tipoEsperado; // tipo de implemento que corresponde a este terreno
    public string cultivoEsperado;

    //private Transform mainCamera;
    private Terrain terrain;
    [SerializeField] private TextMeshPro etiquetaTerrain;
    [SerializeField] private Vector3 offsetEtiqueta = new Vector3(0, -40f, 0);

    void Start()
    {
        terrain = GetComponent<Terrain>();
        if (etiquetaTerrain == null)
        {
            GameObject textoObj = new GameObject("etiquetaTerrain");
            textoObj.transform.SetParent(transform);
            textoObj.transform.localPosition = offsetEtiqueta;

            etiquetaTerrain = textoObj.AddComponent<TextMeshPro>();
            etiquetaTerrain.text = description;
            etiquetaTerrain.alignment = TextAlignmentOptions.Center;
            etiquetaTerrain.fontSize = 18f;
            etiquetaTerrain.color = Color.white;
        }
        etiquetaTerrain.text = description;
        // Posiciona inicialmente el texto
        UpdateLabelPosition();
    }
    void LateUpdate()
    {
        if(etiquetaTerrain != null && Camera.main != null)
        {
            etiquetaTerrain.transform.LookAt(Camera.main.transform);
            etiquetaTerrain.transform.Rotate(0, 180f, 0);
        }
    }

    private void UpdateLabelPosition()
    {
        if (terrain != null)
        {
            // Calcula el centro real del terreno
            Vector3 center = terrain.transform.position + terrain.terrainData.size / 2f;
            etiquetaTerrain.transform.position = center + offsetEtiqueta;
        }
        else
        {
            // Si no hay Terrain, usa la posición base del objeto
            etiquetaTerrain.transform.position = transform.position + offsetEtiqueta;
        }
    }
}

