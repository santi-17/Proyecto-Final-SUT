using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{

    public Rigidbody rb; // Referencia al Rigidbody del tractor
    
    public TextMeshProUGUI velocidad; // Referencia al componente TextMeshProUGUI para mostrar la velocidad
    public TextMeshProUGUI rpm; // Referencia al componente TextMeshProUGUI para mostrar el nivel de combustible
    public float maxRpm = 6000f; // RPM máximo del motor

    [SerializeField] private CampoTracker campoTracker;
    [SerializeField] private TextMeshProUGUI progresoText;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float velocidadActual = rb.velocity.magnitude*3.6f; // Obtener la velocidad actual del Rigidbody de m/s a km/h
        //velocidad.text = "Velocidad: " + velocidadActual.ToString("F2") + " km/h"; // Actualizar el texto de velocidad
        velocidad.text = $"Velocidad: {velocidadActual:F1} km/h";

        //float rpmActual = (rb.velocity.magnitude / 3.6f) * (maxRpm / 100); // Calcular el RPM actual basado en la velocidad
        float rmpActual = Mathf.Clamp(velocidadActual * 100, 800, maxRpm); // Calcular el RPM actual basado en la velocidad y limitarlo al máximo
        //rpm.text = "RPM: " + rmpActual.ToString("F0"); // Actualizar el texto de RPM
        rpm.text = $"RPM: {rmpActual:F0}"; // Actualizar el texto de RPM con formato

        // Actualizar el progreso del campo
        //progresoText.text = "Progreso: " + (campoTracker.ObtenerProgreso() * 100f).ToString("F2") + "%";
    }
}
