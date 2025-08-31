using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SimulacionControlador : MonoBehaviour
{
    public SimulacionApiClient apiClient;

    public TextMeshProUGUI porcentajeTerreno1Text;
    public TextMeshProUGUI porcentajeTerreno2Text;
    public TextMeshProUGUI porcentajeTerreno3Text;

    public int simulacionId;
    // Start is called before the first frame update

    public void EnviarResultadosSimulacion()
    {
        // Actualizar la interfaz de usuario
        float c1 = ExtraerPorcentaje (porcentajeTerreno1Text.text);
        float c2 = ExtraerPorcentaje (porcentajeTerreno2Text.text);
        float c3 = ExtraerPorcentaje (porcentajeTerreno3Text.text);
        // Enviar los resultados a la API
        apiClient.EnviarResultado(simulacionId, c1, c2, c3);
    }

    // Función para extraer el valor numérico del porcentaje desde el texto
    private float ExtraerPorcentaje(string texto)
    {
        try
        {
            int indiceDosPuntos = texto.IndexOf(':');
            if (indiceDosPuntos < 0)
                return 0f;
            // Tomamos la subcadena después de ":"
            string subcadena = texto.Substring(indiceDosPuntos + 1).Trim();
            // Quitamos el símbolo de porcentaje si existe
            subcadena = subcadena.Replace("%", "").Trim();
            // Parseamos a float
            if (float.TryParse(subcadena, out float valor))
                return valor;
            else
                return 0f;
        }
        catch
        {
            return 0f;
        }
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
