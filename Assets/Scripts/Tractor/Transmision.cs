//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public enum TransmisionType
//{
//    Automatic,
//    Manual
//}

//public class Transmision : MonoBehaviour
//{
//    public TransmisionType transmisionType = TransmisionType.Automatic; // Tipo de transmisión: Automática o Manual
//    public int currentGear = 1; // Engranaje actual
//    public float[] gearRatios = new float[] { 0f, 2f, 3.5f, 5f }; // Relaciones de engranaje para cada marcha 
//    public float engineRPM = 0f; // Revoluciones por minuto del motor

//    public float GetTorque()
//    {
//        // Calcula el torque basado en la entrada del acelerador y la relación de engranaje actual
//        //float torque = throttleInput * gearRatios[currentGear] * 100; // Ajusta el factor de escala según sea necesario
//        //return torque;
//        return gearRatios[currentGear] * 100; // Retorna el torque basado en la relación de engranaje actual
//    }

//    public void UpdateGear(float speed)
//    {
//        if (transmisionType == TransmisionType.Automatic)
//        {
//            // Lógica para cambiar de marcha automáticamente basada en la velocidad
//            if (speed > 15f && currentGear < gearRatios.Length - 1) // La marcha no puede ser mayor a la última
//            {
//                currentGear++; // Aumenta la marcha si no es la última
//            }
//            else if (speed < 5f && currentGear > 1) // La marcha no puede ser menor a 1
//            {
//                currentGear--; // Disminuye la marcha si no es la primera
//            }
//        }
//        else
//        {
//            // Lógica para cambiar de marcha manualmente (puedes implementar controles aquí)
//            // Por ejemplo, podrías usar teclas para aumentar o disminuir currentGear
//            if (Input.GetKeyDown(KeyCode.X) && currentGear < gearRatios.Length - 1) // La 'X' se usa para aumentar la marcha
//            {
//                currentGear++; // Aumenta la marcha si no es la última
//            }
//            else if (Input.GetKeyDown(KeyCode.Z) && currentGear > 1) // La 'Z' se usa para disminuir la marcha
//            {
//                currentGear--; // Disminuye la marcha si no es la primera
//            }
//        }
//    }
//        // Start is called before the first frame update
//        void Start()
//    {

//    }

//    // Update is called once per frame
//    void Update()
//    {

//    }
//}


using UnityEngine;

public class Transmision : MonoBehaviour
{
    [System.Serializable]
    public class Gear
    {
        public float gearRatio;
        public float maxSpeed;
    }

    public Gear[] gears = new Gear[]
    {
        new Gear { gearRatio = 0.5f, maxSpeed = 5f }, // First gear
        new Gear { gearRatio = 0.8f, maxSpeed = 10f }, // Second gear
        new Gear { gearRatio = 1.0f, maxSpeed = 20f }, // Third gear
        new Gear { gearRatio = 1.3f, maxSpeed = 35f }, // Fourth gear
        new Gear { gearRatio = 1.5f, maxSpeed = 50f } // Fifth gear
    };
    public bool isAutomatic = true; // Flag to determine if the transmission is automatic or manual
    private int currentGear = 0;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        float speed = rb.velocity.magnitude;

        if (isAutomatic)
        {
            AutoShift(speed);
        }
        else
        {
            ManualShift();
        }
    }

    private void AutoShift(float speed)
    {
        for (int i = 0; i < gears.Length; i++)
        {
            if (speed < gears[i].maxSpeed)
            {
                currentGear = i;
                return;
            }
        }
        currentGear = gears.Length - 1;
    }

    private void ManualShift()
    {
        if (Input.GetKeyDown(KeyCode.Z)) currentGear = Mathf.Max(currentGear - 1, 0);
        if (Input.GetKeyDown(KeyCode.X)) currentGear = Mathf.Min(currentGear + 1, gears.Length - 1);
    }

    public float GetTorque()
    {
        return gears[currentGear].gearRatio;
    }

    public int GetCurrentGear()
    {
        return currentGear + 1;
    }
}
