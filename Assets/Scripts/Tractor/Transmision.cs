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
