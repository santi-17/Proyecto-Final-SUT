using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WheelColliderData
{
    public WheelCollider wheelCollider;
    public Transform wheelTransform;
    public float springStrength = 35000; // La fuerza del resorte
    public float springDamper = 4500; // La resistencia del amortiguador
}
public class TractorSuspencion : MonoBehaviour
{
    public List<WheelColliderData> wheelColliders = new List<WheelColliderData>();

    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setSuspension(float strength, float damper)
    {
        foreach (var wheel in wheelColliders) // Recorre cada WheelColliderData en la lista
        {
            JointSpring spring = wheel.wheelCollider.suspensionSpring; // Obtiene el resorte actual del WheelCollider
            spring.spring = strength; // Establece la fuerza del resorte
            spring.damper = damper; // Establece la resistencia del amortiguador
            wheel.wheelCollider.suspensionSpring = spring; // Asigna el resorte actualizado al WheelCollider
        }
    }

    string detectTerrain(WheelCollider wc)
    {
        RaycastHit hit;
        if (Physics.Raycast(wc.transform.position, -wc.transform.up, out hit, wc.suspensionDistance + 0.1f))
        {
            return hit.collider.gameObject.tag; // Devuelve el tag del objeto con el que colisiona
        }
        return "None"; // Si no hay colisión, devuelve "None"
    }
}
