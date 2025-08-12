using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AradoDisco : MonoBehaviour
{
    public KeyCode activarArado = KeyCode.G; // Tecla para activar el arado
    [SerializeField] private Transform puntoRaycast;
    [SerializeField] private Terrain terrain;
    [SerializeField] private ParticleSystem particulasTierra; // Partículas de tierra al arado de disco
    private ParticleSystem instanciaParticulas; // Instancia de las partículas de tierra

    public float distanciaDeteccion = 5f; // Distancia de detección del arado
    public LayerMask Suelo; // Capa del suelo para detectar colisiones
    public int materialAradoIndex = 1; // Índice del material del arado en el array de materiales

    public float profundidadSurco = 0.002f; // Profundidad del arado en el terreno
    public int size = 70; // Tamaño del área afectada por el arado

    bool aradoActivo = false; // Estado del arado

    //animacion del arado de disco
    [SerializeField] private Transform modeloVisual; // Parte visual del arado que se baja
    [SerializeField] private float alturaReposo = 0f; // Altura original del arado
    [SerializeField] private float alturaTrabajo = -1.2f; // Altura cuando está arando
    [SerializeField] private float velocidadMovimiento = 2f; // Velocidad de bajada/subida

    private Coroutine movimientoArado;
    private float tiempoEntreArados = 0.25f; // Tiempo entre cada arado
    private float temporizadorArado = 0f;

    private Vector3 ultimaPosicion;
    private float velocidadDeMovimiento;


    // Start is called before the first frame update
    void Start()
    {
        ultimaPosicion = transform.position;
        if (particulasTierra != null)
        {
            instanciaParticulas = Instantiate(particulasTierra, transform);
            instanciaParticulas.Stop();
        }
    }

    // Update is called once per frame
    void Update()
    {
        velocidadDeMovimiento = (transform.position - ultimaPosicion).magnitude / Time.deltaTime;
        ultimaPosicion = transform.position;
        if (Input.GetKeyDown(activarArado))
        {
            aradoActivo = !aradoActivo; // Cambiar el estado del arado
            if (movimientoArado != null) StopCoroutine(movimientoArado);
            float destinoY = aradoActivo ? alturaTrabajo : alturaReposo;
            movimientoArado = StartCoroutine(MoverArado(destinoY));
        }

        if (!aradoActivo)
        {
            terrain.Flush();
            return; // Si el arado no está activo, salir del método
        }
        if (velocidadMovimiento < 0.1f) {return;}

        // Si el arado está activo, reproducir las partículas de tierra
        if (aradoActivo && instanciaParticulas != null && !instanciaParticulas.isPlaying) // Verifica si las partículas no están reproduciéndose
        {
            instanciaParticulas.transform.position = puntoRaycast.position; // Asegurarse de que las partículas se posicionen correctamente
            instanciaParticulas.Play(); // Reproducir las partículas de tierra
        }
        else if (!aradoActivo && instanciaParticulas != null && instanciaParticulas.isPlaying) // Si el arado no está activo, detener las partículas
        {
            instanciaParticulas.Stop(); // Detener las partículas de tierra
        }

        if (aradoActivo)
        {
            temporizadorArado -= Time.deltaTime;
            if (temporizadorArado <= 0f)
            {
                if (velocidadDeMovimiento > 0.1f) ArarTerreno();
                else return;
                
                temporizadorArado = tiempoEntreArados;
            }
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
        if (Physics.Raycast(puntoRaycast.position, Vector3.down, out RaycastHit hit, distanciaDeteccion))
        {
            Debug.DrawRay(puntoRaycast.position, Vector3.down * distanciaDeteccion, Color.red);

            if (terrain != null)
            {

                Vector3 terrainPos = hit.point - terrain.transform.position;
                TerrainData data = terrain.terrainData;

                int heightmapX = (int)((terrainPos.x / data.size.x) * data.heightmapResolution);
                int heightmapZ = (int)((terrainPos.z / data.size.z) * data.heightmapResolution);

                // Asegurarse de que el área afectada no se salga de los límites del heightmap
                int startX = Mathf.Clamp(heightmapX - size / 2, 0, data.heightmapResolution - 1);
                int startZ = Mathf.Clamp(heightmapZ - size / 2, 0, data.heightmapResolution - 1);

                //pinto la textura del terreno

                int alphamapX = (int)((terrainPos.x / data.size.x) * data.alphamapWidth);
                int alphamapZ = (int)((terrainPos.z / data.size.z) * data.alphamapHeight);

                int startAlphaX = Mathf.Clamp(alphamapX - size / 2, 0, data.alphamapWidth - 1);
                int startAlphaZ = Mathf.Clamp(alphamapZ - size / 2, 0, data.alphamapHeight - 1);

                //Cambiar la textura del terreno (splatmap)
                float[,,] splatmap = data.GetAlphamaps(startAlphaX, startAlphaZ, size, size);

                for (int x = 0; x < size; x++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        for (int i = 0; i < data.alphamapLayers; i++)
                            splatmap [x, z, i] = 0;

                        splatmap[x, z, materialAradoIndex] = 1;
                    }
                }

                data.SetAlphamaps(startAlphaX, startAlphaZ, splatmap);

                //2. deformar el terreno (heigthmap)
                float[,] heights = data.GetHeights(startX, startZ, size, size);

                for (int x = 0; x < size; x++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        float distanciaCentro = Vector2.Distance(new Vector2(x, z), new Vector2(size / 2f, size / 2f)); // Distancia al centro del área afectada

                        float falloff = Mathf.Clamp01(1f - (distanciaCentro / (size / 2f))); // Factor de caída basado en la distancia al centro

                        float factor = Random.Range(0.7f, 1.3f); //Le doy irregularidad al terreno //1f - (distanciaCentro / (size / 2f));

                        heights[z, x] -= profundidadSurco * falloff * factor; // Reducir la altura del terreno
                        heights[z, x] = Mathf.Clamp01(heights[z, x]); // Asegurarse de que la altura no se salga de los límites

                    }
                }

                data.SetHeights(startX, startZ, heights);

               //   terrain.Flush();

            }
            else
            {
                Debug.LogWarning("El objeto no tiene un Renderer para cambiar el material.");
            }
            if (instanciaParticulas != null && !instanciaParticulas.isPlaying)
            {
                instanciaParticulas.transform.position = puntoRaycast.position;
                instanciaParticulas.Play();
            }
        }
        else
        {
            // Si no se detecta el suelo, desactivar el arado
            Debug.Log("No hay suelo debajo del arado");
        }
    }
}
