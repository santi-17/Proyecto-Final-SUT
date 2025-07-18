using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // El objetivo al que la cámara seguirá
    public Vector3 offset = new Vector3(0, 5, -10); // Desplazamiento de la cámara respecto al objetivo
    public float rotationSpeed = 5f; // Velocidad de rotacion de la cámara
    public float zoomSpeed = 2f; // Velocidad de zoom de la cámara
    public float minZoom = 5f; // Zoom mínimo
    public float maxZoom = 20f; // Zoom máximo
    public Transform cameraTransform; // Transform de la cámara

    private float currentYaw = 0f; // Ángulo de rotación en el eje Y
    private float currentPitch = 15f; // Ángulo de rotación en el eje X
    private float minPitch = -10f; // Ángulo mínimo de inclinación de la cámara
    private float maxPitch = 60f; // Ángulo máximo de inclinación de la cámara

    private void LateUpdate()
    {
        if (Input.GetMouseButton(1)) // Botón derecho del ratón para rotar
        {
            currentYaw += Input.GetAxis("Mouse X") * rotationSpeed; // Rotación horizontal
            currentPitch -= Input.GetAxis("Mouse Y") * rotationSpeed; // Rotación vertical
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch); // Limitar la inclinación de la cámara

        }

        // Zoom con la rueda del ratón
        float scroll = Input.GetAxis("Mouse ScrollWheel"); // Obtener el valor de la rueda del ratón
        if (scroll != 0f) // Si se está desplazando la rueda del ratón
        {
            float zoomAmount = scroll * zoomSpeed; // Calcular el cambio de zoom
            offset.z = Mathf.Clamp(offset.z + zoomAmount, -maxZoom, -minZoom); // Limitar el zoom
        }
        // Posición de la cámara
        Quaternion quaternion = Quaternion.Euler(currentPitch, currentYaw, 0); // Crear una rotación basada en los ángulos actuales
        Vector3 desiredPosition = target.position + quaternion * offset; // Calcular la posición deseada de la cámara

        // Actualizar la posición y rotación de la cámara
        cameraTransform.position = desiredPosition; // Establecer la posición de la cámara
        cameraTransform.LookAt(target.position); // Hacer que la cámara mire hacia el objetivo
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
