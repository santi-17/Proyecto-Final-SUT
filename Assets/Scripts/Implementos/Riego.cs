using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Riego : MonoBehaviour
{
    public enum MisionRiego { Mision1, Mision2, Mision3 }
    public MisionRiego misionActual = MisionRiego.Mision1;

    [Header("Control")]
    public KeyCode teclaActivar = KeyCode.R;
    private bool riegoActivo = false;

    [Header("Referencias")]
    public Terrain terreno;
    public ParticleSystem[] aspersores;
    [SerializeField] private Vector3 offsetEtiqueta = new Vector3(0, 3.5f, 0);

    [Header("Configuración de Riego")]
    public int indiceCapaHumedad = 1;
    private float radioRiego;
    private float intervaloDeActualizacion;
    private float velocidadUmbral;
    private string tipoRiego;
    private string frecuenciaRiego;
    private string horarioRiego;
    private string cultivo;
    private string suelo;
    private string clima;

    private TerrainData data;
    private float tiempoUltimaActualizacion;
    private Vector3 ultimaPosicion;
    private float velocidadActual;

    private TMP_Text etiquetaRiego;

    void Start()
    {
        ConfigurarMision();

        if (terreno == null)
        {
            terreno = FindObjectOfType<Terrain>();
            if (terreno == null)
            {
                Debug.LogError("[Riego] No se encontró ningún Terrain en la escena.");
                enabled = false;
                return;
            }
        }

        data = terreno.terrainData;
        if (data == null)
        {
            Debug.LogError("[Riego] El Terrain no tiene TerrainData asignado.");
            enabled = false;
            return;
        }

        // Crear etiqueta visual
        GameObject textoObj = new GameObject("EtiquetaRiego");
        textoObj.transform.SetParent(transform);
        textoObj.transform.localPosition = offsetEtiqueta;

        etiquetaRiego = textoObj.AddComponent<TextMeshPro>();
        etiquetaRiego.alignment = TextAlignmentOptions.Center;
        etiquetaRiego.fontSize = 7f;
        etiquetaRiego.color = new Color(0.2f, 0.7f, 1f);
        etiquetaRiego.text = $"{tipoRiego}\nFrecuencia: {frecuenciaRiego}\nHorario: {horarioRiego}\nCultivo: {cultivo}";

        ultimaPosicion = transform.position;
    }

    void Update()
    {
        if (Input.GetKeyDown(teclaActivar))
        {
            riegoActivo = !riegoActivo;
            ActivarAspersores(riegoActivo);
            Debug.Log($"[Riego] Activado: {riegoActivo}");
        }

        // Calcular velocidad del tractor o implemento
        if (ultimaPosicion != Vector3.zero)
            velocidadActual = (transform.position - ultimaPosicion).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        ultimaPosicion = transform.position;

        if (riegoActivo && velocidadActual > velocidadUmbral && Time.time - tiempoUltimaActualizacion >= intervaloDeActualizacion)
        {
            PintarTerreno();
            tiempoUltimaActualizacion = Time.time;
        }
    }

    private void LateUpdate()
    {
        if (etiquetaRiego != null && Camera.main != null)
        {
            etiquetaRiego.transform.LookAt(Camera.main.transform);
            etiquetaRiego.transform.Rotate(0, 180f, 0);
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
        if (terreno == null || data == null) return;

        float sizeX = Mathf.Max(data.size.x, 0.0001f);
        float sizeZ = Mathf.Max(data.size.z, 0.0001f);

        int paintRadius = Mathf.RoundToInt((radioRiego / sizeX) * data.alphamapWidth);
        paintRadius = Mathf.Max(paintRadius, 1);
        int paintSize = paintRadius * 2 + 1;

        foreach (var aspersor in aspersores)
        {
            if (aspersor == null) continue;

            Vector3 posicion = aspersor.transform.position - terreno.transform.position;
            int mapX = Mathf.RoundToInt((posicion.x / sizeX) * data.alphamapWidth);
            int mapZ = Mathf.RoundToInt((posicion.z / sizeZ) * data.alphamapHeight);

            int startX = Mathf.Clamp(mapX - paintRadius, 0, data.alphamapWidth - paintSize);
            int startZ = Mathf.Clamp(mapZ - paintRadius, 0, data.alphamapHeight - paintSize);

            float[,,] alphas = data.GetAlphamaps(startX, startZ, paintSize, paintSize);

            for (int x = 0; x < paintSize; x++)
            {
                for (int z = 0; z < paintSize; z++)
                {
                    float dist = Vector2.Distance(new Vector2(x, z), new Vector2(paintRadius, paintRadius));
                    if (dist <= paintRadius)
                    {
                        for (int i = 0; i < data.alphamapLayers; i++)
                            alphas[z, x, i] = (i == indiceCapaHumedad) ? 1f : 0f;
                    }
                }
            }

            data.SetAlphamaps(startX, startZ, alphas);
        }
    }

    private void ConfigurarMision()
    {
        switch (misionActual)
        {
            case MisionRiego.Mision1:
                cultivo = "Lechuga";
                suelo = "Arenoso";
                clima = "Muy caluroso";
                tipoRiego = "Goteo";
                frecuenciaRiego = "Diario";
                horarioRiego = "Mañana o atardecer";
                radioRiego = 3f;
                intervaloDeActualizacion = 0.3f;
                velocidadUmbral = 0.05f;
                break;

            case MisionRiego.Mision2:
                cultivo = "Trigo";
                suelo = "Arcilloso del sur";
                clima = "Frío y muy húmedo";
                tipoRiego = "Aspersión";
                frecuenciaRiego = "Cada 10 días";
                horarioRiego = "Sin restricción";
                radioRiego = 7f;
                intervaloDeActualizacion = 0.8f;
                velocidadUmbral = 0.1f;
                break;

            case MisionRiego.Mision3:
                cultivo = "Maíz";
                suelo = "Franco profundo";
                clima = "Cálido y seco";
                tipoRiego = "Aspersión";
                frecuenciaRiego = "Cada 4-5 días";
                horarioRiego = "Preferencia tarde";
                radioRiego = 6f;
                intervaloDeActualizacion = 0.5f;
                velocidadUmbral = 0.1f;
                break;
        }
    }
}
