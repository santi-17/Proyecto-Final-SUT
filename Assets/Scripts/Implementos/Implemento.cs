using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Implemento : MonoBehaviour
{
    public string nombreDelImplemento;
    public bool esActivo = false;
    private Transform puntoAcople; // Transform del punto de acople 

    private Rigidbody rb; // Rigidbody del implemento

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Obtiene el Rigidbody del implemento
    }

    public void Acoplar(Transform puntoAcopleTractor)
    {
        Debug.Log("Se ejecutó el método Acoplar");
        // Asigna el punto de acople del tractor al implemento
        puntoAcople = puntoAcopleTractor;
        transform.SetParent(puntoAcople); // Establece el implemento como hijo del punto de acople
        transform.localPosition = Vector3.zero; // Resetea la posición local para que esté en el mismo lugar que el punto de acople
        transform.localRotation = Quaternion.identity; // Resetea la rotación local
        rb.isKinematic = true; // Asegura que el Rigidbody sea cinemático para que no se vea afectado por la física mientras está acoplado
        esActivo = true; // Marca el implemento como activo
    }

    public void Desacoplar()
    {
        // Desacopla el implemento del tractor
        transform.SetParent(null); // Elimina el padre del implemento para desacoplarlo
        rb.isKinematic = false; // Asegura que el Rigidbody no sea cinemático para que pueda moverse libremente
        puntoAcople = null;
        esActivo = false; // Marca el implemento como inactivo
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
