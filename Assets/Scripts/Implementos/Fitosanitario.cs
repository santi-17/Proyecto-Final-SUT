using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Fitosanitario : MonoBehaviour
{
    public enum MisionFitosanitario { Mision1, Mision2, Mision3 }
    public MisionFitosanitario misionActual = MisionFitosanitario.Mision1;

    public KeyCode teclaActivar = KeyCode.T; // La tecla que se debe presionar para activar el fitosanitario
    public bool fitosanitarioActivo = false; // El estado del fitosanitario, activo o inactivo


    public ParticleSystem[] aspersores; // Array de las particulas de los aspersores que se activarán al presionar la tecla
    public Terrain terreno; // El terreno donde se aplicará el fitosanitario
    [SerializeField] private Vector3 offsetEtiqueta = new Vector3(0, 3.5f, 0); // Offset para la etiqueta de estado del fitosanitario

    private int indiceCapaHumedad = 1; // El índice de la capa de humedad en el terreno
    private float radioFitosanitario; // El radio del área del fitosanitario
    private float intervaloDeActualizacion; // Intervalo de actualización del fitosanitario
    private float velocidadUmbral; // Velocidad mínima para que el riego funcione
    private string cultivo;
    private string producto;
    private string tipoProducto;
    private string momentoAplicacion;
    private string suelo;
    private string clima;

    private TerrainData data;
    private float tiempoUltimaActualizacion; // Tiempo de la última actualización del fitosanitario 
    private Vector3 ultimaPosicion;
    private float velocidadActual;
    private TMP_Text etiquetaFitosanitario;

    // Start is called before the first frame update
    void Start()
    {
        ConfigurarMision();

        if (terreno == null)
        {
            terreno = FindObjectOfType<Terrain>(); // Auto-busca el Terrain si no está asignado
            if (terreno == null)
            {
                Debug.LogError("[Fitosanitario] No se encontró ningún Terrain en la escena. Asigna uno manualmente.");
                enabled = false;
                return;
            }
        }
        data = terreno.terrainData;
        if (data == null)
        {
            Debug.LogError("[Fitosanitario] El Terrain no tiene TerrainData asignado.");
            enabled = false;
            return;
        }
        if (aspersores == null || aspersores.Length == 0)
            Debug.LogWarning("[Fitosanitario] No hay aspersores asignados. El fitosanitario no tendrá efecto visual.");
        // Verificación: Asegura que haya al menos 2 capas (0 y 1)
        if (data.alphamapLayers <= indiceCapaHumedad)
        {
            Debug.LogWarning($"[Fitosanitario] Índice de capa {indiceCapaHumedad} fuera de rango (capas totales: {data.alphamapLayers}). Se usará 0.");
            indiceCapaHumedad = Mathf.Clamp(indiceCapaHumedad, 0, data.alphamapLayers - 1);
        }

        // Crear etiqueta visual
        GameObject etiquetaObj = new GameObject("EtiquetaFitosanitario"); 
        etiquetaObj.transform.SetParent(transform);
        etiquetaObj.transform.localPosition = offsetEtiqueta;

        etiquetaFitosanitario = etiquetaObj.AddComponent<TextMeshPro>();
        etiquetaFitosanitario.alignment = TextAlignmentOptions.Center;
        etiquetaFitosanitario.fontSize = 8f;
        etiquetaFitosanitario.color = new Color(0.1f, 0.9f, 0.3f);
        etiquetaFitosanitario.text = $"{tipoProducto}\n{producto}\n{cultivo}\nMomento: {momentoAplicacion}";

        ultimaPosicion = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(teclaActivar))
        {
            fitosanitarioActivo = !fitosanitarioActivo;
            ActivarAspersores(fitosanitarioActivo);
            Debug.Log($"[Fitosanitario] Activado: {fitosanitarioActivo}");
        }
        if(ultimaPosicion != Vector3.zero)
            velocidadActual = (transform.position - ultimaPosicion).magnitude / Mathf.Max(Time.deltaTime, 0.0001f) ;
        // Calcular velocidad (distancia recorrida por segundo)
        ultimaPosicion = transform.position;

        if (fitosanitarioActivo && velocidadActual > velocidadUmbral && Time.time - tiempoUltimaActualizacion >= intervaloDeActualizacion)
        {
            PintarTerreno();
            tiempoUltimaActualizacion = Time.time;
        }

    }

    private void LateUpdate()
    {
        if (etiquetaFitosanitario != null && Camera.main != null)
        {
            etiquetaFitosanitario.transform.LookAt(Camera.main.transform); // Hace que la etiqueta siempre mire a la cámara
            etiquetaFitosanitario.transform.Rotate(0, 180f, 0); // Ajuste para que no quede invertido
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
        if (terreno == null || data == null)
        {
            Debug.LogError("[Fitosanitario] No hay terrain valido para pintar.");
            return;
        }
        float sizeX = Mathf.Max(data.size.x, 0.0001f);
        float sizeZ = Mathf.Max(data.size.z, 0.0001f);

        int paintRadius = Mathf.RoundToInt((radioFitosanitario / sizeX) * data.alphamapWidth); // tamaño del área a pintar
        paintRadius = Mathf.Max(paintRadius, 1); // Asegura que el radio sea al menos 1
        int paintSize = paintRadius * 2 + 1; // tamaño del área a pintar (diámetro)
     
        foreach (var aspersor in aspersores)
        {
            if (aspersor == null) continue;
            Vector3 posicion = aspersor.transform.position - terreno.transform.position;

            int mapX = Mathf.RoundToInt((posicion.x / sizeX) * data.alphamapWidth);
            int mapZ = Mathf.RoundToInt((posicion.z / sizeZ) * data.alphamapHeight);


            //clamp para evitar que se salga del terreno
            int StartX = Mathf.Clamp(mapX - paintRadius, 0, data.alphamapWidth - paintSize);
            int StartZ = Mathf.Clamp(mapZ - paintRadius, 0, data.alphamapHeight - paintSize);

            if (StartX < 0 || StartZ < 0) continue;

            float[,,] alphas = data.GetAlphamaps(StartX, StartZ, paintSize, paintSize);

            for (int x = 0; x < paintSize; x++) 
            {
                for (int z = 0; z < paintSize; z++)
                {
                    float dist = Vector2.Distance (new Vector2(x, z), new Vector2(paintRadius, paintRadius)); // Distancia al centro del área afectada
                    if (dist <= paintRadius)
                    {
                        for (int i = 0; i < data.alphamapLayers; i++)
                        {
                            alphas[z, x, i] = (i == indiceCapaHumedad) ? 1f : 0f;
                        }
                    }
                }
            }
            data.SetAlphamaps(StartX, StartZ, alphas);
        }
    }

    private void ConfigurarMision()
    {
        switch (misionActual)
        {
            case MisionFitosanitario.Mision1:
                radioFitosanitario = 6f;
                intervaloDeActualizacion = 0.4f;
                velocidadUmbral = 0.05f;
                cultivo = "Soja con malezas";
                producto = "Glifosato";
                tipoProducto = "Herbicida Sistémicco";
                momentoAplicacion = "Pre-siembra";
                suelo = "Franco-arcilloso";
                clima = "Templado";
                break;
            case MisionFitosanitario.Mision2:
                radioFitosanitario = 7f;
                intervaloDeActualizacion = 0.6f;
                velocidadUmbral = 0.1f;
                cultivo = "Trigo con roya";
                producto = "Triazoles";
                tipoProducto = "Fungicida";
                momentoAplicacion = "Al aparecer síntomas";
                suelo = "Fértil y húmedo";
                clima = "Lluvioso";
                break;
            case MisionFitosanitario.Mision3:
                radioFitosanitario = 5f;
                intervaloDeActualizacion = 0.5f;
                velocidadUmbral = 0.08f;
                cultivo = "Huerta de tomate con plaga";
                producto = "De contacto";
                tipoProducto = "Insecticida";
                momentoAplicacion = "Al detectar plaga";
                suelo = "Liviano de horticultura";
                clima = "Cálido y húmedo";
                break;
        }
    }
}
