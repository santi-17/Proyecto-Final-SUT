using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class ConfiguracionArado
{
    public string nombreMision;
    public string tipoArado;
    public string descripcionSuelo;
    public float profundidadSurco;
    public int materialAradoIndex;
    public Color colorParticulas;
    public float tiempoEntreArados;
}

public class Arado : MonoBehaviour
{
    public KeyCode activarArado = KeyCode.F;
    [SerializeField] private Transform puntoRaycast;
    [SerializeField] private Terrain terrain;
    [SerializeField] private ParticleSystem particulasTierra;
    [SerializeField] private Transform modeloVisual;

    [Header("Configuración General")]
    public float distanciaDeteccion = 5f;
    public LayerMask Suelo;
    public int size = 70;

    private bool aradoActivo = false;
    private Coroutine movimientoArado;
    private float temporizadorArado = 0f;
    private Vector3 ultimaPosicion;
    private float velocidadDeMovimiento;
    private ParticleSystem instanciaParticulas;

    [Header("Animación Visual")]
    [SerializeField] private float alturaReposo = 0f;
    [SerializeField] private float alturaTrabajo = -0.7f;
    [SerializeField] private float velocidadMovimiento = 2f;

    [Header("Configuraciones por misión")]
    public int misionActual = 1;
    public List<ConfiguracionArado> configuraciones = new List<ConfiguracionArado>();

    [Header("Etiqueta visual")]
    [SerializeField] private TextMeshPro etiquetaTipoArado;
    [SerializeField] private Vector3 offsetEtiqueta = new Vector3(0, 3.5f, 0);

    private ConfiguracionArado configActiva;

    void Start()
    {
        if (terrain == null)
        {
            Debug.LogError("[Arado] No se asignó un Terrain.");
            enabled = false;
            return;
        }

        // Crear configuraciones si no existen
        if (configuraciones.Count == 0)
        {
            configuraciones.AddRange(new[]
            {
                new ConfiguracionArado {
                    nombreMision = "Litoral Oeste (Vertedera)",
                    tipoArado = "Vertedera",
                    descripcionSuelo = "Franco-arcilloso profundo, húmedo moderado",
                    profundidadSurco = 0.0020f,
                    materialAradoIndex = 1,
                    colorParticulas = new Color(0.25f, 0.15f, 0.05f),
                    tiempoEntreArados = 0.1f
                },
                new ConfiguracionArado {
                    nombreMision = "Norte Seco (Cincel)",
                    tipoArado = "Cincel",
                    descripcionSuelo = "Arenoso, clima caluroso y seco",
                    profundidadSurco = 0.0015f,
                    materialAradoIndex = 1,
                    colorParticulas = new Color(0.7f, 0.6f, 0.4f),
                    tiempoEntreArados = 0.15f
                },
                new ConfiguracionArado {
                    nombreMision = "Zona Hortícola (Superficial)",
                    tipoArado = "Superficial",
                    descripcionSuelo = "Franco-limoso húmedo, templado",
                    profundidadSurco = 0.00007f,
                    materialAradoIndex = 1,
                    colorParticulas = new Color(0.4f, 0.3f, 0.15f),
                    tiempoEntreArados = 0.08f
                }
            });
        }

        int index = Mathf.Clamp(misionActual - 1, 0, configuraciones.Count - 1);
        configActiva = configuraciones[index];

        Debug.Log($"[Arado] Misión: {configActiva.nombreMision}");

        if (particulasTierra != null)
        {
            instanciaParticulas = Instantiate(particulasTierra, transform);
            var main = instanciaParticulas.main;
            main.startColor = configActiva.colorParticulas;
            instanciaParticulas.Stop();
        }

        if (etiquetaTipoArado == null)
        {
            GameObject textoObj = new GameObject("EtiquetaTipoArado");
            textoObj.transform.SetParent(transform);
            textoObj.transform.localPosition = offsetEtiqueta;

            etiquetaTipoArado = textoObj.AddComponent<TextMeshPro>();
            etiquetaTipoArado.alignment = TextAlignmentOptions.Center;
            etiquetaTipoArado.fontSize = 8f;
            etiquetaTipoArado.color = Color.yellow;
        }

        etiquetaTipoArado.text = configActiva.tipoArado;
    }

    void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        velocidadDeMovimiento = (transform.position - ultimaPosicion).magnitude / dt;
        ultimaPosicion = transform.position;

        if (Input.GetKeyDown(activarArado))
        {
            aradoActivo = !aradoActivo;
            Debug.Log($"[Arado] {(aradoActivo ? "Activado" : "Desactivado")} ({configActiva.tipoArado})");

            if (movimientoArado != null) StopCoroutine(movimientoArado);
            movimientoArado = StartCoroutine(MoverArado(aradoActivo ? alturaTrabajo : alturaReposo));
        }

        if (!aradoActivo) return;
        if (velocidadDeMovimiento < 0.1f) return;

        if (instanciaParticulas != null)
        {
            if (aradoActivo && !instanciaParticulas.isPlaying) instanciaParticulas.Play();
            if (!aradoActivo && instanciaParticulas.isPlaying) instanciaParticulas.Stop();
        }

        temporizadorArado -= Time.deltaTime;
        if (temporizadorArado <= 0f)
        {
            ArarTerreno();
            temporizadorArado = configActiva.tiempoEntreArados;
        }
    }
    void LateUpdate()
    {
        if (etiquetaTipoArado != null && Camera.main != null)
        {
            // Hacer que mire hacia la cámara, pero no al revés
            etiquetaTipoArado.transform.LookAt(Camera.main.transform);
            etiquetaTipoArado.transform.Rotate(0, 180f, 0); // 🔄 Invierte el texto para que no se vea al revés
        }
    }

    private IEnumerator MoverArado(float destinoY)
    {
        Vector3 inicio = modeloVisual.localPosition;
        Vector3 destino = new Vector3(inicio.x, destinoY, inicio.z);
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * velocidadMovimiento;
            modeloVisual.localPosition = Vector3.Lerp(inicio, destino, t);
            yield return null;
        }
    }

    private void ArarTerreno()
    {
        if (!Physics.Raycast(puntoRaycast.position, Vector3.down, out RaycastHit hit, distanciaDeteccion))
            return;

        Vector3 terrainPos = hit.point - terrain.transform.position;
        TerrainData data = terrain.terrainData;

        // --- PINTADO (TEXTURA) ---
        float sizeX = Mathf.Max(data.size.x, 0.0001f);
        float sizeZ = Mathf.Max(data.size.z, 0.0001f);

        int alphamapX = (int)((terrainPos.x / sizeX) * data.alphamapWidth);
        int alphamapZ = (int)((terrainPos.z / sizeZ) * data.alphamapHeight);

        int startAlphaX = Mathf.Clamp(alphamapX - size / 2, 0, data.alphamapWidth - 1);
        int startAlphaZ = Mathf.Clamp(alphamapZ - size / 2, 0, data.alphamapHeight - 1);

        int alphamapWidth = Mathf.Min(size, data.alphamapWidth - startAlphaX);
        int alphamapHeight = Mathf.Min(size, data.alphamapHeight - startAlphaZ);

        float[,,] splatmap = data.GetAlphamaps(startAlphaX, startAlphaZ, alphamapWidth, alphamapHeight);

        for (int x = 0; x < alphamapWidth; x++)
        {
            for (int z = 0; z < alphamapHeight; z++)
            {
                for (int i = 0; i < data.alphamapLayers; i++)
                    splatmap[x, z, i] = 0;

                splatmap[x, z, configActiva.materialAradoIndex] = 1;
            }
        }
        data.SetAlphamaps(startAlphaX, startAlphaZ, splatmap);

        // --- DEFORMACIÓN (ALTURA) ---
        int heightmapX = (int)((terrainPos.x / sizeX) * data.heightmapResolution);
        int heightmapZ = (int)((terrainPos.z / sizeZ) * data.heightmapResolution);

        int startX = Mathf.Clamp(heightmapX - size / 2, 0, data.heightmapResolution - 1);
        int startZ = Mathf.Clamp(heightmapZ - size / 2, 0, data.heightmapResolution - 1);

        int heightmapWidth = Mathf.Min(size, data.heightmapResolution - startX);
        int heightmapHeight = Mathf.Min(size, data.heightmapResolution - startZ);

        float[,] heights = data.GetHeights(startX, startZ, heightmapWidth, heightmapHeight);
        float mitadX = Mathf.Max(heightmapWidth / 2f, 0.0001f);
        float mitadZ = Mathf.Max(heightmapHeight / 2f, 0.0001f);

        for (int x = 0; x < heightmapWidth; x++)
        {
            for (int z = 0; z < heightmapHeight; z++)
            {
                float distanciaCentroX = Mathf.Abs(x - mitadX) / mitadX;
                float distanciaCentroZ = Mathf.Abs(z - mitadZ) / mitadZ;
                float distanciaCentro = Mathf.Max(distanciaCentroX, distanciaCentroZ);

                float deformacion = 0f;
                if (distanciaCentro < 0.2f)
                    deformacion = -configActiva.profundidadSurco * (1f - distanciaCentro * 5f);
                else if (distanciaCentro < 0.6f)
                    deformacion = configActiva.profundidadSurco * 0.5f * (1 - Mathf.Abs(distanciaCentro - 0.4f) * 5f);

                heights[z, x] = Mathf.Clamp01(heights[z, x] + deformacion);
            }
        }
        data.SetHeights(startX, startZ, heights);
    }
}