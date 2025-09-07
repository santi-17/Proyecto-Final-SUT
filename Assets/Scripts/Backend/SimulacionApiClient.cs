using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulacionApiClient : MonoBehaviour
{

    [System.Serializable]
    public class ResultadoSimulacionDto
    {
        public int simualcionId;
        public float cobertura1;
        public float cobertura2;
        public float cobertura3;
    }

    public void EnviarResultado(int simulacionId, float c1, float c2, float c3)
    {
        ResultadoSimulacionDto resultado = new ResultadoSimulacionDto
        {
            simualcionId = simulacionId,
            cobertura1 = c1,
            cobertura2 = c2,
            cobertura3 = c3
        };

        StartCoroutine(PostResultado(resultado));
    }

    private IEnumerator PostResultado(ResultadoSimulacionDto resultado)
    {
        string url = "http://localhost:5200/api/simulador/resultadoSimulacion"; // Reemplaza con la URL real de la API
        string jsonData = JsonUtility.ToJson(resultado);

        using (UnityEngine.Networking.UnityWebRequest request = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError || request.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error al enviar resultado: " + request.error);
            }
            else
            {
                Debug.Log("Resultado enviado exitosamente: " + request.downloadHandler.text);
            }
        }
    }
    [System.Serializable]
    public class SimulacionCreateDto
    {
        public string moduloSlug;
        public bool cobertura;
    }
    [System.Serializable]
    public class SimulacionCreateResponseDto
    {
        public int simulacionId;
        public int moduloiD;
        public int panelControlId;
        public bool cobertura;
    }

    public void CrearSimulacion(string moduloSlug)
    {
        SimulacionCreateDto dto = new SimulacionCreateDto
        {
            moduloSlug = moduloSlug,
            cobertura = false // o true si quieres
        };
        StartCoroutine(PostCrearSimulacion(dto));
    }

    private IEnumerator PostCrearSimulacion(SimulacionCreateDto dto)
    {
        string url = "http://localhost:5200/api/simulador"; // POST para crear simulacion
        string jsonData = JsonUtility.ToJson(dto);
        using (UnityEngine.Networking.UnityWebRequest request = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError || request.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error al crear simulación: " + request.error);
            }
            else
            {
                Debug.Log("Simulación creada: " + request.downloadHandler.text);
                // Parsear respuesta para obtener simulacionId
                SimulacionCreateResponseDto response = JsonUtility.FromJson<SimulacionCreateResponseDto>(request.downloadHandler.text);
                // Guardar el id para usarlo luego
                //simulacionId = response.simulacionId;
            }
        }

    }
}