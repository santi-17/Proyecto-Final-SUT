using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public class Acoplador : MonoBehaviour
{
    public KeyCode teclaAcoplar = KeyCode.E; // Tecla para acoplar/desacoplar
    public KeyCode teclaDesacoplar = KeyCode.Q; // Tecla para desacoplar

    private Implemento implementoActual = null; // Referencia al implemento actual acoplado
    private Implemento implementoCercano = null; // Referencia al implemento cercano que se puede acoplar

    //private Transform puntoAcople; // Transform del punto de acople del tractor
    // Start is called before the first frame update
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entró algo al trigger: " + other.name);
        // Verifica si el objeto con el que colisiona es un implemento
        Implemento implemento = other.GetComponent<Implemento>();
        if (implemento != null  && !implemento.esActivo)
        {
            Debug.Log("Implemento detectado: " + implemento.nombreDelImplemento);
            implementoCercano = implemento; // Guarda el implemento cercano
        }

    }

    private void OnTriggerExit(Collider other)
    {
        // Verifica si el objeto que sale del trigger es el implemento cercano
        if (implementoCercano != null && other.GetComponent<Implemento>() == implementoCercano)
        {
            implementoCercano = null; // Limpia la referencia al implemento cercano
        }
    }
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(teclaAcoplar))
            Debug.Log("Tecla de acople presionada");

        if (Input.GetKeyDown(teclaAcoplar) && implementoCercano != null && implementoActual == null)
        {
            Debug.Log("Acoplando " + implementoCercano.nombreDelImplemento);
            // Si se presiona la tecla de acoplar y hay un implemento cercano, acopla el implemento
            implementoCercano.Acoplar(transform); // Acopla el implemento al punto de acople del tractor
            implementoActual = implementoCercano; // Guarda el implemento actual
            implementoCercano = null; // Limpia la referencia al implemento cercano
        }
        if (Input.GetKeyDown(teclaDesacoplar) && implementoActual != null)
        {
            // Si se presiona la tecla de desacoplar y hay un implemento acoplado, desacopla el implemento
            implementoActual.Desacoplar(); // Desacopla el implemento del tractor
            implementoActual = null; // Limpia la referencia al implemento actual
        }
    }
}