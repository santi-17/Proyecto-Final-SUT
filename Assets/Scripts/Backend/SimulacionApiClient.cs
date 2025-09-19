using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SimulacionApiClient : MonoBehaviour
{
    [Serializable]
    public class SimulacionCreateDto
    {
        // nombres  que espera el backend 
        public int moduloId;
        public int usuarioId;
        public float tiempoSimulacion; // segundos con decimales
        public int cobertura1;         // 0..100
        public int cobertura2;         // 0..100
        public int cobertura3;         // 0..100
    }

    // se llama cuando termine la simulación
    public void EnviarSimulacion(int moduloId, int usuarioId, float tiempoSegundos, int c1, int c2, int c3)
    {
        var dto = new SimulacionCreateDto
        {
            moduloId = moduloId,
            usuarioId = usuarioId,
            tiempoSimulacion = tiempoSegundos,
            cobertura1 = c1,
            cobertura2 = c2,
            cobertura3 = c3
        };

        StartCoroutine(PostSimulacion(dto));
    }

    private IEnumerator PostSimulacion(SimulacionCreateDto dto)
    {
        
        const string url = "http://localhost:5200/api/simulaciones";

        string json = JsonUtility.ToJson(dto);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[POST /api/simulaciones] {req.responseCode} {req.error}\n{req.downloadHandler.text}");
            }
            else
            {
                Debug.Log($"[POST /api/simulaciones] OK {req.responseCode}\n{req.downloadHandler.text}");
            }
        }
    }
}
