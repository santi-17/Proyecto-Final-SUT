using TMPro;
using UnityEngine;
using UnityEngine.UI; // Para usar UI

public class ShowTerrainName : MonoBehaviour
{
    public TextMeshProUGUI terrainNameText; // Referencia al texto UI
    public float raycastDistance = 10f; // Distancia para detectar el terreno

    void Start()
    {
        if (terrainNameText != null)
        {
            terrainNameText.gameObject.SetActive(false); // Ocultar texto al inicio
        }
    }

    void Update()
    {
        RaycastHit hit;
        // Lanzar un rayo hacia abajo desde la posición del jugador
        if (Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance))
        {
            // Verificar si el objeto tocado es un Terrain
            TerrainInfo terrainInfo = hit.collider.GetComponent<TerrainInfo>();
            if (terrainInfo != null)
            {
                // Mostrar el nombre del terreno
                terrainNameText.text = terrainInfo.description;
                terrainNameText.gameObject.SetActive(true);
                return;
            }
        }
        // Si no está sobre un terreno, ocultar el texto
        if (terrainNameText != null)
        {
            terrainNameText.gameObject.SetActive(false);
        }
    }
}