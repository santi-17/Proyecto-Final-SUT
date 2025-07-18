using UnityEngine;

public class ImplementAttachment : MonoBehaviour
{
    public Transform attachPoint; // punto en el tractor
    public float attachRange = 3f; // distancia de acople
    private GameObject attachedImplement;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) // Acoplar
        {
            if (attachedImplement == null)
                TryAttachImplement();
        }
        else if (Input.GetKeyDown(KeyCode.Q)) // Desacoplar
        {
            if (attachedImplement != null)
                DetachImplement();
        }
    }

    void TryAttachImplement()
    {
        Collider[] colliders = Physics.OverlapSphere(attachPoint.position, attachRange);
        foreach (var col in colliders)
        {
            if (col.CompareTag("Implement"))
            {
                GameObject implement = col.gameObject;

                implement.transform.position = attachPoint.position;
                implement.transform.rotation = attachPoint.rotation;
                implement.transform.SetParent(attachPoint);

                Rigidbody rb = implement.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                attachedImplement = implement;
                break;
            }
        }
    }

    void DetachImplement()
    {
        if (attachedImplement != null)
        {
            attachedImplement.transform.SetParent(null);
            Rigidbody rb = attachedImplement.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            attachedImplement = null;
        }
    }
}
