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
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
