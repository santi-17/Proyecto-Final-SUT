using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class SimulacionManager : MonoBehaviour
{
    public TextMeshProUGUI porcentajeTerreno1Text;
    public TextMeshProUGUI porcentajeTerreno2Text;
    public TextMeshProUGUI porcentajeTerreno3Text;
    public Button enviarButton;

    private string moduloSlug; // nombre de la escena actual
    private int userId; // id del usuario

    // Clase para parsear respuesta API
    [System.Serializable]
    public class RespuestaSimulacionApi
    {
        public int simulacionId;
        public int moduloiD;
        public int panelControlId;
        public bool cobertura;
    }

    [System.Obsolete]
    void Start()
    {
        moduloSlug = SceneManager.GetActiveScene().name; // obtener nombre de la escena
        //Notificar a react que el gameObject esta listo
        Application.ExternalCall("onSimulacionManagerReady");
    }
    public void SetUserId(string id)
    {
        Debug.Log("SetUser Id llamado con id: " + id);
        if (int.TryParse(id, out int parsedId))
        {
            userId = parsedId;
            Debug.Log("User  Id recibido y parseado: " + userId);
        }
        else
        {
            Debug.LogWarning("User Id inválido recibido: " + id);
        }
    }
    public void EnviarSimulacion()
    {
        Debug.Log("EnviarSimulacion llamado");
        //deshabilitar boton para evitar multiples envios
        if (enviarButton != null)
            enviarButton.interactable = false;

        float c1 = ExtraerPorcentaje(porcentajeTerreno1Text.text);
        float c2 = ExtraerPorcentaje(porcentajeTerreno2Text.text);
        float c3 = ExtraerPorcentaje(porcentajeTerreno3Text.text);

        bool aprobado = (c1 >= 75f) && (c2 >= 75f) && (c3 >= 75f);

        SimulacionCreateDTO dto = new SimulacionCreateDTO
        {
            ModuloSlug = moduloSlug,
            Cobertura = aprobado,
            UsuarioId = userId
        };

        StartCoroutine(PostSimulacion(dto));
    }

    private float ExtraerPorcentaje(string texto)
    {
        int idx = texto.IndexOf(':');
        if (idx < 0) return 0f;
        string sub = texto.Substring(idx + 1).Replace("%", "").Trim();
        if (float.TryParse(sub, out float val))
            return val;
        return 0f;
    }

    private IEnumerator PostSimulacion(SimulacionCreateDTO dto)
    {
        string url = "http://localhost:5200/api/simulador/resultadoSimulacion"; // URL de la API para crear simulacion
        string json = JsonUtility.ToJson(dto);
        Debug.Log("JSON a enviar: " + json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al enviar simulación: " + request.error + "-" + request.downloadHandler.text);
                // Rehabilitar el botón para permitir reintentos
                if (enviarButton != null)
                    enviarButton.interactable = true;
            }
            else
            {
                Debug.Log("Simulación enviada correctamente: " + request.downloadHandler.text);

                //respuesta de la API 
                var respuesta = JsonUtility.FromJson<RespuestaSimulacionApi>(request.downloadHandler.text);

                //guardar los datos de la escena para el resultado
                ResultadoSimulacionData.Aprobado = respuesta.cobertura;
                ResultadoSimulacionData.ModuloNombre = dto.ModuloSlug;
                ResultadoSimulacionData.EscenaAnterior = SceneManager.GetActiveScene().name;

                //cargo la escena resultado 
                SceneManager.LoadScene("ResultadoSimulacion");
            }
        }
    }


}