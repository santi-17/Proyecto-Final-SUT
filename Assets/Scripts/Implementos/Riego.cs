using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System;

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
    private Vector3 offsetEtiqueta = new Vector3(0, 6f, 0);

    [Header("Configuración de Riego")]
    public int indiceCapaHumedad = 1;
    private float radioRiego;
    private float intervaloDeActualizacion;
    private float velocidadUmbral;
    private string tipoRiego;
    private string frecuenciaRiego;
    private string horarioRiego;
    private string cultivo;
    
    private TerrainData data;
    private float tiempoUltimaActualizacion;
    private Vector3 ultimaPosicion;
    private float velocidadActual;

    private TMP_Text etiquetaRiego;

    [Header("UI")]
    [SerializeField] private GameObject cartelAdvertencia;
    [SerializeField] private TextMeshProUGUI textoAdvertencia;

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
        etiquetaRiego.fontSize = 8f;
        etiquetaRiego.color = Color.white; //new Color(0.2f, 0.7f, 1f);
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

        if (riegoActivo)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 10f))
            {
                Terrain hitTerrain = hit.collider.GetComponent<Terrain>();

                if (hitTerrain == null)
                {
                    return;
                }

                TerrainInfo info = hitTerrain.GetComponent<TerrainInfo>();

                if (info != null &&
                    (!string.Equals(info.tipoEsperado, tipoRiego, StringComparison.OrdinalIgnoreCase) ||
                     !string.Equals(info.cultivoEsperado, cultivo, StringComparison.OrdinalIgnoreCase)))
                {
                    MostrarAdvertencia($"Atención: este terreno requiere una regadora '{info.tipoEsperado}' para '{info.cultivoEsperado}', no '{tipoRiego}' para '{cultivo}'.\n¡Por favor cambia la herramienta!");
                    return;
                }

                //OcultarAdvertencia(); //Si está todo bien, ocultar

                // Solo pintar si velocidad > umbral
                if (velocidadActual > velocidadUmbral && Time.time - tiempoUltimaActualizacion >= intervaloDeActualizacion)
                {
                    PintarTerreno(hitTerrain); 
                    tiempoUltimaActualizacion = Time.time;
                }
            }
            else
            {
                MostrarAdvertencia(" No estás sobre ningún terreno.");
            }
        }
        //else EsconderCartel();
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

    void PintarTerreno(Terrain hitTerrain)
    {
        //if (terreno == null || data == null) return;
        if(hitTerrain == null) return;

        TerrainData dataLocal = hitTerrain.terrainData;
        if (dataLocal == null) return;

        float sizeX = Mathf.Max(dataLocal.size.x, 0.0001f);
        float sizeZ = Mathf.Max(dataLocal.size.z, 0.0001f);

        int paintRadius = Mathf.RoundToInt((radioRiego / sizeX) * dataLocal.alphamapWidth);
        paintRadius = Mathf.Max(paintRadius, 1);
        int paintSize = paintRadius * 2 + 1;

        foreach (var aspersor in aspersores)
        {
            if (aspersor == null) continue;

            Vector3 posicion = aspersor.transform.position - hitTerrain.transform.position;
            int mapX = Mathf.RoundToInt((posicion.x / sizeX) * dataLocal.alphamapWidth);
            int mapZ = Mathf.RoundToInt((posicion.z / sizeZ) * dataLocal.alphamapHeight);

            int startX = Mathf.Clamp(mapX - paintRadius, 0, dataLocal.alphamapWidth - paintSize);
            int startZ = Mathf.Clamp(mapZ - paintRadius, 0, dataLocal.alphamapHeight - paintSize);

            float[,,] alphas = dataLocal.GetAlphamaps(startX, startZ, paintSize, paintSize);

            for (int x = 0; x < paintSize; x++)
            {
                for (int z = 0; z < paintSize; z++)
                {
                    float dist = Vector2.Distance(new Vector2(x, z), new Vector2(paintRadius, paintRadius));
                    if (dist <= paintRadius)
                    {
                        for (int i = 0; i < dataLocal.alphamapLayers; i++)
                            alphas[z, x, i] = (i == indiceCapaHumedad) ? 1f : 0f;
                    }
                }
            }

            dataLocal.SetAlphamaps(startX, startZ, alphas);
        }
    }

    private void ConfigurarMision()
    {
        switch (misionActual)
        {
            case MisionRiego.Mision1:
                cultivo = "Lechuga";
                tipoRiego = "Goteo";
                frecuenciaRiego = "Diario";
                horarioRiego = "Mañana o atardecer";
                radioRiego = 3f;
                intervaloDeActualizacion = 0.3f;
                velocidadUmbral = 0.05f;
                break;

            case MisionRiego.Mision2:
                cultivo = "Trigo";
                tipoRiego = "Aspersión";
                frecuenciaRiego = "Cada 10 días";
                horarioRiego = "Sin restricción";
                radioRiego = 7f;
                intervaloDeActualizacion = 0.8f;
                velocidadUmbral = 0.1f;
                break;

            case MisionRiego.Mision3:
                cultivo = "Maíz";
                tipoRiego = "Aspersión";
                frecuenciaRiego = "Cada 4-5 días";
                horarioRiego = "Preferencia tarde";
                radioRiego = 6f;
                intervaloDeActualizacion = 0.5f;
                velocidadUmbral = 0.1f;
                break;
        }
    }


    private void MostrarAdvertencia(string mensaje)
    {
        if (cartelAdvertencia == null || textoAdvertencia == null)
        {
            Debug.LogError("[Riego] No se asignó el CartelAdvertencia o el TextoAdvertencia en el inspector.");
            return;
        }

        textoAdvertencia.text = mensaje;
        cartelAdvertencia.SetActive(true);

        // Si ya hay una corrutina de ocultar en curso, la reiniciamos
        StopCoroutine(nameof(EsconderCartel));
        StartCoroutine(EsconderCartel(5f));
    }

    private IEnumerator EsconderCartel(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (cartelAdvertencia != null)
            cartelAdvertencia.SetActive(false);
    }
}
