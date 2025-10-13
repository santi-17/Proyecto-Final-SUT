using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TractorControler : MonoBehaviour
{
    private float horizontalInput, verticalInput;
    private float currentSteerAngle;
    private float currentbreakForce;
    private bool isBreaking;

    // Settings
    //[SerializeField] private AnimationCurve torqueCurve; // Maximum speed in km/h
    [SerializeField] private float maxRPM = 6000f; // Maximum RPM of the engine
    [SerializeField] private float motorForce = 3000f;
    [SerializeField] private Transmision transmision; // Assuming Transmision is a class that handles gear ratios and torque
    [SerializeField] private float breakForce = 3000f;
    [SerializeField] private float maxSteerAngle = 35f;
    [SerializeField] public float steeringSpeed = 5f;

    //Estabilidad y recuperacion del tractor
    public KeyCode teclaReinicio = KeyCode.C; // tecla para reiniciar el tractor
    [SerializeField] private float umbralVuelco = 60f; // umbral de inclinacion para considerar que el tractor este volcado
    [SerializeField] private float alturaReinicio = 1.5f; // altura a la que se reiniciara el tractor
    [SerializeField] private float fuerzaDesatasco = 3f; // fuerza aplicada para desatascar el tractor
    [SerializeField] private Vector3 centroMasaAjustado = new Vector3(0, -0.5f, 0); // ajuste del centro de masa para mejorar la estabilidad
    private float tiempoUltimoVuelco = 0f; // tiempo en el que se detecto el ultimo vuelco

    //Guardar la posicion inicial del tractor
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;

    //Rigidbody
    public Rigidbody rb; // Reference to the Rigidbody component of the tractor

    // Wheel Colliders
    [SerializeField] private WheelCollider frontLeftWheelCollider, frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider, rearRightWheelCollider;

    // Wheels
    [SerializeField] private Transform frontLeftWheelTransform, frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform, rearRightWheelTransform;

    // Start is called before the first frame update
    void Start()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        // Ajustar el centro de masa para mejorar la estabilidad
        rb.centerOfMass = centroMasaAjustado;

        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void FixedUpdate()
    {
        GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();
        VerificarVuelco();
    }

    private void GetInput()
    {
        // Steering Input
        horizontalInput = Input.GetAxis("Horizontal");

        // Acceleration Input
        verticalInput = Input.GetAxis("Vertical");

        // Breaking Input
        isBreaking = Input.GetKey(KeyCode.Space);
    }

    private void HandleMotor()
    {
        //float torque = verticalInput * motorForce * transmision.GetTorque();
        float rpm = rb.velocity.magnitude * 60f / (2f * Mathf.PI * frontLeftWheelCollider.radius); // Calculate RPM based on wheel speed
        //float torque = torqueCurve.Evaluate(rpm / maxRPM) * transmision.GetTorque() * verticalInput * motorForce;
        float torque = transmision.GetTorque() * verticalInput * motorForce;
        //float appliedForce = verticalInput * motorForce;
        frontLeftWheelCollider.motorTorque = torque;
        frontRightWheelCollider.motorTorque = torque;
        currentbreakForce = isBreaking ? breakForce : 0f;
        ApplyBreaking();
    }

    private void ApplyBreaking()
    {
        frontRightWheelCollider.brakeTorque = currentbreakForce;
        frontLeftWheelCollider.brakeTorque = currentbreakForce;
        rearLeftWheelCollider.brakeTorque = currentbreakForce;
        rearRightWheelCollider.brakeTorque = currentbreakForce;
    }

    private void HandleSteering()
    {
        float  targetAngle = horizontalInput * maxSteerAngle;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetAngle, Time.deltaTime * steeringSpeed);
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    }

    private void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }

    private void VerificarVuelco()
    {
        // Verificar si el tractor esta volcado
        float anguloInclinacion = Vector3.Angle(transform.up, Vector3.up);
        
        if (anguloInclinacion > umbralVuelco)
        {
            tiempoUltimoVuelco += Time.time;

            if (tiempoUltimoVuelco > Time.time)
                Debug.Log("[Tractor] El tractor esta volcado. Presiona 'C' para reiniciarlo.");
        }
        else
            tiempoUltimoVuelco = 0f; // Reiniciar el tiempo si el tractor no esta volcado
        
        // Reiniciar el tractor si se presiona la tecla y ha pasado el tiempo necesario desde el ultimo vuelco
        if (Input.GetKeyDown(teclaReinicio) /*&& Time.time - tiempoUltimoVuelco > tiempoReinicio*/)
        {
            ReiniciarTractor();
        }
        // Aplicar fuerza para desatascar el tractor si esta atascado
        if (rb.velocity.magnitude < 0.1f && Mathf.Abs(verticalInput) > 0.1f)
        {
            //rb.AddForce(transform.forward * verticalInput * fuerzaDesatasco, ForceMode.Acceleration);
            rb.AddForce(Vector3.up * fuerzaDesatasco, ForceMode.Impulse);
        }
    }

    private void ReiniciarTractor()
    {
        // Reiniciar la posicion y rotacion del tractor
        //Vector3 pos = transform.position; 
        //pos.y = alturaReinicio;

        transform.position = posicionInicial;//new Vector3(transform.position.x, alturaReinicio, transform.position.z);
        transform.rotation = rotacionInicial;//Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
