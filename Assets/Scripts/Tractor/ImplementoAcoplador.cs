using UnityEngine;

public class ImplementoAcoplador : MonoBehaviour
{
    [Header("Referencia del punto de acople")]
    public Transform puntoDeAcople;

    [Header("Tag del implemento válido")]
    public string tagImplemento = "Implement";

    [Header("Teclas de acción")]
    public KeyCode teclaAcople = KeyCode.E;
    public KeyCode teclaDesacople = KeyCode.Q;

    private FixedJoint jointActual;
    private Rigidbody implementoActual;
    private Collider implementoDetectado;

    private void Update()
    {
        // Si hay uno detectado y presiona E, lo acopla
        if (implementoDetectado != null && Input.GetKeyDown(teclaAcople))
        {
            Acoplar(implementoDetectado);
        }

        // Si hay algo acoplado y presiona Q, lo desacopla
        if (jointActual != null && Input.GetKeyDown(teclaDesacople))
        {
            Desacoplar();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (jointActual != null) return; // Ya hay algo acoplado

        if (other.CompareTag(tagImplemento))
        {
            implementoDetectado = other;
            Debug.Log("Implemento detectado: " + other.name + ". Presiona E para acoplar.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (implementoDetectado == other)
        {
            implementoDetectado = null;
            Debug.Log("Implemento fuera de rango.");
        }
    }

    private void Acoplar(Collider other)
    {
        if (jointActual != null || other == null) return;

        Rigidbody rbImplemento = other.attachedRigidbody;
        if (rbImplemento != null)
        {
            // Reposicionar
            rbImplemento.transform.position = puntoDeAcople.position;
            rbImplemento.transform.rotation = puntoDeAcople.rotation;

            // Crear joint
            jointActual = gameObject.AddComponent<FixedJoint>();
            jointActual.connectedBody = rbImplemento;
            jointActual.breakForce = Mathf.Infinity;
            jointActual.breakTorque = Mathf.Infinity;

            implementoActual = rbImplemento;
            implementoDetectado = null;

            Debug.Log("Implemento acoplado con tecla E: " + implementoActual.name);
        }
    }

    private void Desacoplar()
    {
        if (jointActual != null)
        {
            Destroy(jointActual);
            if (implementoActual != null)
            {
                implementoActual.useGravity = true;
            }

            Debug.Log("Implemento desacoplado con tecla Q.");
            jointActual = null;
            implementoActual = null;
        }
    }
}
