using UnityEngine;

public class Cultivo : MonoBehaviour
{
    //private Rigidbody rb;
    //private bool aplastado = false;

    //void Start()
    //{
    //    rb = GetComponent<Rigidbody>();
    //    rb.isKinematic = true; // no simula físicas hasta que lo toquen
    //}

    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (aplastado) return;

    //    if (collision.gameObject.CompareTag("Tractor") || collision.gameObject.CompareTag("Implemento"))
    //    {
    //        aplastado = true;

    //        // Simular aplastamiento reduciendo altura
    //        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.3f, transform.localScale.z  * 0.3f);

    //        // Opcional: hacer que deje de interactuar
    //        Destroy(rb); // o rb.isKinematic = true;
    //        Destroy(GetComponent<Collider>()); // ya no choca más
    //    }
    //}
    private bool aplastado = false;
    private Vector3 escalaOriginal;
    private Vector3 escalaAplastada;
    private float velocidadAplastado = 8f;

    void Start()
    {
        // Guardar escala original
        escalaOriginal = transform.localScale;

        // Definir escala aplastada (más bajo en Y)
        escalaAplastada = new Vector3(escalaOriginal.x, escalaOriginal.y * 0.2f, escalaOriginal.z * 0.2f);

        // Si tenés Rigidbody, lo volvemos estático para que no se caiga
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (aplastado) return;

        if (collision.gameObject.CompareTag("Tractor") || collision.gameObject.CompareTag("Implement"))
        {
            aplastado = true;

            // Opcional: desactivar el collider para que no interfiera después
            Destroy(GetComponent<Collider>());
        }
    }

    void Update()
    {
        if (aplastado)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, escalaAplastada, Time.deltaTime * velocidadAplastado);
        }
    }
}
