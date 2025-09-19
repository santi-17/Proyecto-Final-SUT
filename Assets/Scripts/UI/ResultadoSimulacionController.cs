using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultadoSimulacionController : MonoBehaviour
{
    public TextMeshProUGUI mensajeText;

    private bool aprobado;
    private string moduloSlug;
    private string escenaAnterior;

    //metodo para configurar el resultado antes de cargar la escena
    public void ConfigurarResultado(bool aprobado, string moduloSlug, string escenaAnterior)
    {
        this.aprobado = aprobado;
        this.moduloSlug = moduloSlug;
        this.escenaAnterior = escenaAnterior;
    }

    // Start is called before the first frame update
    void Start()
    {
        aprobado = ResultadoSimulacionData.Aprobado;
        moduloSlug = ResultadoSimulacionData.ModuloNombre;
        escenaAnterior = ResultadoSimulacionData.EscenaAnterior;

        if (aprobado)
        {
            mensajeText.text = $"¡Felicitaciones! Has aprobado el módulo {moduloSlug}.\n" +
                "Para continuar con la evaluación, debes apretar el botón de enviar en la página para tener feedback.";
        }
        else
        {
            mensajeText.text = "Fallaste. Serás redirigido para reintentar el módulo.";

            //Despues de un tiempo voy al modulo anterior para reintentar la simulacion
            Invoke(nameof(VolverEscenaAnterior), 5f);
        }
    }

    public void VolverEscenaAnterior()
    {
        SceneManager.LoadScene(escenaAnterior);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
